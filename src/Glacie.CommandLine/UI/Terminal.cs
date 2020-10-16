// Hacky feature
#define ENABLE_TERMINAL_ANIMATION_THREAD
#undef ENABLE_TERMINAL_ANIMATION_THREAD

using System;
using System.Collections.Generic;
using System.Threading;

using Glacie.CommandLine.IO;
using Glacie.CommandLine.Rendering;

using E = Glacie.Error;
using SC = System.Console;

namespace Glacie.CommandLine.UI
{
    // TODO: Under high load timer will not raise event... need use separate
    // thread instead...

    public sealed class Terminal : ITerminal, IDisposable
    {
        internal readonly object SyncRoot = new object();

        private readonly TerminalStreamWriter _out;
        private readonly TerminalStreamWriter _error;
        private readonly bool _isOutputRedirected;
        private readonly bool _isErrorRedirected;
        private readonly bool _isInputRedirected;

        private bool _disposed;
        private bool _disableViews;
        private bool _initalCursorVisible;

        private Timer _timer;
        private List<View> _views;
        private bool _paintScheduled;
        private int _clearY = 0;
        private int _clearHeight = 0;
        private int _clearWidth = 0;
        private string? _clearString;
        private string? _paintedNewLinesString;
        private List<string> _paintedLines = new List<string>();

#if ENABLE_TERMINAL_ANIMATION_THREAD
        private Thread _thread;
#endif

        public Terminal(bool? disableViews = null)
        {
            _disableViews = disableViews ?? SC.IsOutputRedirected;

            if (_disableViews)
            {
                _out = new TerminalStreamWriter(this, new StandardStreamWriter(SC.Out));
                _error = new TerminalStreamWriter(this, new StandardStreamWriter(SC.Error));
                _initalCursorVisible = true;
            }
            else
            {
                _out = new BufferedTerminalStreamWriter(this, new StandardStreamWriter(SC.Out));
                _error = new BufferedTerminalStreamWriter(this, new StandardStreamWriter(SC.Error));
                _initalCursorVisible = SC.CursorVisible;
            }

            _isOutputRedirected = SC.IsOutputRedirected;
            _isErrorRedirected = SC.IsErrorRedirected;
            _isInputRedirected = SC.IsInputRedirected;

            _views = new List<View>();

#if ENABLE_TERMINAL_ANIMATION_THREAD
            if (!_disableViews)
            {
                _thread = new Thread(AnimationThread);
                _thread.Start();
            }
#else
            _timer = _disableViews ? null! : new Timer(OnTimer);
#endif
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _timer?.Dispose();

                    _out.Dispose();
                    _error.Dispose();

                    if (!_disableViews)
                    {
                        SC.CursorVisible = _initalCursorVisible;
                    }
                }

                _disposed = true;
            }
        }

        #region IConsole

        public IStandardStreamWriter Out => _out;

        public bool IsOutputRedirected => _isOutputRedirected;

        public IStandardStreamWriter Error => _error;

        public bool IsErrorRedirected => _isErrorRedirected;

        public bool IsInputRedirected => _isInputRedirected;

        #endregion

        public void Add(View view)
        {
            lock (SyncRoot)
            {
                view.Attach(this);
                _views.Add(view);

                if (!_disableViews && _timer != null)
                {
                    // Schedule Timer
                    if (!_paintScheduled && view is ProgressView pv)
                    {
                        if (!_timer.Change(pv.VisibleAfterMilliseconds, Timeout.Infinite))
                        {
                            throw E.InvalidOperation("Can't change timer.");
                        }
                        SetPaintScheduled(true);
                    }
                }
            }
        }

        public void Remove(View view)
        {
            lock (SyncRoot)
            {
                if (_views.Remove(view))
                {
                    view.Detach();
                }

                // When no more views remains, force to out any buffered data.
                // This will help to see output immediately without waiting until
                // terminal disposed.
                if (_views.Count == 0)
                {
                    if (!_disableViews)
                    {
                        OnTimer(null);
                    }
                }
            }
        }

        internal bool PaintScheduled => _paintScheduled;

        private void SetPaintScheduled(bool value)
        {
            if (_paintScheduled != value)
            {
                SC.CursorVisible = !value;
            }
            _paintScheduled = value;
        }

#if ENABLE_TERMINAL_ANIMATION_THREAD
        private void AnimationThread(object? state)
        {
            while (!_disposed)
            {
                OnTimer(null);
                Thread.Sleep(1000 / 18);
            }
        }
#endif

        private void OnTimer(object? state)
        {
            DebugCheck.That(!_disableViews);

            lock (SyncRoot)
            {
                OnPaint(out var nextPaintTimeout);

                var timer = _timer;
                if (timer != null)
                {
                    if (!timer.Change(nextPaintTimeout, Timeout.Infinite))
                    {
                        throw E.InvalidOperation("Can't change timer.");
                    }
                    SetPaintScheduled(nextPaintTimeout != Timeout.Infinite);
                }
            }
        }

        private void OnPaint(out int nextPaintTimeout)
        {
            // TODO: Sometimes when lot of output messages, it draws buggy... why?

            var width = SC.BufferWidth;

            if (_out.ShouldFlushBuffer || _error.ShouldFlushBuffer)
            {
                ClearPaintedViews(width);
                _out.FlushBuffer();
                _error.FlushBuffer();
            }

            // TODO: Use SC.GetCursorPosition on NET5
            //var initialX = SC.CursorLeft;
            //var initialY = SC.CursorTop;

            nextPaintTimeout = int.MaxValue;

            var totalHeigh = 0;
            _paintedLines.Clear();
            for (var i = 0; i < _views.Count; i++)
            {
                var view = _views[i];
                if (!view.Visible) continue;

                if (view is ProgressView pv)
                {
                    nextPaintTimeout = Math.Min(nextPaintTimeout, pv.RefreshAfterMilliseconds);
                }

                view.Width = width;

                var content = view.Render();
                var viewHeight = content.Length; // view.Height;
                totalHeigh += viewHeight;

                _paintedLines.AddRange(content);
            }

            SC.Out.Write(GetNewLineString(_paintedLines.Count));

            var newClearY = SC.CursorTop - totalHeigh;
            if (newClearY < 0) newClearY = 0;

            var y = newClearY;
            for (var i = 0; i < _paintedLines.Count; i++)
            {
                SC.SetCursorPosition(0, y + i);
                SC.Out.Write(_paintedLines[i]);
            }
            _paintedLines.Clear();

            var newClearHeight = totalHeigh;
            ClearPaintedPortion(newClearY, width, newClearHeight, 0);

            SC.SetCursorPosition(0, y);

            if (nextPaintTimeout == int.MaxValue)
            {
                nextPaintTimeout = Timeout.Infinite;
            }

            // SC.SetCursorPosition(initialX, initialY);
        }

        private void ClearPaintedPortion(int y, int width, int height, int ydiff)
        {
            // y/height is currently occupied space
            // while in _clearY and _clearHeight is previously occupied space

            if (height < _clearHeight)
            {
                var cy = y + height + ydiff;
                var ch = _clearHeight - height;

                PaintClear(cy, width, ch);
            }

            _clearY = y;
            _clearWidth = width;
            _clearHeight = height;
        }

        internal void ClearPaintedViews()
        {
            if (_clearHeight > 0)
            {
                var clearY = _clearY;
                ClearPaintedViews(_clearWidth);
            }
        }

        private void ClearPaintedViews(int width)
        {
            if (_clearHeight > 0)
            {
                PaintClear(_clearY, width, _clearHeight);
                _clearHeight = 0;
                SC.SetCursorPosition(0, _clearY);
            }
        }

        private void PaintClear(int y, int width, int height)
        {
            if (height <= 0) return;

            var clearString = GetClearString(width);
            SC.SetCursorPosition(0, y);
            for (var i = 0; i < height; i++)
            {
                SC.Write(clearString);
            }
        }

        private string GetNewLineString(int count)
        {
            var paintedNewLinesString = _paintedNewLinesString;
            if (paintedNewLinesString == null || paintedNewLinesString.Length != count)
            {
                return _paintedNewLinesString = new string('\n', count);
            }
            return paintedNewLinesString;
        }

        private string GetClearString(int length)
        {
            var clearString = _clearString;
            if (clearString == null || clearString.Length != length)
            {
                _clearString = clearString = new string('\x20', length);
            }
            return clearString;
        }
    }
}
