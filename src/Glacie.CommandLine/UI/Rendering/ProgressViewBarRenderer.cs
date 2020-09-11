using System;
using System.Text;

namespace Glacie.CommandLine.UI
{
    internal sealed class ProgressViewBarRenderer
    {
        private readonly StringBuilder _leftBar = new StringBuilder();
        private readonly StringBuilder _centerBar = new StringBuilder();
        private readonly StringBuilder _rightBar = new StringBuilder();
        private readonly StringBuilder _result = new StringBuilder();
        private int _frameNo;

        public ProgressViewBarRenderer()
        {
        }

        private int GetNextFrameNo() => _frameNo++;

        public string Render(ProgressView progressView, int width)
        {
            _leftBar.Clear();
            _centerBar.Clear();
            _rightBar.Clear();
            _result.Clear();

            var frameNo = GetNextFrameNo();

            bool indeterminate = progressView.Indeterminate;
            long value = progressView.Value;
            long minimumValue = progressView.MinimumValue;
            long maximumValue = progressView.MaximumValue;
            string? unitName = progressView.ValueUnitName;
            bool unitScale = progressView.ValueUnitScale;
            ProgressValueScaleType unitScaleType = progressView.ValueUnitScaleType;
            bool showValueUnitSuffix = progressView.ShowValueUnitSuffix;
            var elapsed = progressView.Elapsed;
            var showRate = progressView.ShowRate;
            var showElapsedTime = progressView.ShowElapsedTime;
            var showRemainingTime = progressView.ShowRemainingTime;
            var showTotalTime = progressView.ShowTotalTime;
            var showValue = progressView.ShowValue;
            var showMaximumValue = progressView.ShowMaximumValue;

            // calculated
            var fraction = GetFraction(value, minimumValue, maximumValue);
            double rate;
            TimeSpan totalTime;
            TimeSpan remainingTime;

            if (fraction > 0)
            {
                var totalTicks = elapsed.Ticks / fraction;
                if (double.IsFinite(totalTicks))
                {
                    totalTime = elapsed / fraction;
                    remainingTime = totalTime - elapsed;
                    showTotalTime &= true;
                    showRemainingTime &= true;
                }
                else
                {
                    totalTime = TimeSpan.MaxValue;
                    remainingTime = TimeSpan.MaxValue;
                    showTotalTime &= true;
                    showRemainingTime &= true;
                }
            }
            else
            {
                totalTime = default;
                remainingTime = default;
                showTotalTime = false;
                showRemainingTime = false;
            }

            var totalSeconds = elapsed.TotalSeconds;
            if (totalSeconds > 0)
            {
                rate = value / totalSeconds;
                showRate &= true;
            }
            else
            {
                rate = 0;
                showRate = false;
            }

            string rateString = unitScaleType switch
            {
                ProgressValueScaleType.Si => UnitFormatter.FormatSi(rate, unitName),
                ProgressValueScaleType.Iec => UnitFormatter.FormatIecBytes(rate, unitName),
                _ => throw Error.Unreachable(),
            };

            // LeftBar
            const string spinnerChars = @"/-\|";
            var leftBar = indeterminate switch
            {
                true => _leftBar.AppendFormat("| {0} |", spinnerChars[frameNo % spinnerChars.Length]),
                false => AppendPercentage(_leftBar, fraction),
            };

            string valueString;
            if (unitScale)
            {
                valueString = unitScaleType switch
                {
                    ProgressValueScaleType.Si => UnitFormatter.FormatSi(value, showValueUnitSuffix ? unitName : null),
                    ProgressValueScaleType.Iec => UnitFormatter.FormatIecBytes(value, showValueUnitSuffix ? unitName : null),
                    _ => throw Error.Unreachable(),
                };
            }
            else
            {
                valueString = value.ToString();
            }

            string maximumValueString;
            if (unitScale)
            {
                maximumValueString = unitScaleType switch
                {
                    ProgressValueScaleType.Si => UnitFormatter.FormatSi(maximumValue, showValueUnitSuffix ? unitName : null),
                    ProgressValueScaleType.Iec => UnitFormatter.FormatIecBytes(maximumValue, showValueUnitSuffix ? unitName : null),
                    _ => throw Error.Unreachable(),
                };
            }
            else
            {
                maximumValueString = maximumValue.ToString();
            }

            var rightBar = _rightBar;
            rightBar.Append('|');
            if (showValue)
            {
                rightBar.AppendFormat(" {0}", valueString);
            }
            if (showMaximumValue && !indeterminate)
            {
                if (showValue) rightBar.Append('/');

                rightBar.AppendFormat("{0}", maximumValueString);
            }

            if (showElapsedTime || showRemainingTime || showTotalTime || showRate)
            {
                rightBar.Append(" [");
                if (showElapsedTime)
                {
                    rightBar.AppendFormat("{0}", FormatTimeSpan(elapsed));
                }
                if (showRemainingTime)
                {
                    if (showElapsedTime)
                    {
                        rightBar.Append('<');
                    }
                    rightBar.AppendFormat("{0}", FormatTimeSpan(remainingTime));
                }
                if (showTotalTime)
                {
                    if (showElapsedTime || showRemainingTime)
                    {
                        rightBar.Append('/');
                    }
                    rightBar.AppendFormat("{0}", FormatTimeSpan(totalTime));
                }
                if (showRate)
                {
                    if (showElapsedTime || showRemainingTime || showTotalTime)
                    {
                        rightBar.Append(", ");
                    }
                    rightBar.AppendFormat("{0}/s", rateString);
                }
                rightBar.Append(']');
            }

            // TODO: try fit as much as possible, but keep right bar (and remove left bar)
            // and then keep right bar remove left bar, and then eventually just show
            // one-char single spinner.

            var centerBarWidth = width - leftBar.Length - rightBar.Length;
            string centerBar;
            if (centerBarWidth <= 0)
            {
                centerBar = "";
            }
            else
            {
                if (indeterminate)
                {
                    centerBar = IndeterminateBarFormatter.Format(centerBarWidth, frameNo);
                }
                else
                {
                    centerBar = BarFormatter.Format(fraction,
                        centerBarWidth,
                        BarFormatter.UtfWindows);
                }
            }

            return _result.AppendFormat("{0}{1}{2}", leftBar, centerBar, rightBar).ToString();
        }

        private StringBuilder AppendPercentage(StringBuilder builder, double fraction)
        {
            // TODO: Make configurable precision
            int percentagePrecision = 0;

            if (fraction >= 1)
            {
                // TODO: Make configurable hiding percentage fraction after 100%.
                percentagePrecision = 0;
            }

            // TODO: Make configurable spacing (e.g. 0,2 is enough to show 99%, but not 100%).
            // alternatively would be nice to have multi-character spinner, so it can be adapted
            // to width.
            var formatString = percentagePrecision switch
            {
                0 => "|{0,2:F0}%|",
                1 => "|{0,4:F1}%|",
                _ => "|{0,5:F2}%|",
            };
            builder.AppendFormat(formatString, fraction * 100.0);
            return builder;
        }

        private static double GetFraction(long value, long minValue, long maxValue)
        {
            if (minValue == 0)
            {
                if (maxValue == 0) return 0;

                if (value < 0) return 0;
                if (value > maxValue) return 1;

                return (double)value / maxValue;
            }
            else
            {
                if (maxValue < minValue) return 0;
                if (maxValue == minValue) return 0;

                if (value < minValue) return 0;
                if (value > maxValue) return 1.0;

                ulong umax = (ulong)maxValue;
                ulong uval = (ulong)value;

                throw Error.NotImplemented();
            }
        }

        private static string FormatTimeSpan(TimeSpan value)
        {
            var totalHours = value.TotalHours;
            if (totalHours >= 1)
            {
                if (totalHours >= 24)
                {
                    return string.Format(@"{0:d\.hh\:mm\:ss}", value);
                }
                else
                {
                    return string.Format(@"{0:h\:mm\:ss}", value);
                }
            }
            else
            {
                return string.Format(@"{0:mm\:ss}", value);
            }
        }
    }
}
