using System.Threading.Tasks;

namespace NotepadKit
{
    internal enum BleInputProperty
    {
        Disabled,
        Notification,
        Indication
    }

    internal class NotepadType
    {
        private BleType _bleType;
        private readonly NotepadClient _notepadClient;

        internal NotepadType(NotepadClient notepadClient, BleType bleType)
        {
            _bleType = bleType;
            _notepadClient = notepadClient;
        }

        public async Task ConfigCharacteristics()
        {
            foreach (var characteristic in _notepadClient.InputIndicationCharacteristics)
                await ConfigInputCharacteristic(characteristic, BleInputProperty.Indication);
        }

        private async Task ConfigInputCharacteristic((string, string) serviceCharacteristic, BleInputProperty inputProperty)
        {
            await _bleType.SetNotifiable(serviceCharacteristic, inputProperty);
        }
    }
}