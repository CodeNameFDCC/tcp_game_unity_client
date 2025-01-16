// ClientHandler.cs
using UnityEngine;
using System;

public class ClientHandler
{

    private MessageParser messageParser;
    public event Action<byte> OnInt8Received;
    public event Action<short> OnInt16Received;
    public event Action<int> OnInt32Received;
    public event Action<float> OnFloatReceived;
    public event Action<bool> OnBoolReceived;
    public event Action<string> OnStringReceived;
    public event Action<Vector3> OnPositionReceived;
    public event Action<Quaternion> OnQuaternionReceived;
    public event Action<string> OnPingReceived;

    public event Action<int, Vector3, Quaternion> OnPlayerSpawn;
    public event Action<int> OnPlayerDespawn;
    public event Action<int, Vector3> OnPlayerPositionUpdate;
    public event Action<int, Quaternion> OnPlayerRotationUpdate;



    public ClientHandler()
    {
        messageParser = new MessageParser();
    }

    public void ClearEventHandlers()
    {
        OnInt8Received = null;
        OnInt16Received = null;
        OnInt32Received = null;
        OnFloatReceived = null;
        OnBoolReceived = null;
        OnStringReceived = null;
        OnPositionReceived = null;
        OnQuaternionReceived = null;
        OnPingReceived = null;


        OnPlayerSpawn = null;
        OnPlayerDespawn = null;
        OnPlayerPositionUpdate = null;
        OnPlayerRotationUpdate = null;
    }

    ~ClientHandler()
    {
        ClearEventHandlers();
    }

    public void HandleMessage(byte[] data)
    {
        try
        {
            if (data == null || data.Length == 0)
            {
                Debug.LogError("Received null or empty data");
                return;
            }

            // 메시지 길이 로깅 추가
            Debug.Log($"Received data. Length: {data.Length}, Raw data: {BitConverter.ToString(data)}");
            if (data.Length < 4)
            {
                throw new ArgumentException($"Data too short: {data.Length} bytes");
            }

            var (type, value) = messageParser.ParseMessage(data);
            Debug.Log($"Parsed message type: {type}");

            switch (type)
            {
                case MessageType.INT8_RESPONSE:
                    if (value is byte byteValue)
                    {
                        OnInt8Received?.Invoke(byteValue);
                    }
                    break;
                case MessageType.INT16_RESPONSE:
                    OnInt16Received?.Invoke((short)value);
                    break;
                case MessageType.INT32_RESPONSE:
                    OnInt32Received?.Invoke((int)value);
                    break;
                case MessageType.FLOAT_RESPONSE:
                    OnFloatReceived?.Invoke((float)value);
                    break;
                case MessageType.BOOL_RESPONSE:
                    OnBoolReceived?.Invoke((bool)value);
                    break;
                case MessageType.STRING_RESPONSE:
                    OnStringReceived?.Invoke((string)value);
                    break;
                case MessageType.POSITION_RESPONSE:
                    OnPositionReceived?.Invoke((Vector3)value);
                    break;
                case MessageType.QUATERNION_RESPONSE:
                    OnQuaternionReceived?.Invoke((Quaternion)value);
                    break;
                case MessageType.PING_RESPONSE:
                    OnStringReceived?.Invoke((string)value);
                    break;
                case MessageType.PLAYER_SPAWN:
                    var spawnData = (PlayerSpawnData)value;
                    OnPlayerSpawn?.Invoke(spawnData.PlayerId, spawnData.Position, spawnData.Rotation);
                    break;

                case MessageType.PLAYER_DESPAWN:
                    var despawnData = (PlayerDespawnData)value;
                    OnPlayerDespawn?.Invoke(despawnData.PlayerId);
                    break;

                case MessageType.PLAYER_POSITION_UPDATE:
                    var posData = (PlayerPositionData)value;
                    OnPlayerPositionUpdate?.Invoke(posData.PlayerId, posData.Position);
                    break;

                case MessageType.PLAYER_ROTATION_UPDATE:
                    var rotData = (PlayerRotationData)value;
                    OnPlayerRotationUpdate?.Invoke(rotData.PlayerId, rotData.Rotation);
                    break;
                default:
                    if ((int)type % 2 == 0) // If it's a response message
                    {
                        Debug.Log($"Received response: {value}");
                    }
                    else
                    {
                        throw new InvalidCastException($"Expected byte value but got {value?.GetType().Name ?? "null"}");
                    }
                    break;
            }
        }
        catch (ArgumentException ae)
        {
            Debug.LogError($"Invalid message data: {ae.Message}");
        }
        catch (InvalidCastException ice)
        {
            Debug.LogError($"Data type mismatch: {ice.Message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Unexpected error handling message: {e}");
        }
    }

    // Methods to create request messages
    public byte[] CreateInt8Request() => messageParser.CreateInt8Request();
    public byte[] CreateInt16Request() => messageParser.CreateInt16Request();
    public byte[] CreateInt32Request() => messageParser.CreateInt32Request();
    public byte[] CreateFloatRequest() => messageParser.CreateFloatRequest();
    public byte[] CreateBoolRequest() => messageParser.CreateBoolRequest();
    public byte[] CreateStringRequest() => messageParser.CreateStringRequest();
    public byte[] CreatePositionRequest() => messageParser.CreatePositionRequest();
    public byte[] CreateQuaternionRequest() => messageParser.CreateQuaternionRequest();

    // Methods to create response messages
    public byte[] CreateInt8Response(int value) => messageParser.CreateInt8Response(value);
    public byte[] CreateInt16Response(int value) => messageParser.CreateInt16Response(value);
    public byte[] CreateInt32Response(int value) => messageParser.CreateInt32Response(value);
    public byte[] CreateFloatResponse(float value) => messageParser.CreateFloatResponse(value);
    public byte[] CreateBoolResponse(bool value) => messageParser.CreateBoolResponse(value);
    public byte[] CreateStringResponse(string value) => messageParser.CreateStringResponse(value);
    public byte[] CreatePositionResponse(Vector3 position) => messageParser.CreatePositionResponse(position);
    public byte[] CreateQuaternionResponse(Quaternion rotation) => messageParser.CreateQuaternionResponse(rotation);

    public byte[] CreatePositionUpdateMessage(Vector3 position)
    {
        return messageParser.CreatePositionUpdateMessage(position);
    }

    public byte[] CreateRotationUpdateMessage(Quaternion rotation)
    {
        return messageParser.CreateRotationUpdateMessage(rotation);
    }
}
