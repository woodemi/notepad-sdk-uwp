using System.Collections.Generic;
using System.Threading.Tasks;

namespace NotepadKit
{
    public abstract class NotepadClient
    {
        internal NotepadType _notepadType;
        public abstract (string, string) CommandRequestCharacteristic { get; }

        public abstract (string, string) CommandResponseCharacteristic { get; }

        public abstract IReadOnlyList<(string, string)> InputIndicationCharacteristics { get; }

        internal abstract Task CompleteConnection();
    }
}