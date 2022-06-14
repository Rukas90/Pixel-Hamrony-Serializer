using System;
using Unity.Mathematics;

namespace PixelHarmony.Serialization
{
    internal class Float3FormatAttribute : FormatObjectAttribute
    {
        public override int Trait => 2541;

        public override object Read(string data, params string[] args)
        {
            var s = data.Split('x');
            if (s.Length != 3)
            {
                throw new ArgumentOutOfRangeException();
            }
            return new float3(Convert.ToSingle(s[0]), Convert.ToSingle(s[1]), Convert.ToSingle(s[2]));
        }
        public override int Write(object value, out string format, out string parameters)
        {
            if (base.Write(value, out format, out parameters) == 0) { return 0; }

            if (value.GetType() != typeof(float3))
            { return -1; }

            var i = (float3)value;
            format = i.x + "x" + i.y + "x" + i.z;
            return 1;
        }
    }
}