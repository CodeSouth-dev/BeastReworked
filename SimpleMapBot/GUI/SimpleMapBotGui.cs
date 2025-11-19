using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SimpleMapBot.Configuration;

namespace SimpleMapBot.GUI
{
    /// <summary>
    /// GUI for SimpleMapBot configuration - Pure C# implementation (no XAML)
    /// </summary>
    public class SimpleMapBotGui : UserControl
    {
        // Map checkboxes
        private CheckBox cbCrater;
        private CheckBox cbUndergroundSea;
        private CheckBox cbPort;
        private CheckBox cbTower;
        private CheckBox cbPhantasmagoria;
        private CheckBox cbChannel;
        private CheckBox cbWaterways;
        private CheckBox cbFrozenCabins;
        private CheckBox cbSilo;
        private CheckBox cbToxicSewers;
        private CheckBox cbAtoll;
        private CheckBox cbWastepool;
        private CheckBox cbBeach;
        private CheckBox cbDunes;

        // Scarab dropdowns
        private ComboBox cmbScarab1;
        private ComboBox cmbScarab2;
        private ComboBox cmbScarab3;
        private ComboBox cmbScarab4;
        private ComboBox cmbScarab5;

        // Scarab panels (for adding to UI)
        private StackPanel scarab1Panel;
        private StackPanel scarab2Panel;
        private StackPanel scarab3Panel;
        private StackPanel scarab4Panel;
        private StackPanel scarab5Panel;

        // Save button
        private Button btnSave;

        private static readonly List<string> AvailableScarabs = new List<string>
        {
            "None",
            "Rusted Ambush Scarab",
            "Polished Ambush Scarab",
            "Gilded Ambush Scarab",
            "Winged Ambush Scarab",
            "Rusted Bestiary Scarab",
            "Polished Bestiary Scarab",
            "Gilded Bestiary Scarab",
            "Winged Bestiary Scarab",
            "Rusted Breach Scarab",
            "Polished Breach Scarab",
            "Gilded Breach Scarab",
            "Winged Breach Scarab",
            "Rusted Cartography Scarab",
            "Polished Cartography Scarab",
            "Gilded Cartography Scarab",
            "Winged Cartography Scarab",
            "Rusted Divination Scarab",
            "Polished Divination Scarab",
            "Gilded Divination Scarab",
            "Winged Divination Scarab",
            "Rusted Harbinger Scarab",
            "Polished Harbinger Scarab",
            "Gilded Harbinger Scarab",
            "Winged Harbinger Scarab",
            "Rusted Legion Scarab",
            "Polished Legion Scarab",
            "Gilded Legion Scarab",
            "Winged Legion Scarab",
            "Rusted Reliquary Scarab",
            "Polished Reliquary Scarab",
            "Gilded Reliquary Scarab",
            "Winged Reliquary Scarab",
            "Rusted Sulphite Scarab",
            "Polished Sulphite Scarab",
            "Gilded Sulphite Scarab",
            "Winged Sulphite Scarab",
            "Rusted Torment Scarab",
            "Polished Torment Scarab",
            "Gilded Torment Scarab",
            "Winged Torment Scarab"
        };

        public SimpleMapBotGui()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Main container
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(10)
            };

            var mainPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            // Bot Information Section
            var infoGroup = CreateGroupBox("Bot Information");
            var infoPanel = new StackPanel { Orientation = Orientation.Vertical };
            infoPanel.Children.Add(new TextBlock
            {
                Text = "SimpleMapBot Configuration",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 5)
            });
            infoPanel.Children.Add(new TextBlock
            {
                Text = "This bot handles complete map running workflow.\n\n" +
                       "Required Setup:\n" +
                       "1. Select BeastMover as PlayerMover\n" +
                       "2. Select BeastCombatRoutine as Routine\n" +
                       "3. Put maps and scarabs in your STASH (bot takes from stash)\n" +
                       "4. Start the bot while in your hideout\n\n" +
                       "What the bot does:\n" +
                       "- Withdraws maps from stash (matches your enabled maps below)\n" +
                       "- Navigates to map device\n" +
                       "- Places map and scarabs in device\n" +
                       "- Activates device and enters portal\n" +
                       "- Loots currency, maps, and divination cards\n" +
                       "- Returns to hideout when inventory is full\n" +
                       "- Stashes loot and starts next map\n" +
                       "- BeastMover handles movement, BeastCombatRoutine handles combat",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            });
            infoGroup.Content = infoPanel;
            mainPanel.Children.Add(infoGroup);

            // Map Selection Section
            var mapGroup = CreateGroupBox("Map Selection");
            var mapGrid = new Grid();
            mapGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mapGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var leftPanel = new StackPanel { Orientation = Orientation.Vertical };
            var rightPanel = new StackPanel { Orientation = Orientation.Vertical };

            // Create map checkboxes
            cbCrater = CreateCheckBox("Crater");
            cbUndergroundSea = CreateCheckBox("Underground Sea");
            cbPort = CreateCheckBox("Port");
            cbTower = CreateCheckBox("Tower");
            cbPhantasmagoria = CreateCheckBox("Phantasmagoria");
            cbChannel = CreateCheckBox("Channel");
            cbWaterways = CreateCheckBox("Waterways");

            cbFrozenCabins = CreateCheckBox("Frozen Cabins");
            cbSilo = CreateCheckBox("Silo");
            cbToxicSewers = CreateCheckBox("Toxic Sewers");
            cbAtoll = CreateCheckBox("Atoll");
            cbWastepool = CreateCheckBox("Wastepool");
            cbBeach = CreateCheckBox("Beach");
            cbDunes = CreateCheckBox("Dunes");

            // Add to left column
            leftPanel.Children.Add(cbCrater);
            leftPanel.Children.Add(cbUndergroundSea);
            leftPanel.Children.Add(cbPort);
            leftPanel.Children.Add(cbTower);
            leftPanel.Children.Add(cbPhantasmagoria);
            leftPanel.Children.Add(cbChannel);
            leftPanel.Children.Add(cbWaterways);

            // Add to right column
            rightPanel.Children.Add(cbFrozenCabins);
            rightPanel.Children.Add(cbSilo);
            rightPanel.Children.Add(cbToxicSewers);
            rightPanel.Children.Add(cbAtoll);
            rightPanel.Children.Add(cbWastepool);
            rightPanel.Children.Add(cbBeach);
            rightPanel.Children.Add(cbDunes);

            Grid.SetColumn(leftPanel, 0);
            Grid.SetColumn(rightPanel, 1);
            mapGrid.Children.Add(leftPanel);
            mapGrid.Children.Add(rightPanel);
            mapGroup.Content = mapGrid;
            mainPanel.Children.Add(mapGroup);

            // Scarab Selection Section
            var scarabGroup = CreateGroupBox("Scarab Selection");
            var scarabPanel = new StackPanel { Orientation = Orientation.Vertical };

            scarab1Panel = CreateScarabComboBox("Scarab Slot 1:", out cmbScarab1);
            scarab2Panel = CreateScarabComboBox("Scarab Slot 2:", out cmbScarab2);
            scarab3Panel = CreateScarabComboBox("Scarab Slot 3:", out cmbScarab3);
            scarab4Panel = CreateScarabComboBox("Scarab Slot 4:", out cmbScarab4);
            scarab5Panel = CreateScarabComboBox("Scarab Slot 5:", out cmbScarab5);

            scarabPanel.Children.Add(scarab1Panel);
            scarabPanel.Children.Add(scarab2Panel);
            scarabPanel.Children.Add(scarab3Panel);
            scarabPanel.Children.Add(scarab4Panel);
            scarabPanel.Children.Add(scarab5Panel);

            scarabGroup.Content = scarabPanel;
            mainPanel.Children.Add(scarabGroup);

            // Save Button
            btnSave = new Button
            {
                Content = "Save Settings",
                Width = 120,
                Height = 30,
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            btnSave.Click += BtnSave_Click;
            mainPanel.Children.Add(btnSave);

            scrollViewer.Content = mainPanel;
            this.Content = scrollViewer;

            // Load settings
            LoadSettings();
        }

        private GroupBox CreateGroupBox(string header)
        {
            return new GroupBox
            {
                Header = header,
                Margin = new Thickness(0, 5, 0, 5),
                Padding = new Thickness(5)
            };
        }

        private CheckBox CreateCheckBox(string content)
        {
            return new CheckBox
            {
                Content = content,
                Margin = new Thickness(5, 2, 5, 2)
            };
        }

        private StackPanel CreateScarabComboBox(string label, out ComboBox comboBox)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5, 2, 5, 2) };
            panel.Children.Add(new TextBlock
            {
                Text = label,
                Width = 100,
                VerticalAlignment = VerticalAlignment.Center
            });

            comboBox = new ComboBox
            {
                Width = 200,
                ItemsSource = AvailableScarabs
            };

            panel.Children.Add(comboBox);

            return panel;
        }

        private void LoadSettings()
        {
            var settings = SimpleMapBotSettings.Instance;

            // Map checkboxes
            cbCrater.IsChecked = settings.EnableCrater;
            cbUndergroundSea.IsChecked = settings.EnableUndergroundSea;
            cbPort.IsChecked = settings.EnablePort;
            cbTower.IsChecked = settings.EnableTower;
            cbPhantasmagoria.IsChecked = settings.EnablePhantasmagoria;
            cbChannel.IsChecked = settings.EnableChannel;
            cbWaterways.IsChecked = settings.EnableWaterways;
            cbFrozenCabins.IsChecked = settings.EnableFrozenCabins;
            cbSilo.IsChecked = settings.EnableSilo;
            cbToxicSewers.IsChecked = settings.EnableToxicSewers;
            cbAtoll.IsChecked = settings.EnableAtoll;
            cbWastepool.IsChecked = settings.EnableWastepool;
            cbBeach.IsChecked = settings.EnableBeach;
            cbDunes.IsChecked = settings.EnableDunes;

            // Scarab dropdowns
            cmbScarab1.SelectedItem = settings.ScarabSlot1;
            cmbScarab2.SelectedItem = settings.ScarabSlot2;
            cmbScarab3.SelectedItem = settings.ScarabSlot3;
            cmbScarab4.SelectedItem = settings.ScarabSlot4;
            cmbScarab5.SelectedItem = settings.ScarabSlot5;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var settings = SimpleMapBotSettings.Instance;

            // Save map checkboxes
            settings.EnableCrater = cbCrater.IsChecked ?? false;
            settings.EnableUndergroundSea = cbUndergroundSea.IsChecked ?? false;
            settings.EnablePort = cbPort.IsChecked ?? false;
            settings.EnableTower = cbTower.IsChecked ?? false;
            settings.EnablePhantasmagoria = cbPhantasmagoria.IsChecked ?? false;
            settings.EnableChannel = cbChannel.IsChecked ?? false;
            settings.EnableWaterways = cbWaterways.IsChecked ?? false;
            settings.EnableFrozenCabins = cbFrozenCabins.IsChecked ?? false;
            settings.EnableSilo = cbSilo.IsChecked ?? false;
            settings.EnableToxicSewers = cbToxicSewers.IsChecked ?? false;
            settings.EnableAtoll = cbAtoll.IsChecked ?? false;
            settings.EnableWastepool = cbWastepool.IsChecked ?? false;
            settings.EnableBeach = cbBeach.IsChecked ?? false;
            settings.EnableDunes = cbDunes.IsChecked ?? false;

            // Save scarab dropdowns
            settings.ScarabSlot1 = cmbScarab1.SelectedItem?.ToString() ?? "None";
            settings.ScarabSlot2 = cmbScarab2.SelectedItem?.ToString() ?? "None";
            settings.ScarabSlot3 = cmbScarab3.SelectedItem?.ToString() ?? "None";
            settings.ScarabSlot4 = cmbScarab4.SelectedItem?.ToString() ?? "None";
            settings.ScarabSlot5 = cmbScarab5.SelectedItem?.ToString() ?? "None";

            // Save to disk
            settings.Save();

            MessageBox.Show("Settings saved successfully!", "SimpleMapBot", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
