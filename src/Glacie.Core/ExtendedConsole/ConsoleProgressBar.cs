using System;
using System.Diagnostics;

namespace Glacie.ExtendedConsole
{
    // TODO: Create "ConsoleEx" class which may be used instead of standard
    // console and it might spawn console progress bar.

    // TODO: ProgressBar?

    // TODO: Handle CTRL+C and restore cursor.

    // TODO: Use timer, to start progress...

    // TODO: This class should be internal, once _ConsoleEx will be implemented.

    // setRedrawFrequency(100); // 100 calls
    // maxSecondsBetweenRedraws(0.2); // 200ms
    // minSecondsBetweenRedraws(0.1); // 100ms

    // See TQDM: https://github.com/tqdm/tqdm

    // TODO: ascii / safe / full progress

    // 0x2588 - Full Block (safe?)
    // 0x2589 - Left 7/8 block
    // 0x258A - Left 3/4 block
    // 0x258B - Left 5/8 block
    // 0x258C - Left Half Block (safe?)
    // 0x258D - Left 3/8 block
    // 0x258E - Left 1/4 block
    // 0x258F - Left 1/8 block

    // Follow for NO_COLOR environment variable.

    // TODO: This works incorrectly when we writing on the last line of the buffer.

    public sealed class ConsoleProgressBar : ProgressBar
    {
        private const string SpinnerCharacters = @"|/-\";
        private const long UpdateRateMilliseconds = 16 * 4;

        private readonly Stopwatch _stopwatch;
        private long _lastUpdatedAt;

        private string? s_clearString;

        private int _cursorX;
        private int _cursorY;
        private bool _cursorVisible;

        private int _spinnerFrameNo;
        private bool _disposed;

        public ConsoleProgressBar()
        {
            _stopwatch = Stopwatch.StartNew();

            // TODO: Capture line, and then restore original line content.

            _cursorVisible = Console.CursorVisible;
            _cursorX = Console.CursorLeft;
            _cursorY = Console.CursorTop;

            Console.CursorVisible = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Clear();

                    Console.SetCursorPosition(_cursorX, _cursorY);
                    Console.CursorVisible = _cursorVisible;
                }

                _disposed = true;
            }
        }

        protected override void OnChanged()
        {
            var elapsedTime = _stopwatch.ElapsedMilliseconds;
            if ((elapsedTime - _lastUpdatedAt) >= UpdateRateMilliseconds)
            {
                _lastUpdatedAt = elapsedTime;

                Draw();
            }
        }

        private void Draw()
        {
            if (Indeterminate)
            {
                // TODO: Create nicely animated "spinner"

                var spinnerCharacters = SpinnerCharacters[_spinnerFrameNo % SpinnerCharacters.Length];
                _spinnerFrameNo++;

                // TODO: format message with ellipsis / to console width

                // TODO: instead clearline, prepare line buffer with text and clear inside, so
                // console will not flicker. But it should provide colors too.

                // TODO: Use Span<char>...

                ClearLine(_cursorY);
                Console.SetCursorPosition(0, _cursorY);
                Console.Write("[ {0} ] {1}", spinnerCharacters, Message ?? Title);
            }
            else
            {
                // TODO: format message with ellipsis / to console width

                // TODO: instead clearline, prepare line buffer with text and clear inside, so
                // console will not flicker.

                //   0.0%
                //  99.0%
                // 100.0%

                // TODO: define percentage format

                ClearLine(_cursorY);
                Console.SetCursorPosition(0, _cursorY);
                Console.Write("[{0,5:N1}%] {1}", Percentage, Message ?? Title);
            }
        }

        private void Clear()
        {
            ClearLine(_cursorY);
        }

        private void ClearLine(int cursorY)
        {
            Console.SetCursorPosition(0, cursorY);

            var clearString = GetClearString(Console.WindowWidth);
            Console.Write(clearString);
            Console.Write("\r");
        }

        private string GetClearString(int length)
        {
            var clearString = s_clearString;
            if (clearString == null || clearString.Length != length)
            {
                s_clearString = clearString = new string('\x20', length);
            }
            return clearString;
        }
    }
}
