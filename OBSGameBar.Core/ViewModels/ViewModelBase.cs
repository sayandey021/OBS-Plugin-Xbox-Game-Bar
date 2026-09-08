using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace OBSGameBar.Core.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public static Action<Action> DispatcherRunner { get; set; } = action => action();
        public static Action<string> Logger { get; set; } = msg => { };

        public static void Log(string msg)
        {
            try { Logger?.Invoke(msg); } catch { }
        }

        /// <summary>
        /// Marshals an action to the UI thread (safe to call from any thread).
        /// Shared with Models and Commands that raise change notifications.
        /// </summary>
        public static void Marshal(Action action)
        {
            var runner = DispatcherRunner;
            if (runner != null)
            {
                runner(action);
            }
            else
            {
                action();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            RunOnUIThread(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }

        protected void RunOnUIThread(Action action)
        {
            if (DispatcherRunner != null)
            {
                DispatcherRunner(action);
            }
            else
            {
                action();
            }
        }
    }
}
