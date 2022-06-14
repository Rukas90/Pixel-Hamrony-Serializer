using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static PixelHarmony.Utils;

namespace PixelHarmony.Serialization
{
    internal partial class PHSerializer
    {
        class Serializer
        {
            class EntriesCounter
            {
                int[] entries = new int[0];
                public int[] Entries => entries;

                public int this[int index] => Entries[index];

                private int position = 0;
                public int Length => Entries.Length;

                public void SetLength(int length)
                {
                    Array.Resize(ref entries, Entries.Length + length);
                }
                public void SetEntry(int entry)
                {
                    Entries[position] = entry;
                }
                public void MoveNext() => position++;
            }
            public Serializer() { }

            public string Execute(object target)
            {
                return Serialize(target);
            }
            string Serialize(object target)
            {
                EntriesCounter counter = new EntriesCounter();
                StringBuilder final    = new StringBuilder("[Meta]{...}"); final.AppendLine();

                final.Append(SerializeObject(target, "", true, in counter));
                return final.Replace("{...}", BuildMeta(in counter)).ToString().TrimEnd(Environment.NewLine.ToCharArray());
            }
            string BuildMeta(in EntriesCounter counter)
            {
                StringBuilder meta = new StringBuilder();
                meta.Append(WrapMetaEntry("Version", Version, true));
                meta.Append(WrapMetaEntry("Entries", counter.Length, true));

                StringBuilder sizes = new StringBuilder("{");
                for (int i = 0; i < counter.Length; i++)
                {
                    sizes.Append(counter[i]);

                    if (i < counter.Length - 1)
                    {
                        sizes.Append(Tokens.MetaEntrySeparator);
                    }
                }
                sizes.Append("}");
                meta.Append(WrapMetaEntry("Sizes", sizes, false));

                return meta.ToString();
            }
            StringBuilder WrapMetaEntry(string name, object value, bool separator)
            {
                StringBuilder meta = new StringBuilder(name);
                meta.Append(Tokens.MetaEquals); meta.Append(value); if (separator) { meta.Append(' '); }

                return meta;
            }

            string SerializeObject(object target, string path, bool root, in EntriesCounter counter)
            {
                StringBuilder format = new StringBuilder();
                IEnumerable<FieldInfo> fields = Utilities.GetTargetFormatFields(target);

                foreach (var field in fields)
                {
                    string entry = SerializeField(target, field, path, root, counter, out bool skip);
                    format.Append(entry);

                    if (counter != null && !skip)
                    {
                        if (counter != null)
                        {
                            counter.SetLength(1);
                        }
                        counter.SetEntry(entry.Length);
                        counter.MoveNext();
                    }
                }
                return format.ToString();
            }
            string SerializeField(object target, FieldInfo field, string path, bool root, in EntriesCounter counter, out bool skip)
            {
                var formatAttr  = (FormatAttributeBase)field.GetCustomAttribute(typeof(FormatAttributeBase));
                var sectionAttr = (SectionAttribute)field.GetCustomAttribute(typeof(SectionAttribute));

                skip = false;
                string section = sectionAttr?.ID;

                string s = string.IsNullOrEmpty(path) ? "" : "/";
                path += s + field.Name;

                object value = field.GetValue(target);
                bool isCollection = value is ICollection;

                bool isStructured = formatAttr.GetType() == typeof(StructuredFormatAttribute);
                bool isDynamic    = formatAttr.GetType() == typeof(DynamicFormatAttribute);

                if (isStructured && !isCollection)
                {
                    object v = field.GetValue(target);
                    if (v == null)
                    {
                        return FormatDataEntry(path, Tokens.NullToken, null, null, null, true, isDynamic);
                    }
                    skip = true;
                    return SerializeObject(field.GetValue(target), path, true, counter); 
                }
                else
                {
                    int? trait = null; string formatted, parameters = default;
                    if (isCollection)
                    {
                        Array array; var collection = (ICollection)value;

                        Type elementType = Utilities.GetCollectionElementType(value);
                        bool isAbstract = elementType.IsAbstract;

                        array = new object[collection.Count];
                        collection.CopyTo(array, 0);

                        int length = array.Length;
                        StringBuilder format = new StringBuilder();

                        format.Append(Tokens.ArrayStartToken);
                        format.Append(length);
                        format.Append(Tokens.Separator); 
                        format.Append(value.GetType().AssemblyQualifiedName);
                        format.Append(Tokens.ArrayEndToken);

                        int index = 0;
                        foreach (var elm in array)
                        {
                            format.Append(Tokens.ElementStartToken);
                            if (isStructured)
                            {
                                if (elm == null)
                                {
                                    format.Append(Tokens.NullToken);
                                }
                                else
                                {
                                    if (isAbstract)
                                    {
                                        format.Append(Tokens.AttributesStartChar);
                                        format.Append(WrapMetaEntry("Type", elm.GetType().AssemblyQualifiedName, false));
                                        format.Append(Tokens.AttributesEndChar);
                                    }
                                    format.Append(SerializeObject(elm, "", false, null));
                                }
                            }
                            else
                            {
                                if (!TrySerializeObject(formatAttr, elm, out string f, out trait, out parameters))
                                {
                                    Debug.LogError("Object could not be serialized!");
                                    return null;
                                }
                                format.Append(FormatDataEntry("", f, isDynamic ? trait : null, parameters, null, false, isDynamic));
                            }
                            format.Append(Tokens.ElementEndToken);
                            index++;
                        }
                        formatted = format.ToString();
                    }
                    else
                    {
                        if (value == null)
                        {
                            return FormatDataEntry(path, Tokens.NullToken, null, null, null, true, isDynamic);
                        }
                        if (!TrySerializeObject(formatAttr, value, out formatted, out trait, out parameters))
                        {
                            Debug.Log(value.GetType());
                            Debug.LogError("Object could not be serialized!");
                            return null;
                        }
                    }
                    return FormatDataEntry(
                        path, 
                        formatted,
                        isCollection && !isStructured && !isDynamic || !isCollection ? trait : null,
                        !isCollection ? parameters : null, 
                        section, 
                        root, 
                        isDynamic);
                }
            }

            string FormatDataEntry(string path, string value, int? trait, string parameters, string section, bool tab, bool isDynamic)
            {
                StringBuilder final = new StringBuilder();

                bool enclose = !string.IsNullOrEmpty(path);

                if (enclose)
                { final.Append(Tokens.EntryStartToken); }

                StringBuilder meta = null;
                if (!string.IsNullOrEmpty(section))
                {
                    meta = new StringBuilder();
                    meta.Append(WrapMetaEntry("Section", section, false));
                }
                if (parameters != null)
                {
                    if (meta == null) { meta = new StringBuilder(); } else { meta.Append(' '); } 
                    meta.Append(WrapMetaEntry("Parameters", parameters, false));
                }
                if (trait.HasValue)
                {
                    if (meta == null) { meta = new StringBuilder(); } else { meta.Append(' '); }
                    meta.Append(WrapMetaEntry("Trait", trait.Value, false));
                }
                if (meta != null)
                {
                    final.Append(Tokens.AttributesStartChar);
                    final.Append(meta);
                    final.Append(Tokens.AttributesEndChar);
                }
                if (!string.IsNullOrEmpty(path))
                {
                    final.Append(path); final.Append(Tokens.DefineToken);
                }
                final.Append(value);

                if (enclose)
                { final.Append(Tokens.EntryEndToken); }

                if (tab)
                { final.Append(Environment.NewLine); }

                return final.ToString();
            }
            bool TrySerializeObject(FormatAttributeBase attr, object value, out string format, out int? trait, out string parameters)
            {
                format = null; trait = null; parameters = null;

                bool isDynamic = attr.GetType() == typeof(DynamicFormatAttribute);
                if (isDynamic)
                {
                    var formatters = Conversion.GetEnumerableOfType<FormatObjectAttribute>();
                    foreach (var formatter in formatters)
                    {
                        bool status = formatter.Write(value, out format, out parameters) != -1;
                        if (status)
                        {
                            trait = formatter.Trait;
                            return true;
                        }
                    }
                    return false;
                }
                else
                {
                    FormatObjectAttribute formatter = (FormatObjectAttribute)attr;

                    bool status = formatter.Write(value, out format, out parameters) != -1;
                    trait = formatter.Trait;

                    return status;
                }
            }
        }
    }
}