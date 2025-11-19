using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using SimpleMapBot.Configuration;

namespace SimpleMapBot.GUI
{
    /// <summary>
    /// GUI for SimpleMapBot configuration
    /// </summary>
    public partial class SimpleMapBotGui : UserControl
    {
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
            InitializeGui();
        }

        private void InitializeGui()
        {
            // Populate scarab dropdowns
            cmbScarab1.ItemsSource = AvailableScarabs;
            cmbScarab2.ItemsSource = AvailableScarabs;
            cmbScarab3.ItemsSource = AvailableScarabs;
            cmbScarab4.ItemsSource = AvailableScarabs;
            cmbScarab5.ItemsSource = AvailableScarabs;

            // Load settings
            LoadSettings();

            // Wire up save button
            btnSave.Click += BtnSave_Click;
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
