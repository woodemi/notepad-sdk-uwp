using System;
using System.Diagnostics;
using System.Timers;
using Windows.UI.Input.Preview.Injection;

namespace NotepadKitSample
{
    public class InjectPenHelper : IDisposable
    {
        private readonly InputInjector _inputInjector = InputInjector.TryCreate();

        private readonly Timer _inRangeTimer;

        private InjectedInputPenInfo _preInputPenInfo;

        public InjectPenHelper()
        {
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

        public void InjectInput(int x, int y, double p)
        {
            _inRangeTimer.Stop();

            var penInfo = new InjectedInputPenInfo
            {
                PenParameters = InjectedInputPenParameters.Pressure,
                PointerInfo = ToInputPointerInfo((int) x, (int) y, p),
                Pressure = p,
            };
            Debug.WriteLine($"ToInputPenInfo {penInfo}");

            _inputInjector.InjectPenInput(penInfo);
            _preInputPenInfo = penInfo;

            _inRangeTimer.Start();
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs args)
        {
            Debug.WriteLine($"OnTimedEvent {args.SignalTime}");
            _preInputPenInfo = null;
        }

        private InjectedInputPointerInfo ToInputPointerInfo(int x, int y, double p)
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
    }
}