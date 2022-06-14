using System;

namespace PixelHarmony.Serialization
{
    internal class FloatFormatAttribute : FormatObjectAttribute
    {
        public override int Trait => 7080;

        public override object Read(string data, params string[] args)
        {
            return Convert.ToSingle(data);
        }
        public override int Write(object value, out string format, out string parameters)
        {
            if (base.Write(value, out format, out parameters) == 0) { return 0; }

            if (value.GetType() != typeof(float))
            { return -1; }

            format = Convert.ToString(value);
            return 1;
        }
    }
}