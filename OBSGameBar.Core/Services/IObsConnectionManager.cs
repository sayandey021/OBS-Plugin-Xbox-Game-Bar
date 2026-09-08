using System;
using System.Threading.Tasks;
using OBSGameBar.Core.Models;

namespace OBSGameBar.Core.Services
{
    public interface IObsConnectionManager : IDisposable
    {
        ObsConnectionStatus Status { get; }
        bool IsConnected { get; }
        string StatusMessage { get; }

        event EventHandler<ObsConnectionStateChangedEventArgs> StateChanged;

        void Configure(ConnectionSettings settings);
        Task StartAutoConnectAsync();
        Task StopAutoConnectAsync();
        Task RetryConnectNowAsync();
        void Pause();
        void Resume();
    }
}
