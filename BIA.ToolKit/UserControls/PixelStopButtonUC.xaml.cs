using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace BIA.ToolKit.UserControls;

public partial class PixelStopButtonUC : UserControl
{
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(PixelStopButtonUC));

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    private const int PixelSize = 4;
    private const int GridCols = 8;
    private const int GridRows = 8;

    // 8s cycle: disintegrate 6s, pause 0.5s, rebuild 1s, pause 0.5s
    private static readonly TimeSpan DisintegrateDuration = TimeSpan.FromSeconds(6.0);
    private static readonly TimeSpan RebuildDuration = TimeSpan.FromSeconds(1.0);
    private static readonly TimeSpan PauseDuration = TimeSpan.FromSeconds(0.5);
    private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(0.5);

    private readonly List<PixelInfo> pixels = [];
    private readonly Random rng = new();
    private Storyboard activeStoryboard;

    // Pixel art: simple stop square
    // R = red fill, r = dark red border
    private static readonly string[] StopMap =
    [
        "rrrrrrrr",
        "rRRRRRRr",
        "rRRRRRRr",
        "rRRRRRRr",
        "rRRRRRRr",
        "rRRRRRRr",
        "rRRRRRRr",
        "rrrrrrrr",
    ];

    private static readonly Color RedFill = Color.FromRgb(0xCD, 0x5C, 0x5C);   // IndianRed
    private static readonly Color RedBorder = Color.FromRgb(0x8B, 0x3A, 0x3A);  // Darker red

    public PixelStopButtonUC()
    {
        InitializeComponent();
        BuildPixels();

        IsVisibleChanged += (_, e) =>
        {
            if (e.NewValue is true)
                StartAnimation();
            else
                StopAnimation();
        };
    }

    private void OnClick(object sender, MouseButtonEventArgs e)
    {
        if (Command?.CanExecute(null) == true)
            Command.Execute(null);
    }

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        LabelText.Opacity = 0.7;
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        LabelText.Opacity = 1.0;
    }

    private void BuildPixels()
    {
        double canvasW = 40;
        double canvasH = 40;
        double offsetX = (canvasW - GridCols * PixelSize) / 2.0;
        double offsetY = (canvasH - GridRows * PixelSize) / 2.0;

        for (int row = 0; row < StopMap.Length && row < GridRows; row++)
        {
            string line = StopMap[row];
            for (int col = 0; col < line.Length && col < GridCols; col++)
            {
                char c = line[col];
                if (c == '.')
                    continue;

                var color = c switch
                {
                    'R' => RedFill,
                    _ => RedBorder,
                };

                var translate = new TranslateTransform(0, 0);

                var rect = new Rectangle
                {
                    Width = PixelSize,
                    Height = PixelSize,
                    Fill = new SolidColorBrush(color),
                    RadiusX = 1,
                    RadiusY = 1,
                    RenderTransform = translate,
                };

                double homeX = offsetX + col * PixelSize;
                double homeY = offsetY + row * PixelSize;

                Canvas.SetLeft(rect, homeX);
                Canvas.SetTop(rect, homeY);
                StopCanvas.Children.Add(rect);

                pixels.Add(new PixelInfo
                {
                    Rect = rect,
                    Transform = translate,
                });
            }
        }
    }

    private void StartAnimation()
    {
        StopAnimation();

        foreach (var p in pixels)
        {
            p.Transform.X = 0;
            p.Transform.Y = 0;
            p.Rect.Opacity = 1;
        }

        var storyboard = new Storyboard();

        // Cycle: slow disintegrate + pause + fast rebuild + pause
        var cycleDuration = DisintegrateDuration + PauseDuration
                          + RebuildDuration + PauseDuration;

        foreach (var p in pixels)
        {
            double angle = rng.NextDouble() * 2 * Math.PI;
            double distance = 20 + rng.NextDouble() * 30;
            double targetX = Math.Cos(angle) * distance;
            double targetY = Math.Sin(angle) * distance;
            var delay = TimeSpan.FromMilliseconds(rng.NextDouble() * MaxDelay.TotalMilliseconds);

            var disintegrateEnd = delay + DisintegrateDuration;
            var rebuildStart = DisintegrateDuration + PauseDuration + delay;
            var rebuildEnd = rebuildStart + RebuildDuration - delay;

            // --- TranslateTransform.X ---
            var animX = new DoubleAnimationUsingKeyFrames
            {
                RepeatBehavior = RepeatBehavior.Forever,
                Duration = new Duration(cycleDuration),
            };
            animX.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            animX.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(delay))
                { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } });
            animX.KeyFrames.Add(new EasingDoubleKeyFrame(targetX, KeyTime.FromTimeSpan(disintegrateEnd))
                { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } });
            animX.KeyFrames.Add(new EasingDoubleKeyFrame(targetX, KeyTime.FromTimeSpan(rebuildStart)));
            animX.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(rebuildEnd))
                { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
            animX.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(cycleDuration)));

            Storyboard.SetTarget(animX, p.Rect);
            Storyboard.SetTargetProperty(animX, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
            storyboard.Children.Add(animX);

            // --- TranslateTransform.Y ---
            var animY = new DoubleAnimationUsingKeyFrames
            {
                RepeatBehavior = RepeatBehavior.Forever,
                Duration = new Duration(cycleDuration),
            };
            animY.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            animY.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(delay))
                { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } });
            animY.KeyFrames.Add(new EasingDoubleKeyFrame(targetY, KeyTime.FromTimeSpan(disintegrateEnd))
                { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } });
            animY.KeyFrames.Add(new EasingDoubleKeyFrame(targetY, KeyTime.FromTimeSpan(rebuildStart)));
            animY.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(rebuildEnd))
                { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
            animY.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(cycleDuration)));

            Storyboard.SetTarget(animY, p.Rect);
            Storyboard.SetTargetProperty(animY, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
            storyboard.Children.Add(animY);

            // --- Opacity ---
            var animOpacity = new DoubleAnimationUsingKeyFrames
            {
                RepeatBehavior = RepeatBehavior.Forever,
                Duration = new Duration(cycleDuration),
            };
            animOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            animOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(delay)));
            animOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(disintegrateEnd))
                { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } });
            animOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(rebuildStart)));
            animOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(rebuildEnd))
                { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
            animOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(cycleDuration)));

            Storyboard.SetTarget(animOpacity, p.Rect);
            Storyboard.SetTargetProperty(animOpacity, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(animOpacity);
        }

        activeStoryboard = storyboard;
        storyboard.Begin(this, true);
    }

    private void StopAnimation()
    {
        if (activeStoryboard != null)
        {
            activeStoryboard.Stop(this);
            activeStoryboard = null;
        }
    }

    private sealed class PixelInfo
    {
        public Rectangle Rect;
        public TranslateTransform Transform;
    }
}
