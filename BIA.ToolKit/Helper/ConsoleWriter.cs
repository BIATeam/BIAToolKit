namespace BIA.ToolKit.Helper
{
    using BIA.ToolKit.Application.Helper;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Threading;

    public class ConsoleWriter : IConsoleWriter
    {
        TextBlock OutputText;
        ScrollViewer OutputTextViewer;
        Window WindowOwner;
        IDialogService dialogService;
        List<Message> messages = [];
        readonly List<Message> displayedMessages = [];

        public bool IsDarkTheme { get; set; } = true;

        public ConsoleWriter()
        {

        }

        public void InitOutput(TextBlock _outputText, ScrollViewer _outputTextViewer, Window _windowOwner, IDialogService _dialogService)
        {
            OutputText = _outputText;
            OutputTextViewer = _outputTextViewer;
            WindowOwner = _windowOwner;
            dialogService = _dialogService;
        }

        public struct Message
        {
            public string message;
            public string color;
        }

        public void AddMessageLine(string message, string color = null, bool refreshimediate = true)
        {
            if (!refreshimediate)
            {
                messages.Add(new Message { message = message, color = color });
            }
            else
            {
                if (messages.Count > 0)
                {
                    var run = new Run(@"[🔍 OPEN LOG DETAIL]" + "\r\n")
                    {
                        Foreground = IsDarkTheme ? Brushes.YellowGreen : Brushes.DarkOliveGreen,
                        Cursor = Cursors.Hand,
                        TextDecorations = TextDecorations.Underline
                    };
                    run.MouseDown += new MouseButtonEventHandler(OpenDetail);
                    run.DataContext = messages;
                    OutputText.Inlines.Add(run);

                    messages = [];
                }
                //foreach (Message msg in messages)
                //{
                //    _AddMsgLine(msg.message, msg.color, refreshimediate);
                //}
                //messages.Clear();
                AddMsgLine(OutputText, OutputTextViewer, message, color, refreshimediate, IsDarkTheme);
                displayedMessages.Add(new Message { message = message, color = color });
            }
        }

        private void OpenDetail(object sender, MouseButtonEventArgs e)
        {
            var rawMessages = (List<Message>)((Run)sender).DataContext;
            var logMessages = rawMessages.Select(m => new LogMessage { Text = m.message, Color = m.color }).ToList();
            dialogService?.ShowLogDetail(logMessages);
        }

        public void Clear()
        {
            displayedMessages.Clear();
            OutputText.Inlines.Clear();
        }

        public void CopyToClipboard()
        {
            Clipboard.SetText(string.Join(Environment.NewLine, displayedMessages.Select(m => m.message)));
        }

        /// <summary>
        /// Re-renders all displayed messages with current theme colors.
        /// Called when the user toggles between dark and light themes.
        /// </summary>
        public void ReRenderMessages()
        {
            OutputText.Inlines.Clear();
            foreach (var msg in displayedMessages)
            {
                AddMsgLine(OutputText, OutputTextViewer, msg.message, msg.color, false, IsDarkTheme);
            }
            OutputTextViewer.ScrollToEnd();
        }

        public static void AddMsgLine(TextBlock OutputText, ScrollViewer OutputTextViewer, string message, string color, bool refreshimediate = true, bool isDarkTheme = true)
        {
            Brush brush;
            if (string.IsNullOrEmpty(color))
            {
                brush = isDarkTheme ? Brushes.White : Brushes.Black;
            }
            else
            {
                var resolvedColor = isDarkTheme ? color : MapColorForLightTheme(color);
                var col = (Color)ColorConverter.ConvertFromString(resolvedColor);
                brush = new SolidColorBrush(col);
            }
            AddMessageLine(OutputText, OutputTextViewer, message, brush, refreshimediate);
        }

        private static string MapColorForLightTheme(string color)
        {
            return color.ToLowerInvariant() switch
            {
                "green" => "DarkGreen",
                "yellow" => "#795508",  // Dark amber (VS Code warning style)
                "yellowgreen" => "#556B2F", // DarkOliveGreen
                "red" => "DarkRed",
                "orange" => "#CC7000",
                "blue" => "#1565C0", // Blue 800
                _ => color
            };
        }

        public static void AddMessageLine(TextBlock OutputText, ScrollViewer OutputTextViewer, string message, Brush brush, bool refreshimediate = true)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(
                DispatcherPriority.Normal,
            (ThreadStart)delegate
            {

                var run = new Run(message + "\r\n")
                {
                    Foreground = brush
                };
                OutputText.Inlines.Add(run);
                if (refreshimediate)
                {
                    if (OutputTextViewer.VerticalOffset == OutputTextViewer.ScrollableHeight)
                    {
                        OutputTextViewer.ScrollToEnd();
                    }
                    System.Windows.Forms.Application.DoEvents();
                }
            });
        }
    }
}
