// ClientMessageType.cs
public enum MessageType
{
    // 기본 데이터 타입 요청 및 응답
    // 기본 데이터 타입 요청 및 응답
    INT8_REQUEST = 0, // 정수 요청 8 비트
    INT8_RESPONSE = 1, // 정수 응답 8 비트
    INT16_REQUEST = 2, // 정수 요청 16 비트
    INT16_RESPONSE = 3, // 정수 응답 16 비트
    INT32_REQUEST = 4, // 정수 요청 32 비트
    INT32_RESPONSE = 5, // 정수 응답 32 비트

    FLOAT_REQUEST = 10, //실수 요청
    FLOAT_RESPONSE = 11, // 실수 응답

    BOOL_REQUEST = 20, // 불리언 요청
    BOOL_RESPONSE = 21, // 불리언 응답

    STRING_REQUEST = 30, // 문자열 요청
    STRING_RESPONSE = 31, // 문자열 응답

    POSITION_REQUEST = 40, // 위치 요청
    POSITION_RESPONSE = 41, // 위치 응답

    QUATERNION_REQUEST = 50, // 쿼터니언 요청
    QUATERNION_RESPONSE = 51, // 쿼터니언 응답

    PING_REQUEST = 60, // 핑 요청
    PING_RESPONSE = 61, // 핑 응답

    PLAYER_JOIN = 100,         // 플레이어 입장
    PLAYER_JOIN_ACK = 101,     // 입장 승인 (서버 -> 새로운 클라이언트)
    PLAYER_LEAVE = 102,        // 플레이어 퇴장
    PLAYER_LIST = 103,         // 현재 플레이어 목록
    PLAYER_SPAWN = 104,        // 다른 플레이어 스폰
    PLAYER_DESPAWN = 105,      // 플레이어 디스폰

    // 위치 동기화 관련
    PLAYER_POSITION_UPDATE = 110,  // 플레이어 위치 업데이트
    PLAYER_ROTATION_UPDATE = 111,  // 플레이어 회전 업데이트
    PLAYER_TRANSFORM_SYNC = 112,   // 전체 Transform 동기화
}