using System;
using System.Text;

namespace PixelHarmony.Serialization
{
    [AttributeUsage(AttributeTargets.Field)]
    internal abstract class FormatAttributeBase : Attribute { }

    internal abstract class SectionDefineAttribute : Attribute { }

    /// <summary>
    /// Only Applicable for root format fields. Sub structured format fields will be ignored.
    /// </summary>
    internal class SectionAttribute : SectionDefineAttribute
    {
        public readonly string ID = "";
        public SectionAttribute(string ID)
        {
            this.ID = ID;
        }
    }

    internal class StructuredFormatAttribute : FormatAttributeBase { }
    internal abstract class FormatObjectAttribute : FormatAttributeBase
    {
        public abstract int Trait { get; }

        public virtual int Write(object value, out string format, out string parameters)
        {
            parameters = null;
            if (value == null)
            {
                format = "Null";
                return 0;
            }
            format = null; return 1;
        }
        public abstract object Read(string data, params string[] args);
        protected bool NullCheck(in string data)
        {
            if (data == "Null")
            {
                return true;
            }
            return false;
        }
        protected string AppendParameters(in string parameters, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                StringBuilder format = new StringBuilder(parameters);
                if (format.Length > 0 && format[format.Length - 1] != ',')
                {
                    format.Append(",");
                }

                int index = 0;
                foreach (var arg in args)
                {
                    format.Append(arg);
                    if (index < args.Length - 1)
                    {
                        format.Append(",");
                    }
                    index++;
                }
                return format.ToString();
            }
            return parameters;
        }
    }

    internal class DynamicFormatAttribute : FormatAttributeBase { }
}