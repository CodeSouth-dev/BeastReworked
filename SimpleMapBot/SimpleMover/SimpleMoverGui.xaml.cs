using System.Windows.Controls;

namespace SimpleMover
{
    /// <summary>
    /// Interaction logic for BeastMoverGui.xaml
    /// </summary>
    public partial class BeastMoverGui : UserControl
    {
        public BeastMoverGui()
        {
            InitializeComponent();
            DataContext = SimpleMoverSettings.Instance;
        }
    }
}

