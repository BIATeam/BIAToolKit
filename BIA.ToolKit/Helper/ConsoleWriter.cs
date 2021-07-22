namespace BIA.ToolKit.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Media;
    using System.Windows.Threading;

    public class ConsoleWriter
    {
        TextBlock OutputText;
        ScrollViewer OutputTextViewer;

        public ConsoleWriter(TextBlock _outputText, ScrollViewer _outputTextViewer)
        {
            OutputText = _outputText;
            OutputTextViewer = _outputTextViewer;
        }

        public void AddMessageLine(string message, Brush brush)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(
                DispatcherPriority.Normal,
            (ThreadStart)delegate
            {

                Run run = new Run(message + "\r\n");
                run.Foreground = brush;
                OutputText.Inlines.Add(run);
                if (OutputTextViewer.VerticalOffset == OutputTextViewer.ScrollableHeight)
                {
                    OutputTextViewer.ScrollToEnd();
                }
                System.Windows.Forms.Application.DoEvents();
            });
        }
    }
}
