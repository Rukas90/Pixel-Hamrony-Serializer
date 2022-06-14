using System;
using Unity.Mathematics;

namespace PixelHarmony.Serialization
{
    internal class Int3FormatAttribute : FormatObjectAttribute
    {
        public override int Trait => 2112;

        public override object Read(string data, params string[] args)
        {
            var s = data.Split('x');
            if (s.Length != 3)
            {
                throw new ArgumentOutOfRangeException();
            }
            return new int3(Convert.ToInt32(s[0]), Convert.ToInt32(s[1]), Convert.ToInt32(s[2]));
        }
        public override int Write(object value, out string format, out string parameters)
        {
            if (base.Write(value, out format, out parameters) == 0) { return 0; }

            if (value.GetType() != typeof(int3))
            { return -1; }

            var i = (int3)value;
            format = i.x + "x" + i.y + "x" + i.z;
            return 1;
        }
    }
}