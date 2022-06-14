using System;
using Unity.Mathematics;

namespace PixelHarmony.Serialization
{
    internal class Float2FormatAttribute : FormatObjectAttribute
    {
        public override int Trait => 2658;

        public override object Read(string data, params string[] args)
        {
            var s = data.Split('x');
            if (s.Length != 2)
            {
                throw new ArgumentOutOfRangeException();
            }
            return new float2(Convert.ToSingle(s[0]), Convert.ToSingle(s[1]));
        }
        public override int Write(object value, out string format, out string parameters)
        {
            if (base.Write(value, out format, out parameters) == 0) { return 0; }

            if (value.GetType() != typeof(float2))
            { return -1; }

            var i = (float2)value;
            format = i.x + "x" + i.y;
            return 1;
        }
    }
}