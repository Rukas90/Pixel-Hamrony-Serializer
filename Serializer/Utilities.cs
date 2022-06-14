using System;
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
        static class Utilities
        {
            public static string WrapSection(string ID, bool end)
            {
                StringBuilder builder = new StringBuilder("<");
                if (end)
                {
                    builder.Append('/');
                }
                builder.Append(ID);
                builder.Append('>');

                return builder.ToString();
            }

            public static IEnumerable<FieldInfo> GetTargetFormatFields(object target)
            {
                return Reflection.GetAllAttributeMarkedFields(target, typeof(FormatAttributeBase), true,
                        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            }
            public static FieldInfo GetFieldFromTarget(object target, string name)
            {
                return Reflection.GetField(target, name,
                        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            }
            public static FormatAttributeBase GetFieldAttribute(FieldInfo field) => (FormatAttributeBase)field.GetCustomAttribute(typeof(FormatAttributeBase));

            public static Type GetCollectionElementType(object collection)
            {
                if (Reflection.IsTypeGenericList(collection.GetType()))
                {
                    return collection.GetType().GetGenericArguments().Single();
                }
                else
                {
                    return collection.GetType().GetElementType();
                }
            }
            public static Type GetCollectionElementType(Type collection)
            {
                if (Reflection.IsTypeGenericList(collection))
                {
                    return collection.GetGenericArguments().Single();
                }
                else
                {
                    return collection.GetElementType();
                }
            }
        }
    }
}