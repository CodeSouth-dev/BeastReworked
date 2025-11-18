using System;
using System.Windows;
using System.Windows.Controls;

namespace BeastCombatRoutine
{
    /// <summary>
    /// GUI for BeastCombatRoutine settings - Pure C# implementation (no XAML)
    /// </summary>
    public class BeastCombatRoutineGui : UserControl
    {
        // Combat Skill ComboBoxes
        private ComboBox SingleTargetMeleeCombo;
        private ComboBox SingleTargetRangedCombo;
        private ComboBox AoeMeleeCombo;
        private ComboBox AoeRangedCombo;
        private ComboBox FallbackCombo;

        // Buff Skill ComboBoxes
        private ComboBox BuffSlot1Combo;
        private ComboBox BuffSlot2Combo;
        private ComboBox BuffSlot3Combo;

        // Range TextBoxes
        private TextBox CombatRangeBox;
        private TextBox MaxMeleeRangeBox;
        private TextBox MaxRangeRangeBox;
        private TextBox AoePackSizeBox;

        // CheckBoxes
        private CheckBox AlwaysAttackInPlaceCheck;
        private CheckBox EnableZoomModeCheck;
        private CheckBox AutoDetectSkillsCheck;

        private Button SaveButton;

        public BeastCombatRoutineGui()
        {
            InitializeControls();
            LoadSettings();
        }

        private void InitializeControls()
        {
            // Create scroll viewer
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            // Create main stack panel
            var mainPanel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            // Header
            var header = new TextBlock
            {
                Text = "Beast Combat Routine Settings",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            mainPanel.Children.Add(header);

            // Combat Skills Group
            mainPanel.Children.Add(CreateCombatSkillsGroup());

            // Buff Skills Group
            mainPanel.Children.Add(CreateBuffSkillsGroup());

            // Combat Settings Group
            mainPanel.Children.Add(CreateCombatSettingsGroup());

            // Information Group
            mainPanel.Children.Add(CreateInfoGroup());

            // Save Button
            SaveButton = new Button
            {
                Content = "Save Settings",
                Width = 100,
                Height = 25,
                Margin = new Thickness(0, 10, 0, 0)
            };
            SaveButton.Click += SaveButton_Click;
            mainPanel.Children.Add(SaveButton);

            scrollViewer.Content = mainPanel;
            this.Content = scrollViewer;
        }

        private GroupBox CreateCombatSkillsGroup()
        {
            var group = new GroupBox
            {
                Header = "Combat Skills",
                Margin = new Thickness(0, 0, 0, 10)
            };

            var panel = new StackPanel { Margin = new Thickness(5) };

            // Single Target Melee
            panel.Children.Add(CreateSkillRow("Single Target (Melee):", out SingleTargetMeleeCombo,
                "Skill to use in melee range against single targets"));

            // Single Target Ranged
            panel.Children.Add(CreateSkillRow("Single Target (Ranged):", out SingleTargetRangedCombo,
                "Skill to use outside melee range against single targets"));

            // AOE Melee
            panel.Children.Add(CreateSkillRow("AOE (Melee):", out AoeMeleeCombo,
                "Skill to use in melee range against packs"));

            // AOE Ranged
            panel.Children.Add(CreateSkillRow("AOE (Ranged):", out AoeRangedCombo,
                "Skill to use outside melee range against packs"));

            // Fallback
            panel.Children.Add(CreateSkillRow("Fallback Skill:", out FallbackCombo,
                "Skill to use if primary skill cannot be cast"));

            group.Content = panel;
            return group;
        }

        private GroupBox CreateBuffSkillsGroup()
        {
            var group = new GroupBox
            {
                Header = "Buff Skills",
                Margin = new Thickness(0, 0, 0, 10)
            };

            var panel = new StackPanel { Margin = new Thickness(5) };

            // Buff Slot 1
            panel.Children.Add(CreateSkillRow("Buff Slot 1:", out BuffSlot1Combo,
                "First buff skill (auras, buffs, etc)"));

            // Buff Slot 2
            panel.Children.Add(CreateSkillRow("Buff Slot 2:", out BuffSlot2Combo,
                "Second buff skill"));

            // Buff Slot 3
            panel.Children.Add(CreateSkillRow("Buff Slot 3:", out BuffSlot3Combo,
                "Third buff skill"));

            group.Content = panel;
            return group;
        }

        private GroupBox CreateCombatSettingsGroup()
        {
            var group = new GroupBox
            {
                Header = "Combat Settings",
                Margin = new Thickness(0, 0, 0, 10)
            };

            var panel = new StackPanel { Margin = new Thickness(5) };

            // Combat Range
            var combatRangePanel = new DockPanel { Margin = new Thickness(0, 5, 0, 5) };
            combatRangePanel.Children.Add(new Label { Content = "Combat Range:", Width = 150 });
            CombatRangeBox = new TextBox { Width = 50, ToolTip = "Only attack mobs within this range" };
            combatRangePanel.Children.Add(CombatRangeBox);
            panel.Children.Add(combatRangePanel);

            // Max Melee Range
            var meleeRangePanel = new DockPanel { Margin = new Thickness(0, 5, 0, 5) };
            meleeRangePanel.Children.Add(new Label { Content = "Max Melee Range:", Width = 150 });
            MaxMeleeRangeBox = new TextBox { Width = 50, ToolTip = "Distance to trigger melee skills" };
            meleeRangePanel.Children.Add(MaxMeleeRangeBox);
            panel.Children.Add(meleeRangePanel);

            // Max Range Range
            var rangeRangePanel = new DockPanel { Margin = new Thickness(0, 5, 0, 5) };
            rangeRangePanel.Children.Add(new Label { Content = "Max Range Range:", Width = 150 });
            MaxRangeRangeBox = new TextBox { Width = 50, ToolTip = "Distance to trigger ranged skills" };
            rangeRangePanel.Children.Add(MaxRangeRangeBox);
            panel.Children.Add(rangeRangePanel);

            // AOE Pack Size
            var aoePackPanel = new DockPanel { Margin = new Thickness(0, 5, 0, 5) };
            aoePackPanel.Children.Add(new Label { Content = "AOE Pack Size:", Width = 150 });
            AoePackSizeBox = new TextBox { Width = 50, ToolTip = "Minimum mobs near target to use AOE skills" };
            aoePackPanel.Children.Add(AoePackSizeBox);
            panel.Children.Add(aoePackPanel);

            // Always Attack In Place
            AlwaysAttackInPlaceCheck = new CheckBox
            {
                Content = "Always Attack In Place",
                Margin = new Thickness(0, 5, 0, 5),
                ToolTip = "Hold Shift when attacking to prevent movement"
            };
            panel.Children.Add(AlwaysAttackInPlaceCheck);

            // Enable Zoom Mode
            EnableZoomModeCheck = new CheckBox
            {
                Content = "Enable Zoom Mode (Beast Farming)",
                Margin = new Thickness(0, 5, 0, 5),
                ToolTip = "When enabled, only fights near beasts/caches (zooms past regular mobs). When disabled, fights all enemies."
            };
            panel.Children.Add(EnableZoomModeCheck);

            // Auto-Detect Skills
            AutoDetectSkillsCheck = new CheckBox
            {
                Content = "Auto-Detect Skills on Start",
                Margin = new Thickness(0, 5, 0, 5),
                ToolTip = "Automatically scan and assign skills from skillbar when bot starts"
            };
            panel.Children.Add(AutoDetectSkillsCheck);

            group.Content = panel;
            return group;
        }

        private StackPanel CreateSkillRow(string label, out ComboBox combo, string tooltip)
        {
            var panel = new DockPanel { Margin = new Thickness(0, 5, 0, 5) };

            var labelControl = new Label { Content = label, Width = 180 };
            panel.Children.Add(labelControl);

            combo = new ComboBox
            {
                Width = 100,
                ToolTip = tooltip
            };
            combo.ItemsSource = BeastCombatRoutineSettings.AllKeybinds;
            panel.Children.Add(combo);

            var stackPanel = new StackPanel();
            stackPanel.Children.Add(panel);
            return stackPanel;
        }

        private GroupBox CreateInfoGroup()
        {
            var group = new GroupBox
            {
                Header = "Information",
                Margin = new Thickness(0, 0, 0, 10)
            };

            var panel = new StackPanel { Margin = new Thickness(5) };

            panel.Children.Add(new TextBlock
            {
                Text = "BeastCombatRoutine - Context-Aware Combat",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            });
            panel.Children.Add(new TextBlock
            {
                Text = "• Zoom Mode: Only fights near beasts/caches (default)",
                Margin = new Thickness(10, 0, 0, 2)
            });
            panel.Children.Add(new TextBlock
            {
                Text = "• Zooms past regular mobs for fast beast farming",
                Margin = new Thickness(10, 0, 0, 2)
            });
            panel.Children.Add(new TextBlock
            {
                Text = "• Uses AOE skills when pack size threshold met",
                Margin = new Thickness(10, 0, 0, 10)
            });

            panel.Children.Add(new TextBlock
            {
                Text = "IMPORTANT - Keybind Setup:",
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.Red,
                Margin = new Thickness(0, 5, 0, 5)
            });
            panel.Children.Add(new TextBlock
            {
                Text = "DO NOT use mouse buttons for attack skills!",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10, 0, 0, 2)
            });
            panel.Children.Add(new TextBlock
            {
                Text = "Use keyboard keys only (Q, W, E, R, T, Spacebar, Y, etc.)",
                Margin = new Thickness(10, 0, 0, 2)
            });
            panel.Children.Add(new TextBlock
            {
                Text = "Set Left Click to 'Move Only' in PoE (no attack skill)",
                Margin = new Thickness(10, 0, 0, 10)
            });

            panel.Children.Add(new TextBlock
            {
                Text = "How to Configure:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 5, 0, 5)
            });
            panel.Children.Add(new TextBlock
            {
                Text = "1. In-game: Bind your skills to keyboard keys",
                Margin = new Thickness(10, 0, 0, 2)
            });
            panel.Children.Add(new TextBlock
            {
                Text = "2. In GUI: Select the keybind you used in-game",
                Margin = new Thickness(10, 0, 0, 2)
            });
            panel.Children.Add(new TextBlock
            {
                Text = "Example: If you bound Cyclone to Spacebar, select 'Spacebar'",
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(10, 0, 0, 2)
            });

            group.Content = panel;
            return group;
        }

        private void LoadSettings()
        {
            var settings = BeastCombatRoutineSettings.Instance;

            // Combat Skills - Convert slot numbers to keybind names
            SingleTargetMeleeCombo.SelectedItem = BeastCombatRoutineSettings.SlotToKeybind(settings.SingleTargetMeleeSlot);
            SingleTargetRangedCombo.SelectedItem = BeastCombatRoutineSettings.SlotToKeybind(settings.SingleTargetRangedSlot);
            AoeMeleeCombo.SelectedItem = BeastCombatRoutineSettings.SlotToKeybind(settings.AoeMeleeSlot);
            AoeRangedCombo.SelectedItem = BeastCombatRoutineSettings.SlotToKeybind(settings.AoeRangedSlot);
            FallbackCombo.SelectedItem = BeastCombatRoutineSettings.SlotToKeybind(settings.FallbackSlot);

            // Buff Skills - Convert slot numbers to keybind names
            BuffSlot1Combo.SelectedItem = BeastCombatRoutineSettings.SlotToKeybind(settings.BuffSlot1);
            BuffSlot2Combo.SelectedItem = BeastCombatRoutineSettings.SlotToKeybind(settings.BuffSlot2);
            BuffSlot3Combo.SelectedItem = BeastCombatRoutineSettings.SlotToKeybind(settings.BuffSlot3);

            // Combat Settings
            CombatRangeBox.Text = settings.CombatRange.ToString();
            MaxMeleeRangeBox.Text = settings.MaxMeleeRange.ToString();
            MaxRangeRangeBox.Text = settings.MaxRangeRange.ToString();
            AoePackSizeBox.Text = settings.AoePackSize.ToString();
            AlwaysAttackInPlaceCheck.IsChecked = settings.AlwaysAttackInPlace;
            EnableZoomModeCheck.IsChecked = settings.EnableZoomMode;
            AutoDetectSkillsCheck.IsChecked = settings.AutoDetectSkills;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = BeastCombatRoutineSettings.Instance;

                // Save Combat Skills - Convert keybind names to slot numbers
                var stMelee = SingleTargetMeleeCombo.SelectedItem as string ?? "None";
                settings.SingleTargetMeleeSlot = BeastCombatRoutineSettings.KeybindToSlot.ContainsKey(stMelee)
                    ? BeastCombatRoutineSettings.KeybindToSlot[stMelee] : -1;

                var stRanged = SingleTargetRangedCombo.SelectedItem as string ?? "None";
                settings.SingleTargetRangedSlot = BeastCombatRoutineSettings.KeybindToSlot.ContainsKey(stRanged)
                    ? BeastCombatRoutineSettings.KeybindToSlot[stRanged] : -1;

                var aoeMelee = AoeMeleeCombo.SelectedItem as string ?? "None";
                settings.AoeMeleeSlot = BeastCombatRoutineSettings.KeybindToSlot.ContainsKey(aoeMelee)
                    ? BeastCombatRoutineSettings.KeybindToSlot[aoeMelee] : -1;

                var aoeRanged = AoeRangedCombo.SelectedItem as string ?? "None";
                settings.AoeRangedSlot = BeastCombatRoutineSettings.KeybindToSlot.ContainsKey(aoeRanged)
                    ? BeastCombatRoutineSettings.KeybindToSlot[aoeRanged] : -1;

                var fallback = FallbackCombo.SelectedItem as string ?? "None";
                settings.FallbackSlot = BeastCombatRoutineSettings.KeybindToSlot.ContainsKey(fallback)
                    ? BeastCombatRoutineSettings.KeybindToSlot[fallback] : -1;

                // Save Buff Skills - Convert keybind names to slot numbers
                var buff1 = BuffSlot1Combo.SelectedItem as string ?? "None";
                settings.BuffSlot1 = BeastCombatRoutineSettings.KeybindToSlot.ContainsKey(buff1)
                    ? BeastCombatRoutineSettings.KeybindToSlot[buff1] : -1;

                var buff2 = BuffSlot2Combo.SelectedItem as string ?? "None";
                settings.BuffSlot2 = BeastCombatRoutineSettings.KeybindToSlot.ContainsKey(buff2)
                    ? BeastCombatRoutineSettings.KeybindToSlot[buff2] : -1;

                var buff3 = BuffSlot3Combo.SelectedItem as string ?? "None";
                settings.BuffSlot3 = BeastCombatRoutineSettings.KeybindToSlot.ContainsKey(buff3)
                    ? BeastCombatRoutineSettings.KeybindToSlot[buff3] : -1;

                // Save Combat Settings
                if (int.TryParse(CombatRangeBox.Text, out int combatRange))
                    settings.CombatRange = combatRange;

                if (int.TryParse(MaxMeleeRangeBox.Text, out int maxMelee))
                    settings.MaxMeleeRange = maxMelee;

                if (int.TryParse(MaxRangeRangeBox.Text, out int maxRange))
                    settings.MaxRangeRange = maxRange;

                if (int.TryParse(AoePackSizeBox.Text, out int aoeSize))
                    settings.AoePackSize = aoeSize;

                settings.AlwaysAttackInPlace = AlwaysAttackInPlaceCheck.IsChecked ?? false;
                settings.EnableZoomMode = EnableZoomModeCheck.IsChecked ?? true;
                settings.AutoDetectSkills = AutoDetectSkillsCheck.IsChecked ?? true;

                settings.Save();

                MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
