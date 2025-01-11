// src/Codes/Packets.cs

/**
 * 패킷을 직렬화하고 역직렬화하는 기능을 제공하는 클래스입니다.
 * 다양한 패킷 유형과 페이로드에 대한 정의를 포함하고 있습니다.
 */

using UnityEngine; // Unity 관련 기능을 사용하기 위한 네임스페이스
using ProtoBuf; // Protobuf 직렬화를 사용하기 위한 네임스페이스
using System.IO; // 파일 및 스트림 작업을 위한 네임스페이스
using System.Buffers; // 버퍼 작성을 위한 네임스페이스
using System.Collections.Generic; // 컬렉션을 사용하기 위한 네임스페이스
using System; // 기본 시스템 기능을 위한 네임스페이스

public class Packets : MonoBehaviour
{
    // 패킷 유형을 정의하는 열거형
    public enum PacketType { Ping, Normal, Location = 3 } // Ping, Normal, Location 패킷 유형

    // 핸들러 ID를 정의하는 열거형
    public enum HandlerIds
    {
        Init = 0, // 초기화 핸들러 ID
        LocationUpdate = 2 // 위치 업데이트 핸들러 ID 
    }

    // 직렬화 메서드
    public static void Serialize<T>(IBufferWriter<byte> writer, T data)
    {
        Serializer.Serialize(writer, data); // Protobuf를 사용하여 데이터를 직렬화
    }

    // 역직렬화 메서드
    public static T Deserialize<T>(byte[] data)
    {
        try
        {
            using (var stream = new MemoryStream(data)) // 메모리 스트림 생성
            {
                return ProtoBuf.Serializer.Deserialize<T>(stream); // Protobuf를 사용하여 데이터를 역직렬화
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Deserialize: Failed to deserialize data. Exception: {ex}"); // 오류 로그 출력
            throw; // 예외 재던지기
        }
    }
}

// 초기화 페이로드를 정의하는 클래스
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

// 공통 패킷을 정의하는 클래스
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
    public uint sequence { get; set; }  // 시퀀스 번호

    [ProtoMember(5)]
    public byte[] payload { get; set; } // 패킷 데이터
}

// 위치 업데이트 페이로드를 정의하는 클래스
[ProtoContract]
public class LocationUpdatePayload
{
    [ProtoMember(1)]
    public List<UserLocation> users { get; set; } // 사용자 위치 리스트

    // JSON 직렬화를 위한 기본 생성자
    public LocationUpdatePayload()
    {
        users = new List<UserLocation>(); // 사용자 위치 리스트 초기화
    }

    // 사용자 위치 정보를 정의하는 클래스
    [ProtoContract]
    public class UserLocation
    {
        [ProtoMember(1)]
        public string id { get; set; } // 사용자 ID

        [ProtoMember(2)]
        public float x { get; set; } // X 좌표

        [ProtoMember(3)]
        public float y { get; set; } // Y 좌표

        [ProtoMember(4)]
        public string status { get; set; } // 사용자 상태
    }
}

// 위치 업데이트를 정의하는 클래스
[ProtoContract]
public class LocationUpdate
{
    [ProtoMember(1)]
    public List<UserLocation> users { get; set; } // 사용자 위치 리스트

    // 기본 생성자
    public LocationUpdate()
    {
        users = new List<UserLocation>(); // 사용자 위치 리스트 초기화
    }

    // 사용자 위치 정보를 정의하는 클래스
    [ProtoContract]
    public class UserLocation
    {
        [ProtoMember(1)]
        public string id { get; set; } // 사용자 ID

        [ProtoMember(2)]
        public float x { get; set; } // X 좌표

        [ProtoMember(3)]
        public float y { get; set; } // Y 좌표

        [ProtoMember(4)]
        public string status { get; set; } // 사용자 상태

        [ProtoMember(5)]
        public uint playerId { get; set; } // 플레이어 ID

        [ProtoMember(6)]  // 추가 필드
        public long lastUpdateTime { get; set; } // 마지막 업데이트 시간
    }
}

// 응답 패킷을 정의하는 클래스
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
    public byte[] data { get; set; } // 응답 데이터
}
