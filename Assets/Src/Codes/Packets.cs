/**
 * 패킷을 직렬화하고 역직렬화하는 기능을 제공하는 클래스입니다.
 * 다양한 패킷 유형과 페이로드에 대한 정의를 포함하고 있습니다.
 */

using UnityEngine;
using ProtoBuf;
using System.IO;
using System.Buffers;
using System.Collections.Generic;
using System;

public class Packets : MonoBehaviour
{
    public enum PacketType { Ping, Normal, Location = 3 } // 패킷 유형 열거형
    public enum HandlerIds
    {
        Init = 0, // 초기화 핸들러 ID
        LocationUpdate = 2 // 위치 업데이트 핸들러 ID 
    }

    // 직렬화 메서드
    public static void Serialize<T>(IBufferWriter<byte> writer, T data)
    {
        Serializer.Serialize(writer, data); // Protobuf를 사용하여 직렬화
    }

    // 역직렬화 메서드
    public static T Deserialize<T>(byte[] data)
    {
        try
        {
            using (var stream = new MemoryStream(data))
            { // 메모리 스트림 생성
                return ProtoBuf.Serializer.Deserialize<T>(stream); // Protobuf를 사용하여 역직렬화
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Deserialize: Failed to deserialize data. Exception: {ex}"); // 오류 로그
            throw; // 예외 재던지기
        }
    }
}

[ProtoContract]
public class InitialPayload
{
    [ProtoMember(1, IsRequired = true)]
    public string deviceId { get; set; } // 디바이스 ID

    [ProtoMember(2, IsRequired = true)]
    public uint playerId { get; set; } // 플레이어 ID

    [ProtoMember(3, IsRequired = true)]
    public float latency { get; set; } // 지연 시간
}

[ProtoContract]
public class CommonPacket
{
    [ProtoMember(1)]
    public uint handlerId { get; set; } // 핸들러 ID

    [ProtoMember(2)]
    public string userId { get; set; } // 사용자 ID

    [ProtoMember(3)]
    public string version { get; set; } // 버전 정보

    [ProtoMember(4)]
    public byte[] payload { get; set; } // 페이로드 데이터
}

[ProtoContract]
public class LocationUpdatePayload
{
    [ProtoMember(1, IsRequired = true)]
    public float x { get; set; } // x 좌표
    [ProtoMember(2, IsRequired = true)]
    public float y { get; set; } // y 좌표
}

[ProtoContract]
public class LocationUpdate
{
    [ProtoMember(1)]
    public List<UserLocation> users { get; set; } // 사용자 위치 목록

    [ProtoContract]
    public class UserLocation
    {
        [ProtoMember(1)]
        public string id { get; set; } // 사용자 ID

        [ProtoMember(2)]
        public uint playerId { get; set; } // 플레이어 ID

        [ProtoMember(3)]
        public float x { get; set; } // x 좌표

        [ProtoMember(4)]
        public float y { get; set; } // y 좌표
    }
}

[ProtoContract]
public class Response
{
    [ProtoMember(1)]
    public uint handlerId { get; set; } // 핸들러 ID

    [ProtoMember(2)]
    public uint responseCode { get; set; } // 응답 코드

    [ProtoMember(3)]
    public long timestamp { get; set; } // 타임스탬프

    [ProtoMember(4)]
    public byte[] data { get; set; } // 데이터
}
