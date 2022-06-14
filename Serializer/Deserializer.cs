using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine;
using static PixelHarmony.Utils;

namespace PixelHarmony.Serialization
{
    internal static partial class PHSerializer
    {
        class Deserializer
        {
            struct ParsedEntry
            {
                private readonly string[] path;
                public string[] Path => path;

                private readonly char[] value;
                public ReadOnlySpan<char> Value => value;

                public bool IsNull => Value.SequenceEqual(Tokens.NullArray);

                public readonly string[] parameters;
                public readonly int trait;

                public readonly bool isCollection;
                public int length;
                public Type collectionType;

                public ParsedEntry(ReadOnlySpan<char> path, ReadOnlySpan<char> value, string[] parameters, int trait, bool isCollection, int length, Type collectionType)
                {
                    this.path           = path.ToString().Split(Tokens.PathSeparator);
                    this.value          = value.ToArray();

                    this.parameters     = parameters;
                    this.trait          = trait;
                    this.isCollection   = isCollection;
                    this.length         = length;
                    this.collectionType = collectionType;                    
                }
            }

            bool hasSectionIDs = false; string[] sectionIDs = null;
            bool debug = false;

            public Deserializer() { }

            public T Execute<T>(string value, DeserializeMode mode, bool debug)
            {
                this.debug = debug;

                var target = (T)Reflection.GetTypeObjectInstance(typeof(T));
                return Deserialize(target, value, mode);
            }
            public object Execute(Type type, string value, DeserializeMode mode, bool debug)
            {
                this.debug = debug;

                var target = Reflection.GetTypeObjectInstance(type);
                return Deserialize(target, value, mode);
            }

            public T Execute<T>(string value, DeserializeMode mode, bool debug, params string[] sectionIDs)
            {
                this.debug = debug;

                this.sectionIDs = sectionIDs;
                hasSectionIDs = sectionIDs != null && sectionIDs.Length > 0;

                var target = (T)Reflection.GetTypeObjectInstance(typeof(T));
                return Deserialize(target, value, mode);
            }
            public object Execute(Type type, string value, DeserializeMode mode, bool debug, params string[] sectionIDs)
            {
                this.debug = debug;

                this.sectionIDs = sectionIDs;
                hasSectionIDs = sectionIDs != null && sectionIDs.Length > 0;

                var target = Reflection.GetTypeObjectInstance(type);
                return Deserialize(target, value, mode);
            }

            T Deserialize<T>(T target, string value, DeserializeMode mode)
            {
                return mode switch
                {
                    DeserializeMode.File    => DeserializeFile(target, value),
                    _                       => DeserializeContent(target, value),
                };
            }

            T DeserializeFile<T>(T target, string path)
            {
                bool parsedHeader = false; int[] sizes = null; long position = 0; int currentEntry = 0;

                using (FileStream file = File.Open(path, FileMode.Open))
                {
                    using (BufferedStream stream = new BufferedStream(file))
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            while (stream.Position < stream.Length - 1)
                            {
                                if (!parsedHeader)
                                {
                                    Debug.Log(stream.Position);
                                    if (!ParseFileHeader(reader.ReadLine().AsSpan(), out int totalEntries, out sizes))
                                    {
                                        return default;
                                    }
                                    Debug.Log("Parsed Header");
                                    parsedHeader = true;
                                }
                                else
                                {
                                    Debug.Log("Parse Entry");

                                    position = stream.Position; Debug.Log(stream.Position);
                                    ParseSectionID(in reader, out ReadOnlySpan<char> ID); Debug.Log(ID == null ? "null" : ID.ToString());
                                    reader.DiscardBufferedData(); stream.Position = position;

                                    if (DoesBelongToDesiredSection(ID.ToString()))
                                    {
                                        ReadOnlySpan<char> line = reader.ReadLine().AsSpan();
                                        target = DeserializeLine(target, ref line);
                                    }
                                    else { stream.Position += sizes[currentEntry]; }
                                    currentEntry++;
                                }
                            }
                        }
                    }
                }
                return target;
            }
            void ParseSectionID(in StreamReader reader, out ReadOnlySpan<char> ID)
            {
                ID = null; int pos = 0;
                while (pos < Tokens.EntryStartToken.Length)
                {
                    reader.Read(); pos++;
                }
                if (reader.Peek() != Tokens.AttributesStartChar) { return; }

                bool parsed = false; List<char> attributes = new List<char>();
                while (!parsed)
                {
                    char character = (char)reader.Read();
                    attributes.Add(character);

                    if (character == Tokens.AttributesEndChar)
                    {
                        parsed = true;
                    }
                }


                ID = new ReadOnlySpan<char>(attributes.ToArray());
            }
            bool ParseFileHeader(ReadOnlySpan<char> line, out int totalEntries, out int[] sizes)
            {
                totalEntries = 0; sizes = null;
                ReadOnlySpan<char> meta = line.Slice(Tokens.MetaHeader.Length, line.Length - Tokens.MetaHeader.Length);

                ParseMetaAttribute(in meta, "Entries", out ReadOnlySpan<char> attribute);
                if (attribute == null)
                {
                    return false;
                }
                totalEntries = Convert.ToInt32(attribute.ToString());

                ParseMetaAttribute(in meta, "Sizes", out attribute);
                if (attribute == null)
                {
                    return false;
                }
                string[] split = attribute.ToString().TrimStart(Tokens.ElementStartToken).TrimEnd(Tokens.ElementEndToken).Split(Tokens.Separator);

                if (split.Length != totalEntries) { return false; }

                sizes = new int[split.Length];
                for (int i = 0; i < split.Length; i++)
                {
                    sizes[i] = Convert.ToInt32(split[i]);
                }
                return true;
            }

            T DeserializeContent<T>(T target, string format)
            {
                LineSplitEnumerator entries = format.SplitLines(); int index = 0;
                foreach (var entry in entries)
                {
                    if (index == 0) { index++; continue; }

                    ReadOnlySpan<char> line = entry.Line;
                    target = DeserializeLine(target, ref line);
                }
                return target;
            }

            T DeserializeLine<T>(T target, ref ReadOnlySpan<char> line) => DeserializeObject(target, in line);

            T DeserializeObject<T>(T target, in ReadOnlySpan<char> span)
            {
                List<int2> entries = GetEntries(span, Tokens.EntryStartToken, Tokens.EntryEndToken);

                for (int i = 0; i < entries.Count; i++) 
                {
                    ReadOnlySpan<char> entry = span.Slice(entries[i].x, entries[i].y);

                    bool state = ParseDataEntry(ref entry, out ParsedEntry parsedEntry, out string section);
                    if (state)
                    {
                        if (!DoesBelongToDesiredSection(in section))
                        {
                            continue;
                        }
                        target = DeserializeField(target, in parsedEntry, 0);
                    }
                    else
                    {
                        Debug.LogError("could not parse data entry");
                        return default;
                    }
                }
                return target;
            }

            bool DoesBelongToDesiredSection(in string section)
            {
                if (!hasSectionIDs) { return true; }
                else
                {
                    if (section == null) { return false; }
                }
                for (int i = 0; i < sectionIDs.Length; i++)
                {
                    if (sectionIDs[i] == section)
                    {
                        return true;
                    }
                }
                return false;
            }

            T DeserializeField<T>(T target, in ParsedEntry parsedEntry, int level)
            {
                var name = parsedEntry.Path[level];

                var field = GetFieldByName(in target, name, out object fieldTarget);
                if (field == null)
                {
                    //Handle errors (to do) in all instances
                    return default;
                }
                level++;

                if (fieldTarget == null)
                {
                    fieldTarget = Reflection.GetFieldObjectInstance(field);
                }
                if (level < parsedEntry.Path.Length)
                {
                    fieldTarget = DeserializeField(fieldTarget, in parsedEntry, level);
                    field.SetValue(target, fieldTarget);

                    return target;
                }
                fieldTarget = DeserializeValue(fieldTarget, field, in parsedEntry);
                field.SetValue(target, fieldTarget);
                return target;
            }

            object DeserializeValue(object target, FieldInfo field, in ParsedEntry parsedEntry)
            {
                if (parsedEntry.IsNull)
                {
                    return null;
                }
                var attribute = Utilities.GetFieldAttribute(field);

                if (parsedEntry.isCollection)
                {
                    return ParseCollection(parsedEntry, attribute, parsedEntry.Value, parsedEntry.length, parsedEntry.collectionType);
                }
                if (attribute.GetType() == typeof(StructuredFormatAttribute))
                {
                    return DeserializeObject(target, parsedEntry.Value);
                }
                return GetValue(parsedEntry.trait, parsedEntry.Value.ToString(), parsedEntry.parameters);
            }
            object GetValue(int trait, string value, in string[] parameters)
            {
                var formatter = TryGetDeserializer(trait);
                if (formatter == null)
                {
                    Debug.LogError("formatter not found");
                    //Handle errors (to do) in all instances
                    return default;
                }
                return formatter.Read(value, parameters);
            }

            List<int2> GetEntries(in ReadOnlySpan<char> span, string openToken, string closeToken)
            {
                List<int2> entries = new List<int2>();
                ReadOnlySpan<char> openSpan = openToken.AsSpan(); ReadOnlySpan<char> closeSpan = closeToken.AsSpan();

                int openings = 0; int startIndex = 0, len = 0;
                for (int i = 0; i < span.Length; i++)
                {
                    char c = span[i];
                     
                    if (c == openSpan[0])
                    {
                        if (openSpan.Length == 1 || ValidateToken(in span, i, in openSpan))
                        {
                            openings++;

                            len += openSpan.Length;
                            i   += openSpan.Length - 1; continue;
                        }
                    }
                    if (c == closeSpan[0])
                    {
                        if (closeSpan.Length == 1 || ValidateToken(in span, i, in closeSpan))
                        {
                            openings--;
                            if (openings == 0)
                            {
                                entries.Add(new int2(startIndex + openSpan.Length, len - openSpan.Length));                                
                                startIndex += len + closeToken.Length; len = 0;
                            }
                            else { len += closeSpan.Length; }
                            
                            i   += closeSpan.Length - 1; continue;
                        }
                    }
                    len++;
                }
                return entries;
            }
            bool ValidateToken(in ReadOnlySpan<char> span, int pos, in ReadOnlySpan<char> token)
            {
                if (pos + token.Length - 1 >= span.Length) { return false; }

                for (int i = 0; i < token.Length; i++)
                {
                    if (span[pos + i] != token[i])
                    {
                        return false;
                    }
                }
                return true;
            }

            bool ParseDataEntry(ref ReadOnlySpan<char> entry, out ParsedEntry parsedEntry, out string section)
            {
                section = null; string[] parameters = null; int trait = -1; bool isCollection; int length = 0; Type collectionType = null;

                bool hasAttributes = entry[0] == Tokens.AttributesStartChar;
                if (hasAttributes)
                {
                    ReadOnlySpan<char> attributes = GetEntrySlice(ref entry, Tokens.AttributesEndChar, null);
                    attributes = attributes.Slice(1, attributes.Length - 2);

                    ParseMetaAttribute(in attributes, "Parameters", out ReadOnlySpan<char> attributeVal);
                    if (attributeVal != null)
                    {
                        parameters = attributeVal.ToString().Split(',');
                    }
                    ParseMetaAttribute(in attributes, "Trait", out attributeVal);
                    if (attributeVal != null)
                    {
                        trait = Convert.ToInt32(attributeVal.ToString());
                    }
                    ParseMetaAttribute(in attributes, "Section", out attributeVal);
                    if (attributeVal != null)
                    {
                        section = attributeVal.ToString();
                    }
                }

                ReadOnlySpan<char> path = GetEntrySlice(ref entry, Tokens.DefineToken, null);
                path = path.Slice(0, path.Length - 1);

                isCollection = entry[0] == Tokens.ArrayStartToken;

                if (isCollection)
                {
                    ReadOnlySpan<char> array = GetEntrySlice(ref entry, Tokens.ArrayEndToken, Tokens.ArrayStartToken);
                    array = array.Slice(1, array.Length - 2);

                    ReadOnlySpan<char> arrayLen = GetEntrySlice(ref array, '|', null);
                    arrayLen = arrayLen.Slice(0, arrayLen.Length - 1);

                    length         = Convert.ToInt32(arrayLen.ToString());
                    collectionType = Type.GetType(array.ToString());
                }
                parsedEntry = new ParsedEntry(path, entry, parameters, trait, isCollection, length, collectionType);
                return true;
            }

            ReadOnlySpan<char> GetEntrySlice(ref ReadOnlySpan<char> entry, char token, char? openToken)
            {
                int len = 1; int openings = 0;
                for (int i = 0; i < entry.Length; i++)
                {
                    if (openToken.HasValue && entry[i] == openToken.Value)
                    {
                        openings++;
                    }
                    else if (entry[i] == token)
                    {
                        if (openToken.HasValue)
                        {
                            openings--;
                            if (openings == 0)
                            {
                                break;
                            }
                        }
                        else { break; }
                    }
                    len++;
                }
                ReadOnlySpan<char> slice    = entry.Slice(0, len);
                entry                       = entry.Slice(slice.Length, entry.Length - slice.Length);

                return slice;
            }

            void ParseMetaAttribute(in ReadOnlySpan<char> meta, string id, out ReadOnlySpan<char> value)
            {
                value = null; ReadOnlySpan<char> idSpan = id.AsSpan();

                bool found = false; int index = 0; int len = 0;
                for (int i = 0; i < meta.Length; i++)
                {
                    bool end = i == meta.Length - 1;

                    if (meta[i] == Tokens.MetaEquals)
                    {
                        if (meta.Slice(index, len).Equals(idSpan, StringComparison.Ordinal))
                        {
                            found = true;
                            index = i + 1; len = 0;
                        }
                    }
                    else if (meta[i] == ' ' || end)
                    {
                        if (found)
                        {
                            if (end)
                            { len++; }

                            value = meta.Slice(index, len);
                            return;
                        }
                        else
                        {
                            index = i + 1; len = 0;

                            if (!end && meta[index] == ' ')
                            { index++; len--; }

                            continue;
                        }
                    }
                    else
                    {
                        len++;
                    }
                }
            }
            
            object ParseCollection(in ParsedEntry parsedEntry, FormatAttributeBase attribute, in ReadOnlySpan<char> format, int length, Type type)
            {
                var elements = GetEntries(in format, Tokens.ElementStartToken.ToString(), Tokens.ElementEndToken.ToString());
                if (elements.Count != length)
                {
                    Debug.LogError("element could is not equals to parsed length");
                    return default;
                }
                bool isAbstract = Utilities.GetCollectionElementType(type).IsAbstract;
                
                if (type.IsArray)
                {
                    return ParseArray(parsedEntry, attribute, in format, elements, type, isAbstract);
                }
                else if (Reflection.IsTypeGenericList(type))
                {
                    return ParseList(parsedEntry, attribute, in format, elements, type, isAbstract);
                }
                Debug.LogError("could not parse a collection");
                return default;
            }

            object ParseArray(in ParsedEntry parsedEntry, FormatAttributeBase attribute, in ReadOnlySpan<char> format, List<int2> elements, Type type, bool isAbstract)
            {
                Type elementType = Utilities.GetCollectionElementType(type);
                Array array = Array.CreateInstance(elementType, elements.Count);

                bool isStructured = attribute.GetType() == typeof(StructuredFormatAttribute);
                bool isDynamic    = attribute.GetType() == typeof(DynamicFormatAttribute);

                for (int i = 0; i < array.Length; i++)
                {
                    ReadOnlySpan<char> span = format.Slice(elements[i].x, elements[i].y);
                    ReadOnlySpan<char> attributes = null;

                    bool hasParameters = span[0] == Tokens.AttributesStartChar;

                    if (isAbstract)
                    {
                        attributes = GetElementParameters(ref span);

                        ParseMetaAttribute(in attributes, "Type", out ReadOnlySpan<char> elementTypeName);
                        elementType = Type.GetType(elementTypeName.ToString());
                    }
                    if (span.SequenceEqual(Tokens.NullToken.AsSpan()))
                    {
                        array.SetValue(null, i);
                        continue;
                    }
                    object element = Reflection.GetTypeObjectInstance(elementType);
                    if (isStructured)
                    {
                        element = DeserializeObject(element, in span);
                    }
                    else
                    {
                        int trait = parsedEntry.trait;
                        if (isDynamic)
                        {
                            if (attributes == null)
                            {
                                attributes = GetElementParameters(ref span);
                            }
                            ParseMetaAttribute(in attributes, "Trait", out ReadOnlySpan<char> elementTrait);
                            if (elementTrait != null)
                            {
                                trait = Convert.ToInt32(elementTrait.ToString());
                            }
                        }
                        string[] parameters = null;
                        if (hasParameters)
                        {
                            if (attributes == null)
                            {
                                attributes = GetElementParameters(ref span);
                            }
                            ParseMetaAttribute(in attributes, "Parameters", out ReadOnlySpan<char> param);
                            if (param != null)
                            {
                                parameters = param.ToString().Split(',');
                            }
                        }
                        element = GetValue(trait, span.ToString(), parameters);
                    }
                    array.SetValue(element, i);
                }
                return array;
            }
            object ParseList(in ParsedEntry parsedEntry, FormatAttributeBase attribute, in ReadOnlySpan<char> format, List<int2> elements, Type type, bool isAbstract)
            {
                Type elementType = Utilities.GetCollectionElementType(type);
                IList list = (IList)Reflection.GetTypeObjectInstance(type);

                bool isStructured   = attribute.GetType() == typeof(StructuredFormatAttribute);
                bool isDynamic      = attribute.GetType() == typeof(DynamicFormatAttribute);

                for (int i = 0; i < elements.Count; i++)
                {
                    ReadOnlySpan<char> span = format.Slice(elements[i].x, elements[i].y);
                    ReadOnlySpan<char> attributes = null;

                    bool hasParameters = span[0] == Tokens.AttributesStartChar;

                    if (isAbstract)
                    {
                        attributes = GetElementParameters(ref span);

                        ParseMetaAttribute(in attributes, "Type", out ReadOnlySpan<char> elementTypeName);
                        elementType = Type.GetType(elementTypeName.ToString());
                    }
                    if (span.SequenceEqual(Tokens.NullToken.AsSpan()))
                    {
                        list.Add(null);
                        continue;
                    }
                    object element = Reflection.GetTypeObjectInstance(elementType);
                    if (isStructured)
                    {
                        element = DeserializeObject(element, in span);
                    }
                    else
                    {
                        int trait = parsedEntry.trait;
                        if (isDynamic)
                        {
                            if (attributes == null)
                            {
                                attributes = GetElementParameters(ref span);
                            }
                            ParseMetaAttribute(in attributes, "Trait", out ReadOnlySpan<char> elementTrait);
                            if (elementTrait != null)
                            {
                                trait = Convert.ToInt32(elementTrait.ToString());
                            }
                        }
                        string[] parameters = null;
                        if (hasParameters)
                        {
                            if (attributes == null)
                            {
                                attributes = GetElementParameters(ref span);
                            }
                            ParseMetaAttribute(in attributes, "Parameters", out ReadOnlySpan<char> param);
                            if (param != null)
                            {
                                parameters = param.ToString().Split(',');
                            }
                        }
                        element = GetValue(trait, span.ToString(), parameters);
                    }
                    list.Add(element);
                }
                return list;
            }
            ReadOnlySpan<char> GetElementParameters(ref ReadOnlySpan<char> span)
            {
                ReadOnlySpan<char> attributes = GetEntrySlice(ref span, Tokens.AttributesEndChar, null);
                attributes = attributes.Slice(1, attributes.Length - 2);

                return attributes;
            }

            FieldInfo GetFieldByName<T>(in T target, string name, out object fieldTarget)
            {
                FieldInfo field = Utilities.GetFieldFromTarget(target, name);
                fieldTarget = field?.GetValue(target);

                return field;
            }
            FormatObjectAttribute TryGetDeserializer(int trait)
            {
                var formatters = Conversion.GetEnumerableOfType<FormatObjectAttribute>();
                foreach (var formatter in formatters)
                {
                    if (formatter.Trait == trait)
                    {
                        return formatter;
                    }
                }
                return null;
            }
        }
    }
}