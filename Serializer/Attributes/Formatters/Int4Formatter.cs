using System;
using Unity.Mathematics;

namespace PixelHarmony.Serialization
{
    internal class Int4FormatAttribute : FormatObjectAttribute
    {
        public override int Trait => 2125;

        public override object Read(string data, params string[] args)
        {
            var s = data.Split('x');
            if (s.Length != 4)
            {
                throw new ArgumentOutOfRangeException();
            }
            return new int4(Convert.ToInt32(s[0]), Convert.ToInt32(s[1]), Convert.ToInt32(s[2]), Convert.ToInt32(s[3]));
        }
        public override int Write(object value, out string format, out string parameters)
        {
            if (base.Write(value, out format, out parameters) == 0) { return 0; }

            if (value.GetType() != typeof(int4))
            { return -1; }

            var i = (int4)value;
            format = i.x + "x" + i.y + "x" + i.z + "x" + i.w;
            return 1;
        }
    }
}