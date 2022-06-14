using System;
using System.Text;

namespace PixelHarmony.Serialization
{
    internal class DateTimeFormat : FormatObjectAttribute
    {
        public override int Trait => 9964;

        public override object Read(string data, params string[] args)
        {
            var s = data.Split('-');

            if (s.Length != 7) { return default; }

            return new DateTime(Convert.ToInt32(s[0]), Convert.ToInt32(s[1]), Convert.ToInt32(s[2]), Convert.ToInt32(s[3]), Convert.ToInt32(s[4]), Convert.ToInt32(s[5]), Convert.ToInt32(s[6]));
        }
        public override int Write(object value, out string format, out string parameters)
        {
            if (base.Write(value, out format, out parameters) == 0) { return 0; }

            if (value.GetType() != typeof(DateTime))
            { return -1; }

            DateTime dateTime = (DateTime)value;

            StringBuilder builder = new StringBuilder();
            builder.Append(dateTime.Year); builder.Append("-");
            builder.Append(dateTime.Month); builder.Append("-");
            builder.Append(dateTime.Day); builder.Append("-");
            builder.Append(dateTime.Hour); builder.Append("-");
            builder.Append(dateTime.Minute); builder.Append("-");
            builder.Append(dateTime.Second); builder.Append("-");
            builder.Append(dateTime.Millisecond);

            format = builder.ToString();
            return 1;
        }
    }
}