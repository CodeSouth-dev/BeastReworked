using System;
using System.Windows;
using System.Windows.Controls;
using Beasts.Configuration;

namespace Beasts.Core
{
    /// <summary>
    /// GUI for BeastRoutine settings - Pure C# implementation (no XAML)
    /// </summary>
    public class BeastRoutineGui : UserControl
    {
        // Beast Capture Controls
        private CheckBox BeastCaptureEnabled;
        private CheckBox CaptureUnique;
        private CheckBox CaptureRare;
        private CheckBox CaptureMagic;
        private CheckBox CaptureNormal;
        private TextBox BeastDetectionRange;
        private ComboBox FilterModeCombo;

        // Cache Controls
        private CheckBox CacheEnabled;
        private TextBox CacheDetectionRange;

        // Combat Controls
        private TextBox CombatRange;
        private TextBox MaxMeleeRange;
        private TextBox MaxRangeRange;

        // Flask Controls
        private CheckBox UseFlasksInCombat;
        private TextBox LifeFlaskPercent;
        private TextBox ManaFlaskPercent;
        private CheckBox UseQuicksilver;
        private CheckBox UseDefensiveFlasks;
        private TextBox DefensiveFlaskPercent;
        private CheckBox UseUtilityFlasks;
        private CheckBox UseOffensiveFlasks;
        private CheckBox UseOffensiveFlasksOnRares;
        private CheckBox UseOffensiveFlasksOnBosses;
        private CheckBox UseTincture;
        private TextBox TinctureKeybind;
        private CheckBox UseTinctureOnRares;
        private CheckBox UseTinctureOnBosses;

        // Loot Controls
        private CheckBox PickupCurrency;
        private CheckBox PickupBlueprints;
        private CheckBox PickupContracts;
        private CheckBox PickupUniques;
        private CheckBox PickupMaps;
        private CheckBox PickupDivinationCards;
        private TextBox MaxLootRange;

        // Exit Condition Controls
        private CheckBox ExitOnInventoryFull;
        private CheckBox ExitOnLowResources;
        private CheckBox ExitOnLowPortalScrolls;
        private TextBox MinHealthPercent;
        private TextBox MinFlasks;
        private TextBox MinPortalScrolls;

        // Buttons
        private Button SaveButton;
        private Button LoadButton;

        public BeastRoutineGui()
        {
            InitializeControls();
            LoadSettings();
        }

        private void InitializeControls()
        {
            // Create main scroll viewer
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
                Text = "Beast Routine Settings",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            mainPanel.Children.Add(header);

            // Beast Capture Settings
            mainPanel.Children.Add(CreateBeastCaptureGroup());

            // Cache Settings
            mainPanel.Children.Add(CreateCacheGroup());

            // Combat Settings
            mainPanel.Children.Add(CreateCombatGroup());

            // Flask Settings
            mainPanel.Children.Add(CreateFlaskGroup());

            // Loot Settings
            mainPanel.Children.Add(CreateLootGroup());

            // Exit Conditions
            mainPanel.Children.Add(CreateExitConditionsGroup());

            // Buttons
            mainPanel.Children.Add(CreateButtonPanel());

            scrollViewer.Content = mainPanel;
            this.Content = scrollViewer;
        }

        private GroupBox CreateBeastCaptureGroup()
        {
            var panel = new StackPanel();

            BeastCaptureEnabled = new CheckBox { Content = "Enable Beast Farming", Margin = new Thickness(5) };
            panel.Children.Add(BeastCaptureEnabled);

            // Filter Mode
            var filterPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
            filterPanel.Children.Add(new TextBlock { Text = "Filter Mode:", VerticalAlignment = VerticalAlignment.Center, Width = 150 });
            FilterModeCombo = new ComboBox { Width = 150 };
            FilterModeCombo.Items.Add("Capture All");
            FilterModeCombo.Items.Add("Whitelist (Valuable Only)");
            FilterModeCombo.Items.Add("Blacklist");
            FilterModeCombo.SelectedIndex = 1; // Default to Whitelist
            filterPanel.Children.Add(FilterModeCombo);
            panel.Children.Add(filterPanel);

            // Rarity filters
            var rarityLabel = new TextBlock
            {
                Text = "Rarity Filter (Red = Rare, Yellow = Magic):",
                Margin = new Thickness(5, 10, 5, 5),
                FontWeight = FontWeights.SemiBold
            };
            panel.Children.Add(rarityLabel);

            CaptureUnique = new CheckBox { Content = "Capture Unique Beasts", Margin = new Thickness(5) };
            CaptureRare = new CheckBox { Content = "Capture Rare Beasts (RED - Valuable)", Margin = new Thickness(5) };
            CaptureMagic = new CheckBox { Content = "Capture Magic Beasts (Yellow)", Margin = new Thickness(5) };
            CaptureNormal = new CheckBox { Content = "Capture Normal Beasts", Margin = new Thickness(5) };

            panel.Children.Add(CaptureUnique);
            panel.Children.Add(CaptureRare);
            panel.Children.Add(CaptureMagic);
            panel.Children.Add(CaptureNormal);

            // Detection Range
            var rangePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
            rangePanel.Children.Add(new TextBlock { Text = "Detection Range:", VerticalAlignment = VerticalAlignment.Center, Width = 150 });
            BeastDetectionRange = new TextBox { Width = 60, Text = "80" };
            rangePanel.Children.Add(BeastDetectionRange);
            panel.Children.Add(rangePanel);

            return new GroupBox { Header = "Beast Capture", Margin = new Thickness(0, 0, 0, 10), Content = panel };
        }

        private GroupBox CreateCacheGroup()
        {
            var panel = new StackPanel();

            CacheEnabled = new CheckBox { Content = "Enable Cache Farming", Margin = new Thickness(5) };
            panel.Children.Add(CacheEnabled);

            var rangePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
            rangePanel.Children.Add(new TextBlock { Text = "Detection Range:", VerticalAlignment = VerticalAlignment.Center, Width = 150 });
            CacheDetectionRange = new TextBox { Width = 60, Text = "80" };
            rangePanel.Children.Add(CacheDetectionRange);
            panel.Children.Add(rangePanel);

            return new GroupBox { Header = "Smuggler's Cache", Margin = new Thickness(0, 0, 0, 10), Content = panel };
        }

        private GroupBox CreateCombatGroup()
        {
            var panel = new StackPanel();

            // Combat Range
            var combatRangePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
            combatRangePanel.Children.Add(new TextBlock { Text = "Combat Range:", VerticalAlignment = VerticalAlignment.Center, Width = 150 });
            CombatRange = new TextBox { Width = 60, Text = "60" };
            combatRangePanel.Children.Add(CombatRange);
            panel.Children.Add(combatRangePanel);

            // Max Melee Range
            var meleePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
            meleePanel.Children.Add(new TextBlock { Text = "Max Melee Range:", VerticalAlignment = VerticalAlignment.Center, Width = 150 });
            MaxMeleeRange = new TextBox { Width = 60, Text = "30" };
            meleePanel.Children.Add(MaxMeleeRange);
            panel.Children.Add(meleePanel);

            // Max Ranged Range
            var rangedPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
            rangedPanel.Children.Add(new TextBlock { Text = "Max Ranged Range:", VerticalAlignment = VerticalAlignment.Center, Width = 150 });
            MaxRangeRange = new TextBox { Width = 60, Text = "60" };
            rangedPanel.Children.Add(MaxRangeRange);
            panel.Children.Add(rangedPanel);

            return new GroupBox { Header = "Combat Ranges", Margin = new Thickness(0, 0, 0, 10), Content = panel };
        }

        private GroupBox CreateFlaskGroup()
        {
            var panel = new StackPanel();

            UseFlasksInCombat = new CheckBox { Content = "Use Flasks in Combat", Margin = new Thickness(5) };
            panel.Children.Add(UseFlasksInCombat);

            // Life Flask
            var lifePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
            lifePanel.Children.Add(new TextBlock { Text = "Use Life Flask Below %:", VerticalAlignment = VerticalAlignment.Center, Width = 180 });
            LifeFlaskPercent = new TextBox { Width = 60, Text = "50" };
            lifePanel.Children.Add(LifeFlaskPercent);
            panel.Children.Add(lifePanel);

            // Mana Flask
            var manaPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
            manaPanel.Children.Add(new TextBlock { Text = "Use Mana Flask Below %:", VerticalAlignment = VerticalAlignment.Center, Width = 180 });
            ManaFlaskPercent = new TextBox { Width = 60, Text = "30" };
            manaPanel.Children.Add(ManaFlaskPercent);
            panel.Children.Add(manaPanel);

            // Utility Flasks
            var utilLabel = new TextBlock
            {
                Text = "Utility Flasks:",
                Margin = new Thickness(5, 10, 5, 5),
                FontWeight = FontWeights.SemiBold
            };
            panel.Children.Add(utilLabel);

            UseQuicksilver = new CheckBox { Content = "Use Quicksilver Flask", Margin = new Thickness(5) };
            panel.Children.Add(UseQuicksilver);

            UseUtilityFlasks = new CheckBox { Content = "Use Utility Flasks (Diamond, Silver, etc.)", Margin = new Thickness(5) };
            panel.Children.Add(UseUtilityFlasks);

            UseDefensiveFlasks = new CheckBox { Content = "Use Defensive Flasks", Margin = new Thickness(5) };
            panel.Children.Add(UseDefensiveFlasks);

            var defPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
            defPanel.Children.Add(new TextBlock { Text = "Use Defensive Below %:", VerticalAlignment = VerticalAlignment.Center, Width = 180 });
            DefensiveFlaskPercent = new TextBox { Width = 60, Text = "60" };
            defPanel.Children.Add(DefensiveFlaskPercent);
            panel.Children.Add(defPanel);

            // Offensive Flasks
            var offensiveLabel = new TextBlock
            {
                Text = "Offensive Flasks (Beast/Boss Fights):",
                Margin = new Thickness(5, 10, 5, 5),
                FontWeight = FontWeights.SemiBold
            };
            panel.Children.Add(offensiveLabel);

            UseOffensiveFlasks = new CheckBox { Content = "Use Offensive Flasks (Diamond, Silver, Sulphur)", Margin = new Thickness(5) };
            panel.Children.Add(UseOffensiveFlasks);

            UseOffensiveFlasksOnRares = new CheckBox { Content = "Use on Rare Beasts", Margin = new Thickness(5) };
            panel.Children.Add(UseOffensiveFlasksOnRares);

            UseOffensiveFlasksOnBosses = new CheckBox { Content = "Use on Unique/Boss Beasts", Margin = new Thickness(5) };
            panel.Children.Add(UseOffensiveFlasksOnBosses);

            // Tinctures
            var tinctureLabel = new TextBlock
            {
                Text = "Tincture (Rosethorn, etc.):",
                Margin = new Thickness(5, 10, 5, 5),
                FontWeight = FontWeights.SemiBold
            };
            panel.Children.Add(tinctureLabel);

            UseTincture = new CheckBox { Content = "Use Tincture in Combat", Margin = new Thickness(5) };
            panel.Children.Add(UseTincture);

            var tinctureKeyPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
            tinctureKeyPanel.Children.Add(new TextBlock { Text = "Tincture Keybind (1-5):", VerticalAlignment = VerticalAlignment.Center, Width = 180 });
            TinctureKeybind = new TextBox { Width = 60, Text = "5" };
            tinctureKeyPanel.Children.Add(TinctureKeybind);
            panel.Children.Add(tinctureKeyPanel);

            UseTinctureOnRares = new CheckBox { Content = "Use on Rare Beasts", Margin = new Thickness(5) };
            panel.Children.Add(UseTinctureOnRares);

            UseTinctureOnBosses = new CheckBox { Content = "Use on Unique/Boss Beasts", Margin = new Thickness(5) };
            panel.Children.Add(UseTinctureOnBosses);

            return new GroupBox { Header = "Flask Configuration", Margin = new Thickness(0, 0, 0, 10), Content = panel };
        }

        private GroupBox CreateLootGroup()
        {
            var panel = new StackPanel();

            PickupCurrency = new CheckBox { Content = "Pickup Currency", Margin = new Thickness(5) };
            PickupBlueprints = new CheckBox { Content = "Pickup Blueprints", Margin = new Thickness(5) };
            PickupContracts = new CheckBox { Content = "Pickup Contracts", Margin = new Thickness(5) };
            PickupUniques = new CheckBox { Content = "Pickup Uniques", Margin = new Thickness(5) };
            PickupMaps = new CheckBox { Content = "Pickup Maps", Margin = new Thickness(5) };
            PickupDivinationCards = new CheckBox { Content = "Pickup Divination Cards", Margin = new Thickness(5) };

            panel.Children.Add(PickupCurrency);
            panel.Children.Add(PickupBlueprints);
            panel.Children.Add(PickupContracts);
            panel.Children.Add(PickupUniques);
            panel.Children.Add(PickupMaps);
            panel.Children.Add(PickupDivinationCards);

            var rangePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
            rangePanel.Children.Add(new TextBlock { Text = "Loot Range:", VerticalAlignment = VerticalAlignment.Center, Width = 150 });
            MaxLootRange = new TextBox { Width = 60, Text = "40" };
            rangePanel.Children.Add(MaxLootRange);
            panel.Children.Add(rangePanel);

            return new GroupBox { Header = "Loot", Margin = new Thickness(0, 0, 0, 10), Content = panel };
        }

        private GroupBox CreateExitConditionsGroup()
        {
            var panel = new StackPanel();

            ExitOnInventoryFull = new CheckBox { Content = "Exit When Inventory Full", Margin = new Thickness(5) };
            ExitOnLowResources = new CheckBox { Content = "Exit On Low Resources", Margin = new Thickness(5) };
            ExitOnLowPortalScrolls = new CheckBox { Content = "Exit When Low on Portal Scrolls", Margin = new Thickness(5) };

            panel.Children.Add(ExitOnInventoryFull);
            panel.Children.Add(ExitOnLowResources);
            panel.Children.Add(ExitOnLowPortalScrolls);

            // Min Health %
            var healthPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
            healthPanel.Children.Add(new TextBlock { Text = "Min Health %:", VerticalAlignment = VerticalAlignment.Center, Width = 150 });
            MinHealthPercent = new TextBox { Width = 60, Text = "10" };
            healthPanel.Children.Add(MinHealthPercent);
            panel.Children.Add(healthPanel);

            // Min Flasks
            var flasksPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
            flasksPanel.Children.Add(new TextBlock { Text = "Min Flasks:", VerticalAlignment = VerticalAlignment.Center, Width = 150 });
            MinFlasks = new TextBox { Width = 60, Text = "1" };
            flasksPanel.Children.Add(MinFlasks);
            panel.Children.Add(flasksPanel);

            // Min Portal Scrolls
            var portalPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
            portalPanel.Children.Add(new TextBlock { Text = "Min Portal Scrolls:", VerticalAlignment = VerticalAlignment.Center, Width = 150 });
            MinPortalScrolls = new TextBox { Width = 60, Text = "1" };
            portalPanel.Children.Add(MinPortalScrolls);
            panel.Children.Add(portalPanel);

            return new GroupBox { Header = "Exit Conditions", Margin = new Thickness(0, 0, 0, 10), Content = panel };
        }

        private StackPanel CreateButtonPanel()
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };

            SaveButton = new Button { Content = "Save", Width = 80, Margin = new Thickness(5) };
            SaveButton.Click += SaveButton_Click;
            panel.Children.Add(SaveButton);

            LoadButton = new Button { Content = "Load", Width = 80, Margin = new Thickness(5) };
            LoadButton.Click += LoadButton_Click;
            panel.Children.Add(LoadButton);

            return panel;
        }

        private void LoadSettings()
        {
            var settings = BeastRoutineSettings.Instance;

            // Beast Capture
            BeastCaptureEnabled.IsChecked = settings.BeastCapture.Enabled;
            CaptureUnique.IsChecked = settings.BeastCapture.CaptureUnique;
            CaptureRare.IsChecked = settings.BeastCapture.CaptureRare;
            CaptureMagic.IsChecked = settings.BeastCapture.CaptureMagic;
            CaptureNormal.IsChecked = settings.BeastCapture.CaptureNormal;
            BeastDetectionRange.Text = settings.BeastCapture.MaxDetectionRange.ToString();

            // Filter mode
            switch (settings.BeastCapture.FilterMode)
            {
                case BeastFilterMode.CaptureAll:
                    FilterModeCombo.SelectedIndex = 0;
                    break;
                case BeastFilterMode.Whitelist:
                    FilterModeCombo.SelectedIndex = 1;
                    break;
                case BeastFilterMode.Blacklist:
                    FilterModeCombo.SelectedIndex = 2;
                    break;
            }

            // Cache
            CacheEnabled.IsChecked = settings.CacheSettings.Enabled;
            CacheDetectionRange.Text = settings.CacheSettings.MaxDetectionRange.ToString();

            // Combat
            CombatRange.Text = settings.Combat.CombatRange.ToString();
            MaxMeleeRange.Text = settings.Combat.MaxMeleeRange.ToString();
            MaxRangeRange.Text = settings.Combat.MaxRangeRange.ToString();

            // Flasks
            UseFlasksInCombat.IsChecked = settings.Combat.UseFlasksInCombat;
            LifeFlaskPercent.Text = settings.Combat.UseLifeFlaskPercent.ToString();
            ManaFlaskPercent.Text = settings.Combat.UseManaFlaskPercent.ToString();
            UseQuicksilver.IsChecked = settings.Combat.UseQuicksilverFlask;
            UseUtilityFlasks.IsChecked = settings.Combat.UseUtilityFlasks;
            UseDefensiveFlasks.IsChecked = settings.Combat.UseDefensiveFlasks;
            DefensiveFlaskPercent.Text = settings.Combat.UseDefensiveFlaskPercent.ToString();
            UseOffensiveFlasks.IsChecked = settings.Combat.UseOffensiveFlasks;
            UseOffensiveFlasksOnRares.IsChecked = settings.Combat.UseOffensiveFlasksOnRares;
            UseOffensiveFlasksOnBosses.IsChecked = settings.Combat.UseOffensiveFlasksOnBosses;
            UseTincture.IsChecked = settings.Combat.UseTincture;
            TinctureKeybind.Text = settings.Combat.TinctureKeybind;
            UseTinctureOnRares.IsChecked = settings.Combat.UseTinctureOnRares;
            UseTinctureOnBosses.IsChecked = settings.Combat.UseTinctureOnBosses;

            // Loot
            PickupCurrency.IsChecked = settings.Loot.PickupCurrency;
            PickupBlueprints.IsChecked = settings.Loot.PickupBlueprints;
            PickupContracts.IsChecked = settings.Loot.PickupContracts;
            PickupUniques.IsChecked = settings.Loot.PickupUniques;
            PickupMaps.IsChecked = settings.Loot.PickupMaps;
            PickupDivinationCards.IsChecked = settings.Loot.PickupDivinationCards;
            MaxLootRange.Text = settings.Loot.MaxLootRange.ToString();

            // Exit Conditions
            ExitOnInventoryFull.IsChecked = settings.ExitConditions.ExitOnInventoryFull;
            ExitOnLowResources.IsChecked = settings.ExitConditions.ExitOnLowResources;
            ExitOnLowPortalScrolls.IsChecked = settings.ExitConditions.ExitOnLowPortalScrolls;
            MinHealthPercent.Text = settings.ExitConditions.MinHealthPercent.ToString();
            MinFlasks.Text = settings.ExitConditions.MinFlasks.ToString();
            MinPortalScrolls.Text = settings.ExitConditions.MinPortalScrolls.ToString();
        }

        private void SaveSettings()
        {
            var settings = BeastRoutineSettings.Instance;

            // Beast Capture
            settings.BeastCapture.Enabled = BeastCaptureEnabled.IsChecked ?? true;
            settings.BeastCapture.CaptureUnique = CaptureUnique.IsChecked ?? true;
            settings.BeastCapture.CaptureRare = CaptureRare.IsChecked ?? true;
            settings.BeastCapture.CaptureMagic = CaptureMagic.IsChecked ?? false;
            settings.BeastCapture.CaptureNormal = CaptureNormal.IsChecked ?? false;

            if (float.TryParse(BeastDetectionRange.Text, out float beastRange))
                settings.BeastCapture.MaxDetectionRange = beastRange;

            // Filter mode
            switch (FilterModeCombo.SelectedIndex)
            {
                case 0:
                    settings.BeastCapture.FilterMode = BeastFilterMode.CaptureAll;
                    break;
                case 1:
                    settings.BeastCapture.FilterMode = BeastFilterMode.Whitelist;
                    break;
                case 2:
                    settings.BeastCapture.FilterMode = BeastFilterMode.Blacklist;
                    break;
            }

            // Cache
            settings.CacheSettings.Enabled = CacheEnabled.IsChecked ?? true;

            if (float.TryParse(CacheDetectionRange.Text, out float cacheRange))
                settings.CacheSettings.MaxDetectionRange = cacheRange;

            // Combat
            if (int.TryParse(CombatRange.Text, out int combatRange))
                settings.Combat.CombatRange = combatRange;

            if (int.TryParse(MaxMeleeRange.Text, out int meleeRange))
                settings.Combat.MaxMeleeRange = meleeRange;

            if (int.TryParse(MaxRangeRange.Text, out int rangeRange))
                settings.Combat.MaxRangeRange = rangeRange;

            // Flasks
            settings.Combat.UseFlasksInCombat = UseFlasksInCombat.IsChecked ?? true;

            if (int.TryParse(LifeFlaskPercent.Text, out int lifeFlask))
                settings.Combat.UseLifeFlaskPercent = lifeFlask;

            if (int.TryParse(ManaFlaskPercent.Text, out int manaFlask))
                settings.Combat.UseManaFlaskPercent = manaFlask;

            settings.Combat.UseQuicksilverFlask = UseQuicksilver.IsChecked ?? true;
            settings.Combat.UseUtilityFlasks = UseUtilityFlasks.IsChecked ?? true;
            settings.Combat.UseDefensiveFlasks = UseDefensiveFlasks.IsChecked ?? true;

            if (int.TryParse(DefensiveFlaskPercent.Text, out int defFlask))
                settings.Combat.UseDefensiveFlaskPercent = defFlask;

            settings.Combat.UseOffensiveFlasks = UseOffensiveFlasks.IsChecked ?? true;
            settings.Combat.UseOffensiveFlasksOnRares = UseOffensiveFlasksOnRares.IsChecked ?? true;
            settings.Combat.UseOffensiveFlasksOnBosses = UseOffensiveFlasksOnBosses.IsChecked ?? true;

            settings.Combat.UseTincture = UseTincture.IsChecked ?? true;
            settings.Combat.TinctureKeybind = TinctureKeybind.Text ?? "5";
            settings.Combat.UseTinctureOnRares = UseTinctureOnRares.IsChecked ?? true;
            settings.Combat.UseTinctureOnBosses = UseTinctureOnBosses.IsChecked ?? true;

            // Loot
            settings.Loot.PickupCurrency = PickupCurrency.IsChecked ?? true;
            settings.Loot.PickupBlueprints = PickupBlueprints.IsChecked ?? true;
            settings.Loot.PickupContracts = PickupContracts.IsChecked ?? true;
            settings.Loot.PickupUniques = PickupUniques.IsChecked ?? true;
            settings.Loot.PickupMaps = PickupMaps.IsChecked ?? true;
            settings.Loot.PickupDivinationCards = PickupDivinationCards.IsChecked ?? true;

            if (float.TryParse(MaxLootRange.Text, out float lootRange))
                settings.Loot.MaxLootRange = lootRange;

            // Exit Conditions
            settings.ExitConditions.ExitOnInventoryFull = ExitOnInventoryFull.IsChecked ?? true;
            settings.ExitConditions.ExitOnLowResources = ExitOnLowResources.IsChecked ?? true;
            settings.ExitConditions.ExitOnLowPortalScrolls = ExitOnLowPortalScrolls.IsChecked ?? true;

            if (int.TryParse(MinHealthPercent.Text, out int minHp))
                settings.ExitConditions.MinHealthPercent = minHp;

            if (int.TryParse(MinFlasks.Text, out int minFlasks))
                settings.ExitConditions.MinFlasks = minFlasks;

            if (int.TryParse(MinPortalScrolls.Text, out int minPortalScrolls))
                settings.ExitConditions.MinPortalScrolls = minPortalScrolls;

            // Save to disk
            settings.Save();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            MessageBox.Show("Settings saved successfully!", "BeastRoutine", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            // Settings are automatically loaded when Instance is accessed
            LoadSettings();
            MessageBox.Show("Settings loaded successfully!", "BeastRoutine", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
