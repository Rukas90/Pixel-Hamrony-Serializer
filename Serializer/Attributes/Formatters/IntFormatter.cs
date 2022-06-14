using System;

namespace PixelHarmony.Serialization
{
    internal class IntFormatAttribute : FormatObjectAttribute
    {
        public override int Trait => 3920;

        public override object Read(string data, params string[] args)
        {
            return Convert.ToInt32(data);
        }
        public override int Write(object value, out string format, out string parameters)
        {
            if (base.Write(value, out format, out parameters) == 0) { return 0; }

            if (value.GetType() != typeof(int))
            { return -1; }

            format = Convert.ToString(value);
            return 1;
        }
    }
}