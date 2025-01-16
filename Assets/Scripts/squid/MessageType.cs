// MessageType.cs

namespace squid_game
{
    public enum MessageType
    {
        HEARTBEAT = 0, // 하트비트 메시지 타입
        HEARTBEAT_ACK = 1, // 하트비트 응답 메시지 타입
        JOIN_REQUEST = 2, // 참가 요청 메시지 타입
        JOIN_SUCCESS = 3, // 참가 성공 메시지 타입
        PLAYER_LIST = 4, // 플레이어 목록 메시지 타입
        GAME_START = 5, // 게임 시작 메시지 타입
        GAME_END = 6, // 게임 종료 메시지 타입
        PLAYER_POSITION = 7, // 플레이어 위치 메시지 타입
        PLAYER_STATE = 8, // 플레이어 상태 메시지 타입
        TURN_STATE = 9, // 턴 상태 메시지 타입
        PLAYER_STATE_CHANGE = 10, // 플레이어 상태 변경 메시지 타입
        WAITING_TIME_UPDATE = 11, // 남은 시간 업데이트 메시지 타입
        ALL_POSITIONS = 12, // 모든 위치 메시지 타입
        PLAYER_LEFT = 13 // 플레이어 퇴장 메시지 타입
    }

}