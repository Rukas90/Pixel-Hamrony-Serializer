using System;

namespace PixelHarmony.Serialization
{
    internal class StringFormatAttribute : FormatObjectAttribute
    {
        public override int Trait => 5717;

        public override object Read(string data, params string[] args)
        {
            if (NullCheck(in data)) { return null; }
            return data.TrimStart('\"').TrimEnd('\"');
        }
        public override int Write(object value, out string format, out string parameters)
        {
            if (base.Write(value, out format, out parameters) == 0) { return 0; }

            if (value.GetType() != typeof(string))
            { return -1; }

            format = "\"" + Convert.ToString(value) + "\"";
            return 1;
        }
    }
}