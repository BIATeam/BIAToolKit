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
        RichTextBox OutputRichTextBox;
        Window WindowOwner;
        IDialogService dialogService;
        List<Message> messages = [];
        readonly List<Message> displayedMessages = [];

        public bool IsDarkTheme { get; set; } = true;

        public ConsoleWriter()
        {

        }

        public void InitOutput(RichTextBox _outputRichTextBox, Window _windowOwner, IDialogService _dialogService)
        {
            OutputRichTextBox = _outputRichTextBox;
            WindowOwner = _windowOwner;
            dialogService = _dialogService;
        }

        public struct Message
        {
            public string message;
            public string color;
            public DateTime timestamp;
        }

        public void AddMessageLine(string message, string color = null, bool refreshimediate = true)
        {
            var now = DateTime.Now;
            if (!refreshimediate)
            {
                messages.Add(new Message { message = message, color = color, timestamp = now });
            }
            else
            {
                if (messages.Count > 0)
                {
                    var run = new Run(@"[🔍 OPEN LOG DETAIL]")
                    {
                        Foreground = IsDarkTheme ? Brushes.YellowGreen : Brushes.DarkOliveGreen,
                        Cursor = Cursors.Hand,
                        TextDecorations = TextDecorations.Underline
                    };
                    run.MouseDown += new MouseButtonEventHandler(OpenDetail);
                    run.DataContext = messages;
                    AppendInline(OutputRichTextBox, run);

                    messages = [];
                }
                AddMsgLine(OutputRichTextBox, message, color, refreshimediate, IsDarkTheme, now);
                displayedMessages.Add(new Message { message = message, color = color, timestamp = now });
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
            OutputRichTextBox.Document.Blocks.Clear();
        }

        public void CopyToClipboard()
        {
            Clipboard.SetText(string.Join(Environment.NewLine, displayedMessages.Select(m => $"[{m.timestamp:HH:mm:ss}]  {m.message}")));
        }

        /// <summary>
        /// Re-renders all displayed messages with current theme colors.
        /// Called when the user toggles between dark and light themes.
        /// </summary>
        public void ReRenderMessages()
        {
            OutputRichTextBox.Document.Blocks.Clear();
            foreach (var msg in displayedMessages)
            {
                AddMsgLine(OutputRichTextBox, msg.message, msg.color, false, IsDarkTheme, msg.timestamp);
            }
            OutputRichTextBox.ScrollToEnd();
        }

        public static void AddMsgLine(RichTextBox richTextBox, string message, string color, bool refreshimediate = true, bool isDarkTheme = true, DateTime? timestamp = null)
        {
            Brush brush;
            if (string.IsNullOrEmpty(color))
            {
                brush = isDarkTheme ? Brushes.White : Brushes.Black;
            }
            else
            {
                var resolvedColor = isDarkTheme ? MapColorForDarkTheme(color) : MapColorForLightTheme(color);
                var col = (Color)ColorConverter.ConvertFromString(resolvedColor);
                brush = new SolidColorBrush(col);
            }
            var timestampBrush = isDarkTheme ? new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80)) : new SolidColorBrush(Color.FromRgb(0x90, 0x90, 0x90));
            AddMessageLine(richTextBox, message, brush, refreshimediate, timestamp ?? DateTime.Now, timestampBrush);
        }

        // Dark theme palette — bright / saturated colors that read well on a dark background.
        // Tune this list independently of the light palette.
        private static string MapColorForDarkTheme(string color)
        {
            return color.ToLowerInvariant() switch
            {
                "green" => "#4CAF50",       // Green 500
                "lightgreen" => "#81C784",  // Green 300
                "lime" => "#CDDC39",        // Lime 500
                "yellow" => "#FFEB3B",      // Yellow 500
                "yellowgreen" => "YellowGreen",
                "red" => "#EF5350",         // Red 400
                "orange" => "#FFA726",      // Orange 400
                "blue" => "#42A5F5",        // Blue 400
                "lightblue" => "#4FC3F7",   // Light Blue 300
                "pink" => "#F06292",        // Pink 300
                "purple" => "#BA68C8",      // Purple 300
                "white" => "White",
                "gray" => "#B0B0B0",
                "darkgray" => "#9E9E9E",
                _ => color
            };
        }

        // Light theme palette — darker / muted variants for readability on a white background.
        // Tune this list independently of the dark palette.
        private static string MapColorForLightTheme(string color)
        {
            return color.ToLowerInvariant() switch
            {
                "green" => "DarkGreen",
                "lightgreen" => "#2E7D32",  // Green 800
                "lime" => "#558B2F",        // Light Green 800
                "yellow" => "#795508",      // Dark amber (VS Code warning style)
                "yellowgreen" => "#556B2F", // DarkOliveGreen
                "red" => "DarkRed",
                "orange" => "#CC7000",
                "blue" => "#1565C0",        // Blue 800
                "lightblue" => "#0277BD",   // Light Blue 800
                "pink" => "#C2185B",        // Pink 700
                "purple" => "#6A1B9A",      // Purple 800
                "white" => "Black",
                "gray" => "#616161",
                "darkgray" => "#424242",
                _ => color
            };
        }

        public static void AddMessageLine(RichTextBox richTextBox, string message, Brush brush, bool refreshimediate = true, DateTime? timestamp = null, Brush timestampBrush = null)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(
                DispatcherPriority.Normal,
            (ThreadStart)delegate
            {
                var ts = timestamp ?? DateTime.Now;
                var tsBrush = timestampBrush ?? Brushes.Gray;
                var timestampRun = new Run($"[{ts:HH:mm:ss}]  ") { Foreground = tsBrush };
                AppendInline(richTextBox, timestampRun, addLineBreak: false);

                var run = new Run(message) { Foreground = brush };
                AppendInline(richTextBox, run);

                if (refreshimediate)
                {
                    richTextBox.ScrollToEnd();
                }
            });
        }

        private static void AppendInline(RichTextBox richTextBox, Inline inline, bool addLineBreak = true)
        {
            var doc = richTextBox.Document;
            if (doc.Blocks.LastBlock is not Paragraph paragraph)
            {
                paragraph = new Paragraph { Margin = new Thickness(0), LineHeight = 1 };
                doc.Blocks.Add(paragraph);
            }
            paragraph.Inlines.Add(inline);
            if (addLineBreak)
            {
                paragraph.Inlines.Add(new LineBreak());
            }
        }
    }
}
