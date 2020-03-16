using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.AppService;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using NotepadKit;
using Size = System.Drawing.Size;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace NotepadKitSample
{
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly InjectPointHelper _injectPointHelper = new InjectPointHelper(new Size(14800, 21000), 512);
        private readonly NotepadConnector _notepadConnector = new NotepadConnector();
        private readonly NotepadScanner _notepadScanner = new NotepadScanner();
        private readonly List<NotepadScanResult> _scanResultList = new List<NotepadScanResult>();
        private NotepadClient _notepadClient;
        private AppServiceConnection _appServiceConnection;

        public MainPage()
        {
            InitializeComponent();
            _notepadScanner.Found += (sender, args) => { _scanResultList.Add(args); };
            _notepadConnector.ConnectionChanged += OnConnectionChanged;

            var displayInformation = DisplayInformation.GetForCurrentView();
            var scaleFactor = displayInformation.RawPixelsPerViewPixel;
            var screenSize = new Size((int) displayInformation.ScreenWidthInRawPixels,
                (int) displayInformation.ScreenHeightInRawPixels);
            var appBounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var appSize = new Size((int) (appBounds.Width * scaleFactor), (int) (appBounds.Height * scaleFactor));
            Debug.WriteLine($"scaleFactor {scaleFactor}, screenSize {screenSize}, appSize {appSize}");
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
            {
                Debug.WriteLine($"OnSyncPointerReceived {pointer}");
                var (x, y, p) = _injectPointHelper.ToInputPoint(pointer.x, pointer.y, pointer.p);
                if (_appServiceConnection != null)
                {
                    var message = new ValueSet();
                    message["Request"] = "InjectInput";
                    message["Args"] = new ValueSet
                    {
                        ["Pointer"] = new ValueSet
                        {
                            ["X"] = x,
                            ["Y"] = y,
                            ["P"] = p,
                        }
                    };
                    _appServiceConnection.SendMessageAsync(message);
                }
            }
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
            //_notepadClient.GetMemoSummary().ToObservable()
            //    .Subscribe(memoSummary => Debug.WriteLine($"GetMemoSummary {memoSummary}"));
            StartInjectAsync();
        }

        private async void StartInjectAsync()
        {
            if (_appServiceConnection != null) return;

            _appServiceConnection = new AppServiceConnection();
            _appServiceConnection.AppServiceName = "InjectPenHelper";
            _appServiceConnection.PackageFamilyName = "450af03b-4d3c-40a2-aa50-60be67d40dce_7t2jb1g64jh92";
            var status = await _appServiceConnection.OpenAsync();

            if (status != AppServiceConnectionStatus.Success)
            {
                Debug.WriteLine($"OpenAsync fail: {status}");
                _appServiceConnection?.Dispose();
                _appServiceConnection = null;
            }
        }

        private void button6_Click(object sender, RoutedEventArgs e)
        {
            //_notepadClient.GetMemoInfo().ToObservable()
            //    .Subscribe(memoInfo => Debug.WriteLine($"GetMemoInfo {memoInfo}"));
            _appServiceConnection?.Dispose();
            _appServiceConnection = null;
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

    public class InjectPointHelper
    {
        private readonly Rect _validBounds;

        private readonly double _viewScale;

        private readonly int _maxPressure;

        public InjectPointHelper(Size padSize, int maxPressure)
        {
            var displayInformation = DisplayInformation.GetForCurrentView();
            var scaleFactor = displayInformation.RawPixelsPerViewPixel;
            var screenSize = new Size((int) (displayInformation.ScreenWidthInRawPixels * scaleFactor),
                (int) (displayInformation.ScreenHeightInRawPixels * scaleFactor));
            Debug.WriteLine($"screenSize {screenSize.Width}, {screenSize.Height}");
            _viewScale = Math.Max(screenSize.Width * 1.0 / padSize.Width, screenSize.Height * 1.0 / padSize.Height);
            var widthPadding = padSize.Width * _viewScale - screenSize.Width;
            var heightPadding = padSize.Height * _viewScale - screenSize.Height;
            Debug.WriteLine($"_viewScale {_viewScale}, widthPadding {widthPadding}, heightPadding {heightPadding}");
            _validBounds = new Rect(widthPadding / 2, heightPadding / 2, screenSize.Width, screenSize.Height);
            Debug.WriteLine($"_validBounds {_validBounds}");

            _maxPressure = maxPressure;
        }

        public (int, int, double) ToInputPoint(int penX, int penY, int penP)
        {
            var screenPoint = new Point(penX * _viewScale, penY * _viewScale);
            var x = Math.Max(_validBounds.Left, Math.Min(screenPoint.X, _validBounds.Right)) - _validBounds.Left;
            var y = Math.Max(_validBounds.Top, Math.Min(screenPoint.Y, _validBounds.Bottom)) - _validBounds.Top;
            return ((int) x, (int) y, penP * 1.0 / _maxPressure);
        }
    }
}