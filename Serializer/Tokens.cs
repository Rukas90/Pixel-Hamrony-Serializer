namespace PixelHarmony.Serialization
{
    internal partial class PHSerializer
    { 
        static class Tokens
        {
            public const string EntryStartToken        = "<Data>";
            public const string EntryEndToken          = "</Data>";

            public const char PathSeparator            = '/';

            public const char AttributesStartChar      = '<';
            public const char AttributesEndChar        = '>'; 

            public const string MetaHeader             = "[Meta]";
            public const char MetaSeparator            = ',';
            public const char MetaEquals               = ':';
            public const char MetaEntrySeparator       = '|';

            public const char DefineToken              = '=';
            public const string TypeFormat             = "T";
            public const char Separator                = '|';

            public const char ArrayStartToken          = '[';
            public const char ArrayEndToken            = ']';

            public static readonly string ArrayToken   = ArrayStartToken + "C" + Separator + TypeFormat + ArrayEndToken;
            public const char ElementStartToken        = '{';
            public const char ElementEndToken          = '}';

            public const string NullToken              = "Null";
            public static readonly char[] NullArray = new char[4] { 'N', 'u', 'l', 'l' };

            public const char BreakToken               = '\\';
            public const char StringToken              = '"';
            public const char CharToken                = '\'';
        }
    }
}