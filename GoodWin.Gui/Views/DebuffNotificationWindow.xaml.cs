using System.Windows;

namespace GoodWin.Gui.Views
{
    public partial class DebuffNotificationWindow : Window
    {
        public DebuffNotificationWindow(string title, string description)
        {
            InitializeComponent();
            TitleText.Text = title;
            DescriptionText.Text = description;
        }
    }
}
