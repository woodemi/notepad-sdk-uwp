using System.Threading.Tasks;

namespace NotepadKit
{
    internal enum BleInputProperty
    {
        Disabled,
        Notification,
        Indication
    }

    public class NotepadType
    {
        private BleType _bleType;
        private readonly NotepadClient _notepadClient;

        public NotepadType(NotepadClient notepadClient)
        {
            _notepadClient = notepadClient;
        }

        public async Task ConfigCharacteristics()
        {
            foreach (var characteristic in _notepadClient.InputIndicationCharacteristics)
                ConfigInputCharacteristic(characteristic, BleInputProperty.Indication);
        }

        private void ConfigInputCharacteristic((string, string) serviceCharacteristic, BleInputProperty inputProperty)
        {
            _bleType.SetNotifiable(serviceCharacteristic, inputProperty);
        }
    }
}