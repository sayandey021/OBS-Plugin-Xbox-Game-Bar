using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.Gaming.XboxGameBar;
using OBSGameBar.Core.ViewModels;

namespace OBSGameBar.Views
{
    public sealed partial class SettingsView : Page
    {
        public SettingsViewModel ViewModel => App.SettingsViewModel;
        private XboxGameBarWidget _widget;

        public SettingsView()
        {
            this.InitializeComponent();
            this.DataContext = ViewModel;

            if (!string.IsNullOrEmpty(ViewModel.Password))
            {
                PasswordBoxInput.Password = ViewModel.Password;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is XboxGameBarWidget widget)
            {
                _widget = widget;
            }

            if (!string.IsNullOrEmpty(ViewModel.Password))
            {
                PasswordBoxInput.Password = ViewModel.Password;
            }
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            ViewModel.Password = PasswordBoxInput.Password;
        }
    }
}
