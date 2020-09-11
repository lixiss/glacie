using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Glacie.CommandLine.UI
{
    // TODO: Support shell progress (windows), support dependent progresses
    // TODO: Implement progress smoothing

    public sealed class ProgressView : View,
        IIncrementalProgress<int>, IIncrementalProgress<long>,
        ICumulativeProgress<int>, ICumulativeProgress<long>
    {
        private const int DefaultVisibleAfterMilliseconds = 500; // 500;
        private const int DefaultFrameRate = 18; // 18;
        private const int DefaultRefreshRateMilliseconds = 1000 / DefaultFrameRate;

        public static long Frequency => Stopwatch.Frequency;
        public static bool IsHighResolution => Stopwatch.IsHighResolution;

        private long _value;
        private long _minimumValue;
        private long _maximumValue;
        private readonly Stopwatch _stopwatch;

        private string? _valueUnitName;
        private bool _valueUnitScale;
        private ProgressValueScaleType _valueUnitScaleType;
        private bool _showValueUnitSuffix;
        private bool _showRate;
        private bool _showElapsedTime;
        private bool _showRemainingTime;
        private bool _showTotalTime;
        private bool _showValue;
        private bool _showMaximumValue;

        private string? _title;
        private string? _message;

        // View
        private ProgressViewBarRenderer? _renderer;
        private string[]? _renderedResult;
        private int _visibleAfterMilliseconds;
        private int _refreshRateMilliseconds;
        private StringBuilder? _titleBuilder;
        private StringBuilder? _messageBuilder;

        public ProgressView()
        {
            _stopwatch = new Stopwatch();

            _visibleAfterMilliseconds = DefaultVisibleAfterMilliseconds;
            _refreshRateMilliseconds = DefaultRefreshRateMilliseconds;

            SetValueUnit(ProgressValueUnit.Iteration);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stopwatch.Stop();

                // Automatically remove progress from terminal.
                Terminal?.Remove(this);
            }

            base.Dispose(disposing);
        }

        public void Reset()
        {
            _value = 0;
            _minimumValue = 0;
            _maximumValue = 0;
            _stopwatch.Reset();
        }

        public void Restart()
        {
            Reset();
            Start();
        }

        public void Start()
        {
            _stopwatch.Start();
        }

        public void Stop()
        {
            _stopwatch.Stop();
        }

        #region Progress Meter

        public bool Indeterminate => _maximumValue <= 0;

        public TimeSpan Elapsed => _stopwatch.Elapsed;

        public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;

        public long ElapsedTicks => _stopwatch.ElapsedTicks;

        public bool IsRunning => _stopwatch.IsRunning;

        public long Value
        {
            get => _value;
            set => _value = value;
        }

        public long MinimumValue
        {
            get => _minimumValue;
            set => _minimumValue = value;
        }

        public long MaximumValue
        {
            get => _maximumValue;
            set => _maximumValue = value;
        }

        public string? Title
        {
            get => _title;
            set => _title = value;
        }

        public void SetTitle(string? value)
        {
            _title = value;
        }

        public string? Message
        {
            get => _message;
            set => _message = value;
        }

        public void SetMessage(string? value)
        {
            _message = value;
        }

        public string? ValueUnitName
        {
            get => _valueUnitName;
            set => _valueUnitName = value;
        }

        public bool ValueUnitScale
        {
            get => _valueUnitScale;
            set => _valueUnitScale = value;
        }

        public ProgressValueScaleType ValueUnitScaleType
        {
            get => _valueUnitScaleType;
            set => _valueUnitScaleType = value;
        }

        public bool ShowValueUnitSuffix
        {
            get => _showValueUnitSuffix;
            set => _showValueUnitSuffix = value;
        }

        public void SetValueUnit(string name, bool scale = false, ProgressValueScaleType scaleType = ProgressValueScaleType.Si, bool showValueUnitSuffix = false)
        {
            _valueUnitName = name;
            _valueUnitScale = scale;
            _valueUnitScaleType = scaleType;
            _showValueUnitSuffix = showValueUnitSuffix;
        }

        public void SetValueUnit(ProgressValueUnit valueUnit)
        {
            switch (valueUnit)
            {
                case ProgressValueUnit.Iteration:
                    SetValueUnit("it",
                        scale: false,
                        scaleType: ProgressValueScaleType.Si,
                        showValueUnitSuffix: false);
                    break;

                case ProgressValueUnit.Bytes:
                    SetValueUnit("B",
                        scale: true,
                        scaleType: ProgressValueScaleType.Iec,
                        showValueUnitSuffix: true);
                    break;

                default:
                    throw Error.Argument(nameof(valueUnit));
            }
        }

        public bool ShowRate
        {
            get => _showRate;
            set => _showRate = value;
        }

        public bool ShowElapsedTime
        {
            get => _showElapsedTime;
            set => _showElapsedTime = value;
        }

        public bool ShowRemainingTime
        {
            get => _showRemainingTime;
            set => _showRemainingTime = value;
        }

        public bool ShowTotalTime
        {
            get => _showTotalTime;
            set => _showTotalTime = value;
        }

        public bool ShowValue
        {
            get => _showValue;
            set => _showValue = value;
        }

        public bool ShowMaximumValue
        {
            get => _showMaximumValue;
            set => _showMaximumValue = value;
        }

        public void AddValue(int value)
        {
            Interlocked.Add(ref _value, value);
        }

        public void AddMaximumValue(int value)
        {
            Interlocked.Add(ref _maximumValue, value);
        }

        public void AddValue(long value)
        {
            // _value += value;
            Interlocked.Add(ref _value, value);
        }

        public void AddMaximumValue(long value)
        {
            // _maximumValue += value;
            Interlocked.Add(ref _maximumValue, value);
        }

        public void SetValue(int value)
        {
            _value = value;
        }

        public void SetMaximumValue(int value)
        {
            _maximumValue = value;
        }

        public void SetValue(long value)
        {
            _value = value;
        }

        public void SetMaximumValue(long value)
        {
            _maximumValue = value;
        }

        #endregion

        public int VisibleAfterMilliseconds
        {
            get => _visibleAfterMilliseconds;
            set => _visibleAfterMilliseconds = value;
        }

        public int RefreshAfterMilliseconds
        {
            get => _refreshRateMilliseconds;
            set => _refreshRateMilliseconds = value;
        }

        public override int Height
        {
            get
            {
                if (_renderedResult != null) return _renderedResult.Length;
                else return GetActualHeight();
            }
        }

        private int GetActualHeight()
        {
            return 1
                + (_title != null ? 1 : 0)
                + (_message != null ? 1 : 0);
        }

        protected override string[] OnRender()
        {
            if (_renderer == null) _renderer = new ProgressViewBarRenderer();

            var width = Width;

            var actualHeight = GetActualHeight();
            var result = _renderedResult;
            if (result == null || result.Length != actualHeight)
            {
                result = _renderedResult = new string[actualHeight];
            }

            var resultIndex = 0;
            if (_title != null)
            {
                result[resultIndex] = RenderTitle(width);
                resultIndex++;
            }

            result[resultIndex] = _renderer.Render(this, width);
            resultIndex++;

            if (_message != null)
            {
                result[resultIndex] = RenderMessage(width);
                // resultIndex++;
            }

            return result;
        }

        private string RenderTitle(int width)
        {
            return RenderLine(_title, width, ref _titleBuilder);
        }

        private string RenderMessage(int width)
        {
            return RenderLine(_message, width, ref _messageBuilder);
        }

        private string RenderLine(string? text, int width, ref StringBuilder? stringBuilder)
        {
            // TODO: Replace control characters text with space.
            // TODO: Cache result if text was not changed.

            text = StringUtilities.Excerpt(text, width);
            if (text == null || text.Length < width)
            {
                if (stringBuilder == null) stringBuilder = new StringBuilder(width);
                stringBuilder.Clear();
                stringBuilder.Append(text);
                var textLength = text != null ? text.Length : 0;
                stringBuilder.Append(' ', width - textLength);
                text = stringBuilder.ToString();
            }
            return text;
        }
    }
}
