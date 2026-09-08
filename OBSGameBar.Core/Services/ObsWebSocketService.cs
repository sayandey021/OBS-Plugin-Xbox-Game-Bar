using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OBSGameBar.Core.Models;
using OBSGameBar.Core.Protocol;

namespace OBSGameBar.Core.Services
{
    public class ObsWebSocketService : IObsWebSocketService
    {
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cts;
        private Task _receiveLoopTask;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement?>> _pendingRequests =
            new ConcurrentDictionary<string, TaskCompletionSource<JsonElement?>>();

        private TaskCompletionSource<bool> _handshakeTcs;
        private string _password;
        private ObsConnectionStatus _status = ObsConnectionStatus.Disconnected;
        private long _requestCounter = 0;

        public event EventHandler<ObsEventReceivedEventArgs> EventReceived;
        public event EventHandler<ObsConnectionStateChangedEventArgs> StateChanged;

        public ObsConnectionStatus Status
        {
            get => _status;
            private set
            {
                if (_status != value)
                {
                    var old = _status;
                    _status = value;
                    StateChanged?.Invoke(this, new ObsConnectionStateChangedEventArgs(old, _status, _status.ToString()));
                }
            }
        }

        public bool IsConnected => _status == ObsConnectionStatus.Connected && _webSocket?.State == WebSocketState.Open;

        public async Task ConnectAsync(string host, int port, string password = null, CancellationToken cancellationToken = default)
        {
            await DisconnectAsync();

            _password = password;
            Status = ObsConnectionStatus.Connecting;

            _cts = new CancellationTokenSource();
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken))
            {
                try
                {
                    _webSocket = new ClientWebSocket();
                    _webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);

                    var uri = new Uri($"ws://{host.Trim()}:{port}");
                    _handshakeTcs = new TaskCompletionSource<bool>();

                    await _webSocket.ConnectAsync(uri, linkedCts.Token).ConfigureAwait(false);

                    // Start background receive loop which will process Hello and Identify
                    _receiveLoopTask = Task.Run(() => ReceiveLoopAsync(_cts.Token), _cts.Token);

                    // Wait for Identified opcode (op 2) or handshake timeout
                    using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                    using (var combinedTimeout = CancellationTokenSource.CreateLinkedTokenSource(linkedCts.Token, timeoutCts.Token))
                    {
                        using (combinedTimeout.Token.Register(() => _handshakeTcs.TrySetCanceled()))
                        {
                            await _handshakeTcs.Task.ConfigureAwait(false);
                        }
                    }

                    Status = ObsConnectionStatus.Connected;
                }
                catch (OperationCanceledException)
                {
                    if (Status != ObsConnectionStatus.AuthFailed)
                    {
                        Status = ObsConnectionStatus.Error;
                    }
                    await CleanupAsync();
                    throw;
                }
                catch (Exception ex)
                {
                    if (Status != ObsConnectionStatus.AuthFailed)
                    {
                        Status = ObsConnectionStatus.Error;
                    }
                    await CleanupAsync();
                    throw new InvalidOperationException($"Unable to connect to OBS Studio at {host}:{port}: {ex.Message}", ex);
                }
            }
        }

        public async Task DisconnectAsync()
        {
            if (_status == ObsConnectionStatus.Disconnected && _webSocket == null)
            {
                return;
            }

            try
            {
                if (_cts != null && !_cts.IsCancellationRequested)
                {
                    _cts.Cancel();
                }

                if (_webSocket != null && (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.CloseReceived))
                {
                    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnected", timeout.Token).ConfigureAwait(false);
                    }
                }
            }
            catch
            {
                // Ignore disconnect exceptions
            }
            finally
            {
                await CleanupAsync();
                Status = ObsConnectionStatus.Disconnected;
            }
        }

        public async Task<JsonElement?> SendRequestAsync(string requestType, object requestData = null, TimeSpan? timeout = null)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Not connected to OBS Studio.");
            }

            var requestId = $"req_{Interlocked.Increment(ref _requestCounter)}_{Guid.NewGuid():N}";
            var tcs = new TaskCompletionSource<JsonElement?>();
            _pendingRequests[requestId] = tcs;

            var envelope = new ObsRequestEnvelope
            {
                Op = (int)ObsOpCode.Request,
                Payload = new ObsRequestPayload
                {
                    RequestType = requestType,
                    RequestId = requestId,
                    RequestData = requestData
                }
            };

            string json = JsonSerializer.Serialize(envelope);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            try
            {
                await _sendLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (_webSocket.State != WebSocketState.Open)
                    {
                        throw new InvalidOperationException("WebSocket is closed.");
                    }

                    await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token).ConfigureAwait(false);
                }
                finally
                {
                    _sendLock.Release();
                }

                var timeoutDuration = timeout ?? TimeSpan.FromSeconds(5);
                using (var reqTimeoutCts = new CancellationTokenSource(timeoutDuration))
                using (reqTimeoutCts.Token.Register(() => tcs.TrySetException(new TimeoutException($"OBS request '{requestType}' timed out after {timeoutDuration.TotalSeconds}s."))))
                {
                    return await tcs.Task.ConfigureAwait(false);
                }
            }
            finally
            {
                _pendingRequests.TryRemove(requestId, out _);
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[8192];
            using (var ms = new MemoryStream())
            {
                while (!cancellationToken.IsCancellationRequested && _webSocket != null && _webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        ms.SetLength(0);
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                int closeCode = (int?)_webSocket.CloseStatus ?? 0;
                                if (closeCode == 4009) // obs-websocket AuthenticationFailed
                                {
                                    Status = ObsConnectionStatus.AuthFailed;
                                }
                                else
                                {
                                    Status = ObsConnectionStatus.Disconnected;
                                }
                                _handshakeTcs?.TrySetException(new InvalidOperationException($"OBS closed connection: {result.CloseStatusDescription} (Code {closeCode})"));
                                return;
                            }
                            ms.Write(buffer, 0, result.Count);
                        } while (!result.EndOfMessage);

                        string text = Encoding.UTF8.GetString(ms.ToArray());
                        ProcessIncomingMessage(text);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            _handshakeTcs?.TrySetException(ex);
                            Status = ObsConnectionStatus.Disconnected;
                        }
                        break;
                    }
                }
            }

            if (Status == ObsConnectionStatus.Connected)
            {
                Status = ObsConnectionStatus.Disconnected;
            }
        }

        private void ProcessIncomingMessage(string json)
        {
            try
            {
                using (var doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    if (!root.TryGetProperty("op", out var opProp)) return;

                    int op = opProp.GetInt32();
                    if (!root.TryGetProperty("d", out var dProp)) return;

                    switch ((ObsOpCode)op)
                    {
                        case ObsOpCode.Hello:
                            HandleHello(dProp);
                            break;

                        case ObsOpCode.Identified:
                            _handshakeTcs?.TrySetResult(true);
                            break;

                        case ObsOpCode.Event:
                            HandleEvent(dProp);
                            break;

                        case ObsOpCode.RequestResponse:
                            HandleRequestResponse(dProp);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ObsWebSocketService] Message processing error: {ex.Message}");
            }
        }

        private async void HandleHello(JsonElement data)
        {
            try
            {
                string authString = null;
                if (data.TryGetProperty("authentication", out var authProp) &&
                    authProp.ValueKind == JsonValueKind.Object)
                {
                    string challenge = authProp.GetProperty("challenge").GetString();
                    string salt = authProp.GetProperty("salt").GetString();

                    if (string.IsNullOrEmpty(_password))
                    {
                        Status = ObsConnectionStatus.AuthFailed;
                        _handshakeTcs?.TrySetException(new UnauthorizedAccessException("OBS Studio requires a password, but none was configured."));
                        return;
                    }

                    authString = ObsAuthHelper.GenerateAuthResponse(_password, salt, challenge);
                }

                // Send Identify
                var identifyData = new ObsIdentifyData
                {
                    RpcVersion = 1,
                    Authentication = authString,
                    EventSubscriptions = (uint)ObsEventSubscriptions.AllStandard
                };

                var envelope = new
                {
                    op = (int)ObsOpCode.Identify,
                    d = identifyData
                };

                string json = JsonSerializer.Serialize(envelope);
                byte[] bytes = Encoding.UTF8.GetBytes(json);

                await _sendLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                    {
                        await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token).ConfigureAwait(false);
                    }
                }
                finally
                {
                    _sendLock.Release();
                }
            }
            catch (Exception ex)
            {
                _handshakeTcs?.TrySetException(ex);
            }
        }

        private void HandleRequestResponse(JsonElement data)
        {
            if (!data.TryGetProperty("requestId", out var reqIdProp)) return;
            string requestId = reqIdProp.GetString();

            if (_pendingRequests.TryGetValue(requestId, out var tcs))
            {
                bool success = true;
                string comment = null;
                int code = 100;

                if (data.TryGetProperty("requestStatus", out var statusProp))
                {
                    if (statusProp.TryGetProperty("result", out var resultProp))
                        success = resultProp.GetBoolean();
                    if (statusProp.TryGetProperty("code", out var codeProp))
                        code = codeProp.GetInt32();
                    if (statusProp.TryGetProperty("comment", out var commentProp))
                        comment = commentProp.GetString();
                }

                if (success)
                {
                    if (data.TryGetProperty("responseData", out var respDataProp) && respDataProp.ValueKind != JsonValueKind.Null)
                    {
                        tcs.TrySetResult(respDataProp.Clone());
                    }
                    else
                    {
                        tcs.TrySetResult(null);
                    }
                }
                else
                {
                    tcs.TrySetException(new InvalidOperationException($"OBS Request failed ({code}): {comment}"));
                }
            }
        }

        private void HandleEvent(JsonElement data)
        {
            string eventType = data.GetProperty("eventType").GetString();
            uint eventIntent = data.TryGetProperty("eventIntent", out var intentProp) ? intentProp.GetUInt32() : 0;
            JsonElement eventData = data.TryGetProperty("eventData", out var edProp) ? edProp.Clone() : default;

            EventReceived?.Invoke(this, new ObsEventReceivedEventArgs(eventType, eventIntent, eventData));
        }

        private async Task CleanupAsync()
        {
            foreach (var kvp in _pendingRequests)
            {
                kvp.Value.TrySetCanceled();
            }
            _pendingRequests.Clear();

            if (_webSocket != null)
            {
                try
                {
                    _webSocket.Dispose();
                }
                catch { }
                _webSocket = null;
            }

            if (_cts != null)
            {
                try
                {
                    _cts.Dispose();
                }
                catch { }
                _cts = null;
            }

            await Task.CompletedTask;
        }

        public void Dispose()
        {
            DisconnectAsync().Wait(1000);
            _sendLock.Dispose();
        }
    }
}
