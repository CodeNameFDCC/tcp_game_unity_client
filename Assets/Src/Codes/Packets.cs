using UnityEngine;
using ProtoBuf;
using System.IO;
using System.Buffers;
using System.Collections.Generic;
using System;

public class Packets : MonoBehaviour
{
    // 패킷 유형 정의
    public enum PacketType { Ping, Normal, GameStart, Location = 3 }

    // 핸들러 ID 정의
    public enum HandlerIds
    {
        Init = 0, // 초기화 핸들러
        LocationUpdate = 2 // 위치 업데이트 핸들러
    }

    // 객체를 직렬화하는 메서드
    public static void Serialize<T>(IBufferWriter<byte> writer, T data)
    {
        Serializer.Serialize(writer, data); // 주어진 데이터를 직렬화하여 writer에 기록
    }

    // 바이트 배열을 역직렬화하여 객체로 변환하는 메서드
    public static T Deserialize<T>(byte[] data)
    {
        try
        {
            using (var stream = new MemoryStream(data))
            {
                return ProtoBuf.Serializer.Deserialize<T>(stream); // 데이터 스트림에서 객체로 역직렬화
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Deserialize: Failed to deserialize data. Exception: {ex}"); // 에러 로그 출력
            throw; // 예외를 다시 던짐
        }
    }
}

// Ping 패킷 정의
[ProtoContract]
public class Ping
{
    [ProtoMember(1)]
    public ulong timestamp { get; set; } // 타임스탬프 (부호 없는 64비트 정수)
}

// 초기 페이로드 정의
[ProtoContract]
public class InitialPayload
{
    [ProtoMember(1, IsRequired = true)]
    public string deviceId { get; set; } // 장치 ID (문자열)

    [ProtoMember(2, IsRequired = true)]
    public uint playerId { get; set; } // 플레이어 ID (부호 없는 32비트 정수)

    [ProtoMember(3, IsRequired = true)]
    public float latency { get; set; } // 지연 시간 (부동소수점 수)

    [ProtoMember(4, IsRequired = true)]
    public float speed { get; set; } // 속도 (부동소수점 수)
}

// 공통 패킷 정의
[ProtoContract]
public class CommonPacket
{
    [ProtoMember(1)]
    public uint handlerId { get; set; } // 핸들러 ID (부호 없는 32비트 정수)

    [ProtoMember(2)]
    public string userId { get; set; } // 사용자 ID (문자열)

    [ProtoMember(3)]
    public string version { get; set; } // 버전 정보 (문자열)

    [ProtoMember(4)]
    public byte[] payload { get; set; } // 페이로드 데이터 (바이트 배열)
}

// 위치 업데이트 페이로드 정의
[ProtoContract]
public class LocationUpdatePayload
{
    [ProtoMember(1, IsRequired = true)]
    public float x { get; set; } // x 좌표 (부동소수점 수)

    [ProtoMember(2, IsRequired = true)]
    public float y { get; set; } // y 좌표 (부동소수점 수)

    [ProtoMember(3, IsRequired = true)]
    public float inputX { get; set; } // 입력 x 좌표 (부동소수점 수)

    [ProtoMember(4, IsRequired = true)]
    public float inputY { get; set; } // 입력 y 좌표 (부동소수점 수)
}

// 위치 업데이트 패킷 정의
[ProtoContract]
public class LocationUpdate
{
    [ProtoMember(1)]
    public List<UserLocation> users { get; set; } // 사용자 위치 목록

    // 사용자 위치 정의
    [ProtoContract]
    public class UserLocation
    {
        [ProtoMember(1)]
        public string id { get; set; } // 사용자 ID (문자열)

        [ProtoMember(2)]
        public uint playerId { get; set; } // 플레이어 ID (부호 없는 32비트 정수)

        [ProtoMember(3)]
        public float x { get; set; } // x 좌표 (부동소수점 수)

        [ProtoMember(4)]
        public float y { get; set; } // y 좌표 (부동소수점 수)
    }
}

// 초기 응답 정의
[ProtoContract]
public class InitialResponse
{
    [ProtoMember(1)]
    public string gameId { get; set; } // 게임 ID (문자열)

    [ProtoMember(2)]
    public ulong timestamp { get; set; } // 타임스탬프 (부호 없는 64비트 정수)

    [ProtoMember(3)]
    public float x { get; set; } // x 좌표 (부동소수점 수)

    [ProtoMember(4)]
    public float y { get; set; } // y 좌표 (부동소수점 수)
}

// 응답 정의
[ProtoContract]
public class Response
{
    [ProtoMember(1)]
    public uint handlerId { get; set; } // 핸들러 ID (부호 없는 32비트 정수)

    [ProtoMember(2)]
    public uint responseCode { get; set; } // 응답 코드 (부호 없는 32비트 정수)

    [ProtoMember(3)]
    public ulong timestamp { get; set; } // 타임스탬프 (부호 없는 64비트 정수)

    [ProtoMember(4)]
    public byte[] data { get; set; } // 응답 데이터 (바이트 배열)
}
