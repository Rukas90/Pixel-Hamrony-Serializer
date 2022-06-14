using System;
using UnityEngine;

namespace PixelHarmony.Serialization
{
    internal class RenderTextureFormatAttribute : FormatObjectAttribute
    {
        public override int Trait => 4510;

        public override object Read(string data, params string[] args)
        {
            if (NullCheck(in data)) { return null; }

            return Utils.Conversion.SetRenderTextureRawData(Convert.FromBase64String(data), Convert.ToInt32(args[0]), Convert.ToInt32(args[1]));
        }
        public override int Write(object value, out string format, out string parameters)
        {
            if (base.Write(value, out format, out parameters) == 0) { return 0; }

            if (value.GetType() != typeof(RenderTexture))
            { return -1; }

            var tex = (RenderTexture)value;

            parameters = AppendParameters(in parameters, tex.width, tex.height);
            format = Convert.ToBase64String(Utils.Conversion.GetRawData(tex));

            return 1;
        }
    }
}