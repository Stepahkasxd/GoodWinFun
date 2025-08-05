using System.Windows;

namespace GoodWin.Gui.Views
{
    public partial class DebuffNotificationWindow : Window
    {
        public DebuffNotificationWindow(string message)
        {
            InitializeComponent();
            MessageText.Text = message;
        }
    }
}
