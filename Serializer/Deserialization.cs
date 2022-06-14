using System;

namespace PixelHarmony.Serialization
{
    internal static partial class PHSerializer
    {
        internal enum DeserializeMode { File, Content }

        public static T Deserialize<T>(string value, DeserializeMode mode, bool debug = false)              => new Deserializer().Execute<T>(value, mode, debug);
        public static object Deserialize(Type type, string value, DeserializeMode mode, bool debug = false) => new Deserializer().Execute(type, value, mode, debug);

        public static T Deserialize<T>(string value, DeserializeMode mode, bool debug = false, params string[] sectionIDs)               => new Deserializer().Execute<T>(value, mode, debug, sectionIDs);
        public static object Deserialize(Type type, string value, DeserializeMode mode, bool debug = false, params string[] sectionIDs)  => new Deserializer().Execute(type, value, mode, debug, sectionIDs);
    }
}