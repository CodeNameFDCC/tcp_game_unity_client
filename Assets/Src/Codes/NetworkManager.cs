//src/Codes/NetworkManager.cs

/* 네트워크를 통해 클라이언트와 서버 간의 연결을 관리하는 클래스입니다.
IP와 포트를 입력받아 서버에 연결하고, 데이터를 송수신합니다.
패킷의 생성 및 처리, 게임 시작 로직을 포함합니다. */

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager instance; // 네트워크 매니저 인스턴스

    public InputField ipInputField; // IP 입력 필드
    public InputField portInputField; // 포트 입력 필드
    public InputField deviceIdInputField; // 디바이스 ID 입력 필드
    public GameObject uiNotice; // 사용자 알림 UI
    private TcpClient tcpClient; // TCP 클라이언트
    private NetworkStream stream; // 네트워크 스트림

    WaitForSecondsRealtime wait; // 대기 시간

    private byte[] receiveBuffer = new byte[4096]; // 수신 버퍼
    private List<byte> incompleteData = new List<byte>(); // 불완전한 데이터 저장 리스트

    private bool isReconnecting = false;
    private int maxReconnectAttempts = 3;
    private float reconnectDelay = 5f;
    private int currentReconnectAttempt = 0;

    private uint currentSequence = 0;  // sequence 카운터 추가

    // sequence 번호를 증가시키고 반환하는 메서드
    private uint GetNextSequence()
    {
        return ++currentSequence;  // 시퀀스 값을 증가시키고 반환
    }

    private Queue<PacketData> packetQueue = new Queue<PacketData>();
    private object queueLock = new object();

    // 패킷 데이터 구조체
    private struct PacketData
    {
        public Packets.PacketType Type { get; set; }
        public byte[] Data { get; set; }
    }


    void Awake()
    {
        instance = this; // 인스턴스 초기화
        wait = new WaitForSecondsRealtime(5); // 5초 대기 시간 설정
    }

    void Update()
    {
        // 큐에 있는 패킷 처리
        while (true)
        {
            PacketData packet;
            lock (queueLock)
            {
                if (packetQueue.Count == 0)
                    break;
                packet = packetQueue.Dequeue();
            }

            try
            {
                switch (packet.Type)
                {
                    case Packets.PacketType.Normal:
                        HandleNormalPacket(packet.Data);
                        break;
                    case Packets.PacketType.Location:
                        HandleLocationPacket(packet.Data);
                        break;
                    case Packets.PacketType.Ping:
                        HandlePingPacket(packet.Data);
                        break;
                    default:
                        Debug.LogWarning($"Unknown packet type: {packet.Type}");
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error processing packet: {e.Message}");
            }
        }
    }

    private void HandlePingPacket(byte[] data)
    {
        Debug.Log("Ping packet received.");
    }

    // public void OnStartButtonClicked()
    // {
    //     string ip = ipInputField.text; // 입력된 IP
    //     string port = portInputField.text; // 입력된 포트

    //     if (IsValidPort(port)) // 포트 유효성 검사
    //     {
    //         int portNumber = int.Parse(port); // 포트 번호로 변환

    //         if (deviceIdInputField.text != "") // 디바이스 ID가 입력된 경우
    //         {
    //             GameManager.instance.deviceId = deviceIdInputField.text; // 입력된 ID 사용
    //         }
    //         else
    //         {
    //             if (GameManager.instance.deviceId == "") // 디바이스 ID가 비어있는 경우
    //             {
    //                 GameManager.instance.deviceId = GenerateUniqueID(); // 고유 ID 생성
    //             }
    //         }

    //         if (ConnectToServer(ip, portNumber)) // 서버에 연결 성공
    //         {
    //             StartGame(); // 게임 시작
    //         }
    //         else
    //         {
    //             AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp); // 실패 사운드 재생
    //             StartCoroutine(NoticeRoutine(1)); // 알림 표시
    //         }

    //     }
    //     else // 포트가 유효하지 않은 경우
    //     {
    //         AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp); // 실패 사운드 재생
    //         StartCoroutine(NoticeRoutine(0)); // 알림 표시
    //     }
    // }


    // public void OnStartButtonClicked()
    // {
    //     string ip = ipInputField.text;
    //     string port = portInputField.text;

    //     if (IsValidPort(port))
    //     {
    //         int portNumber = int.Parse(port);

    //         // 디바이스 ID 설정 로직
    //         if (deviceIdInputField.text != "")
    //         {
    //             GameManager.instance.deviceId = deviceIdInputField.text;
    //         }
    //         else if (GameManager.instance.deviceId == "")
    //         {
    //             GameManager.instance.deviceId = GenerateUniqueID();
    //         }

    //         // 서버 연결 시도
    //         if (ConnectToServer(ip, portNumber))
    //         {
    //             // 초기 패킷 전송을 통한 게임 시작
    //             SendInitialPacket();
    //             StartGame();  // 서버 연결 성공 시 StartGame 호출;
    //         }
    //         else
    //         {
    //             AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp);
    //             StartCoroutine(NoticeRoutine(1));
    //         }
    //     }
    //     else
    //     {
    //         AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp);
    //         StartCoroutine(NoticeRoutine(0));
    //     }
    // }


    public void OnStartButtonClicked()
    {
        string ip = ipInputField.text;
        string port = portInputField.text;

        if (IsValidPort(port))
        {
            int portNumber = int.Parse(port);

            if (deviceIdInputField.text != "")
            {
                GameManager.instance.deviceId = deviceIdInputField.text;
            }
            else if (GameManager.instance.deviceId == "")
            {
                GameManager.instance.deviceId = GenerateUniqueID();
            }

            Debug.Log($"Connecting with DeviceId: {GameManager.instance.deviceId}");

            if (ConnectToServer(ip, portNumber))
            {
                StartGame();
            }
            else
            {
                AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp);
                StartCoroutine(NoticeRoutine(1));
            }
        }
        else
        {
            AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp);
            StartCoroutine(NoticeRoutine(0));
        }
    }



    bool IsValidIP(string ip)
    {
        // 간단한 IP 유효성 검사
        return System.Net.IPAddress.TryParse(ip, out _);
    }

    bool IsValidPort(string port)
    {
        // 간단한 포트 유효성 검사 (0 - 65535)
        if (int.TryParse(port, out int portNumber))
        {
            return portNumber > 0 && portNumber <= 65535;
        }
        return false;
    }

    bool ConnectToServer(string ip, int port)
    {
        try
        {
            tcpClient = new TcpClient(ip, port); // TCP 클라이언트 생성
            stream = tcpClient.GetStream(); // 네트워크 스트림 가져오기
            Debug.Log($"Connected to {ip}:{port}"); // 연결 성공 메시지

            return true; // 연결 성공
        }
        catch (SocketException e)
        {
            HandleError("Failed to connect to server", e); // 예외 발생 시 에러 메시지
            return false; // 연결 실패
        }
    }

    string GenerateUniqueID()
    {
        return System.Guid.NewGuid().ToString(); // 고유 ID 생성
    }

    void StartGame()
    {
        // 게임 시작 코드 작성
        Debug.Log("Game Started"); // 게임 시작 메시지
        StartReceiving(); // 데이터 수신 시작
        SendInitialPacket(); // 초기 패킷 전송
    }

    IEnumerator NoticeRoutine(int index)
    {
        uiNotice.SetActive(true); // 알림 UI 활성화
        uiNotice.transform.GetChild(index).gameObject.SetActive(true); // 특정 알림 표시

        yield return wait; // 대기

        uiNotice.SetActive(false); // 알림 UI 비활성화
        uiNotice.transform.GetChild(index).gameObject.SetActive(false); // 특정 알림 숨김
    }

    public static byte[] ToBigEndian(byte[] bytes)
    {
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes); // 리틀 엔디안이면 바이트 배열 역순
        }
        return bytes; // 변환된 배열 반환
    }



    // byte[] CreatePacketHeader(int dataLength, Packets.PacketType packetType)
    // {
    //     int packetLength = 4 + 1 + dataLength; // 전체 패킷 길이 (헤더 포함)
    //     byte[] header = new byte[5]; // 4바이트 길이 + 1바이트 타입

    //     // 첫 4바이트: 패킷 전체 길이
    //     byte[] lengthBytes = BitConverter.GetBytes(packetLength);
    //     lengthBytes = ToBigEndian(lengthBytes); // 빅 엔디안으로 변환
    //     Array.Copy(lengthBytes, 0, header, 0, 4); // 길이 복사

    //     // 다음 1바이트: 패킷 타입
    //     header[4] = (byte)packetType; // 타입 설정

    //     return header; // 헤더 반환
    // }




    // 공통 패킷 생성 함수

    byte[] CreatePacketHeader(int dataLength, Packets.PacketType packetType)
    {
        // 헤더 길이(4) + 패킷 타입(1) + 데이터 길이
        int packetLength = 4 + 1 + dataLength;
        byte[] header = new byte[5];

        // 패킷 길이를 빅 엔디안으로 변환
        byte[] lengthBytes = BitConverter.GetBytes(packetLength);
        lengthBytes = ToBigEndian(lengthBytes);
        Array.Copy(lengthBytes, 0, header, 0, 4);

        // 패킷 타입
        header[4] = (byte)packetType;

        return header;
    }


    async void SendPacket<T>(T payload, uint handlerId)
    {
        var payloadWriter = new ArrayBufferWriter<byte>();
        Packets.Serialize(payloadWriter, payload);
        byte[] payloadData = payloadWriter.WrittenSpan.ToArray();

        CommonPacket commonPacket = new CommonPacket
        {
            handlerId = handlerId,
            userId = GameManager.instance.deviceId,
            version = GameManager.instance.version,
            sequence = 0,  // 시퀀스 번호 관리 필요
            payload = payloadData
        };

        var commonPacketWriter = new ArrayBufferWriter<byte>();
        Packets.Serialize(commonPacketWriter, commonPacket);
        byte[] data = commonPacketWriter.WrittenSpan.ToArray();

        // 디버그용 로그
        Debug.Log($"Sending packet - HandlerId: {handlerId}, UserId: {GameManager.instance.deviceId}, PayloadLength: {payloadData.Length}");

        byte[] header = CreatePacketHeader(data.Length, Packets.PacketType.Normal);
        byte[] packet = new byte[header.Length + data.Length];
        Array.Copy(header, 0, packet, 0, header.Length);
        Array.Copy(data, 0, packet, header.Length, data.Length);

        await Task.Delay(GameManager.instance.latency);
        stream.Write(packet, 0, packet.Length);
    }


    public void SendLocationUpdatePacket(float x, float y)
    {
        if (!tcpClient.Connected)
        {
            Debug.LogWarning("Cannot send location update: Not connected");
            return;
        }
        try
        {
            float serverX = ConvertToServerX(x);
            float serverY = ConvertToServerY(y);

            LocationUpdate locationUpdate = new LocationUpdate();
            locationUpdate.users.Add(new LocationUpdate.UserLocation
            {
                id = GameManager.instance.deviceId,
                playerId = GameManager.instance.playerId,  // playerId 추가
                x = serverX,
                y = serverY,
                status = "active"
            });

            Debug.Log($"Sending position - Unity: ({x}, {y}), Server: ({serverX}, {serverY})");
            SendPacket(locationUpdate, (uint)Packets.HandlerIds.LocationUpdate);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending location update: {e.Message}\n{e.StackTrace}");
        }
    }

    // 좌표 변환 메서드들
    private float ConvertToUnityX(float serverX)
    {
        return serverX * GameManager.instance.gridSize;
    }

    private float ConvertToServerX(float unityX)
    {
        return unityX / GameManager.instance.gridSize;
    }

    private float ConvertToUnityY(float serverY)
    {
        return -serverY * GameManager.instance.gridSize;  // Y축 반전
    }

    private float ConvertToServerY(float unityY)
    {
        return -unityY / GameManager.instance.gridSize;  // Y축 반전
    }





    void SendInitialPacket()
    {
        InitialPayload initialPayload = new InitialPayload
        {
            deviceId = GameManager.instance.deviceId,
            playerId = GameManager.instance.playerId,
            latency = GameManager.instance.latency
        };

        Debug.Log($"Sending initial packet - DeviceId: {initialPayload.deviceId}, PlayerId: {initialPayload.playerId}");
        SendPacket(initialPayload, (uint)Packets.HandlerIds.Init);
    }

    void StartReceiving()
    {
        _ = ReceivePacketsAsync(); // 비동기 데이터 수신 시작
    }

    async System.Threading.Tasks.Task ReceivePacketsAsync()
    {
        while (tcpClient.Connected) // TCP 클라이언트가 연결된 동안
        {
            try
            {
                int bytesRead = await stream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length); // 데이터 읽기
                if (bytesRead > 0) // 읽은 바이트가 있을 경우
                {
                    ProcessReceivedData(receiveBuffer, bytesRead); // 수신 데이터 처리
                }
            }
            catch (Exception e)
            {
                HandleError("Error receiving data", e); // 수신 오류 메시지
                break; // 루프 종료
            }
        }
    }


    void ProcessReceivedData(byte[] data, int length)
    {
        try
        {
            incompleteData.AddRange(data.AsSpan(0, length).ToArray());

            while (incompleteData.Count >= 5)
            {
                // 패킷 길이 확인
                byte[] lengthBytes = incompleteData.GetRange(0, 4).ToArray();
                int packetLength = BitConverter.ToInt32(ToBigEndian(lengthBytes), 0);

                // 완전한 패킷을 수신할 때까지 대기
                if (incompleteData.Count < packetLength)
                {
                    return;
                }

                Packets.PacketType packetType = (Packets.PacketType)incompleteData[4];
                byte[] packetData = incompleteData.GetRange(5, packetLength - 5).ToArray();

                // 처리된 데이터 제거
                incompleteData.RemoveRange(0, packetLength);

                // 패킷을 큐에 추가
                lock (queueLock)
                {
                    packetQueue.Enqueue(new PacketData
                    {
                        Type = packetType,
                        Data = packetData
                    });
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in ProcessReceivedData: {e.Message}");
        }
    }


    void HandleNormalPacket(byte[] packetData)
    {
        try
        {
            var response = Packets.Deserialize<Response>(packetData);
            Debug.Log($"Received response - HandlerId: {response.handlerId}, ResponseCode: {response.responseCode}");

            if (response.responseCode != 0 && !uiNotice.activeSelf)
            {
                AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp);
                StartCoroutine(NoticeRoutine(2));
                return;
            }

            if (response.data != null && response.data.Length > 0)
            {
                if (response.handlerId == 0) // Initial 응답
                {
                    GameManager.instance.GameStart();
                }
                else
                {
                    ProcessResponseData(response.data, response.handlerId);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing normal packet: {e.Message}");
        }
    }

    void ProcessResponseData(byte[] data, uint handlerId)
    {
        try
        {
            string jsonString = Encoding.UTF8.GetString(data);
            Debug.Log($"Processing response data for handlerId: {handlerId}, Data: {jsonString}");

            switch (handlerId)
            {
                case (uint)Packets.HandlerIds.LocationUpdate:
                    var locationUpdate = JsonUtility.FromJson<LocationUpdatePayload>(jsonString);
                    if (locationUpdate != null && locationUpdate.users != null && locationUpdate.users.Count > 0)
                    {
                        var user = locationUpdate.users[0];
                        Debug.Log($"Received location update - X: {user.x}, Y: {user.y}, Status: {user.status}");
                        // 위치 업데이트 처리
                    }
                    break;
                    // 다른 핸들러 케이스 추가
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing response data: {e.Message}");
        }
    }
    void HandleLocationPacket(byte[] data)
    {
        try
        {
            LocationUpdate response = Packets.Deserialize<LocationUpdate>(data);
            if (response.users != null && response.users.Count > 0)
            {
                LocationUpdate convertedResponse = new LocationUpdate
                {
                    users = new List<LocationUpdate.UserLocation>()
                };

                foreach (var user in response.users)
                {
                    float unityX = ConvertToUnityX(user.x);
                    float unityY = ConvertToUnityY(user.y);

                    var convertedUser = new LocationUpdate.UserLocation
                    {
                        id = user.id,
                        x = unityX,
                        y = unityY,
                        status = user.status
                    };

                    convertedResponse.users.Add(convertedUser);
                    Debug.Log($"User {user.id} - Server: ({user.x}, {user.y}), Unity: ({unityX}, {unityY}), Status: {user.status}");
                }

                Spawner.instance.Spawn(convertedResponse);
            }
        }
        catch (Exception e)
        {
            HandleError("Error processing location packet", e);
        }
    }

    void HandleError(string message, Exception e = null)
    {
        Debug.LogError($"Network Error: {message}");
        if (e != null)
        {
            Debug.LogError($"Exception: {e}");
        }

        AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp);
        StartCoroutine(NoticeRoutine(2));

        if (tcpClient != null && !tcpClient.Connected && !isReconnecting)
        {
            StartCoroutine(TryReconnect());
        }
    }


    IEnumerator TryReconnect()
    {
        if (isReconnecting) yield break;

        isReconnecting = true;
        currentReconnectAttempt = 0;

        while (!tcpClient.Connected && currentReconnectAttempt < maxReconnectAttempts)
        {
            try
            {
                if (tcpClient != null)
                {
                    tcpClient.Close();
                    tcpClient = null;
                }

                currentReconnectAttempt++;
                Debug.Log($"Attempting to reconnect... Attempt {currentReconnectAttempt}/{maxReconnectAttempts}");

                if (ConnectToServer(ipInputField.text, int.Parse(portInputField.text)))
                {
                    stream = tcpClient.GetStream();
                    StartReceiving();
                    SendInitialPacket();
                    isReconnecting = false;
                    Debug.Log("Reconnection successful!");
                    yield break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Reconnection attempt failed: {e.Message}");
            }

            yield return new WaitForSeconds(reconnectDelay);
        }

        if (!tcpClient.Connected)
        {
            Debug.LogError("Failed to reconnect after maximum attempts");
            StartCoroutine(NoticeRoutine(1));
        }

        isReconnecting = false;
    }


    void OnApplicationQuit()
    {
        if (tcpClient != null && tcpClient.Connected)
        {
            tcpClient.Close();
        }
    }
}

