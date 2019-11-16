using System.Collections.Generic;
using System.Linq;
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
        private readonly NotepadConnector _notepadConnector = new NotepadConnector();
        private readonly NotepadScanner _notepadScanner = new NotepadScanner();
        private readonly List<NotepadScanResult> _scanResultList = new List<NotepadScanResult>();

        public MainPage()
        {
            InitializeComponent();
            _notepadScanner.Found += (sender, args) => { _scanResultList.Add(args); };
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            _notepadScanner.StartScan();
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            _notepadScanner.StopScan();
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            _notepadConnector.Connect(_scanResultList.First());
        }

        private void button4_Click(object sender, RoutedEventArgs e)
        {
            _notepadConnector.Disconnect();
        }
    }
}