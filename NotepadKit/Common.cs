using System.Linq;
using Windows.Storage.Streams;

namespace NotepadKit
{
    public static class Constants
    {
        internal static readonly byte[] WOODEMI_PREFIX = {0x57, 0x44, 0x4D};
    }

    internal static class Extensions
    {
        public static byte[] ToByteArray(this IBuffer value)
        {
            var bytes = new byte[value.Length];
            using (var reader = DataReader.FromBuffer(value))
            {
                reader.ReadBytes(bytes);
            }

            return bytes;
        }

        public static bool StartWith(this byte[] value, byte[] prefix)
        {
            return value.Length >= prefix.Length && Enumerable.Range(0, prefix.Length).All(i => value[i] == prefix[i]);
        }
    }
}