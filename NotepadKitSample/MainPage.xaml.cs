using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Timers;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Input.Preview.Injection;
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
        private readonly InjectPenHelper _injectPenHelper = new InjectPenHelper(new Size(14800, 21000), 512);
        private readonly NotepadConnector _notepadConnector = new NotepadConnector();
        private readonly NotepadScanner _notepadScanner = new NotepadScanner();
        private readonly List<NotepadScanResult> _scanResultList = new List<NotepadScanResult>();
        private NotepadClient _notepadClient;

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
                _injectPenHelper.InjectInput(pointer);
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

    public class InjectPenHelper : IDisposable
    {
        private readonly InputInjector _inputInjector = InputInjector.TryCreate();

        private readonly Timer _inRangeTimer;

        private readonly int _maxPressure;

        private readonly Rect _validBounds;

        private readonly double _viewScale;

        // private NotePenPointer? _lastNotePenPointer;

        private InjectedInputPenInfo _preInputPenInfo;

        public InjectPenHelper(Size padSize, int maxPressure)
        {
            var displayInformation = DisplayInformation.GetForCurrentView();
            var scaleFactor = displayInformation.RawPixelsPerViewPixel;
            var screenSize = new Size((int) (displayInformation.ScreenWidthInRawPixels * scaleFactor),
                (int) (displayInformation.ScreenHeightInRawPixels * scaleFactor));
            _viewScale = Math.Max(screenSize.Width * 1.0 / padSize.Width, screenSize.Height * 1.0 / padSize.Height);
            var widthPadding = padSize.Width * _viewScale - screenSize.Width;
            var heightPadding = padSize.Height * _viewScale - screenSize.Height;
            Debug.WriteLine($"_viewScale {_viewScale}, widthPadding {widthPadding}, heightPadding {heightPadding}");
            _validBounds = new Rect(widthPadding / 2, heightPadding / 2, screenSize.Width, screenSize.Height);
            Debug.WriteLine($"_validBounds {_validBounds}");

            _maxPressure = maxPressure;

            _inRangeTimer = new Timer
            {
                Interval = 500,
                AutoReset = false
            };
            _inRangeTimer.Elapsed += OnTimedEvent;

            _inputInjector.InitializePenInjection(InjectedInputVisualizationMode.Default);
        }

        public void Dispose()
        {
            _inputInjector.UninitializePenInjection();

            if (_inRangeTimer != null)
                _inRangeTimer.Elapsed -= OnTimedEvent;
            _inRangeTimer?.Dispose();
        }

        public void InjectInput(NotePenPointer pointer)
        {
            _inRangeTimer.Stop();

            // var deltaX = _lastNotePenPointer.HasValue ? pointer.x - _lastNotePenPointer.Value.x : 0;
            // var deltaY = _lastNotePenPointer.HasValue ? pointer.y - _lastNotePenPointer.Value.y : 0;
            //
            // var penInfo = ToInputPenInfo(deltaX, deltaY, pointer.p);
            // var penInfo = ToInputPenInfo(pointer);
            // Debug.WriteLine($"ToInputPenInfo {penInfo}");

            var penInfo = ToInputPenInfo2(pointer);
            Debug.WriteLine($"ToInputPenInfo2 {penInfo}");

            _inputInjector.InjectPenInput(penInfo);
            _preInputPenInfo = penInfo;

            // _lastNotePenPointer = pointer;

            _inRangeTimer.Start();
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs args)
        {
            Debug.WriteLine($"OnTimedEvent {args.SignalTime}");
            // _lastNotePenPointer = null;
            _preInputPenInfo = null;
        }

        private InjectedInputPointerInfo ToInputPointerInfo(int x, int y, int p)
        {
            var pointerInfo = new InjectedInputPointerInfo
            {
                PixelLocation = new InjectedInputPoint {PositionX = x, PositionY = y}
            };

            if (_preInputPenInfo == null)
                pointerInfo.PointerOptions = InjectedInputPointerOptions.New;
            else if (_preInputPenInfo.Pressure <= 0 && p <= 0)
                pointerInfo.PointerOptions |= InjectedInputPointerOptions.Update | InjectedInputPointerOptions.InRange;
            else if (_preInputPenInfo.Pressure <= 0 && p > 0)
                pointerInfo.PointerOptions |= InjectedInputPointerOptions.PointerDown |
                                              InjectedInputPointerOptions.InRange |
                                              InjectedInputPointerOptions.InContact;
            else if (_preInputPenInfo.Pressure > 0 && p > 0)
                pointerInfo.PointerOptions |= InjectedInputPointerOptions.Update | InjectedInputPointerOptions.InRange |
                                              InjectedInputPointerOptions.InContact;
            else if (_preInputPenInfo.Pressure > 0 && p <= 0)
                pointerInfo.PointerOptions |=
                    InjectedInputPointerOptions.PointerUp | InjectedInputPointerOptions.InRange;

            return pointerInfo;
        }

        private InjectedInputPenInfo ToInputPenInfo(int deltaX, int deltaY, int p)
        {
            var preX = _preInputPenInfo?.PointerInfo.PixelLocation.PositionX ?? 0;
            var preY = _preInputPenInfo?.PointerInfo.PixelLocation.PositionY ?? 0;
            var x = (int) (preX + deltaX * _viewScale);
            var y = (int) (preY + deltaY * _viewScale);
            var pointerInfo = ToInputPointerInfo(x, y, p);

            return new InjectedInputPenInfo
            {
                PenParameters = InjectedInputPenParameters.Pressure,
                PointerInfo = pointerInfo,
                Pressure = p / 512.0
            };
        }

        private InjectedInputPenInfo ToInputPenInfo2(NotePenPointer penPointer)
        {
            var screenPoint = new Point(penPointer.x * _viewScale, penPointer.y * _viewScale);
            var x = Math.Max(_validBounds.Left, Math.Min(screenPoint.X, _validBounds.Right)) - _validBounds.Left;
            var y = Math.Max(_validBounds.Top, Math.Min(screenPoint.Y, _validBounds.Bottom)) - _validBounds.Top;
            var pointerInfo = ToInputPointerInfo((int) x, (int) y, penPointer.p);

            return new InjectedInputPenInfo
            {
                PenParameters = InjectedInputPenParameters.Pressure,
                PointerInfo = pointerInfo,
                Pressure = penPointer.p / _maxPressure
            };
        }
    }
}