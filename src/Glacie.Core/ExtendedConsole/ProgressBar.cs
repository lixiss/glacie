using System;

namespace Glacie.ExtendedConsole
{
    public abstract class ProgressBar : IProgress, IDisposable
    {
        private long _value;
        private long _maxValue;
        private string? _title;
        private string? _message;

        protected ProgressBar()
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);

        protected abstract void OnChanged();

        public bool Indeterminate
        {
            get => _maxValue <= 0;
        }

        public double Percentage
        {
            get
            {
                var value = _value;
                var maxValue = _maxValue;
                if (value < 0) return 0;
                if (maxValue <= 0) return 0;
                return (double)value / maxValue * 100.0;
            }
        }

        public long Value
        {
            get => _value;
            set
            {
                _value = value;
                OnChanged();
            }
        }

        public long MaxValue
        {
            get => _maxValue;
            set
            {
                _maxValue = value;
                OnChanged();
            }
        }

        public string? Title
        {
            get => _title;
            set
            {
                _title = value;
                OnChanged();
            }
        }

        public string? Message
        {
            get => _message;
            set
            {
                _message = value;
                OnChanged();
            }
        }

        public void AddValue(long value)
        {
            _value += value;
            OnChanged();
        }

        public void AddMaxValue(long value)
        {
            _maxValue += value;
            OnChanged();
        }

        public void Report(long value, string message)
        {
            _value = value;
            _message = message;
            OnChanged();
        }
    }
}
