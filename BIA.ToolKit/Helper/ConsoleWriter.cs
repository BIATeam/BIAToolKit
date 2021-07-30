namespace BIA.ToolKit.Helper
{
    using BIA.ToolKit.Application.Helper;
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

    public class ConsoleWriter : IConsoleWriter
    {
        TextBlock OutputText;
        ScrollViewer OutputTextViewer;

        public ConsoleWriter()
        {
        }

        public void InitOutput(TextBlock _outputText, ScrollViewer _outputTextViewer)
        {
            OutputText = _outputText;
            OutputTextViewer = _outputTextViewer;
        }

        public void AddMessageLine(string message, string color = null)
        {
            Brush brush = null;
            if (string.IsNullOrEmpty(color))
            {
                brush = Brushes.White;
            }
            else
            {
                Color col = (Color)ColorConverter.ConvertFromString(color);
                brush = new SolidColorBrush(col);
            }
            AddMessageLine(message, brush);
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
