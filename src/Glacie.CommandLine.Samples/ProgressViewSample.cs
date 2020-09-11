using System.Collections.Generic;
using System.Threading;

using Glacie.CommandLine.UI;

namespace Glacie.CommandLine.Samples
{
    internal sealed class ProgressViewSample
    {
        private readonly Terminal _terminal;
        private List<ProgressView> _progressViews;

        public ProgressViewSample(Terminal terminal)
        {
            _terminal = terminal;
            _progressViews = new List<ProgressView>();
        }

        private ProgressView StartProgress(string title)
        {
            var progressView = new ProgressView();
            progressView.Title = title;
            _terminal.Add(progressView);
            progressView.Start();

            _progressViews.Add(progressView);

            return progressView;
        }

        public void Run()
        {
            // TODO: Make better samples.

            using var pInd1 = StartProgress("Indeterminate (Default)");

            using var pInd2 = StartProgress("Indeterminate: Full, Unit = Custom");
            pInd2.SetValueUnit("files");
            pInd2.ShowValue = true;
            pInd2.ShowMaximumValue = true;
            pInd2.ShowRate = true;
            pInd2.ShowElapsedTime = true;
            pInd2.ShowRemainingTime = true;
            pInd2.ShowTotalTime = true;

            using var pInd3 = StartProgress("Indeterminate: Unit = Bytes");
            pInd3.SetValueUnit(ProgressValueUnit.Bytes);
            pInd3.ShowRate = true;
            pInd3.ShowValue = true;
            pInd3.ShowMaximumValue = true;

            using var pDet1 = StartProgress("Determinate (Default)");
            pDet1.MaximumValue = 1000;

            using var pDet2 = StartProgress("Determinate: Full, Unit = Custom Scaled");
            pDet2.SetValueUnit("loc", true);
            pDet2.MaximumValue = 5000;
            pDet2.ShowValue = true;
            pDet2.ShowMaximumValue = true;
            pDet2.ShowRate = true;
            pDet2.ShowElapsedTime = true;
            pDet2.ShowRemainingTime = true;
            pDet2.ShowTotalTime = true;

            using var pDet3 = StartProgress("Determinate: Full, Unit = Bytes");
            pDet3.SetValueUnit(ProgressValueUnit.Bytes);
            pDet3.MaximumValue = 10000;
            pDet3.ShowValue = true;
            pDet3.ShowMaximumValue = true;
            pDet3.ShowRate = true;
            pDet3.ShowElapsedTime = true;
            pDet3.ShowRemainingTime = true;
            pDet3.ShowTotalTime = true;

            _terminal.Out.Write("Press CTRL+C to quit...\n");
            for (var i = 0; ; i++)
            {
                Thread.Sleep(1);

                pDet2.Message = $"Item: {i}";

                foreach (var pv in _progressViews)
                {
                    pv.AddValue(1);
                }

                if (i % 100 == 0)
                {
                    _terminal.Out.Write($"Item processed: {i}\n");
                }
            }
        }
    }
}
