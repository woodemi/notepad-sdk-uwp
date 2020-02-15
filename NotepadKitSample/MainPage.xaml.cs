using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Timers;
using Windows.UI.Input.Preview.Injection;
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
        private readonly InjectPenHelper _injectPenHelper = new InjectPenHelper(1.0);
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

        private readonly double _viewScale;

        private NotePenPointer? _lastNotePenPointer;

        private InjectedInputPenInfo _preInputPenInfo;

        public InjectPenHelper(double viewScale)
        {
            _viewScale = viewScale;
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

            var deltaX = _lastNotePenPointer.HasValue ? pointer.x - _lastNotePenPointer.Value.x : 0;
            var deltaY = _lastNotePenPointer.HasValue ? pointer.y - _lastNotePenPointer.Value.y : 0;

            var penInfo = ToInputPenInfo(deltaX, deltaY, pointer.p);
            Debug.WriteLine($"ToInputPenInfo {penInfo}");
            _inputInjector.InjectPenInput(penInfo);
            _preInputPenInfo = penInfo;

            _lastNotePenPointer = pointer;

            _inRangeTimer.Start();
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs args)
        {
            Debug.WriteLine($"OnTimedEvent {args.SignalTime}");
            _lastNotePenPointer = null;
            _preInputPenInfo = null;
        }

        private InjectedInputPenInfo ToInputPenInfo(int deltaX, int deltaY, int p)
        {
            var preX = _preInputPenInfo?.PointerInfo.PixelLocation.PositionX ?? 0;
            var preY = _preInputPenInfo?.PointerInfo.PixelLocation.PositionY ?? 0;
            var pointerInfo = new InjectedInputPointerInfo
            {
                PixelLocation = new InjectedInputPoint
                {
                    PositionX = (int) (preX + deltaX * _viewScale),
                    PositionY = (int) (preY + deltaY * _viewScale)
                }
            };
            var penInfo = new InjectedInputPenInfo
            {
                PenParameters = InjectedInputPenParameters.Pressure,
                PointerInfo = pointerInfo,
                Pressure = p / 512.0
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

            penInfo.PointerInfo = pointerInfo;
            return penInfo;
        }
    }
}