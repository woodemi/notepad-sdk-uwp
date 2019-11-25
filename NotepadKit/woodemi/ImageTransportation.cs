namespace NotepadKit
{
    internal class ImageTransportation
    {
        private static int HEADER_LENGTH = 58;
        public static int EMPTY_LENGTH = HEADER_LENGTH + 6 /*empty imageTagValue*/ + 8 /*crcTagValue*/;
    }
}