using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MahApps.Metro.Controls;

namespace BeastMover
{
    /// <summary>
    /// Code-based GUI for BeastMover settings
    /// </summary>
    public class BeastMoverGui : UserControl
    {
        public BeastMoverGui()
        {
            var settings = BeastMoverSettings.Instance;

            // Main container
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(10)
            };

            var mainPanel = new StackPanel();
            scrollViewer.Content = mainPanel;

            // Movement Settings Section
            mainPanel.Children.Add(CreateSectionHeader("Movement Settings"));
            mainPanel.Children.Add(CreateNumericSetting(
                "Single Use Distance:",
                "Maximum distance for single-use movement skills",
                5, 45, 1,
                () => settings.SingleUseDistance,
                v => settings.SingleUseDistance = (int)v
            ));

            mainPanel.Children.Add(CreateCheckBoxSetting(
                "Allow Portal Movement",
                "Allow using movement skills through portals",
                () => settings.AllowPortalMovement,
                v => settings.AllowPortalMovement = v
            ));

            // Stuck Detection Section
            mainPanel.Children.Add(CreateSectionHeader("Stuck Detection"));
            mainPanel.Children.Add(CreateNumericSetting(
                "Stuck Threshold:",
                "Number of checks before considering stuck",
                1, 10, 1,
                () => settings.StuckThreshold,
                v => settings.StuckThreshold = (int)v
            ));

            mainPanel.Children.Add(CreateNumericSetting(
                "Stuck Distance:",
                "Minimum distance to move to not be stuck",
                1, 20, 0.5,
                () => settings.StuckDistance,
                v => settings.StuckDistance = (float)v
            ));

            // Performance & Debug Section
            mainPanel.Children.Add(CreateSectionHeader("Performance & Debug"));
            mainPanel.Children.Add(CreateNumericSetting(
                "Path Refresh Rate (ms):",
                "How often to recalculate paths",
                100, 5000, 100,
                () => settings.PathRefreshRate,
                v => settings.PathRefreshRate = (int)v
            ));

            mainPanel.Children.Add(CreateCheckBoxSetting(
                "Debug Logging",
                "Enable detailed debug logs",
                () => settings.DebugLogging,
                v => settings.DebugLogging = v
            ));

            // Info Panel
            var infoBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(50, 33, 150, 243)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 33, 150, 243)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 15, 0, 0)
            };

            var infoText = new TextBlock
            {
                Text = "ℹ Recommended Settings:\n" +
                       "• Single Use Distance: 25-35 for smooth movement\n" +
                       "• Stuck Threshold: 3-5 for balanced stuck detection\n" +
                       "• Path Refresh: 500-1000ms for best performance",
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.White
            };

            infoBorder.Child = infoText;
            mainPanel.Children.Add(infoBorder);

            this.Content = scrollViewer;
        }

        private UIElement CreateSectionHeader(string text)
        {
            var header = new TextBlock
            {
                Text = text,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 15, 0, 10),
                Foreground = new SolidColorBrush(Color.FromArgb(255, 33, 150, 243))
            };
            return header;
        }

        private UIElement CreateNumericSetting(string label, string tooltip, double min, double max, double interval,
            Func<double> getter, Action<double> setter)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 5, 0, 5) };

            var labelBlock = new TextBlock
            {
                Text = label,
                Margin = new Thickness(0, 0, 0, 5)
            };
            panel.Children.Add(labelBlock);

            var numericUpDown = new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Interval = interval,
                Value = getter(),
                ToolTip = tooltip,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            numericUpDown.ValueChanged += (s, e) =>
            {
                if (numericUpDown.Value.HasValue)
                    setter(numericUpDown.Value.Value);
            };

            panel.Children.Add(numericUpDown);
            return panel;
        }

        private UIElement CreateCheckBoxSetting(string label, string tooltip, Func<bool> getter, Action<bool> setter)
        {
            var checkBox = new CheckBox
            {
                Content = label,
                IsChecked = getter(),
                ToolTip = tooltip,
                Margin = new Thickness(0, 5, 0, 5)
            };

            checkBox.Checked += (s, e) => setter(true);
            checkBox.Unchecked += (s, e) => setter(false);

            return checkBox;
        }
    }
}
