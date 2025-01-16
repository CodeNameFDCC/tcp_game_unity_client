// 게임의 네트워크 통신을 담당하는 매니저 클래스
// TCP 소켓 통신을 사용하여 서버와 통신
using UnityEngine;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace squid_game
{


    public class NetworkManager : MonoBehaviour
    {
        // 싱글톤 인스턴스
        private static NetworkManager instance;

        // 싱글톤 패턴 구현을 위한 프로퍼티
        public static NetworkManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<NetworkManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("NetworkManager");
                        instance = go.AddComponent<NetworkManager>();
                    }
                }
                return instance;
            }
        }

        [System.Serializable]
        private class PlayerData
        {
            public int id;
            public string state;
        }

        [System.Serializable]
        private class PlayerListData
        {
            public PlayerData[] players;
        }

        [System.Serializable]
        private class PlayerListWrapper<T>
        {
            public List<T> items;
        }

        // 네트워크 통신 관련 변수들
        private TcpClient tcpClient;           // TCP 클라이언트
        private NetworkStream networkStream;    // 네트워크 스트림
        private byte[] receiveBuffer = new byte[1024];  // 수신 버퍼
        private bool isConnected;              // 서버 연결 상태
        private float lastHeartbeatResponseTime;  // 마지막 하트비트 응답 시간

        public int localPlayerId = -1; // 로컬 플레이어 ID 프로퍼티

        // Unity 초기화 시점에 호출되는 메서드
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);  // 씬 전환시에도 파괴되지 않도록 설정
            }
            else if (instance != this)
            {
                Destroy(gameObject);  // 중복 인스턴스 제거
            }
        }

        // 서버 연결 메서드
        public async Task ConnectToServer(string host, int port)
        {
            try
            {
                tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(host, port);  // 서버에 비동기 연결
                networkStream = tcpClient.GetStream();
                isConnected = true;
                lastHeartbeatResponseTime = Time.time;

                // 하트비트 관련 코루틴 시작
                StartCoroutine(HeartbeatCoroutine());
                StartCoroutine(ProcessHeartbeatResponse());
                StartMessageReceiving();  // 메시지 수신 시작

                Debug.Log($"Connected to server {host}:{port}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Server connection failed: {e.Message}");
                throw;
            }
        }

        // 메시지 수신 처리 메서드
        private async void StartMessageReceiving()
        {
            while (isConnected)
            {
                try
                {
                    // 비동기로 메시지 수신
                    int bytesRead = await networkStream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length);
                    if (bytesRead > 0)
                    {
                        ProcessMessage(new ArraySegment<byte>(receiveBuffer, 0, bytesRead));
                    }
                    else
                    {
                        isConnected = false;
                        break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error receiving message: {e.Message}");
                    isConnected = false;
                    break;
                }
            }

            HandleDisconnect();
        }

        // 수신된 메시지 처리 메서드
        private void ProcessMessage(ArraySegment<byte> data)
        {
            byte messageType = data.Array[data.Offset];  // 메시지 타입 추출
            var payload = new ArraySegment<byte>(data.Array, data.Offset + 1, data.Count - 1);  // 페이로드 추출

            // 메시지 타입에 따른 처리
            switch (messageType)
            {
                case (byte)MessageType.HEARTBEAT_ACK:  // 하트비트 응답
                    Debug.Log("하트비트 응답");
                    lastHeartbeatResponseTime = Time.time;
                    break;

                case (byte)MessageType.JOIN_REQUEST:  // 참가 요청
                    Debug.Log("참가 요청 수신");
                    // 서버에서 처리하므로 클라이언트에서는 특별한 처리 불필요
                    break;

                case (byte)MessageType.JOIN_SUCCESS:  // 게임 참가 성공
                    Debug.Log("연결 성공");
                    int playerId = BitConverter.ToInt32(payload.Array, payload.Offset);
                    GameManager.Instance.SetLocalPlayerId(playerId);
                    break;

                case (byte)MessageType.PLAYER_LIST:  // 플레이어 목록 수신
                    Debug.Log("플레이어 목록 수신");
                    ProcessPlayerList(payload);
                    break;

                case (byte)MessageType.GAME_START:  // 게임 시작
                    Debug.Log("게임 시작");
                    GameManager.Instance.StartGame();
                    break;

                case (byte)MessageType.GAME_END:  // 게임 종료
                    Debug.Log("게임 종료");
                    ProcessGameEnd(payload);
                    break;

                case (byte)MessageType.PLAYER_POSITION:  // 개별 플레이어 위치 업데이트
                    ProcessPlayerPosition(payload);
                    break;

                case (byte)MessageType.PLAYER_STATE:  // 플레이어 상태
                    Debug.Log("플레이어 상태 수신");
                    ProcessPlayerState(payload);
                    break;

                case (byte)MessageType.TURN_STATE:  // 턴 상태 변경
                    Debug.Log("턴 상태 변경");
                    bool isLooking = payload.Array[payload.Offset] == 1;
                    GameManager.Instance.UpdateTurnState(isLooking);
                    break;

                case (byte)MessageType.PLAYER_STATE_CHANGE:  // 플레이어 상태 변경
                    Debug.Log("플레이어 상태 변경");
                    ProcessPlayerStateChange(payload);
                    break;

                case (byte)MessageType.WAITING_TIME_UPDATE:  // 대기 시간 업데이트
                    int remainingTime = BitConverter.ToInt32(payload.Array, payload.Offset);
                    Debug.Log("시간 업데이트: " + remainingTime);
                    GameManager.Instance.UpdateWaitingTime(remainingTime);
                    break;

                case (byte)MessageType.ALL_POSITIONS:  // 모든 플레이어 위치 업데이트
                    ProcessAllPositions(payload);
                    break;

                case (byte)MessageType.PLAYER_LEFT:  // 플레이어 퇴장
                    ProcessPlayerLeft(payload);
                    break;
            }
        }

        // 플레이어 상태 변경 처리 메서드
        private void ProcessPlayerStateChange(ArraySegment<byte> payload)
        {
            try
            {
                // 데이터 길이 체크
                if (payload.Count < 4)
                {
                    Debug.LogError($"Invalid payload length: {payload.Count}");
                    return;
                }

                // 플레이어 ID (4바이트)
                int playerId = BitConverter.ToInt32(payload.Array, payload.Offset);

                // 상태 문자열 (나머지 바이트)
                int stateLength = payload.Count - 4;  // ID 길이(4바이트)를 뺀 나머지가 상태 문자열 길이
                if (stateLength <= 0)
                {
                    Debug.LogError($"No state data in payload. Total length: {payload.Count}");
                    return;
                }

                string state = System.Text.Encoding.UTF8.GetString(
                    payload.Array,
                    payload.Offset + 4,
                    stateLength
                );

                Debug.Log($"Processing state change - Player ID: {playerId}, New State: {state}");
                GameManager.Instance.UpdatePlayerState(playerId, state);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error processing player state change: {e.Message}\nPayload length: {payload.Count}\nStack trace: {e.StackTrace}");

                // 디버깅을 위한 추가 정보
                if (payload.Array != null)
                {
                    string hexData = BitConverter.ToString(payload.Array, payload.Offset, payload.Count);
                    Debug.Log($"Raw payload data: {hexData}");
                }
            }
        }

        // 하트비트 전송 코루틴
        private IEnumerator HeartbeatCoroutine()
        {
            while (isConnected)
            {
                SendHeartbeat().ConfigureAwait(false);
                yield return new WaitForSeconds(1f);  // 1초마다 하트비트 전송
            }
        }

        // 하트비트 전송 메서드
        private async Task SendHeartbeat()
        {
            try
            {
                byte[] heartbeat = new byte[1] { (byte)MessageType.HEARTBEAT };
                await networkStream.WriteAsync(heartbeat, 0, heartbeat.Length);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to send heartbeat: {e.Message}");
                isConnected = false;
            }
        }

        // 하트비트 응답 처리 코루틴
        private IEnumerator ProcessHeartbeatResponse()
        {
            while (isConnected)
            {
                // 5초 이상 하트비트 응답이 없으면 연결 끊김으로 처리
                if (Time.time - lastHeartbeatResponseTime > 5f)
                {
                    Debug.LogWarning("Heartbeat timeout");
                    isConnected = false;
                    break;
                }
                yield return new WaitForSeconds(1f);
            }

            if (!isConnected)
            {
                HandleDisconnect();
            }
        }

        // 플레이어 위치 전송 메서드
        public async Task SendPosition(Vector3 position)
        {
            if (!isConnected) return;

            try
            {
                byte[] message = new byte[13]; // 1(type) + 4(x) + 4(y) + 4(z)
                message[0] = (byte)MessageType.PLAYER_POSITION;

                byte[] xBytes = BitConverter.GetBytes(position.x);
                byte[] yBytes = BitConverter.GetBytes(position.y);
                byte[] zBytes = BitConverter.GetBytes(position.z);

                Buffer.BlockCopy(xBytes, 0, message, 1, 4);
                Buffer.BlockCopy(yBytes, 0, message, 5, 4);
                Buffer.BlockCopy(zBytes, 0, message, 9, 4);

                await networkStream.WriteAsync(message, 0, message.Length);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error sending position: {e.Message}");
                HandleDisconnect();
            }
        }

        // 플레이어 상태 전송 메서드
        public async Task SendPlayerState(string state)
        {
            if (!isConnected) return;

            try
            {
                // 상태 데이터 직렬화
                byte[] stateBytes = System.Text.Encoding.UTF8.GetBytes(state);
                byte[] message = new byte[1 + stateBytes.Length];
                message[0] = (byte)MessageType.PLAYER_STATE;
                Buffer.BlockCopy(stateBytes, 0, message, 1, stateBytes.Length);

                await networkStream.WriteAsync(message, 0, message.Length);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error sending player state: {e.Message}");
                HandleDisconnect();
            }
        }

        // 연결 끊김 처리 메서드
        private void HandleDisconnect()
        {
            isConnected = false;
            networkStream?.Dispose();
            tcpClient?.Dispose();

            // UI에 연결 끊김 표시
            var disconnectUI = GameObject.Find("DisconnectScreen");
            if (disconnectUI != null)
            {
                disconnectUI.SetActive(true);
            }
        }

        // Unity 오브젝트 파괴 시 호출되는 메서드
        private void OnDestroy()
        {
            isConnected = false;
            networkStream?.Dispose();
            tcpClient?.Dispose();
        }


        // 플레이어 목록 처리
        private void ProcessPlayerList(ArraySegment<byte> payload)
        {
            try
            {
                if (payload.Count < 4)
                {
                    Debug.LogError($"Invalid payload length: {payload.Count}");
                    return;
                }

                // 플레이어 수 읽기
                byte[] countBytes = new byte[4];
                Buffer.BlockCopy(payload.Array, payload.Offset, countBytes, 0, 4);
                int playerCount = BitConverter.ToInt32(countBytes, 0);
                Debug.Log($"Player count: {playerCount}");

                // 데이터 길이 체크
                int expectedLength = 4 + (playerCount * 4); // 플레이어 수(4) + (ID(4) * 플레이어수)
                if (payload.Count < expectedLength)
                {
                    Debug.LogError($"Data too short. Expected: {expectedLength}, Got: {payload.Count}");
                    return;
                }

                // 기존 플레이어 목록
                HashSet<int> currentPlayers = new HashSet<int>(GameManager.Instance.GetAllPlayerIds());
                HashSet<int> newPlayers = new HashSet<int>();

                // 플레이어 데이터 읽기
                int offset = payload.Offset + 4;
                for (int i = 0; i < playerCount; i++)
                {
                    byte[] idBytes = new byte[4];
                    Buffer.BlockCopy(payload.Array, offset, idBytes, 0, 4);
                    int playerId = BitConverter.ToInt32(idBytes, 0);
                    offset += 4;

                    Debug.Log($"Processing player ID: {playerId}");

                    if (playerId > 0 && playerId != GameManager.Instance.LocalPlayerId)
                    {
                        newPlayers.Add(playerId);
                        if (!GameManager.Instance.HasPlayer(playerId))
                        {
                            Debug.Log($"Creating remote player: {playerId}");
                            GameManager.Instance.CreateRemotePlayer(playerId);
                        }
                    }
                }

                // 연결 끊긴 플레이어 제거
                foreach (int id in currentPlayers)
                {
                    if (id != GameManager.Instance.LocalPlayerId && !newPlayers.Contains(id))
                    {
                        Debug.Log($"Removing disconnected player: {id}");
                        GameManager.Instance.RemovePlayer(id);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error processing player list: {e.Message}\nStack trace: {e.StackTrace}");
                if (payload.Array != null)
                {
                    string hexData = BitConverter.ToString(payload.Array, payload.Offset, payload.Count);
                    Debug.Log($"Raw data (hex): {hexData}");
                }
            }
        }





        // 게임 종료 처리
        private void ProcessGameEnd(ArraySegment<byte> payload)
        {
            try
            {
                int winnerId = BitConverter.ToInt32(payload.Array, payload.Offset);
                GameManager.Instance.HandleGameEnd(winnerId);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error processing game end: {e.Message}");
            }
        }

        // 개별 플레이어 위치 처리
        private void ProcessPlayerPosition(ArraySegment<byte> payload)
        {
            try
            {
                if (payload.Count < 16) // 4(playerId) + 4(x) + 4(y) + 4(z)
                {
                    Debug.LogError($"Invalid position data length: {payload.Count}");
                    return;
                }

                byte[] buffer = new byte[4];

                // 플레이어 ID 읽기
                Buffer.BlockCopy(payload.Array, payload.Offset, buffer, 0, 4);
                int playerId = BitConverter.ToInt32(buffer, 0);

                // x 좌표 읽기
                Buffer.BlockCopy(payload.Array, payload.Offset + 4, buffer, 0, 4);
                float x = BitConverter.ToSingle(buffer, 0);

                // y 좌표 읽기
                Buffer.BlockCopy(payload.Array, payload.Offset + 8, buffer, 0, 4);
                float y = BitConverter.ToSingle(buffer, 0);

                // z 좌표 읽기
                Buffer.BlockCopy(payload.Array, payload.Offset + 12, buffer, 0, 4);
                float z = BitConverter.ToSingle(buffer, 0);

                Vector3 position = new Vector3(x, y, z);
                Debug.Log($"Received position update for player {playerId}: {position}");

                GameManager.Instance.UpdatePlayerPosition(playerId, position);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error processing player position: {e.Message}");
            }
        }

        // 플레이어 상태 처리
        private void ProcessPlayerState(ArraySegment<byte> payload)
        {
            try
            {
                int playerId = BitConverter.ToInt32(payload.Array, payload.Offset);
                string state = System.Text.Encoding.UTF8.GetString(
                    payload.Array,
                    payload.Offset + 4,
                    payload.Count - (payload.Offset + 4)
                );
                GameManager.Instance.UpdatePlayerState(playerId, state);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error processing player state: {e.Message}");
            }
        }

        // 모든 플레이어 위치 처리
        private void ProcessAllPositions(ArraySegment<byte> payload)
        {
            try
            {
                int playerCount = BitConverter.ToInt32(payload.Array, payload.Offset);
                int offset = payload.Offset + 4;

                for (int i = 0; i < playerCount; i++)
                {
                    int playerId = BitConverter.ToInt32(payload.Array, offset);
                    offset += 4;
                    float x = BitConverter.ToSingle(payload.Array, offset);
                    offset += 4;
                    float y = BitConverter.ToSingle(payload.Array, offset);
                    offset += 4;
                    float z = BitConverter.ToSingle(payload.Array, offset);
                    offset += 4;

                    Vector3 position = new Vector3(x, y, z);
                    GameManager.Instance.UpdatePlayerPosition(playerId, position);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error processing all positions: {e.Message}");
            }
        }

        // 플레이어 퇴장 처리
        private void ProcessPlayerLeft(ArraySegment<byte> payload)
        {
            try
            {
                int playerId = BitConverter.ToInt32(payload.Array, payload.Offset);
                GameManager.Instance.RemovePlayer(playerId);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error processing player left: {e.Message}");
            }
        }
    }
}