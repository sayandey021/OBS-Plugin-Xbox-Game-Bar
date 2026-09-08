using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.Gaming.XboxGameBar;
using OBSGameBar.Core.Models;
using OBSGameBar.Core.ViewModels;

namespace OBSGameBar.Views
{
    public sealed partial class MainWidgetView : Page
    {
        public MainWidgetViewModel ViewModel => App.MainViewModel;
        private XboxGameBarWidget _widget;

        public MainWidgetView()
        {
            App.Log("MainWidgetView constructor entered");
            try
            {
                App.SetDispatcher(this.Dispatcher, isPrimary: true);
                this.InitializeComponent();
                App.Log("MainWidgetView InitializeComponent completed");
                this.DataContext = ViewModel;
                App.Log("MainWidgetView DataContext set");

                if (ViewModel != null)
                {
                    ViewModel.ConfirmationRequested += ShowConfirmationDialogAsync;
                }
                App.Log("MainWidgetView constructor completed");
            }
            catch (Exception ex)
            {
                App.Log($"MainWidgetView constructor exception: {ex}");
                throw;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is XboxGameBarWidget widget)
            {
                _widget = widget;
                _widget.RequestedOpacityChanged += OnWidgetRequestedOpacityChanged;
                ApplyRequestedOpacity();
            }
        }

        private async void OnWidgetRequestedOpacityChanged(XboxGameBarWidget sender, object args)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, ApplyRequestedOpacity);
        }

        private void ApplyRequestedOpacity()
        {
            try
            {
                if (_widget != null && RootGrid != null)
                {
                    RootGrid.Opacity = _widget.RequestedOpacity;
                }
            }
            catch { }
        }

        private async Task<bool> ShowConfirmationDialogAsync(string title, string content)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = "Confirm",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        private void OnSceneButtonClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                var scene = (btn.Tag as SceneModel) ?? (btn.DataContext as SceneModel);
                App.Log($"[MainWidgetView] OnSceneButtonClicked: {scene?.Name}, IsStudioMode: {ViewModel?.State?.IsStudioMode}");
                if (scene != null)
                {
                    ViewModel.SwitchSceneCommand.Execute(scene);
                }
            }
        }

        private void OnSourceToggled(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb)
            {
                var source = (cb.DataContext as SourceModel) ?? (cb.Tag as SourceModel);
                if (source != null)
                {
                    bool desiredState = cb.IsChecked == true;
                    App.Log($"[MainWidgetView] OnSourceToggled: '{source.SourceName}', id={source.SceneItemId}, desired={desiredState}");
                    ViewModel.SetSourceVisibilityCommand.Execute((source, desiredState));
                }
            }
        }

        private void OnQuickActionButtonClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is QuickActionItem action)
            {
                ViewModel.ExecuteQuickActionCommand.Execute(action);
            }
        }
    }
}
