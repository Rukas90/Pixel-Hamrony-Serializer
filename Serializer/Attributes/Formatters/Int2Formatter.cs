using System;
using Unity.Mathematics;

namespace PixelHarmony.Serialization
{
    internal class Int2FormatAttribute : FormatObjectAttribute
    {
        public override int Trait => 2053;

        public override object Read(string data, params string[] args)
        {
            var s = data.Split('x');
            if (s.Length != 2)
            {
                throw new ArgumentOutOfRangeException();
            }
            return new int2(Convert.ToInt32(s[0]), Convert.ToInt32(s[1]));
        }
        public override int Write(object value, out string format, out string parameters)
        {
            if (base.Write(value, out format, out parameters) == 0) { return 0; }

            if (value.GetType() != typeof(int2))
            { return -1; }

            var i = (int2)value;
            format = i.x + "x" + i.y;
            return 1;
        }
    }
}