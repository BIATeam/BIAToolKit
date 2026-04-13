using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace BIA.ToolKit.UserControls;

public partial class PixelLeafLoaderUC : UserControl
{
    private const int PixelSize = 8;
    private const int GridCols = 22;
    private const int GridRows = 24;

    private static readonly Duration DisintegrateDuration = new(TimeSpan.FromSeconds(1.2));
    private static readonly Duration PauseDuration = new(TimeSpan.FromSeconds(0.4));
    private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(0.3);

    private readonly List<PixelInfo> pixels = [];
    private readonly Random rng = new();
    private Storyboard activeStoryboard;

    // Pixel art leaf shape: '#' = leaf, 'V' = dark vein, 'L' = lime highlight, 'S' = stem
    private static readonly string[] LeafMap =
    [
        "...............##.....",
        "..............###.....",
        ".............####.....",
        "............#####.....",
        "...........##V###.....",
        "..........##VL####....",
        ".........##V#L####....",
        "........##V##L#####...",
        ".......##V##.L#####...",
        "......##V##..L#####...",
        ".....##V###..L######..",
        "....##V####.L######...",
        "...##V####.L######....",
        "..##V#####L######.....",
        ".##V####L.#####.......",
        ".#V###L..#####........",
        "##V##L..####..........",
        "##V#L.####............",
        ".#VL####..............",
        "..L###................",
        "..SS..................",
        "..SS..................",
        "...S..................",
        "...S..................",
    ];

    public PixelLeafLoaderUC()
    {
        InitializeComponent();
        BuildPixels();

        IsVisibleChanged += (_, e) =>
        {
            if (e.NewValue is true)
            {
                StartAnimation();
            }
            else
            {
                StopAnimation();
            }
        };
    }

    private void BuildPixels()
    {
        double offsetX = (200 - GridCols * PixelSize) / 2.0;
        double offsetY = (200 - GridRows * PixelSize) / 2.0;

        for (int row = 0; row < LeafMap.Length && row < GridRows; row++)
        {
            string line = LeafMap[row];
            for (int col = 0; col < line.Length && col < GridCols; col++)
            {
                char c = line[col];
                if (c == '.')
                    continue;

                var color = c switch
                {
                    'V' => Color.FromRgb(0x2E, 0x7D, 0x32),
                    'L' => Color.FromRgb(0xCD, 0xDC, 0x39),
                    'S' => Color.FromRgb(0x78, 0x78, 0x78),
                    _ => Color.FromRgb(0x6B, 0xB3, 0x2E),
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
                LeafCanvas.Children.Add(rect);

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

        // Reset all pixels to home position
        foreach (var p in pixels)
        {
            p.Transform.X = 0;
            p.Transform.Y = 0;
            p.Rect.Opacity = 1;
        }

        var storyboard = new Storyboard();

        // Total cycle: disintegrate + pause + rebuild + pause
        var cycleDuration = DisintegrateDuration.TimeSpan + PauseDuration.TimeSpan
                          + DisintegrateDuration.TimeSpan + PauseDuration.TimeSpan;

        foreach (var p in pixels)
        {
            // Random scatter direction and distance
            double angle = rng.NextDouble() * 2 * Math.PI;
            double distance = 60 + rng.NextDouble() * 80;
            double targetX = Math.Cos(angle) * distance;
            double targetY = Math.Sin(angle) * distance;
            var delay = TimeSpan.FromMilliseconds(rng.NextDouble() * MaxDelay.TotalMilliseconds);

            // Phase timings
            var disintegrateEnd = delay + DisintegrateDuration.TimeSpan;
            var rebuildStart = DisintegrateDuration.TimeSpan + PauseDuration.TimeSpan + delay;
            var rebuildEnd = rebuildStart + DisintegrateDuration.TimeSpan - delay;

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
