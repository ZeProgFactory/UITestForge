using CommunityToolkit.Maui.Views;

namespace UITestForge.Views
{
    public partial class SyntaxHelpPopup : Popup
    {
        public SyntaxHelpPopup()
        {
            InitializeComponent();
        }

        private void OnOkClicked(object? sender, EventArgs e)
        {
            this.CloseAsync();
        }
    }
}
