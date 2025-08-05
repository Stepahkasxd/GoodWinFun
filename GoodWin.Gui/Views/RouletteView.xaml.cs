using System.Windows;
using System.Windows.Controls;

namespace GoodWin.Gui.Views
{
    public partial class RouletteView : UserControl
    {
        public RouletteView()
        {
            InitializeComponent();
        }

        private void OpenRouletteWindow(object sender, RoutedEventArgs e)
        {
            var window = new RouletteWindow
            {
                Owner = Window.GetWindow(this),
                DataContext = DataContext
            };
            window.Show();
        }
    }
}
