using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using NotepadKit;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace NotepadKitSample
{
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly NotepadScanner _notepadScanner = new NotepadScanner();

        public MainPage()
        {
            InitializeComponent();
            _notepadScanner.Found += (sender, args) => { Debug.WriteLine($"OnNotepadFound {args.BluetoothAddress}"); };
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            _notepadScanner.StartScan();
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            _notepadScanner.StopScan();
        }
    }
}