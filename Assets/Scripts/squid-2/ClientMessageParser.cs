// ClientMessageParser.cs
using System;
using UnityEngine;

public class MessageParser
{
    private BinaryHelper binaryHelper;

    public MessageParser()
    {
        binaryHelper = new BinaryHelper();
    }

    public byte[] CreateInt8Request()
    {
        binaryHelper.Reset();
        // MessageType을 Int32로 쓰기
        binaryHelper.WriteInt32((int)MessageType.INT8_REQUEST);
        byte[] result = binaryHelper.GetBuffer();
        Debug.Log($"Created Int8 request message. Length: {result.Length}, Data: {BitConverter.ToString(result)}");
        return result;
    }

    public byte[] CreateInt8Response(int value)
    {
        binaryHelper.Reset();
        binaryHelper.WriteInt32((int)MessageType.INT8_RESPONSE);
        binaryHelper.WriteInt8(value);
        return binaryHelper.GetBuffer();
    }

    public byte[] CreateInt16Request()
    {
        binaryHelper.Reset();
        binaryHelper.WriteInt32((int)MessageType.INT16_REQUEST);
        return binaryHelper.GetBuffer();
    }

    public byte[] CreateInt16Response(int value)
    {
        binaryHelper.Reset();
        binaryHelper.WriteInt32((int)MessageType.INT16_RESPONSE);
        binaryHelper.WriteInt16(value);
        return binaryHelper.GetBuffer();
    }

    public byte[] CreateInt32Request()
    {
        binaryHelper.Reset();
        binaryHelper.WriteInt32((int)MessageType.INT32_REQUEST);
        return binaryHelper.GetBuffer();
    }

    public byte[] CreateInt32Response(int value)
    {
        binaryHelper.Reset();
        binaryHelper.WriteInt32((int)MessageType.INT32_RESPONSE);
        binaryHelper.WriteInt32(value);
        return binaryHelper.GetBuffer();
    }

    public byte[] CreateFloatRequest()
    {
        binaryHelper.Reset();
        binaryHelper.WriteInt32((int)MessageType.FLOAT_REQUEST);
        return binaryHelper.GetBuffer();
    }

    public byte[] CreateFloatResponse(float value)
    {
        binaryHelper.Reset();
        binaryHelper.WriteInt32((int)MessageType.FLOAT_RESPONSE);
        binaryHelper.WriteFloat(value);
        return binaryHelper.GetBuffer();
    }

    public byte[] CreateBoolRequest()
    {
        binaryHelper.Reset();
        binaryHelper.WriteInt32((int)MessageType.BOOL_REQUEST);
        return binaryHelper.GetBuffer();
    }

    public byte[] CreateBoolResponse(bool value)
    {
        binaryHelper.Reset();
        binaryHelper.WriteInt32((int)MessageType.BOOL_RESPONSE);
        binaryHelper.WriteBool(value);
        return binaryHelper.GetBuffer();
    }

    public byte[] CreateStringRequest()
    {
        binaryHelper.Reset();
        binaryHelper.WriteInt32((int)MessageType.STRING_REQUEST);
        return binaryHelper.GetBuffer();
    }

    public byte[] CreateStringResponse(string value)
    {
        binaryHelper.Reset();
        binaryHelper.WriteInt32((int)MessageType.STRING_RESPONSE);
        binaryHelper.WriteString(value);
        return binaryHelper.GetBuffer();
    }

    public byte[] CreatePositionRequest()
    {
        binaryHelper.Reset();
        binaryHelper.WriteInt32((int)MessageType.POSITION_REQUEST);
        return binaryHelper.GetBuffer();
    }

    public byte[] CreatePositionResponse(Vector3 position)
    {
        binaryHelper.Reset();
        binaryHelper.WriteInt32((int)MessageType.POSITION_RESPONSE);
        binaryHelper.WritePosition(position);
        return binaryHelper.GetBuffer();
    }

    public byte[] CreateQuaternionRequest()
    {
        binaryHelper.Reset();
        binaryHelper.WriteInt32((int)MessageType.QUATERNION_REQUEST);
        return binaryHelper.GetBuffer();
    }

    public byte[] CreateQuaternionResponse(Quaternion rotation)
    {
        binaryHelper.Reset();
        binaryHelper.WriteInt32((int)MessageType.QUATERNION_RESPONSE);
        binaryHelper.WriteQuaternion(rotation);
        return binaryHelper.GetBuffer();
    }

    public byte[] CreatePositionUpdateMessage(Vector3 position)
    {
        binaryHelper.Reset();
        binaryHelper.WriteInt32((int)MessageType.PLAYER_POSITION_UPDATE);
        binaryHelper.WritePosition(position);
        return binaryHelper.GetBuffer();
    }

    public byte[] CreateRotationUpdateMessage(Quaternion rotation)
    {
        binaryHelper.Reset();
        binaryHelper.WriteInt32((int)MessageType.PLAYER_ROTATION_UPDATE);
        binaryHelper.WriteQuaternion(rotation);
        return binaryHelper.GetBuffer();
    }

    // 플레이어 스폰 메시지 생성
    public byte[] CreatePlayerSpawnMessage(int playerId, Vector3 position, Quaternion rotation)
    {
        binaryHelper.Reset();
        binaryHelper.WriteInt32((int)MessageType.PLAYER_SPAWN);
        binaryHelper.WriteInt32(playerId);
        binaryHelper.WritePosition(position);
        binaryHelper.WriteQuaternion(rotation);
        return binaryHelper.GetBuffer();
    }

    // 플레이어 디스폰 메시지 생성
    public byte[] CreatePlayerDespawnMessage(int playerId)
    {
        binaryHelper.Reset();
        binaryHelper.WriteInt32((int)MessageType.PLAYER_DESPAWN);
        binaryHelper.WriteInt32(playerId);
        return binaryHelper.GetBuffer();
    }

    private object ParsePlayerMessage(MessageType messageType)
    {
        switch (messageType)
        {
            case MessageType.PLAYER_SPAWN:
                return new PlayerSpawnData
                {
                    PlayerId = binaryHelper.ReadInt32(),
                    Position = binaryHelper.ReadPosition(),
                    Rotation = binaryHelper.ReadQuaternion()
                };

            case MessageType.PLAYER_DESPAWN:
                return new PlayerDespawnData
                {
                    PlayerId = binaryHelper.ReadInt32()
                };

            case MessageType.PLAYER_POSITION_UPDATE:
                return new PlayerPositionData
                {
                    PlayerId = binaryHelper.ReadInt32(),
                    Position = binaryHelper.ReadPosition()
                };

            case MessageType.PLAYER_ROTATION_UPDATE:
                return new PlayerRotationData
                {
                    PlayerId = binaryHelper.ReadInt32(),
                    Rotation = binaryHelper.ReadQuaternion()
                };

            default:
                throw new Exception($"Unknown player message type: {messageType}");
        }
    }


    public (MessageType type, object value) ParseMessage(byte[] buffer)
    {

        if (buffer == null || buffer.Length < 4)
            throw new ArgumentException($"Invalid buffer: {(buffer == null ? "null" : $"length {buffer.Length}")}");

        binaryHelper.SetBuffer(buffer);

        int rawType = binaryHelper.ReadInt32();
        if (!Enum.IsDefined(typeof(MessageType), rawType))
            throw new ArgumentException($"Unknown message type: {rawType}");

        MessageType messageType = (MessageType)rawType;
        return ParseMessageContent(messageType);

    }

    private (MessageType type, object value) ParseMessageContent(MessageType messageType)
    {

        // 플레이어 관련 메시지 처리
        switch (messageType)
        {
            case MessageType.PLAYER_SPAWN:
            case MessageType.PLAYER_DESPAWN:
            case MessageType.PLAYER_POSITION_UPDATE:
            case MessageType.PLAYER_ROTATION_UPDATE:
                return (messageType, ParsePlayerMessage(messageType));
        }


        switch (messageType)
        {
            case MessageType.INT8_REQUEST:
                return (messageType, null);
            case MessageType.INT8_RESPONSE:
                return (messageType, binaryHelper.ReadInt8());
            case MessageType.INT16_REQUEST:
                return (messageType, null);
            case MessageType.INT16_RESPONSE:
                return (messageType, binaryHelper.ReadInt16());
            case MessageType.INT32_REQUEST:
                return (messageType, null);
            case MessageType.INT32_RESPONSE:
                return (messageType, binaryHelper.ReadInt32());
            case MessageType.FLOAT_REQUEST:
                return (messageType, null);
            case MessageType.FLOAT_RESPONSE:
                return (messageType, binaryHelper.ReadFloat());
            case MessageType.BOOL_REQUEST:
                return (messageType, null);
            case MessageType.BOOL_RESPONSE:
                return (messageType, binaryHelper.ReadBool());
            case MessageType.STRING_REQUEST:
                return (messageType, null);
            case MessageType.STRING_RESPONSE:
                return (messageType, binaryHelper.ReadString());
            case MessageType.POSITION_REQUEST:
                return (messageType, null);
            case MessageType.POSITION_RESPONSE:
                return (messageType, binaryHelper.ReadPosition());
            case MessageType.QUATERNION_REQUEST:
                return (messageType, null);
            case MessageType.QUATERNION_RESPONSE:
                return (messageType, binaryHelper.ReadQuaternion());
            default:
                throw new System.Exception($"Unknown message type: {messageType}");
        }
    }
}