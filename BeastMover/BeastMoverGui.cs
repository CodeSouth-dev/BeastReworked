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
                "Move Range:",
                "Maximum range for basic movement clicks",
                10, 60, 1,
                () => settings.MoveRange,
                v => settings.MoveRange = (int)v
            ));

            mainPanel.Children.Add(CreateNumericSetting(
                "Single Use Distance:",
                "Minimum distance to trigger movement skills",
                5, 45, 1,
                () => settings.SingleUseDistance,
                v => settings.SingleUseDistance = (int)v
            ));

            mainPanel.Children.Add(CreateNumericSetting(
                "Move Min Mana:",
                "Minimum mana required to use movement skills",
                0, 100, 5,
                () => settings.MoveMinMana,
                v => settings.MoveMinMana = (int)v
            ));

            mainPanel.Children.Add(CreateCheckBoxSetting(
                "Ignore Mobs",
                "Ignore mobs when calculating movement path",
                () => settings.IgnoreMobs,
                v => settings.IgnoreMobs = v
            ));

            mainPanel.Children.Add(CreateCheckBoxSetting(
                "Use Blood Magic",
                "Use life instead of mana for movement skills",
                () => settings.UseBloodMagic,
                v => settings.UseBloodMagic = v
            ));

            mainPanel.Children.Add(CreateCheckBoxSetting(
                "Allow Portal Movement",
                "Leave ON for map running - SimpleMapBot handles when to enter portals",
                () => settings.AllowPortalMovement,
                v => settings.AllowPortalMovement = v
            ));

            // Movement Skills Section
            mainPanel.Children.Add(CreateSectionHeader("Movement Skills"));

            // Whirling Blades
            mainPanel.Children.Add(CreateSkillSetting(
                "Whirling Blades",
                () => settings.EnableWhirlingBlades,
                v => settings.EnableWhirlingBlades = v,
                () => settings.WhirlingBladesMinDist,
                v => settings.WhirlingBladesMinDist = (int)v,
                () => settings.WhirlingBladesMaxDist,
                v => settings.WhirlingBladesMaxDist = (int)v
            ));

            // Flame Dash
            mainPanel.Children.Add(CreateSkillSetting(
                "Flame Dash",
                () => settings.EnableFlameDash,
                v => settings.EnableFlameDash = v,
                () => settings.FlameDashMinDist,
                v => settings.FlameDashMinDist = (int)v,
                () => settings.FlameDashMaxDist,
                v => settings.FlameDashMaxDist = (int)v
            ));

            // Shield Charge
            mainPanel.Children.Add(CreateSkillSetting(
                "Shield Charge",
                () => settings.EnableShieldCharge,
                v => settings.EnableShieldCharge = v,
                () => settings.ShieldChargeMinDist,
                v => settings.ShieldChargeMinDist = (int)v,
                () => settings.ShieldChargeMaxDist,
                v => settings.ShieldChargeMaxDist = (int)v
            ));

            // Leap Slam
            mainPanel.Children.Add(CreateSkillSetting(
                "Leap Slam",
                () => settings.EnableLeapSlam,
                v => settings.EnableLeapSlam = v,
                () => settings.LeapSlamMinDist,
                v => settings.LeapSlamMinDist = (int)v,
                () => settings.LeapSlamMaxDist,
                v => settings.LeapSlamMaxDist = (int)v
            ));

            // Dash
            mainPanel.Children.Add(CreateSkillSetting(
                "Dash",
                () => settings.EnableDash,
                v => settings.EnableDash = v,
                () => settings.DashMinDist,
                v => settings.DashMinDist = (int)v,
                () => settings.DashMaxDist,
                v => settings.DashMaxDist = (int)v
            ));

            // Frostblink
            mainPanel.Children.Add(CreateSkillSetting(
                "Frostblink",
                () => settings.EnableFrostblink,
                v => settings.EnableFrostblink = v,
                () => settings.FrostblinkMinDist,
                v => settings.FrostblinkMinDist = (int)v,
                () => settings.FrostblinkMaxDist,
                v => settings.FrostblinkMaxDist = (int)v
            ));

            // Lightning Warp
            mainPanel.Children.Add(CreateSkillSetting(
                "Lightning Warp",
                () => settings.EnableLightningWarp,
                v => settings.EnableLightningWarp = v,
                () => settings.LightningWarpMinDist,
                v => settings.LightningWarpMinDist = (int)v,
                () => settings.LightningWarpMaxDist,
                v => settings.LightningWarpMaxDist = (int)v
            ));

            // Blink Arrow
            mainPanel.Children.Add(CreateSkillSetting(
                "Blink Arrow",
                () => settings.EnableBlinkArrow,
                v => settings.EnableBlinkArrow = v,
                () => settings.BlinkArrowMinDist,
                v => settings.BlinkArrowMinDist = (int)v,
                () => settings.BlinkArrowMaxDist,
                v => settings.BlinkArrowMaxDist = (int)v
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
                Text = "ℹ How to Use:\n" +
                       "• Enable movement skills you have on your skillbar\n" +
                       "• Set Min/Max distance ranges for each skill\n" +
                       "• Lower Min = skill triggers sooner (more often)\n" +
                       "• Higher Max = skill can travel further\n" +
                       "• Leave 'Allow Portal Movement' ON for map running\n\n" +
                       "Recommended:\n" +
                       "• Move Range: 30-35 | Single Use: 25-35\n" +
                       "• Min Mana: 20-40 | Stuck Threshold: 3-5",
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

        private UIElement CreateSkillSetting(string skillName,
            Func<bool> enableGetter, Action<bool> enableSetter,
            Func<int> minGetter, Action<double> minSetter,
            Func<int> maxGetter, Action<double> maxSetter)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 8, 0, 8) };

            // Enable checkbox
            var enableCheckbox = new CheckBox
            {
                Content = $"Enable {skillName}",
                IsChecked = enableGetter(),
                FontWeight = FontWeights.Bold
            };
            enableCheckbox.Checked += (s, e) => enableSetter(true);
            enableCheckbox.Unchecked += (s, e) => enableSetter(false);
            panel.Children.Add(enableCheckbox);

            // Min/Max distance panel
            var distPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(20, 5, 0, 0)
            };

            // Min distance
            distPanel.Children.Add(new TextBlock
            {
                Text = "Min:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0),
                Width = 35
            });

            var minNumeric = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 120,
                Interval = 1,
                Value = minGetter(),
                Width = 80,
                Margin = new Thickness(0, 0, 15, 0)
            };
            minNumeric.ValueChanged += (s, e) =>
            {
                if (minNumeric.Value.HasValue)
                    minSetter(minNumeric.Value.Value);
            };
            distPanel.Children.Add(minNumeric);

            // Max distance
            distPanel.Children.Add(new TextBlock
            {
                Text = "Max:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0),
                Width = 35
            });

            var maxNumeric = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 120,
                Interval = 1,
                Value = maxGetter(),
                Width = 80
            };
            maxNumeric.ValueChanged += (s, e) =>
            {
                if (maxNumeric.Value.HasValue)
                    maxSetter(maxNumeric.Value.Value);
            };
            distPanel.Children.Add(maxNumeric);

            panel.Children.Add(distPanel);

            return panel;
        }
    }
}
