using System;
using Unity.Mathematics;

namespace PixelHarmony.Serialization
{
    internal class Float4FormatAttribute : FormatObjectAttribute
    {
        public override int Trait => 2425;

        public override object Read(string data, params string[] args)
        {
            var s = data.Split('x');
            if (s.Length != 4)
            {
                throw new ArgumentOutOfRangeException();
            }
            return new float4(Convert.ToSingle(s[0]), Convert.ToSingle(s[1]), Convert.ToSingle(s[2]), Convert.ToSingle(s[3]));
        }
        public override int Write(object value, out string format, out string parameters)
        {
            if (base.Write(value, out format, out parameters) == 0) { return 0; }

            if (value.GetType() != typeof(float4))
            { return -1; }

            var i = (float4)value;
            format = i.x + "x" + i.y + "x" + i.z + "x" + i.w;
            return 1;
        }
    }
}