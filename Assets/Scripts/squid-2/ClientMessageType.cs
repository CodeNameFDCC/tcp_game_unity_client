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
}