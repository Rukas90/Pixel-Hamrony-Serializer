using UnityEngine;

namespace PixelHarmony.Serialization
{
    internal class ColorFormatAttribute : FormatObjectAttribute
    {
        public override int Trait => 2886;

        public override object Read(string data, params string[] args)
        {
            if (!ColorUtility.TryParseHtmlString(data.StartsWith("#") ? data : "#" + data, out Color color))
            {
                return (Color)default;
            }
            return color;
        }
        public override int Write(object value, out string format, out string parameters)
        {
            if (base.Write(value, out format, out parameters) == 0) { return 0; }

            if (value.GetType() != typeof(Color))
            { return -1; }

            format = ColorUtility.ToHtmlStringRGBA((Color)value);
            return 1;
        }
    }
}