namespace PixelHarmony.Serialization
{
    internal static partial class PHSerializer
    {
        public static string Serialize(object target)
        {
            return new Serializer().Execute(target);
        }        
    }
}