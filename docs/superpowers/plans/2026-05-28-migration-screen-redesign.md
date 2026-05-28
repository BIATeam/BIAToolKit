# Migration Screen Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild the ModifyProjectUC > Project tab as side-by-side Original/Target panels with a horizontal status-aware MigrationStepperUC and per-step color-coded Output stripes, shipped as V2.14.0.

**Architecture:** Bottom-up build — start with the `MigrationStepStatus` enum and `MigrationStep` VM (data foundation), add status-to-visual converters, extend `IConsoleWriter` with a scoped `BeginStep` API that uses `AsyncLocal` to tag emitted messages, render colored vertical stripes (U+258E) prefixing tagged lines in the existing global `RichTextBox`, build the `MigrationStepperUC` (visual cells over `UniformGrid Columns="6"`), wire the six migration step methods in `ModifyProjectViewModel` (initialize `Steps`, wrap each `MigrateXxxRunAsync` with a `using` scope and status transitions, cascade reset of downstream steps), then rewrite the `ModifyProjectUC.xaml` layout (single-column ScrollViewer holding side-by-side panels + Migrate banner + stepper row, no right sidebar). The inner `VersionAndOptionUserControl` (498 dense lines) stays untouched.

**Tech Stack:** WPF .NET 10 · MaterialDesignThemes 5.3 · CommunityToolkit.Mvvm 8.3 (`ObservableObject`, `[ObservableProperty]`, `IAsyncRelayCommand`) · CommunityToolkit.Mvvm.Messaging · existing `IConsoleWriter` singleton.

**Reference spec:** [`docs/superpowers/specs/2026-05-28-migration-screen-redesign-design.md`](../specs/2026-05-28-migration-screen-redesign-design.md)

**Branch:** `feature/fixes-V2.14.0` (already created from main, CRUD BIA Front fix already cherry-picked at `1fb3729`)

**Testing note:** This project has no unit test infrastructure for ViewModels / Helpers (only template generation tests at `BIA.ToolKit.Test.Templates`). Verification per task = `dotnet build` (compile gate) plus, at the end, the manual UI test plan from the spec. We do not introduce a new test project for this scope.

---

### Task 1: Add `MigrationStepStatus` enum

**Files:**
- Create: `BIA.ToolKit.Domain/Model/MigrationStepStatus.cs`

- [ ] **Step 1: Create the enum file**

```csharp
namespace BIA.ToolKit.Domain.Model
{
    /// <summary>
    /// Lifecycle states of a single step in the project migration workflow.
    /// Used both by <c>MigrationStep</c> (ViewModel) and by the Output stripe
    /// renderer to colorize lines emitted during the step's execution.
    /// </summary>
    public enum MigrationStepStatus
    {
        Pending,
        Running,
        Done,
        Warning,
        Failed,
    }
}
```

- [ ] **Step 2: Build the Domain project to verify the file compiles**

Run: `dotnet build BIA.ToolKit.Domain/BIA.ToolKit.Domain.csproj --nologo -v:m`
Expected: `0 Erreur(s)`

- [ ] **Step 3: Commit**

```bash
git add BIA.ToolKit.Domain/Model/MigrationStepStatus.cs
git commit -m "feat(domain): add MigrationStepStatus enum for migration workflow

Five lifecycle states (Pending / Running / Done / Warning / Failed)
shared by the migration step ViewModel and the Output stripe renderer."
```

---

### Task 2: Add `MigrationStep` ViewModel

**Files:**
- Create: `BIA.ToolKit/ViewModels/MigrationStep.cs`

- [ ] **Step 1: Create the file**

```csharp
namespace BIA.ToolKit.ViewModels
{
    using System.Windows.Input;
    using BIA.ToolKit.Domain.Model;
    using CommunityToolkit.Mvvm.ComponentModel;

    /// <summary>
    /// One cell of the <c>MigrationStepperUC</c>. Carries its number, label,
    /// the command that runs the step, and a status that the visual cell
    /// binds to (driving glyph + color via converters).
    /// </summary>
    public partial class MigrationStep : ObservableObject
    {
        public int Number { get; }
        public string Label { get; }
        public ICommand Command { get; }

        [ObservableProperty]
        private MigrationStepStatus status;

        /// <summary>Last error / warning message, surfaced as tooltip on the cell.</summary>
        [ObservableProperty]
        private string? lastMessage;

        public MigrationStep(int number, string label, ICommand command)
        {
            Number = number;
            Label = label;
            Command = command;
            Status = MigrationStepStatus.Pending;
        }

        /// <summary>Reset the cell to <see cref="MigrationStepStatus.Pending"/> and clear any prior message.</summary>
        public void Reset()
        {
            Status = MigrationStepStatus.Pending;
            LastMessage = null;
        }
    }
}
```

- [ ] **Step 2: Build the main project**

Run: `dotnet build BIA.ToolKit/BIA.ToolKit.csproj --nologo -v:m 2>&1 | tail -5`
Expected: `0 Erreur(s)`

- [ ] **Step 3: Commit**

```bash
git add BIA.ToolKit/ViewModels/MigrationStep.cs
git commit -m "feat(modify-project): add MigrationStep ViewModel

ObservableObject wrapping one stepper cell: Number, Label, ICommand,
Status (MigrationStepStatus), and LastMessage for tooltip. Used by
MigrationStepperUC items binding."
```

---

### Task 3: Add status-to-color and status-to-glyph converters

**Files:**
- Create: `BIA.ToolKit/Converters/MigrationStepStatusToColorConverter.cs`
- Create: `BIA.ToolKit/Converters/MigrationStepStatusToGlyphConverter.cs`

- [ ] **Step 1: Create the color converter**

```csharp
namespace BIA.ToolKit.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;
    using BIA.ToolKit.Domain.Model;

    /// <summary>
    /// Maps a <see cref="MigrationStepStatus"/> to a <see cref="Brush"/>.
    /// Resolves colors through the application's MaterialDesign resources so
    /// dark / light theme switches propagate automatically.
    /// Pending uses a muted MaterialDesign foreground; other states use
    /// MaterialDesign Primary / Validation Success / Validation Warning /
    /// Validation Error brushes when available, falling back to fixed colors.
    /// </summary>
    public sealed class MigrationStepStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not MigrationStepStatus status)
                return Brushes.Gray;

            string resourceKey = status switch
            {
                MigrationStepStatus.Pending  => "MaterialDesign.Brush.Foreground",
                MigrationStepStatus.Running  => "MaterialDesign.Brush.Primary",
                MigrationStepStatus.Done     => "MaterialDesign.Brush.ValidationSuccess",
                MigrationStepStatus.Warning  => "MaterialDesign.Brush.ValidationWarning",
                MigrationStepStatus.Failed   => "MaterialDesign.Brush.ValidationError",
                _ => "MaterialDesign.Brush.Foreground",
            };

            object resource = Application.Current?.TryFindResource(resourceKey);
            if (resource is Brush brush)
                return brush;

            // Hard-coded fallback if MaterialDesign theme is not loaded
            // (e.g. design-time preview). Matches the spec's dark-theme palette.
            return status switch
            {
                MigrationStepStatus.Pending  => new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0)),
                MigrationStepStatus.Running  => new SolidColorBrush(Color.FromRgb(0x42, 0xA5, 0xF5)),
                MigrationStepStatus.Done     => new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
                MigrationStepStatus.Warning  => new SolidColorBrush(Color.FromRgb(0xFF, 0xA7, 0x26)),
                MigrationStepStatus.Failed   => new SolidColorBrush(Color.FromRgb(0xEF, 0x53, 0x50)),
                _ => Brushes.Gray,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
```

- [ ] **Step 2: Create the glyph converter**

```csharp
namespace BIA.ToolKit.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using BIA.ToolKit.Domain.Model;

    /// <summary>
    /// Maps a <see cref="MigrationStepStatus"/> to its display glyph.
    /// </summary>
    public sealed class MigrationStepStatusToGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not MigrationStepStatus status)
                return string.Empty;

            return status switch
            {
                MigrationStepStatus.Pending  => "⏸",   // ⏸ pause
                MigrationStepStatus.Running  => "⚡",   // ⚡ running
                MigrationStepStatus.Done     => "✓",   // ✓ done
                MigrationStepStatus.Warning  => "⚠",   // ⚠ warning
                MigrationStepStatus.Failed   => "✗",   // ✗ failed
                _ => string.Empty,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
```

- [ ] **Step 3: Build and verify**

Run: `dotnet build BIA.ToolKit/BIA.ToolKit.csproj --nologo -v:m 2>&1 | tail -5`
Expected: `0 Erreur(s)`

- [ ] **Step 4: Commit**

```bash
git add BIA.ToolKit/Converters/MigrationStepStatusToColorConverter.cs BIA.ToolKit/Converters/MigrationStepStatusToGlyphConverter.cs
git commit -m "feat(modify-project): add MigrationStepStatus converters (color + glyph)

Color resolves through MaterialDesign theme resources so dark/light
toggle propagates without manual rerender. Glyph maps to unicode pause
(pending) / lightning (running) / check (done) / warning / cross."
```

---

### Task 4: Extend `IConsoleWriter` with `BeginStep`

**Files:**
- Modify: `BIA.ToolKit.Application/Helper/IConsoleWriter.cs`

- [ ] **Step 1: Replace the file content**

```csharp
namespace BIA.ToolKit.Application.Helper
{
    using System;

    public interface IConsoleWriter
    {
        void AddMessageLine(string message, string color = null, bool refreshimediate = true);
        void Clear();
        void CopyToClipboard();

        /// <summary>
        /// Opens a scope tagging subsequent <see cref="AddMessageLine"/> calls
        /// with the given step number. The scope flows through <c>await</c> and
        /// <c>Task.Run</c> via <see cref="System.Threading.AsyncLocal{T}"/>.
        /// Dispose to leave the scope; nested or concurrent scopes are not
        /// expected and behavior is undefined in that case.
        /// </summary>
        /// <param name="number">1-based step number (1..6).</param>
        /// <param name="label">Short label used for diagnostics / tooltip.</param>
        /// <returns>Disposable handle. Dispose to close the scope.</returns>
        IDisposable BeginStep(int number, string label);

        /// <summary>
        /// Re-renders all displayed messages, applying the current per-step
        /// color (whose value lives on the consumer side, e.g. ViewModel).
        /// Called when a step finishes (Running → Done/Warning/Failed) so the
        /// step's batch of stripes update from "Running" blue to the final color.
        /// </summary>
        /// <param name="stepStatusProvider">
        /// Resolver from step number to its current status. Implementations
        /// can call this for each stored message to choose the stripe color.
        /// </param>
        void RefreshStepColors(Func<int, BIA.ToolKit.Domain.Model.MigrationStepStatus?> stepStatusProvider);
    }
}
```

- [ ] **Step 2: Build the Application project (which Owns the interface)**

Run: `dotnet build BIA.ToolKit.Application/BIA.ToolKit.Application.csproj --nologo -v:m 2>&1 | tail -10`
Expected: Likely fails compile in `ConsoleWriter` (the concrete impl in BIA.ToolKit) because it does not implement the new members. That's expected — Task 5 fixes it.

- [ ] **Step 3: Add a project reference from Application to Domain if missing**

Check: `grep -c "BIA.ToolKit.Domain" BIA.ToolKit.Application/BIA.ToolKit.Application.csproj`
If output is 0, append a ProjectReference. If output is >= 1, skip this step.

Add (only if missing) inside the existing first `ItemGroup` containing `ProjectReference` entries:

```xml
<ProjectReference Include="..\BIA.ToolKit.Domain\BIA.ToolKit.Domain.csproj" />
```

- [ ] **Step 4: Commit**

```bash
git add BIA.ToolKit.Application/Helper/IConsoleWriter.cs BIA.ToolKit.Application/BIA.ToolKit.Application.csproj
git commit -m "feat(console): extend IConsoleWriter with BeginStep scope + RefreshStepColors

BeginStep(number, label) returns an IDisposable scope; AsyncLocal in
the implementation will tag messages emitted inside. RefreshStepColors
lets the renderer recompute stripe colors after a step transitions out
of Running. Project reference Application→Domain added if not yet present.

Concrete ConsoleWriter implementation follows in the next commit."
```

---

### Task 5: Implement `BeginStep`, stripes, and `RefreshStepColors` in `ConsoleWriter`

**Files:**
- Modify: `BIA.ToolKit/Helper/ConsoleWriter.cs`

- [ ] **Step 1: Replace the file content**

```csharp
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
                AddMsgLine(OutputRichTextBox, message, color, refreshimediate, IsDarkTheme, now, step, /*stepColor*/ null);
                displayedMessages.Add(new Message { message = message, color = color, timestamp = now, stepNumber = step });
            }
        }

        public IDisposable BeginStep(int number, string label)
        {
            currentStep.Value = number;
            return new StepScope(this, number);
        }

        private sealed class StepScope : IDisposable
        {
            private readonly ConsoleWriter owner;
            private readonly int? previous;
            public StepScope(ConsoleWriter owner, int number)
            {
                this.owner = owner;
                this.previous = currentStep.Value == number ? (int?)null : currentStep.Value;
                // currentStep was set by BeginStep before this scope was returned.
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

        // Latest provider used to color stripes. Null when no step has been opened yet.
        private Func<int, MigrationStepStatus?> stepStatusResolver;

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
                MigrationStepStatus.Done    => "lightgreen",
                MigrationStepStatus.Warning => "orange",
                MigrationStepStatus.Failed  => "red",
                _                            => "lightblue", // Running or unknown
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
```

- [ ] **Step 2: Add a Domain project reference if `BIA.ToolKit` doesn't already have one (it should via Application transitively, but the explicit using of `BIA.ToolKit.Domain.Model` needs the assembly available)**

Check: `grep -c "BIA.ToolKit.Domain" BIA.ToolKit/BIA.ToolKit.csproj`
If 0, add `<ProjectReference Include="..\BIA.ToolKit.Domain\BIA.ToolKit.Domain.csproj" />` into the existing `ItemGroup` that contains `<ProjectReference Include="..\BIA.ToolKit.Application\BIA.ToolKit.Application.csproj" />`. If >= 1, skip.

- [ ] **Step 3: Build the whole solution**

Run: `dotnet build BIAToolKit.sln --nologo -v:m 2>&1 | tail -5`
Expected: `0 Erreur(s)`. Any compile error means the AddMsgLine signature change broke a static caller — see the callers via `grep -rn "ConsoleWriter.AddMsgLine\|ConsoleWriter\.AddMessageLine" BIA.ToolKit*/ --include="*.cs"` and update them to pass the new optional parameters as `null`.

- [ ] **Step 4: Commit**

```bash
git add BIA.ToolKit/Helper/ConsoleWriter.cs BIA.ToolKit/BIA.ToolKit.csproj
git commit -m "feat(console): implement BeginStep scope + stripe rendering + RefreshStepColors

AsyncLocal<int?> tags messages emitted within a BeginStep scope; the
internal Message struct grows a StepNumber. The RichTextBox renderer
prefixes step-tagged lines with a U+258E LEFT VERTICAL BAR run in the
step's status color (lightblue=Running, lightgreen=Done, orange=Warning,
red=Failed), resolved through the existing dark/light palette helpers.

RefreshStepColors(resolver) lets the consumer re-render all stripes
when a step transitions out of Running, by storing the resolver and
calling ReRenderMessages. Non-step messages keep rendering identically
to V2.13.x."
```

---

### Task 6: Create `MigrationStepperUC`

**Files:**
- Create: `BIA.ToolKit/UserControls/MigrationStepperUC.xaml`
- Create: `BIA.ToolKit/UserControls/MigrationStepperUC.xaml.cs`

- [ ] **Step 1: Create the XAML**

```xml
<UserControl x:Class="BIA.ToolKit.UserControls.MigrationStepperUC"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:conv="clr-namespace:BIA.ToolKit.Converters"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             mc:Ignorable="d"
             d:DesignHeight="120" d:DesignWidth="800">
    <UserControl.Resources>
        <conv:MigrationStepStatusToColorConverter x:Key="StatusToColor"/>
        <conv:MigrationStepStatusToGlyphConverter x:Key="StatusToGlyph"/>
    </UserControl.Resources>

    <ItemsControl ItemsSource="{Binding Steps, RelativeSource={RelativeSource AncestorType=UserControl}}">
        <ItemsControl.ItemsPanel>
            <ItemsPanelTemplate>
                <UniformGrid Columns="6" Rows="1"/>
            </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <Grid Margin="6,4" ToolTip="{Binding LastMessage}">
                    <Grid.RowDefinitions>
                        <RowDefinition Height="auto"/>
                        <RowDefinition Height="auto"/>
                    </Grid.RowDefinitions>
                    <Button Grid.Row="0"
                            Command="{Binding Command}"
                            HorizontalAlignment="Center"
                            VerticalAlignment="Center"
                            Width="64" Height="64"
                            Padding="0"
                            BorderThickness="2"
                            BorderBrush="{Binding Status, Converter={StaticResource StatusToColor}}"
                            Background="Transparent"
                            Cursor="Hand">
                        <Button.Resources>
                            <Style TargetType="Border">
                                <Setter Property="CornerRadius" Value="32"/>
                            </Style>
                        </Button.Resources>
                        <StackPanel Orientation="Vertical" VerticalAlignment="Center">
                            <TextBlock Text="{Binding Number}" FontSize="20" FontWeight="Bold" HorizontalAlignment="Center"
                                       Foreground="{Binding Status, Converter={StaticResource StatusToColor}}"/>
                            <TextBlock Text="{Binding Status, Converter={StaticResource StatusToGlyph}}"
                                       FontSize="14"
                                       HorizontalAlignment="Center"
                                       Foreground="{Binding Status, Converter={StaticResource StatusToColor}}"/>
                        </StackPanel>
                    </Button>
                    <TextBlock Grid.Row="1"
                               Text="{Binding Label}"
                               HorizontalAlignment="Center"
                               Margin="0,6,0,0"
                               TextTrimming="CharacterEllipsis"
                               TextAlignment="Center"
                               MaxWidth="100"/>
                </Grid>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</UserControl>
```

- [ ] **Step 2: Create the code-behind**

```csharp
namespace BIA.ToolKit.UserControls
{
    using System.Collections;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Horizontal stepper showing one cell per migration step.
    /// Bind <see cref="Steps"/> to an <c>IEnumerable&lt;MigrationStep&gt;</c>
    /// (typically <c>ModifyProjectViewModel.Steps</c>).
    /// </summary>
    public partial class MigrationStepperUC : UserControl
    {
        public static readonly DependencyProperty StepsProperty =
            DependencyProperty.Register(
                nameof(Steps),
                typeof(IEnumerable),
                typeof(MigrationStepperUC),
                new PropertyMetadata(null));

        public IEnumerable Steps
        {
            get => (IEnumerable)GetValue(StepsProperty);
            set => SetValue(StepsProperty, value);
        }

        public MigrationStepperUC()
        {
            InitializeComponent();
        }
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build BIA.ToolKit/BIA.ToolKit.csproj --nologo -v:m 2>&1 | tail -5`
Expected: `0 Erreur(s)`. If you see a missing `.g.cs` error (XAML codegen race), run `rm -rf BIA.ToolKit/obj && dotnet build BIA.ToolKit/BIA.ToolKit.csproj --nologo -v:m`.

- [ ] **Step 4: Commit**

```bash
git add BIA.ToolKit/UserControls/MigrationStepperUC.xaml BIA.ToolKit/UserControls/MigrationStepperUC.xaml.cs
git commit -m "feat(modify-project): add MigrationStepperUC user control

Six circular step cells in a UniformGrid Columns=6 Rows=1, each
showing number + status glyph in the status color, with the step's
Label below. The whole cell is a Button bound to MigrationStep.Command.
ToolTip exposes LastMessage so a failed/warning step explains itself
on hover. Consumed by ModifyProjectUC in the next commit."
```

---

### Task 7: Wire `Steps` and step status transitions in `ModifyProjectViewModel`

**Files:**
- Modify: `BIA.ToolKit/ViewModels/ModifyProjectViewModel.cs`

- [ ] **Step 1: Read the current file head to recall what's there**

Run: `head -60 BIA.ToolKit/ViewModels/ModifyProjectViewModel.cs`
This just confirms namespaces / fields; no change yet.

- [ ] **Step 2: Add the using and the Steps initialization**

In `BIA.ToolKit/ViewModels/ModifyProjectViewModel.cs`, add the following using directives in the existing using block at the top of the file (alphabetical-ish, keep style consistent with surrounding):

```csharp
    using BIA.ToolKit.Domain.Model;
    using System.Collections.ObjectModel;
```

Then add a public property exposing the six steps. Add this immediately AFTER the existing `ResetMigrationStepStates()` method (around line 118):

```csharp
        // --- Migration steps (V2.14.0 stepper) ---

        /// <summary>
        /// The six steps backing the <c>MigrationStepperUC</c>. Order matters:
        /// each step's downstream peers are reset to Pending when it is
        /// (re-)executed via <see cref="ResetStepsFrom(int)"/>.
        /// </summary>
        public ObservableCollection<MigrationStep> Steps { get; }

        private MigrationStep Step(int n) => Steps[n - 1];

        /// <summary>
        /// Resets the status of every step strictly after <paramref name="number"/>
        /// to <see cref="MigrationStepStatus.Pending"/>. Called when a step is
        /// (re-)triggered so the visual chain reflects the new starting point.
        /// </summary>
        private void ResetStepsFrom(int number)
        {
            foreach (var s in Steps.Where(s => s.Number > number))
                s.Reset();
        }
```

Inside the constructor, after the existing `WeakReferenceMessenger.Default.RegisterAll(this);` line, append:

```csharp
            Steps = new ObservableCollection<MigrationStep>
            {
                new(1, "Generate Only",       MigrateGenerateOnlyCommand),
                new(2, "Open Folder",         MigrateOpenFolderCommand),
                new(3, "Apply Diff",          MigrateApplyDiffCommand),
                new(4, "Merge Rejected",      MigrateMergeRejectedCommand),
                new(5, "Overwrite \\BIA-.*",  MigrateOverwriteBIAFolderCommand),
                new(6, "Fix Usings",          FixUsingsCommand),
            };
```

Replace the body of `ResetMigrationStepStates()` to ALSO reset Steps:

```csharp
        private void ResetMigrationStepStates()
        {
            CanOpenFolder = false;
            CanApplyDiff = false;
            CanMergeRejected = false;
            if (Steps is null) return;          // Receive() can fire before ctor body completes
            foreach (var s in Steps) s.Reset();
        }
```

- [ ] **Step 3: Wrap each step method with status transitions and `BeginStep` scope**

Edit `MigrateGenerateOnlyRunAsync` to apply this transition pattern around the existing body. The current method signature is `private async Task<int> MigrateGenerateOnlyRunAsync(CancellationToken ct = default)`. Wrap as follows (keep the existing body inside the `try`):

```csharp
        private async Task<int> MigrateGenerateOnlyRunAsync(CancellationToken ct = default)
        {
            var step = Step(1);
            step.Status = MigrationStepStatus.Running;
            ResetStepsFrom(1);
            using var stepScope = consoleWriter.BeginStep(1, step.Label);
            try
            {
                // === existing body BEGIN ===
                if (!Directory.Exists(ModifyProject.CurrentProject.Folder) || IsDirectoryEmpty(ModifyProject.CurrentProject.Folder))
                {
                    consoleWriter.AddMessageLine("The project path is empty : " + ModifyProject.CurrentProject.Folder, "red");
                    step.Status = MigrationStepStatus.Failed;
                    step.LastMessage = "Project path is empty";
                    consoleWriter.RefreshStepColors(StepStatusResolver);
                    return -1;
                }

                MigratePreparePath(out _, out var projectOriginPath, out _, out _, out var projectTargetPath, out _);
                await GenerateProjectsAsync(true, projectOriginPath, projectTargetPath, ct);

                CanOpenFolder = true;
                CanApplyDiff = true;
                // === existing body END ===

                step.Status = MigrationStepStatus.Done;
                consoleWriter.RefreshStepColors(StepStatusResolver);
                return 0;
            }
            catch (OperationCanceledException)
            {
                step.Status = MigrationStepStatus.Failed;
                step.LastMessage = "Cancelled";
                consoleWriter.RefreshStepColors(StepStatusResolver);
                throw;
            }
            catch (Exception ex)
            {
                step.Status = MigrationStepStatus.Failed;
                step.LastMessage = ex.Message;
                consoleWriter.RefreshStepColors(StepStatusResolver);
                throw;
            }
        }
```

Apply the same pattern to `MigrateApplyDiffRunAsync` (step 3), but distinguish the return-false case as Warning:

```csharp
        private async Task<bool> MigrateApplyDiffRunAsync(CancellationToken ct = default)
        {
            var step = Step(3);
            step.Status = MigrationStepStatus.Running;
            ResetStepsFrom(3);
            using var stepScope = consoleWriter.BeginStep(3, step.Label);
            try
            {
                bool result = false;

                MigratePreparePath(out var projectOriginalFolderName, out var projectOriginPath, out _, out var projectTargetFolderName, out _, out _);

                if (OverwriteBIAFromOriginal == true)
                {
                    await projectCreatorService.OverwriteBIAFolder(projectOriginPath, ModifyProject.CurrentProject.Folder, false, ct);
                }

                result = await ApplyDiffAsync(true, projectOriginalFolderName, projectTargetFolderName, ct);
                CanMergeRejected = true;

                step.Status = result ? MigrationStepStatus.Done : MigrationStepStatus.Warning;
                if (!result) step.LastMessage = "Apply Diff returned false — inspect output";
                consoleWriter.RefreshStepColors(StepStatusResolver);
                return result;
            }
            catch (OperationCanceledException)
            {
                step.Status = MigrationStepStatus.Failed;
                step.LastMessage = "Cancelled";
                consoleWriter.RefreshStepColors(StepStatusResolver);
                throw;
            }
            catch (Exception ex)
            {
                step.Status = MigrationStepStatus.Failed;
                step.LastMessage = ex.Message;
                consoleWriter.RefreshStepColors(StepStatusResolver);
                throw;
            }
        }
```

Apply the wrap to `MigrateMergeRejectedRunAsync` (step 4):

```csharp
        private async Task MigrateMergeRejectedRunAsync(CancellationToken ct = default)
        {
            var step = Step(4);
            step.Status = MigrationStepStatus.Running;
            ResetStepsFrom(4);
            using var stepScope = consoleWriter.BeginStep(4, step.Label);
            try
            {
                await MergeRejectedAsync(true, ct);
                CanMergeRejected = false;

                await Task.Run(() =>
                {
                    foreach (var biaFront in ModifyProject.CurrentProject.BIAFronts)
                    {
                        ProcessHelper.OpenFolder(Path.Combine(ModifyProject.CurrentProject.Folder, biaFront, "src", "app"));
                    }
                }, ct);

                step.Status = MigrationStepStatus.Done;
                consoleWriter.RefreshStepColors(StepStatusResolver);
            }
            catch (OperationCanceledException)
            {
                step.Status = MigrationStepStatus.Failed;
                step.LastMessage = "Cancelled";
                consoleWriter.RefreshStepColors(StepStatusResolver);
                throw;
            }
            catch (Exception ex)
            {
                step.Status = MigrationStepStatus.Failed;
                step.LastMessage = ex.Message;
                consoleWriter.RefreshStepColors(StepStatusResolver);
                throw;
            }
        }
```

> **NOTE for the executing agent:** the body inside `try` MUST be exactly what currently lives in each method (re-read each method before replacing — do not assume the body matches what's printed here line-for-line in case the file evolved). The wrap pattern stays identical: status=Running + ResetStepsFrom + using BeginStep, transitions on return/throw, RefreshStepColors after each transition.

Apply the wrap to `OverwriteBIAFolderAsync` (step 5):

```csharp
        private async Task OverwriteBIAFolderAsync(bool actionFinishedAtEnd, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var step = Step(5);
            step.Status = MigrationStepStatus.Running;
            ResetStepsFrom(5);
            using var stepScope = consoleWriter.BeginStep(5, step.Label);
            try
            {
                MigratePreparePath(out _, out _, out _, out _, out var projectTargetPath, out _);
                await projectCreatorService.OverwriteBIAFolder(projectTargetPath, ModifyProject.CurrentProject.Folder, actionFinishedAtEnd, ct);

                step.Status = MigrationStepStatus.Done;
                consoleWriter.RefreshStepColors(StepStatusResolver);
            }
            catch (OperationCanceledException)
            {
                step.Status = MigrationStepStatus.Failed;
                step.LastMessage = "Cancelled";
                consoleWriter.RefreshStepColors(StepStatusResolver);
                throw;
            }
            catch (Exception ex)
            {
                step.Status = MigrationStepStatus.Failed;
                step.LastMessage = ex.Message;
                consoleWriter.RefreshStepColors(StepStatusResolver);
                throw;
            }
        }
```

Replace the `FixUsings()` body to wrap the `parserService.FixUsings(ct)` call with the step scope (step 6). The current method is:
```csharp
        private void FixUsings()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) => await parserService.FixUsings(ct)));
        }
```
Replace with:

```csharp
        private void FixUsings()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) =>
            {
                var step = Step(6);
                step.Status = MigrationStepStatus.Running;
                ResetStepsFrom(6);
                using var stepScope = consoleWriter.BeginStep(6, step.Label);
                try
                {
                    await parserService.FixUsings(ct);
                    step.Status = MigrationStepStatus.Done;
                    consoleWriter.RefreshStepColors(StepStatusResolver);
                }
                catch (OperationCanceledException)
                {
                    step.Status = MigrationStepStatus.Failed;
                    step.LastMessage = "Cancelled";
                    consoleWriter.RefreshStepColors(StepStatusResolver);
                    throw;
                }
                catch (Exception ex)
                {
                    step.Status = MigrationStepStatus.Failed;
                    step.LastMessage = ex.Message;
                    consoleWriter.RefreshStepColors(StepStatusResolver);
                    throw;
                }
            }));
        }
```

Update `MigrateOpenFolder` (step 2) to tag its instant success:

```csharp
        [RelayCommand]
        private void MigrateOpenFolder()
        {
            var step = Step(2);
            try
            {
                ResetStepsFrom(2);
                Process.Start("explorer.exe", AppSettings.TmpFolderPath);
                step.Status = MigrationStepStatus.Done;
                consoleWriter.RefreshStepColors(StepStatusResolver);
            }
            catch (Exception ex)
            {
                step.Status = MigrationStepStatus.Failed;
                step.LastMessage = ex.Message;
                consoleWriter.RefreshStepColors(StepStatusResolver);
            }
        }
```

Add the resolver method near the bottom of the file (just before the closing `}` of the class):

```csharp
        // Used by ConsoleWriter.RefreshStepColors to resolve a step's current
        // status when re-rendering output stripes.
        private MigrationStepStatus? StepStatusResolver(int number)
        {
            if (Steps is null || number < 1 || number > Steps.Count) return null;
            return Steps[number - 1].Status;
        }
```

- [ ] **Step 4: Build the solution**

Run: `dotnet build BIAToolKit.sln --nologo -v:m 2>&1 | tail -10`
Expected: `0 Erreur(s)`. If you get errors about `Step()`, `ResetStepsFrom`, `Steps`, or `StepStatusResolver`, you forgot one of the helper additions — re-check.

- [ ] **Step 5: Commit**

```bash
git add BIA.ToolKit/ViewModels/ModifyProjectViewModel.cs
git commit -m "feat(modify-project): expose Steps and wire status transitions

ModifyProjectViewModel now publishes an ObservableCollection<MigrationStep>
Steps backing the new MigrationStepperUC. Each migration step method
(MigrateGenerateOnlyRunAsync, MigrateOpenFolder, MigrateApplyDiffRunAsync,
MigrateMergeRejectedRunAsync, OverwriteBIAFolderAsync, FixUsings) is
wrapped with the consoleWriter.BeginStep scope + status transitions
(Pending -> Running -> Done/Warning/Failed) + downstream reset
(ResetStepsFrom). RefreshStepColors is invoked on each transition so
the Output stripes update from Running blue to the final color.

The Migrate one-shot flow (MigrateRunAsync) already calls the wrapped
methods sequentially, so its behaviour automatically inherits the new
status feedback."
```

---

### Task 8: Rewrite `ModifyProjectUC.xaml` layout

**Files:**
- Modify: `BIA.ToolKit/UserControls/ModifyProjectUC.xaml`

- [ ] **Step 1: Replace the file content**

```xml
<UserControl x:Class="BIA.ToolKit.UserControls.ModifyProjectUC"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:uc="clr-namespace:BIA.ToolKit.UserControls"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             mc:Ignorable="d"
             d:Height="700" d:Width="1280">
    <UserControl.Resources>
        <BooleanToVisibilityConverter x:Key="BoolToVisConverter"/>
    </UserControl.Resources>
    <Grid x:Name="TabModify">
        <Grid Margin="10,0,10,0">
            <Grid Background="{DynamicResource MaterialDesign.Brush.Background}" Opacity="0.2"/>
            <TabControl x:Name="TabActions" SelectedIndex="{Binding SelectedTabIndex}" IsEnabled="{Binding IsProjectSelected}">
                <TabControl.Style>
                    <Style TargetType="TabControl" BasedOn="{StaticResource {x:Type TabControl}}">
                        <Setter Property="Opacity" Value="1"/>
                        <Style.Triggers>
                            <DataTrigger Binding="{Binding IsProjectSelected}" Value="False">
                                <Setter Property="Opacity" Value="0.35"/>
                            </DataTrigger>
                        </Style.Triggers>
                    </Style>
                </TabControl.Style>
                <TabItem x:Name="TabProject" Header="Project">
                    <ScrollViewer Margin="5" IsEnabled="{Binding IsProjectSelected}"
                                  HorizontalScrollBarVisibility="Auto" VerticalScrollBarVisibility="Auto">
                        <StackPanel MinWidth="900">
                            <!-- Side-by-side Original | Target -->
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="1*"/>
                                    <ColumnDefinition Width="1*"/>
                                </Grid.ColumnDefinitions>
                                <GroupBox Grid.Column="0" Margin="10" Header="Original"
                                          Style="{StaticResource MaterialDesignGroupBox}"
                                          materialDesign:ColorZoneAssist.Mode="Custom"
                                          materialDesign:ColorZoneAssist.Background="{DynamicResource MaterialDesign.Brush.Primary}"
                                          materialDesign:ColorZoneAssist.Foreground="White">
                                    <uc:VersionAndOptionUserControl x:Name="MigrateOriginVersionAndOption" ForceAdvanced="True"/>
                                </GroupBox>
                                <GroupBox Grid.Column="1" Margin="10" Header="Target"
                                          Style="{StaticResource MaterialDesignGroupBox}"
                                          materialDesign:ColorZoneAssist.Mode="Custom"
                                          materialDesign:ColorZoneAssist.Background="{DynamicResource MaterialDesign.Brush.Primary}"
                                          materialDesign:ColorZoneAssist.Foreground="White">
                                    <uc:VersionAndOptionUserControl x:Name="MigrateTargetVersionAndOption" ForceAdvanced="True"/>
                                </GroupBox>
                            </Grid>

                            <!-- Migrate banner -->
                            <Border Margin="10,4,10,4"
                                    Padding="14,10"
                                    CornerRadius="6"
                                    BorderThickness="1"
                                    BorderBrush="{DynamicResource MaterialDesign.Brush.Card.Border}"
                                    Background="{DynamicResource MaterialDesign.Brush.Card.Background}">
                                <Grid>
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="*"/>
                                        <ColumnDefinition Width="auto"/>
                                    </Grid.ColumnDefinitions>
                                    <CheckBox Grid.Column="0" VerticalAlignment="Center"
                                              Content="Overwrite BIA first"
                                              IsChecked="{Binding OverwriteBIAFromOriginal, Mode=TwoWay}"/>
                                    <Button Grid.Column="1"
                                            Style="{StaticResource Button.SecondaryAction}"
                                            Height="44" MinWidth="180" Padding="20,0"
                                            Content="🚀  MIGRATE"
                                            FontWeight="SemiBold"
                                            Command="{Binding MigrateCommand}"
                                            IsEnabled="{Binding IsProjectSelected}"/>
                                </Grid>
                            </Border>

                            <!-- Stepper -->
                            <TextBlock Margin="10,8,10,0"
                                       Text="Or step by step"
                                       FontSize="11" Opacity="0.7"/>
                            <uc:MigrationStepperUC Margin="10,4,10,10"
                                                   Steps="{Binding Steps}"/>
                        </StackPanel>
                    </ScrollViewer>
                </TabItem>
                <TabItem x:Name="TabRegenerateFeatures" Header="Features"
                         IsEnabled="{Binding IsTabFeaturesEnabled}">
                    <uc:RegenerateFeaturesUC x:Name="RegenerateFeatures"/>
                </TabItem>
            </TabControl>

            <!-- Empty-state overlay: shown only when no project is selected -->
            <Border HorizontalAlignment="Center" VerticalAlignment="Center"
                    Background="{DynamicResource MaterialDesign.Brush.Card.Background}"
                    BorderBrush="{DynamicResource MaterialDesign.Brush.Primary}"
                    BorderThickness="1"
                    CornerRadius="8"
                    Padding="32,24"
                    IsHitTestVisible="False">
                <Border.Style>
                    <Style TargetType="Border">
                        <Setter Property="Visibility" Value="Collapsed"/>
                        <Style.Triggers>
                            <DataTrigger Binding="{Binding IsProjectSelected}" Value="False">
                                <Setter Property="Visibility" Value="Visible"/>
                            </DataTrigger>
                        </Style.Triggers>
                    </Style>
                </Border.Style>
                <StackPanel Orientation="Vertical" HorizontalAlignment="Center">
                    <materialDesign:PackIcon Kind="FolderOpenOutline" Width="48" Height="48"
                                             HorizontalAlignment="Center"
                                             Foreground="{DynamicResource MaterialDesign.Brush.Primary}"/>
                    <TextBlock Text="Select a project to begin"
                               FontSize="18" FontWeight="SemiBold"
                               Margin="0,12,0,4"
                               HorizontalAlignment="Center"/>
                    <TextBlock Text="Use the project selector above to choose a project to migrate."
                               Style="{StaticResource Text.Caption}"
                               HorizontalAlignment="Center"/>
                </StackPanel>
            </Border>
        </Grid>
    </Grid>

</UserControl>
```

- [ ] **Step 2: Build and clear stale obj artifacts if needed**

Run: `rm -rf BIA.ToolKit/obj && dotnet build BIA.ToolKit/BIA.ToolKit.csproj --nologo -v:m 2>&1 | tail -5`
Expected: `0 Erreur(s)`.

- [ ] **Step 3: Run the app for a 1-minute smoke test**

Run: `dotnet run --project BIA.ToolKit/BIA.ToolKit.csproj` (or launch the built exe at `BIA.ToolKit/bin/Debug/net10.0-windows10.0.26100.0/win-x64/BIA.ToolKit.exe`).

Open any existing project (Modify Project tab). Verify visually:

  - Origin and Target panels render side-by-side at 50/50 width
  - The Migrate banner shows the Overwrite-first checkbox and the prominent MIGRATE button
  - The 6 stepper cells appear in a single horizontal row, all Pending (⏸)
  - The empty-state "Select a project to begin" overlay still appears when no project is selected

If any of the above fails, stop and inspect the binding paths / namespaces. Do not commit a broken UI.

- [ ] **Step 4: Commit**

```bash
git add BIA.ToolKit/UserControls/ModifyProjectUC.xaml
git commit -m "feat(modify-project): redesign Project tab with side-by-side panels and MigrationStepperUC

Replaces the vertical Original-above-Target layout + right-sidebar
button stack with:
  * Side-by-side Original | Target panels (1:1 columns, MinWidth=900,
    horizontal+vertical scrollbars to keep the dense VersionAndOption UC
    legible on narrower screens)
  * A full-width Migrate banner: Overwrite-first checkbox on the left,
    big MIGRATE button on the right
  * The new MigrationStepperUC bound to Steps under an 'Or step by step'
    header
The empty-state overlay (no project selected) and the Features sub-tab
are untouched."
```

---

### Task 9: Manual integration test (no code change — just verification)

**Files:** none

- [ ] **Step 1: Launch the app on a real BIA project (V7+ recommended)**

```powershell
.\BIA.ToolKit\bin\Debug\net10.0-windows10.0.26100.0\win-x64\BIA.ToolKit.exe
```

- [ ] **Step 2: Run the spec's manual test plan in order**

For each test, observe the UI and the Output panel. Stop and investigate at the first failure.

  1. **Layout responsiveness**: resize the window to 1920 / 1280 / 1024 px wide. Confirm both panels stay readable and that the 1024 case shows a horizontal scrollbar instead of squeezing the inner UCs.
  2. **Stepper happy path**: click step 1 (Generate Only). Status goes Pending → Running (⚡, primary color) → Done (✓, green). Output shows lightblue stripes during run, lightgreen after.
  3. **Stepper failure**: select a project with no `.sln`, or break the project to force step 6 (Fix Usings) to throw. The corresponding cell goes Failed (✗, red). Output stripes for that step batch turn red.
  4. **Mid-step cancel**: click step 1, then immediately click the busy-overlay Cancel button. The cell goes Failed with tooltip "Cancelled". Step stripes turn red.
  5. **One-shot Migrate**: with a real project, click MIGRATE. Steps 1-4 run sequentially, each transitioning Running → Done. Output shows 4 distinct color batches.
  6. **Upstream rerun cascade**: complete steps 1-5 (all Done), then re-click step 3. Steps 4-5-6 reset to Pending; step 3 goes Running.
  7. **Theme toggle**: in Settings, switch dark ↔ light. All stepper cells, glyphs, and existing output stripes update legibly (re-render uses `ReRenderMessages`).
  8. **Other tabs unaffected**: open CRUD / DTO / Option generators on any project. Trigger a small generation. Confirm no stripes appear in the Output for those generations (because no `BeginStep` scope is opened there).

If any test fails, capture the symptom in `docs/superpowers/specs/2026-05-28-migration-screen-redesign-design.md` under a new "Test failures observed during V2.14.0 manual verification" section, fix the offending task's code, and re-test from the failing test downward.

- [ ] **Step 3: No commit (verification-only task)**

---

### Task 10: Bump version and finalise V2.14.0

**Files:**
- Modify: `BIA.ToolKit/BIA.ToolKit.csproj` (line containing `<Version>2.13.1</Version>`)

- [ ] **Step 1: Bump the version**

In `BIA.ToolKit/BIA.ToolKit.csproj` replace the `<Version>` line:

Before: `<Version>2.13.1</Version>`
After:  `<Version>2.14.0</Version>`

- [ ] **Step 2: Build the whole solution one final time**

Run: `dotnet build BIAToolKit.sln --nologo -v:m 2>&1 | tail -5`
Expected: `0 Erreur(s)`.

- [ ] **Step 3: Run dotnet format style to catch any obvious style drift**

Run: `dotnet format style --severity info --verify-no-changes 2>&1 | tail -10`
If it reports unfixed style issues, run `dotnet format style --severity info` (without `--verify-no-changes`) to auto-fix, then rebuild.

- [ ] **Step 4: Commit**

```bash
git add BIA.ToolKit/BIA.ToolKit.csproj
git commit -m "Up version to 2.14.0"
```

- [ ] **Step 5: Show the final commit list on the branch for review**

Run: `git log --oneline main..HEAD`
Expected (commit hashes will differ):
```
<hash>  Up version to 2.14.0
<hash>  feat(modify-project): redesign Project tab with side-by-side panels and MigrationStepperUC
<hash>  feat(modify-project): expose Steps and wire status transitions
<hash>  feat(modify-project): add MigrationStepperUC user control
<hash>  feat(console): implement BeginStep scope + stripe rendering + RefreshStepColors
<hash>  feat(console): extend IConsoleWriter with BeginStep scope + RefreshStepColors
<hash>  feat(modify-project): add MigrationStepStatus converters (color + glyph)
<hash>  feat(modify-project): add MigrationStep ViewModel
<hash>  feat(domain): add MigrationStepStatus enum for migration workflow
<hash>  fix(crud-gen): show the BIA Front selector when the project has multiple fronts
<hash>  docs(spec): add V2.14.0 migration screen redesign design
```

- [ ] **Step 6: STOP before gitflow finish**

Do NOT merge into main / develop or push tags autonomously. Per the user's standing convention, gitflow finish and push to main are explicitly user-authorized actions. Report the final commit list and wait for the user's "go gitflow finish V2.14.0 + push" before proceeding to:

```bash
git checkout main && git merge --no-ff feature/fixes-V2.14.0 -m "Merge branch 'feature/fixes-V2.14.0'" && git tag V2.14.0 && git tag V2.14.0.0
git checkout develop && git merge --no-ff feature/fixes-V2.14.0 -m "Merge branch 'feature/fixes-V2.14.0' into develop" && git branch -d feature/fixes-V2.14.0
git push origin main develop --follow-tags
git push origin V2.14.0 V2.14.0.0
```

---

## Self-Review Notes

**Spec coverage check:**
- §1 Layout side-by-side: Task 8
- §2 MigrationStepperUC: Task 6
- §3 MigrationStep state machine: Tasks 1, 2, 7 (transitions + cascade reset)
- §4 Output coloring (`BeginStep` + stripes): Tasks 4, 5
- §5 Impact / files: all enumerated tasks; non-goals respected (VersionAndOptionUserControl untouched; other tabs unaffected — manual test 8)
- Test plan: Task 9 mirrors the spec's 8 tests
- Version bump: Task 10

**Placeholder scan:** none of "TBD / TODO / appropriate handling / similar to" left in the plan. The wrap pattern in Task 7 explicitly shows the full code for each of the six step methods.

**Type consistency:**
- `MigrationStepStatus` (enum) — Task 1 — used by Tasks 2, 3, 5, 6, 7.
- `MigrationStep.Reset()` — Task 2 — called from Task 7 (`ResetStepsFrom`).
- `IConsoleWriter.BeginStep(int, string)` returns `IDisposable` — Task 4 — implemented in Task 5, consumed in Task 7.
- `IConsoleWriter.RefreshStepColors(Func<int, MigrationStepStatus?>)` — Task 4 — implemented in Task 5, consumed in Task 7 via `StepStatusResolver` (defined in Task 7).
- `MigrationStepperUC.Steps` (DependencyProperty of type `IEnumerable`) — Task 6 — bound in Task 8 to `ModifyProjectViewModel.Steps` (Task 7).
