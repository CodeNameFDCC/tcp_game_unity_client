//src/Codes/NetworkManager.cs

/* 네트워크를 통해 클라이언트와 서버 간의 연결을 관리하는 클래스입니다.
IP와 포트를 입력받아 서버에 연결하고, 데이터를 송수신합니다.
패킷의 생성 및 처리, 게임 시작 로직을 포함합니다. */

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager instance; // 네트워크 매니저 인스턴스

    public InputField ipInputField; // IP 주소를 입력받기 위한 입력 필드
    public InputField portInputField; // 포트 번호를 입력받기 위한 입력 필드
    public InputField deviceIdInputField; // 디바이스 ID를 입력받기 위한 입력 필드
    public GameObject uiNotice; // 사용자에게 알림을 표시하기 위한 UI 오브젝트
    private TcpClient tcpClient; // TCP 클라이언트 객체
    private NetworkStream stream; // 네트워크 통신을 위한 스트림

    WaitForSecondsRealtime wait; // 대기 시간을 설정하기 위한 변수

    private byte[] receiveBuffer = new byte[4096]; // 수신 데이터를 저장하기 위한 버퍼
    private List<byte> incompleteData = new List<byte>(); // 불완전한 데이터를 저장하기 위한 리스트

    private bool isReconnecting = false; // 재연결 여부를 나타내는 플래그
    private int maxReconnectAttempts = 3; // 최대 재연결 시도 횟수
    private float reconnectDelay = 5f; // 재연결 지연 시간 (초)
    private int currentReconnectAttempt = 0; // 현재 재연결 시도 횟수

    private float locationUpdateInterval = 1.0f; // 위치 업데이트 간격 (초)
    private float lastUpdateTime = 0f; // 마지막 업데이트 시간

    private uint currentSequence = 0;  // 시퀀스 번호 카운터

    // 시퀀스 번호를 증가시키고 반환하는 메서드
    private uint GetNextSequence()
    {
        return ++currentSequence;  // 시퀀스 값을 증가시키고 반환
    }

    private Queue<PacketData> packetQueue = new Queue<PacketData>(); // 패킷 데이터를 저장하기 위한 큐
    private object queueLock = new object(); // 큐 접근을 위한 잠금 객체

    // 패킷 데이터 구조체
    private struct PacketData
    {
        public Packets.PacketType Type { get; set; } // 패킷 타입
        public byte[] Data { get; set; } // 패킷 데이터
    }

    private Dictionary<string, ClientState> connectedClients = new Dictionary<string, ClientState>(); // 연결된 클라이언트 상태를 저장하는 딕셔너리

    // 각 클라이언트의 상태를 나타내는 클래스
    private class ClientState
    {
        public Vector2 lastPosition; // 마지막 위치
        public long lastUpdateTime; // 마지막 업데이트 시간
        public bool isActive; // 클라이언트의 활성 상태 여부
    }

    // MonoBehaviour의 Awake 메소드, 객체가 생성될 때 호출
    void Awake()
    {
        instance = this; // 네트워크 매니저 인스턴스를 초기화
        wait = new WaitForSecondsRealtime(5); // 5초 대기 시간 설정
    }


    void Update()
    {
        // 게임이 활성화되어 있고 TCP 클라이언트가 연결되어 있는 경우
        if (GameManager.instance.isLive && tcpClient.Connected)
        {
            // 정기적으로 모든 클라이언트의 위치 정보 전송
            if (Time.time - lastUpdateTime >= locationUpdateInterval) // 지정된 업데이트 간격이 지났는지 확인
            {
                // 현재 플레이어의 위치를 서버에 전송
                SendLocationUpdatePacket(GameManager.instance.player.transform.position.x,
                GameManager.instance.player.transform.position.y);
                lastUpdateTime = Time.time; // 마지막 업데이트 시간을 현재 시간으로 갱신
            }
        }

        // 큐에 있는 패킷 처리
        while (true) // 무한 루프를 통해 큐에 있는 모든 패킷을 처리
        {
            PacketData packet; // 패킷 데이터 구조체
            lock (queueLock) // 큐에 대한 동기화 잠금
            {
                // 큐가 비어있다면 루프 종료
                if (packetQueue.Count == 0)
                    break;
                // 큐에서 패킷을 꺼내기
                packet = packetQueue.Dequeue();
            }

            try
            {
                // 패킷 타입에 따라 처리
                switch (packet.Type)
                {
                    case Packets.PacketType.Normal:
                        HandleNormalPacket(packet.Data); // 일반 패킷 처리
                        break;
                    case Packets.PacketType.Location:
                        HandleLocationPacket(packet.Data); // 위치 패킷 처리
                        break;
                    case Packets.PacketType.Ping:
                        HandlePingPacket(packet.Data); // 핑 패킷 처리
                        break;
                    default:
                        // 알 수 없는 패킷 타입 경고 로그 출력
                        Debug.LogWarning($"Unknown packet type: {packet.Type}");
                        break;
                }
            }
            catch (Exception e)
            {
                // 패킷 처리 중 오류가 발생한 경우 오류 로그 출력
                Debug.LogError($"Error processing packet: {e.Message}");
            }
        }
    }

    // 핑 패킷을 처리하는 메소드
    private void HandlePingPacket(byte[] data)
    {
        Debug.Log("Ping packet received."); // 핑 패킷 수신 로그 출력
    }

    // 시작 버튼 클릭 시 호출되는 메소드
    public void OnStartButtonClicked()
    {
        // 입력 필드에서 IP와 포트 정보를 가져옴
        string ip = ipInputField.text;
        string port = portInputField.text;

        // 포트가 유효한지 확인
        if (IsValidPort(port))
        {
            int portNumber = int.Parse(port); // 포트 번호 정수로 변환

            // 디바이스 ID 입력 필드가 비어있지 않은 경우
            if (deviceIdInputField.text != "")
            {
                GameManager.instance.deviceId = deviceIdInputField.text; // 입력된 디바이스 ID 설정
            }
            // 디바이스 ID가 비어있는 경우 고유 ID 생성
            else if (GameManager.instance.deviceId == "")
            {
                GameManager.instance.deviceId = GenerateUniqueID(); // 고유 ID 생성
            }

            // 연결 시도 로그 출력
            Debug.Log($"Connecting with DeviceId: {GameManager.instance.deviceId}");

            // 서버에 연결
            if (ConnectToServer(ip, portNumber))
            {
                StartGame(); // 게임 시작
            }
            else
            {
                // 연결 실패 시 효과음 재생 및 알림 표시
                AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp);
                StartCoroutine(NoticeRoutine(1)); // 알림 루틴 시작
            }
        }
        else
        {
            // 포트가 유효하지 않은 경우
            AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp);
            StartCoroutine(NoticeRoutine(0)); // 알림 루틴 시작
        }
    }

    // IP 유효성 검사 메소드
    bool IsValidIP(string ip)
    {
        // 간단한 IP 유효성 검사: IP 주소 형식이 올바른지 확인
        return System.Net.IPAddress.TryParse(ip, out _);
    }

    // 포트 유효성 검사 메소드
    bool IsValidPort(string port)
    {
        // 간단한 포트 유효성 검사 (0 - 65535)
        if (int.TryParse(port, out int portNumber)) // 포트 문자열을 정수로 변환
        {
            return portNumber > 0 && portNumber <= 65535; // 유효한 포트 범위인지 확인
        }
        return false; // 변환 실패 시 false 반환
    }

    // 서버에 연결하는 메소드
    bool ConnectToServer(string ip, int port)
    {
        try
        {
            tcpClient = new TcpClient(ip, port); // TCP 클라이언트 생성 및 서버에 연결
            stream = tcpClient.GetStream(); // 네트워크 스트림 가져오기
            Debug.Log($"Connected to {ip}:{port}"); // 연결 성공 메시지 출력

            return true; // 연결 성공
        }
        catch (SocketException e) // 소켓 예외 발생 시
        {
            HandleError("Failed to connect to server", e); // 에러 메시지 처리
            return false; // 연결 실패
        }
    }

    // 고유 ID 생성 메소드
    string GenerateUniqueID()
    {
        return System.Guid.NewGuid().ToString(); // 고유 ID 생성
    }

    // 게임 시작 메소드
    void StartGame()
    {
        // 게임 시작 코드 작성
        Debug.Log("Game Started"); // 게임 시작 메시지 출력
        StartReceiving(); // 데이터 수신 시작
        SendInitialPacket(); // 초기 패킷 전송
        var player = GameManager.instance.player; // 현재 플레이어 객체 가져오기
        SendLocationUpdatePacket(player.transform.position.x, player.transform.position.y); // 플레이어 위치 업데이트 전송
    }

    // 알림 UI를 표시하고 숨기는 루틴
    IEnumerator NoticeRoutine(int index)
    {
        uiNotice.SetActive(true); // 알림 UI 활성화
        uiNotice.transform.GetChild(index).gameObject.SetActive(true); // 특정 알림 표시

        yield return wait; // 대기

        uiNotice.SetActive(false); // 알림 UI 비활성화
        uiNotice.transform.GetChild(index).gameObject.SetActive(false); // 특정 알림 숨김
    }

    // 바이트 배열을 빅 엔디안으로 변환하는 메소드
    public static byte[] ToBigEndian(byte[] bytes)
    {
        if (BitConverter.IsLittleEndian) // 현재 시스템이 리틀 엔디안인 경우
        {
            Array.Reverse(bytes); // 바이트 배열을 역순으로 변환
        }
        return bytes; // 변환된 배열 반환
    }

    // 패킷 헤더 생성 메소드
    byte[] CreatePacketHeader(int dataLength, Packets.PacketType packetType)
    {
        // 헤더 길이(4) + 패킷 타입(1) + 데이터 길이
        int packetLength = 4 + 1 + dataLength; // 전체 패킷 길이 계산
        byte[] header = new byte[5]; // 헤더 배열 생성

        // 패킷 길이를 빅 엔디안으로 변환
        byte[] lengthBytes = BitConverter.GetBytes(packetLength); // 패킷 길이 바이트 배열 생성
        lengthBytes = ToBigEndian(lengthBytes); // 빅 엔디안으로 변환
        Array.Copy(lengthBytes, 0, header, 0, 4); // 길이 바이트 복사

        // 패킷 타입 설정
        header[4] = (byte)packetType; // 패킷 타입 추가

        return header; // 생성된 헤더 반환
    }

    // 패킷 전송 메소드
    async void SendPacket<T>(T payload, uint handlerId)
    {
        var payloadWriter = new ArrayBufferWriter<byte>(); // 바이트 배열 작성기 생성
        Packets.Serialize(payloadWriter, payload); // 페이로드 직렬화
        byte[] payloadData = payloadWriter.WrittenSpan.ToArray(); // 직렬화된 데이터 배열로 변환

        // 공통 패킷 생성
        CommonPacket commonPacket = new CommonPacket
        {
            handlerId = handlerId, // 핸들러 ID 설정
            userId = GameManager.instance.deviceId, // 사용자 ID 설정
            version = GameManager.instance.version, // 버전 설정
            sequence = 0,  // 시퀀스 번호 관리 필요
            payload = payloadData // 페이로드 추가
        };

        var commonPacketWriter = new ArrayBufferWriter<byte>(); // 공통 패킷 작성기 생성
        Packets.Serialize(commonPacketWriter, commonPacket); // 공통 패킷 직렬화
        byte[] data = commonPacketWriter.WrittenSpan.ToArray(); // 직렬화된 데이터 배열로 변환

        // 디버그용 로그
        Debug.Log($"Sending packet - HandlerId: {handlerId}, UserId: {GameManager.instance.deviceId}, PayloadLength: {payloadData.Length}");

        // 패킷 헤더 생성
        byte[] header = CreatePacketHeader(data.Length, Packets.PacketType.Normal); // 패킷 헤더 생성
        byte[] packet = new byte[header.Length + data.Length]; // 전체 패킷 배열 생성
        Array.Copy(header, 0, packet, 0, header.Length); // 헤더 복사
        Array.Copy(data, 0, packet, header.Length, data.Length); // 데이터 복사

        await Task.Delay(GameManager.instance.latency); // 지연 시간 대기
        stream.Write(packet, 0, packet.Length); // 패킷 전송
    }

    // 위치 업데이트 패킷 전송 메소드
    public void SendLocationUpdatePacket(float x, float y)
    {
        if (!tcpClient.Connected) // 클라이언트가 연결되어 있지 않은 경우
        {
            Debug.LogWarning("Cannot send location update: Not connected"); // 경고 메시지 출력
            return; // 메소드 종료
        }
        try
        {
            // 서버 좌표로 변환
            float serverX = ConvertToServerX(x);
            float serverY = ConvertToServerY(y);

            // 위치 업데이트 객체 생성
            LocationUpdate locationUpdate = new LocationUpdate();
            locationUpdate.users.Add(new LocationUpdate.UserLocation
            {
                id = GameManager.instance.deviceId, // 사용자 ID 설정
                playerId = GameManager.instance.playerId,  // playerId 추가
                x = serverX, // 변환된 X 좌표 설정
                y = serverY, // 변환된 Y 좌표 설정
                status = "active", // 사용자 상태 설정
                lastUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() // 마지막 업데이트 시간 설정
            });

            // 위치 전송 로그 출력
            Debug.Log($"Sending position - Unity: ({x}, {y}), Server: ({serverX}, {serverY})");
            SendPacket(locationUpdate, (uint)Packets.HandlerIds.LocationUpdate); // 위치 업데이트 패킷 전송
        }
        catch (Exception e)
        {
            // 오류 발생 시 로그 출력
            Debug.LogError($"Error sending location update: {e.Message}\n{e.StackTrace}");
        }
    }

    // 좌표 변환 메서드들
    private float ConvertToUnityX(float serverX)
    {
        return serverX * GameManager.instance.gridSize; // 서버 X 좌표를 Unity X 좌표로 변환
    }

    private float ConvertToServerX(float unityX)
    {
        return unityX / GameManager.instance.gridSize; // Unity X 좌표를 서버 X 좌표로 변환
    }

    private float ConvertToUnityY(float serverY)
    {
        return -serverY * GameManager.instance.gridSize;  // 서버 Y 좌표를 Unity Y 좌표로 반전하여 변환
    }

    private float ConvertToServerY(float unityY)
    {
        return -unityY / GameManager.instance.gridSize;  // Unity Y 좌표를 서버 Y 좌표로 반전하여 변환
    }






    // 초기 패킷 전송 메소드
    void SendInitialPacket()
    {
        // 초기 패킷 생성
        InitialPayload initialPayload = new InitialPayload
        {
            deviceId = GameManager.instance.deviceId, // 디바이스 ID 설정
            playerId = GameManager.instance.playerId, // 플레이어 ID 설정
            latency = GameManager.instance.latency // 지연 시간 설정
        };

        // 초기 패킷 전송 로그 출력
        Debug.Log($"Sending initial packet - DeviceId: {initialPayload.deviceId}, PlayerId: {initialPayload.playerId}");
        SendPacket(initialPayload, (uint)Packets.HandlerIds.Init); // 초기 패킷 전송
    }

    // 데이터 수신 시작 메소드
    void StartReceiving()
    {
        _ = ReceivePacketsAsync(); // 비동기 데이터 수신 시작
    }

    // 비동기 패킷 수신 메소드
    async System.Threading.Tasks.Task ReceivePacketsAsync()
    {
        while (tcpClient.Connected) // TCP 클라이언트가 연결된 동안 반복
        {
            try
            {
                // 데이터 읽기
                int bytesRead = await stream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length);
                if (bytesRead > 0) // 읽은 바이트가 있을 경우
                {
                    ProcessReceivedData(receiveBuffer, bytesRead); // 수신 데이터 처리
                }
            }
            catch (Exception e)
            {
                HandleError("Error receiving data", e); // 수신 오류 메시지 처리
                break; // 루프 종료
            }
        }
    }

    // 수신 데이터 처리 메소드
    void ProcessReceivedData(byte[] data, int length)
    {
        try
        {
            // 수신한 데이터 추가
            incompleteData.AddRange(data.AsSpan(0, length).ToArray());

            // 패킷이 완전할 때까지 반복
            while (incompleteData.Count >= 5)
            {
                // 패킷 길이 확인
                byte[] lengthBytes = incompleteData.GetRange(0, 4).ToArray();
                int packetLength = BitConverter.ToInt32(ToBigEndian(lengthBytes), 0); // 패킷 길이 변환

                // 완전한 패킷을 수신할 때까지 대기
                if (incompleteData.Count < packetLength)
                {
                    return; // 패킷이 완전하지 않으면 종료
                }

                // 패킷 타입과 데이터 추출
                Packets.PacketType packetType = (Packets.PacketType)incompleteData[4];
                byte[] packetData = incompleteData.GetRange(5, packetLength - 5).ToArray();

                // 처리된 데이터 제거
                incompleteData.RemoveRange(0, packetLength);

                // 패킷을 큐에 추가
                lock (queueLock) // 큐에 대한 동기화 잠금
                {
                    packetQueue.Enqueue(new PacketData
                    {
                        Type = packetType, // 패킷 타입
                        Data = packetData // 패킷 데이터
                    });
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in ProcessReceivedData: {e.Message}"); // 처리 중 오류 발생 시 로그 출력
        }
    }

    // 일반 패킷 처리 메소드
    void HandleNormalPacket(byte[] packetData)
    {
        try
        {
            // 패킷 데이터 역직렬화
            var response = Packets.Deserialize<Response>(packetData);
            Debug.Log($"Received response - HandlerId: {response.handlerId}, ResponseCode: {response.responseCode}");

            // 응답 코드가 0이 아닌 경우 경고 표시
            if (response.responseCode != 0 && !uiNotice.activeSelf)
            {
                AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp); // 효과음 재생
                StartCoroutine(NoticeRoutine(2)); // 알림 루틴 시작
                return; // 메소드 종료
            }

            // 응답 데이터가 있는 경우 처리
            if (response.data != null && response.data.Length > 0)
            {
                if (response.handlerId == 0) // 초기 응답 처리
                {
                    GameManager.instance.GameStart(); // 게임 시작
                }
                else
                {
                    ProcessResponseData(response.data, response.handlerId); // 응답 데이터 처리
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing normal packet: {e.Message}"); // 오류 발생 시 로그 출력
        }
    }

    // 응답 데이터 처리 메소드
    void ProcessResponseData(byte[] data, uint handlerId)
    {
        try
        {
            // 데이터 문자열로 변환
            string jsonString = Encoding.UTF8.GetString(data);
            Debug.Log($"Processing response data for handlerId: {handlerId}, Data: {jsonString}");

            switch (handlerId)
            {
                case (uint)Packets.HandlerIds.LocationUpdate: // 위치 업데이트 핸들러
                    var locationUpdate = JsonUtility.FromJson<LocationUpdatePayload>(jsonString); // JSON 변환
                    if (locationUpdate != null && locationUpdate.users != null && locationUpdate.users.Count > 0)
                    {
                        var user = locationUpdate.users[0]; // 첫 번째 사용자 정보
                        Debug.Log($"Received location update - X: {user.x}, Y: {user.y}, Status: {user.status}");
                        // 위치 업데이트 처리 로직 추가
                    }
                    break;
                    // 다른 핸들러 케이스 추가
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing response data: {e.Message}"); // 처리 중 오류 발생 시 로그 출력
        }
    }

    // 위치 패킷 처리 메소드
    void HandleLocationPacket(byte[] data)
    {
        try
        {
            // 위치 업데이트 패킷 역직렬화
            LocationUpdate response = Packets.Deserialize<LocationUpdate>(data);
            if (response.users != null && response.users.Count > 0) // 사용자 정보가 있는 경우
            {
                LocationUpdate convertedResponse = new LocationUpdate
                {
                    users = new List<LocationUpdate.UserLocation>() // 사용자 위치 리스트 초기화
                };

                foreach (var user in response.users) // 각 사용자에 대해 반복
                {
                    // Unity 좌표계로 변환
                    float unityX = ConvertToUnityX(user.x);
                    float unityY = ConvertToUnityY(user.y);

                    // 클라이언트 상태 업데이트
                    if (!connectedClients.ContainsKey(user.id))
                    {
                        connectedClients[user.id] = new ClientState(); // 새로운 클라이언트 상태 추가
                    }

                    var clientState = connectedClients[user.id];
                    clientState.lastPosition = new Vector2(unityX, unityY); // 마지막 위치 업데이트
                    clientState.lastUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // 마지막 업데이트 시간
                    clientState.isActive = true; // 클라이언트 활성화

                    // 변환된 사용자 정보 추가
                    var convertedUser = new LocationUpdate.UserLocation
                    {
                        id = user.id,
                        x = unityX,
                        y = unityY,
                        status = user.status,
                        playerId = user.playerId,
                        lastUpdateTime = clientState.lastUpdateTime
                    };

                    convertedResponse.users.Add(convertedUser); // 변환된 사용자 리스트에 추가
                    Debug.Log($"User {user.id} - Server: ({user.x}, {user.y}), Unity: ({unityX}, {unityY}), Status: {user.status}, LastUpdate: {clientState.lastUpdateTime}");
                }

                // 비활성 클라이언트 처리
                long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                foreach (var client in connectedClients.ToList())
                {
                    if (currentTime - client.Value.lastUpdateTime > 10000) // 10초 이상 업데이트가 없는 경우
                    {
                        client.Value.isActive = false; // 클라이언트 비활성화
                                                       // 비활성 상태의 클라이언트도 위치 정보에 포함
                        convertedResponse.users.Add(new LocationUpdate.UserLocation
                        {
                            id = client.Key,
                            x = client.Value.lastPosition.x,
                            y = client.Value.lastPosition.y,
                            status = "inactive", // 비활성 상태 설정
                            lastUpdateTime = client.Value.lastUpdateTime
                        });
                    }
                }

                Spawner.instance.Spawn(convertedResponse); // 사용자 위치 정보 스폰
            }
        }
        catch (Exception e)
        {
            HandleError("Error processing location packet", e); // 오류 발생 시 처리
        }
    }

    // 오류 처리 메소드
    void HandleError(string message, Exception e = null)
    {
        // 네트워크 오류 메시지 로그 출력
        Debug.LogError($"Network Error: {message}");

        // 예외 정보가 주어진 경우 로그 출력
        if (e != null)
        {
            Debug.LogError($"Exception: {e}");
        }

        // 효과음 재생
        AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp);
        // 알림 루틴 시작
        StartCoroutine(NoticeRoutine(2));

        // TCP 클라이언트가 연결되어 있지 않고 재연결 중이 아닌 경우
        if (tcpClient != null && !tcpClient.Connected && !isReconnecting)
        {
            // 재연결 시도 루틴 시작
            StartCoroutine(TryReconnect());
        }
    }

    // 재연결 시도 루틴
    IEnumerator TryReconnect()
    {
        // 이미 재연결 중인 경우 루틴 종료
        if (isReconnecting) yield break;

        isReconnecting = true; // 재연결 중 상태 설정
        currentReconnectAttempt = 0; // 현재 재연결 시도 횟수 초기화

        // TCP 클라이언트가 연결되지 않았고 최대 재연결 시도 횟수에 도달하지 않은 경우
        while (!tcpClient.Connected && currentReconnectAttempt < maxReconnectAttempts)
        {
            try
            {
                // 기존 TCP 클라이언트 닫기
                if (tcpClient != null)
                {
                    tcpClient.Close();
                    tcpClient = null;
                }

                currentReconnectAttempt++; // 재연결 시도 횟수 증가
                Debug.Log($"Attempting to reconnect... Attempt {currentReconnectAttempt}/{maxReconnectAttempts}");

                // 서버에 재연결 시도
                if (ConnectToServer(ipInputField.text, int.Parse(portInputField.text)))
                {
                    stream = tcpClient.GetStream(); // 네트워크 스트림 가져오기
                    StartReceiving(); // 데이터 수신 시작
                    SendInitialPacket(); // 초기 패킷 전송
                    isReconnecting = false; // 재연결 상태 해제
                    Debug.Log("Reconnection successful!"); // 재연결 성공 메시지 출력
                    yield break; // 루틴 종료
                }
            }
            catch (Exception e)
            {
                // 재연결 시도 중 오류 발생 시 로그 출력
                Debug.LogError($"Reconnection attempt failed: {e.Message}");
            }

            // 재연결 지연 시간 대기
            yield return new WaitForSeconds(reconnectDelay);
        }

        // 재연결 시도 후 여전히 연결되지 않은 경우
        if (!tcpClient.Connected)
        {
            Debug.LogError("Failed to reconnect after maximum attempts"); // 재연결 실패 메시지 출력
            StartCoroutine(NoticeRoutine(1)); // 알림 루틴 시작
        }

        isReconnecting = false; // 재연결 상태 해제
    }

    // 애플리케이션 종료 시 호출되는 메소드
    void OnApplicationQuit()
    {
        // TCP 클라이언트가 연결되어 있는 경우 클라이언트 닫기
        if (tcpClient != null && tcpClient.Connected)
        {
            tcpClient.Close(); // 클라이언트 종료
        }
    }

}

