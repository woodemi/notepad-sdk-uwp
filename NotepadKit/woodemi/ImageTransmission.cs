using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NotepadKit
{
    internal class ImageTransmission
    {
        private static int HEADER_LENGTH = 58;
        public static int EMPTY_LENGTH = HEADER_LENGTH + 6 /*empty imageTagValue*/ + 8 /*crcTagValue*/;

        private byte[] _headerData;

        private byte[] _imageData;

        public byte[] ImageData => _imageData;

        private byte[] _crc;

        private ImageTransmission()
        {
        }

        public static ImageTransmission forInput(byte[] bytes)
        {
            var imageTransmission = new ImageTransmission();
            imageTransmission._headerData = bytes.Take(HEADER_LENGTH).ToArray();
            var totalSize = ParseHeader(imageTransmission._headerData);
            var pendingData = bytes.Skip(HEADER_LENGTH).ToArray();
            if (totalSize != HEADER_LENGTH + pendingData.Length) throw new InvalidDataException("Invalid totalSize");

            var longTagValues = ScanLongTagValue(pendingData).ToList();
            imageTransmission._imageData =
                longTagValues.First(lv => lv.Tag.SequenceEqual(LongTagValue.TAG_IMAGE)).Value;
            imageTransmission._crc = longTagValues.First(lv => lv.Tag.SequenceEqual(LongTagValue.TAG_CRC)).Value;
            return imageTransmission;
        }

        /**
         *  +--------------------------------------------------------------------------------------------------------------------------------+
         *  |                                                             header                                                             |
         *  +----------+----------------+---------------+---------------+------------+----------+---------------+---------------+------------+
         *  | fileId   |  headerVersion |  headerLength |  fieldControl |  companyId |  imageId |  imageVersion |  headerString |  totalSize |
         *  |          |                |               |               |            |          |               |               |            |
         *  | 4 bytes  |  2 bytes       |  2 bytes      |  2 bytes      |  2 bytes   |  2 bytes |  8 bytes      |  32 bytes     |  4 bytes   |
         *  +----------+----------------+---------------+---------------+------------+----------+---------------+---------------+------------+
         */
        private static int ParseHeader(byte[] bytes)
        {
            using (var reader = new BinaryReader(new MemoryStream(bytes)))
            {
                var fileId = reader.ReadBytes(4);
                var headerVersion = reader.ReadBytes(2);
                var headerLength = reader.ReadUInt16();
                if (headerLength != HEADER_LENGTH) throw new InvalidDataException("Invalid headerLength");

                var fieldControl = reader.ReadBytes(2);
                var companyId = reader.ReadBytes(2);
                var imageId = reader.ReadBytes(2);
                var imageVersion = reader.ReadBytes(8);
                var headerString = reader.ReadBytes(32);
                return (int) reader.ReadUInt32();
            }
        }

        private static IEnumerable<LongTagValue> ScanLongTagValue(byte[] bytes)
        {
            var index = 0;
            using (var reader = new BinaryReader(new MemoryStream(bytes)))
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var tag = reader.ReadBytes(2);
                    var length = (int) reader.ReadUInt32();
                    var value = reader.ReadBytes(length);
                    yield return new LongTagValue(tag, value);
                }
            }
        }
    }

    internal struct LongTagValue
    {
        public static readonly byte[] TAG_IMAGE = {0x00, 0x00};
        public static readonly byte[] TAG_CRC = {0x00, 0xF1};

        public byte[] Tag { get; }
        public byte[] Value { get; }

        public LongTagValue(byte[] tag, byte[] value)
        {
            Tag = tag;
            Value = value;
        }
    }
}