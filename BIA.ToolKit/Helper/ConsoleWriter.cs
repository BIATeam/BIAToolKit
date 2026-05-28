namespace BIA.ToolKit.Helper
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Domain.Model;
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

        // Step scoping — AsyncLocal so the value flows across await / Task.Run.
        private static readonly AsyncLocal<int?> currentStep = new();

        // Latest provider used to color stripes. Null when no step has been opened yet.
        private Func<int, MigrationStepStatus?> stepStatusResolver;

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
            public int? stepNumber;
        }

        public void AddMessageLine(string message, string color = null, bool refreshimediate = true)
        {
            var now = DateTime.Now;
            int? step = currentStep.Value;
            if (!refreshimediate)
            {
                messages.Add(new Message { message = message, color = color, timestamp = now, stepNumber = step });
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
                AddMsgLine(OutputRichTextBox, message, color, refreshimediate, IsDarkTheme, now, step, /*stripeColor*/ null);
                displayedMessages.Add(new Message { message = message, color = color, timestamp = now, stepNumber = step });
            }
        }

        public IDisposable BeginStep(int number, string label)
        {
            var previous = currentStep.Value;
            currentStep.Value = number;
            return new StepScope(previous);
        }

        private sealed class StepScope : IDisposable
        {
            private readonly int? previous;
            public StepScope(int? previous)
            {
                this.previous = previous;
            }
            public void Dispose()
            {
                currentStep.Value = previous;
            }
        }

        public void RefreshStepColors(Func<int, MigrationStepStatus?> stepStatusProvider)
        {
            // Re-render everything; the renderer reads the provider for each
            // step-tagged message to choose the stripe color.
            stepStatusResolver = stepStatusProvider;
            ReRenderMessages();
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
        /// Called when the user toggles between dark and light themes,
        /// or when a step transitions out of Running so stripe colors update.
        /// </summary>
        public void ReRenderMessages()
        {
            OutputRichTextBox.Document.Blocks.Clear();
            foreach (var msg in displayedMessages)
            {
                string stripeColor = ResolveStripeColor(msg.stepNumber);
                AddMsgLine(OutputRichTextBox, msg.message, msg.color, false, IsDarkTheme, msg.timestamp, msg.stepNumber, stripeColor);
            }
            OutputRichTextBox.ScrollToEnd();
        }

        // For step-tagged messages, picks the color of the leading stripe.
        // Uses the latest stepStatusResolver if available; otherwise defaults
        // to "running" blue (used during the initial render while the step is
        // still in progress).
        private string ResolveStripeColor(int? stepNumber)
        {
            if (stepNumber is null)
                return null;
            var status = stepStatusResolver?.Invoke(stepNumber.Value);
            return status switch
            {
                MigrationStepStatus.Done => "lightgreen",
                MigrationStepStatus.Warning => "orange",
                MigrationStepStatus.Failed => "red",
                _ => "lightblue", // Running or unknown
            };
        }

        public static void AddMsgLine(RichTextBox richTextBox, string message, string color, bool refreshimediate = true, bool isDarkTheme = true, DateTime? timestamp = null, int? stepNumber = null, string stripeColor = null)
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

            Brush stripeBrush = null;
            if (stepNumber is not null)
            {
                string effectiveStripe = stripeColor ?? "lightblue";
                var resolved = isDarkTheme ? MapColorForDarkTheme(effectiveStripe) : MapColorForLightTheme(effectiveStripe);
                var col = (Color)ColorConverter.ConvertFromString(resolved);
                stripeBrush = new SolidColorBrush(col);
            }

            AddMessageLine(richTextBox, message, brush, refreshimediate, timestamp ?? DateTime.Now, timestampBrush, stripeBrush);
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

        public static void AddMessageLine(RichTextBox richTextBox, string message, Brush brush, bool refreshimediate = true, DateTime? timestamp = null, Brush timestampBrush = null, Brush stripeBrush = null)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(
                DispatcherPriority.Normal,
            (ThreadStart)delegate
            {
                var ts = timestamp ?? DateTime.Now;
                var tsBrush = timestampBrush ?? Brushes.Gray;

                if (stripeBrush != null)
                {
                    // U+258E LEFT VERTICAL BAR — colored prefix marker.
                    var stripeRun = new Run("▎ ") { Foreground = stripeBrush, FontWeight = FontWeights.Bold };
                    AppendInline(richTextBox, stripeRun, addLineBreak: false);
                }

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
