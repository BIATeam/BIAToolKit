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
                AddMsgLine(OutputRichTextBox, message, color, refreshimediate, IsDarkTheme);
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
            OutputRichTextBox.Document.Blocks.Clear();
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
            OutputRichTextBox.Document.Blocks.Clear();
            foreach (var msg in displayedMessages)
            {
                AddMsgLine(OutputRichTextBox, msg.message, msg.color, false, IsDarkTheme);
            }
            OutputRichTextBox.ScrollToEnd();
        }

        public static void AddMsgLine(RichTextBox richTextBox, string message, string color, bool refreshimediate = true, bool isDarkTheme = true)
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
            AddMessageLine(richTextBox, message, brush, refreshimediate);
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

        public static void AddMessageLine(RichTextBox richTextBox, string message, Brush brush, bool refreshimediate = true)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(
                DispatcherPriority.Normal,
            (ThreadStart)delegate
            {
                var run = new Run(message) { Foreground = brush };
                AppendInline(richTextBox, run);

                if (refreshimediate)
                {
                    richTextBox.ScrollToEnd();
                }
            });
        }

        private static void AppendInline(RichTextBox richTextBox, Inline inline)
        {
            var doc = richTextBox.Document;
            if (doc.Blocks.LastBlock is not Paragraph paragraph)
            {
                paragraph = new Paragraph { Margin = new Thickness(0), LineHeight = 1 };
                doc.Blocks.Add(paragraph);
            }
            paragraph.Inlines.Add(inline);
            paragraph.Inlines.Add(new LineBreak());
        }
    }
}
