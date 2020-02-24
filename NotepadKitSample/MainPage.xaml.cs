using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Threading.Tasks;
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
        private NotepadClient _notepadClient;

        public MainPage()
        {
            InitializeComponent();
            _notepadScanner.Found += (sender, args) => { _scanResultList.Add(args); };
            _notepadConnector.ConnectionChanged += OnConnectionChanged;
        }

        private void OnConnectionChanged(NotepadClient sender, ConnectionState args)
        {
            Debug.WriteLine($"OnConnectionChanged {args}");
            if (args == ConnectionState.Connected)
            {
                _notepadClient = sender;
                _notepadClient.SyncPointerReceived += OnSyncPointerReceived;
                _notepadClient.SetMode(NotepadMode.Sync);
            }
            else if (args == ConnectionState.Disconnected)
            {
                if (_notepadClient != null)
                    _notepadClient.SyncPointerReceived -= OnSyncPointerReceived;
                _notepadClient = null;
            }
        }

        private void OnSyncPointerReceived(NotepadClient sender, List<NotePenPointer> args)
        {
            foreach (var pointer in args)
                Debug.WriteLine($"OnSyncPointerReceived {pointer}");
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

        private void button5_Click(object sender, RoutedEventArgs e)
        {
            _notepadClient.GetMemoSummary().ToObservable()
                .Subscribe(memoSummary => Debug.WriteLine($"GetMemoSummary {memoSummary}"));
        }

        private void button6_Click(object sender, RoutedEventArgs e)
        {
            _notepadClient.GetMemoInfo().ToObservable()
                .Subscribe(memoInfo => Debug.WriteLine($"GetMemoInfo {memoInfo}"));
        }

        private void button7_Click(object sender, RoutedEventArgs e)
        {
            ImportMemoAsync();
        }

        private async void ImportMemoAsync()
        {
            var memoData = await _notepadClient.ImportMemo(i => Debug.WriteLine($"ImportMemo progress {i}"));
            Debug.WriteLine($"memoData {memoData.pointers.Count}");
        }

        private void button8_Click(object sender, RoutedEventArgs e)
        {
            DeleteMemoAsync();
        }

        private async void DeleteMemoAsync()
        {
            await _notepadClient.DeleteMemo();
            Debug.WriteLine("DeleteMemo success");
        }
    }
}