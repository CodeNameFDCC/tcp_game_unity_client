// GameClient.cs
using UnityEngine;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class GameClient : MonoBehaviour
{
    private TcpClient client;
    private NetworkStream stream;
    public ClientHandler handler { get; private set; }  // public getter
    private bool isConnected = false;

    [SerializeField] private string serverIP = "127.0.0.1";
    [SerializeField] private int serverPort = 3000;

    [SerializeField] private int maxReconnectAttempts = 3;
    [SerializeField] private float reconnectDelay = 2f;
    private int reconnectAttempts = 0;

    private CancellationTokenSource _cancellationTokenSource;

    private void Awake()
    {
        handler = new ClientHandler();
        SetupEventHandlers();
        ConnectToServer();
    }

    private void Start()
    {
        //_ = ConnectToServerAsync();
    }

    private void SetupEventHandlers()
    {
        handler.OnInt8Received += (value) => Debug.Log($"Received int8: {value}");
        handler.OnInt16Received += (value) => Debug.Log($"Received int16: {value}");
        handler.OnInt32Received += (value) => Debug.Log($"Received int32: {value}");
        handler.OnFloatReceived += (value) => Debug.Log($"Received float: {value}");
        handler.OnBoolReceived += (value) => Debug.Log($"Received bool: {value}");
        handler.OnStringReceived += (value) => Debug.Log($"Received string: {value}");
        handler.OnPositionReceived += (value) => Debug.Log($"Received position: {value}");
        handler.OnQuaternionReceived += (value) => Debug.Log($"Received quaternion: {value}");
        handler.OnPingReceived += (value) => Debug.Log($"Received ping: {value}");
    }

    private async void ConnectToServer()
    {
        try
        {
            client = new TcpClient();
            await client.ConnectAsync(serverIP, serverPort);
            stream = client.GetStream();
            isConnected = true;
            Debug.Log("Connected to server!");
            StartReceiving();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to connect: {e.Message}");
        }
    }

    private async void StartReceiving()
    {
        byte[] buffer = new byte[1024];

        while (isConnected)
        {
            try
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    // Connection closed
                    break;
                }

                byte[] data = new byte[bytesRead];
                Array.Copy(buffer, data, bytesRead);

                // Process received data on main thread
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    handler.HandleMessage(data);
                });
            }
            catch (Exception e)
            {
                if (isConnected)
                {
                    Debug.LogError($"Error receiving data: {e.Message}");
                    break;
                }
            }
        }

        Disconnect();
    }

    public async void SendMessage(byte[] data)
    {
        if (!isConnected)
        {
            Debug.LogWarning("Not connected to server");
            return;
        }

        try
        {
            await stream.WriteAsync(data, 0, data.Length);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending data: {e.Message}");
            Disconnect();
        }
    }


    public void RequestInt8()
    {
        SendMessage(handler.CreateInt8Request());
    }
    public void RequestInt16()
    {
        SendMessage(handler.CreateInt16Request());
    }
    public void RequestInt32()
    {
        SendMessage(handler.CreateInt32Request());
    }

    public void RequestFloat()
    {
        SendMessage(handler.CreateFloatRequest());
    }

    public void RequestBool()
    {
        SendMessage(handler.CreateBoolRequest());
    }

    public void RequestString()
    {
        SendMessage(handler.CreateStringRequest());
    }

    public void RequestPosition()
    {
        SendMessage(handler.CreatePositionRequest());
    }

    public void RequestQuaternion()
    {
        SendMessage(handler.CreateQuaternionRequest());
    }

    // Example methods to send responses
    public void SendInt8Response(int value)
    {
        SendMessage(handler.CreateInt8Response(value));
    }
    public void SendInt16Response(int value)
    {
        SendMessage(handler.CreateInt16Response(value));
    }
    public void SendInt32Response(int value)
    {
        SendMessage(handler.CreateInt32Response(value));
    }

    public void SendFloatResponse(float value)
    {
        SendMessage(handler.CreateFloatResponse(value));
    }

    public void SendBoolResponse(bool value)
    {
        SendMessage(handler.CreateBoolResponse(value));
    }

    public void SendStringResponse(string value)
    {
        SendMessage(handler.CreateStringResponse(value));
    }

    public void SendPositionResponse(Vector3 position)
    {
        SendMessage(handler.CreatePositionResponse(position));
    }

    public void SendQuaternionResponse(Quaternion rotation)
    {
        SendMessage(handler.CreateQuaternionResponse(rotation));
    }

    private void Disconnect()
    {
        isConnected = false;
        if (stream != null)
        {
            stream.Close();
            stream = null;
        }
        if (client != null)
        {
            client.Close();
            client = null;
        }
        Debug.Log("Disconnected from server");
    }

    private async Task StartReceivingAsync()
    {
        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            byte[] buffer = new byte[1024];

            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, _cancellationTokenSource.Token);
                    if (bytesRead == 0) break;

                    byte[] data = new byte[bytesRead];
                    Array.Copy(buffer, data, bytesRead);

                    await UnityMainThreadDispatcher.Instance.EnqueueAsync(() =>
                    {
                        handler.HandleMessage(data);
                    });
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error receiving data: {e}");
                    await Task.Delay(1000, _cancellationTokenSource.Token); // 에러 시 잠시 대기
                }
            }
        }
        finally
        {
            Disconnect();
        }
    }

    private async Task ConnectToServerAsync()
    {
        while (reconnectAttempts < maxReconnectAttempts)
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(serverIP, serverPort);
                stream = client.GetStream();
                isConnected = true;
                reconnectAttempts = 0;
                Debug.Log("Connected to server!");
                _ = StartReceivingAsync();
                return;
            }
            catch (Exception e)
            {
                reconnectAttempts++;
                Debug.LogWarning($"Connection attempt {reconnectAttempts} failed: {e.Message}");

                if (reconnectAttempts < maxReconnectAttempts)
                {
                    Debug.Log($"Retrying in {reconnectDelay} seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(reconnectDelay));
                }
                else
                {
                    Debug.LogError("Max reconnection attempts reached");
                    throw;
                }
            }
        }
    }

    private void OnDestroy()
    {
        _cancellationTokenSource?.Cancel();
        Disconnect();
    }
}

