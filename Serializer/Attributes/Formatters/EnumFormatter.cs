using System;

namespace PixelHarmony.Serialization
{
    internal class EnumFormatAttribute : FormatObjectAttribute
    {
        public override int Trait => 2903;

        public override object Read(string data, params string[] args)
        {
            var type = Type.GetType(args[0]);
            return Enum.ToObject(type, Convert.ToInt32(data));
        }
        public override int Write(object value, out string format, out string parameters)
        {
            if (base.Write(value, out format, out parameters) == 0) { return 0; }

            if (!value.GetType().IsEnum)
            { return -1; }

            parameters = AppendParameters(in parameters, ((Enum)value).GetType());
            format = Convert.ToString(Convert.ToInt32(value));
            return 1;
        }
    }
}