using System.Collections.Generic;

namespace NotepadKit
{
    public abstract class NotepadClient
    {
        public abstract (string, string) CommandRequestCharacteristic { get; }

        public abstract (string, string) CommandResponseCharacteristic { get; }

        public abstract IReadOnlyList<(string, string)> InputIndicationCharacteristics { get; }
    }
}