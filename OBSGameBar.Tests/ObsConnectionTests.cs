using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OBSGameBar.Core.Models;
using OBSGameBar.Core.Services;

namespace OBSGameBar.Tests
{
    [TestClass]
    public class ObsConnectionTests
    {
        [TestMethod]
        public async Task TestConnectionManager_ConnectSuccess_InvokesSyncCallback()
        {
            var mockWs = new MockObsWebSocketService();
            bool syncCalled = false;
            var connMgr = new ObsConnectionManager(mockWs, () =>
            {
                syncCalled = true;
                return Task.CompletedTask;
            });

            var settings = new ConnectionSettings { Host = "127.0.0.1", Port = 4455 };
            connMgr.Configure(settings);

            await connMgr.StartAutoConnectAsync();

            Assert.IsTrue(connMgr.IsConnected);
            Assert.AreEqual(ObsConnectionStatus.Connected, connMgr.Status);
            Assert.IsTrue(syncCalled);
        }

        [TestMethod]
        public async Task TestConnectionManager_AuthFailure_TransitionsToAuthFailed()
        {
            var mockWs = new MockObsWebSocketService { ShouldAuthFail = true };
            var connMgr = new ObsConnectionManager(mockWs);
            var settings = new ConnectionSettings { Host = "127.0.0.1", Port = 4455, Password = "wrong" };
            connMgr.Configure(settings);

            await connMgr.StartAutoConnectAsync();

            Assert.AreEqual(ObsConnectionStatus.AuthFailed, connMgr.Status);
            StringAssert.Contains(connMgr.StatusMessage, "Authentication failed");
        }

        [TestMethod]
        public async Task TestConnectionManager_PauseAndResume_HandlesState()
        {
            var mockWs = new MockObsWebSocketService();
            var connMgr = new ObsConnectionManager(mockWs);
            var settings = new ConnectionSettings { Host = "127.0.0.1", Port = 4455 };
            connMgr.Configure(settings);

            await connMgr.StartAutoConnectAsync();
            Assert.IsTrue(connMgr.IsConnected);

            connMgr.Pause();
            // In paused state, connection manager does not retry
            connMgr.Resume();
            Assert.IsTrue(connMgr.IsConnected);
        }
    }
}
