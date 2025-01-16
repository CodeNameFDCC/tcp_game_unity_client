using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임의 네트워크 통신을 관리하는 매니저 클래스
/// TCP 연결, 패킷 송수신, 게임 데이터 동기화를 담당
/// </summary>
public class NetworkManager : MonoBehaviour
{
    #region Singleton & Variables
    public static NetworkManager instance;

    [Header("UI References")]
    public InputField ipInputField;        // 서버 IP 입력 필드
    public InputField portInputField;      // 포트 번호 입력 필드
    public InputField deviceIdInputField;  // 장치 ID 입력 필드
    public GameObject uiNotice;            // 알림 UI 오브젝트

    [Header("Network Components")]
    private TcpClient tcpClient;           // TCP 클라이언트
    private NetworkStream stream;          // 네트워크 스트림
    private const int BUFFER_SIZE = 4096;  // 수신 버퍼 크기
    private byte[] receiveBuffer;          // 수신 데이터 버퍼
    private List<byte> incompleteData;     // 미완성 패킷 데이터 저장

    private WaitForSecondsRealtime wait;   // 알림 표시 대기 시간
    #endregion

    #region Initialization
    private void Awake()
    {
        instance = this;
        wait = new WaitForSecondsRealtime(5);
        receiveBuffer = new byte[BUFFER_SIZE];
        incompleteData = new List<byte>();
    }

    /// <summary>
    /// 시작 버튼 클릭 시 호출되는 메서드
    /// 서버 연결 및 게임 초기화를 수행
    /// </summary>
    public void OnStartButtonClicked()
    {
        string ip = ipInputField.text;
        string port = portInputField.text;

        if (!IsValidPort(port))
        {
            ShowErrorNotice(0);
            return;
        }

        InitializeDeviceId();
        InitializePlayerId();

        if (ConnectToServer(ip, int.Parse(port)))
        {
            Init();
            StartGame();
        }
        else
        {
            ShowErrorNotice(1);
        }
    }

    /// <summary>
    /// 장치 ID 초기화
    /// </summary>
    private void InitializeDeviceId()
    {
        if (!string.IsNullOrEmpty(deviceIdInputField.text))
        {
            GameManager.instance.deviceId = deviceIdInputField.text;
        }
        else if (string.IsNullOrEmpty(GameManager.instance.deviceId))
        {
            GameManager.instance.deviceId = GenerateUniqueID();
        }
    }

    /// <summary>
    /// 플레이어 ID 초기화
    /// </summary>
    private void InitializePlayerId()
    {
        GameManager.instance.playerId = (uint)UnityEngine.Random.Range(0, 4);
    }
    #endregion

    #region Network Connection
    /// <summary>
    /// IP 주소 유효성 검사
    /// </summary>
    private bool IsValidIP(string ip)
    {
        return System.Net.IPAddress.TryParse(ip, out _);
    }

    /// <summary>
    /// 포트 번호 유효성 검사 (0-65535)
    /// </summary>
    private bool IsValidPort(string port)
    {
        return int.TryParse(port, out int portNumber) &&
            portNumber > 0 &&
            portNumber <= 65535;
    }

    /// <summary>
    /// 서버 연결 시도
    /// </summary>
    private bool ConnectToServer(string ip, int port)
    {
        try
        {
            tcpClient = new TcpClient(ip, port);
            stream = tcpClient.GetStream();
            Debug.Log($"Connected to {ip}:{port}");
            return true;
        }
        catch (SocketException e)
        {
            Debug.LogError($"SocketException: {e}");
            return false;
        }
    }

    /// <summary>
    /// 고유 ID 생성
    /// </summary>
    private string GenerateUniqueID()
    {
        return System.Guid.NewGuid().ToString();
    }
    #endregion

    #region Game Initialization
    /// <summary>
    /// 게임 초기화
    /// </summary>
    private void Init()
    {
        StartReceiving();
        SendInitialPacket();
    }

    /// <summary>
    /// 게임 시작
    /// </summary>
    private void StartGame()
    {
        Debug.Log("Game Started");
        GameManager.instance.GameStart();
    }

    /// <summary>
    /// 알림 표시 코루틴
    /// </summary>
    private IEnumerator NoticeRoutine(int index)
    {
        ShowNotice(index, true);
        yield return wait;
        ShowNotice(index, false);
    }

    /// <summary>
    /// 알림 UI 표시/숨김
    /// </summary>
    private void ShowNotice(int index, bool show)
    {
        uiNotice.SetActive(show);
        uiNotice.transform.GetChild(index).gameObject.SetActive(show);
    }

    /// <summary>
    /// 에러 알림 표시
    /// </summary>
    private void ShowErrorNotice(int index)
    {
        AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp);
        StartCoroutine(NoticeRoutine(index));
    }
    #endregion

    #region Packet Handling
    /// <summary>
    /// 바이트 배열을 빅 엔디안으로 변환
    /// </summary>
    public static byte[] ToBigEndian(byte[] bytes)
    {
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        return bytes;
    }

    /// <summary>
    /// 패킷 헤더 생성
    /// </summary>
    private byte[] CreatePacketHeader(int dataLength, Packets.PacketType packetType)
    {
        int packetLength = 4 + 1 + dataLength;
        byte[] header = new byte[5];

        byte[] lengthBytes = ToBigEndian(BitConverter.GetBytes(packetLength));
        Array.Copy(lengthBytes, 0, header, 0, 4);
        header[4] = (byte)packetType;

        return header;
    }

    /// <summary>
    /// 일반 패킷 전송
    /// </summary>
    private async void SendPacket<T>(T payload, uint handlerId)
    {
        var payloadWriter = new ArrayBufferWriter<byte>();
        Packets.Serialize(payloadWriter, payload);
        byte[] payloadData = payloadWriter.WrittenSpan.ToArray();

        CommonPacket commonPacket = CreateCommonPacket(handlerId, payloadData);
        byte[] packet = CreateFullPacket(commonPacket);

        await Task.Delay(GameManager.instance.latency);
        stream.Write(packet, 0, packet.Length);
    }

    /// <summary>
    /// 공통 패킷 생성
    /// </summary>
    private CommonPacket CreateCommonPacket(uint handlerId, byte[] payloadData)
    {
        return new CommonPacket
        {
            handlerId = handlerId,
            userId = GameManager.instance.deviceId,
            version = GameManager.instance.version,
            payload = payloadData,
        };
    }

    /// <summary>
    /// 전체 패킷 생성 (헤더 + 데이터)
    /// </summary>
    private byte[] CreateFullPacket(CommonPacket commonPacket)
    {
        var writer = new ArrayBufferWriter<byte>();
        Packets.Serialize(writer, commonPacket);
        byte[] data = writer.WrittenSpan.ToArray();
        byte[] header = CreatePacketHeader(data.Length, Packets.PacketType.Normal);

        byte[] packet = new byte[header.Length + data.Length];
        Array.Copy(header, 0, packet, 0, header.Length);
        Array.Copy(data, 0, packet, header.Length, data.Length);

        return packet;
    }
    #endregion

    #region Packet Sending
    /// <summary>
    /// 초기화 패킷 전송
    /// </summary>
    private void SendInitialPacket()
    {
        InitialPayload initialPayload = new InitialPayload
        {
            deviceId = GameManager.instance.deviceId,
            playerId = GameManager.instance.playerId,
            latency = GameManager.instance.latency,
            speed = GameManager.instance.player.speed,
        };

        SendPacket(initialPayload, (uint)Packets.HandlerIds.Init);
    }

    /// <summary>
    /// 위치 업데이트 패킷 전송
    /// </summary>
    public void SendLocationUpdatePacket(float x, float y)
    {
        LocationUpdatePayload locationUpdatePayload = new LocationUpdatePayload
        {
            x = x,
            y = y,
            inputX = GameManager.instance.player.inputVec.x,
            inputY = GameManager.instance.player.inputVec.y,
        };

        SendPacket(locationUpdatePayload, (uint)Packets.HandlerIds.LocationUpdate);
    }
    #endregion

    #region Packet Receiving
    /// <summary>
    /// 패킷 수신 시작
    /// </summary>
    private void StartReceiving()
    {
        _ = ReceivePacketsAsync();
    }

    /// <summary>
    /// 패킷 비동기 수신 처리
    /// </summary>
    private async Task ReceivePacketsAsync()
    {
        while (tcpClient.Connected)
        {
            try
            {
                int bytesRead = await stream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length);
                if (bytesRead > 0)
                {
                    ProcessReceivedData(receiveBuffer, bytesRead);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Receive error: {e.Message}");
                break;
            }
        }
    }

    /// <summary>
    /// 수신된 데이터 처리
    /// </summary>
    private void ProcessReceivedData(byte[] data, int length)
    {
        incompleteData.AddRange(data.AsSpan(0, length).ToArray());

        while (incompleteData.Count >= 5)
        {
            // 패킷 길이와 타입 확인
            int packetLength = BitConverter.ToInt32(
                ToBigEndian(incompleteData.GetRange(0, 4).ToArray()), 0);
            Packets.PacketType packetType = (Packets.PacketType)incompleteData[4];

            if (incompleteData.Count < packetLength)
            {
                return;
            }

            // 패킷 데이터 추출 및 처리
            byte[] packetData = incompleteData.GetRange(5, packetLength - 5).ToArray();
            incompleteData.RemoveRange(0, packetLength);

            HandlePacket(packetType, packetData);
        }
    }

    /// <summary>
    /// 패킷 타입별 처리
    /// </summary>
    private void HandlePacket(Packets.PacketType packetType, byte[] packetData)
    {
        switch (packetType)
        {
            case Packets.PacketType.Ping:
                HandlePingPacket(packetData);
                break;
            case Packets.PacketType.Normal:
                HandleNormalPacket(packetData);
                break;
            case Packets.PacketType.GameStart:
                HandleInitialResponsePacket(packetData);
                break;
            case Packets.PacketType.Location:
                HandleLocationPacket(packetData);
                break;
        }
    }
    #endregion

    #region Packet Handlers
    /// <summary>
    /// Ping 패킷 처리
    /// </summary>
    private async void HandlePingPacket(byte[] packetData)
    {
        var response = Packets.Deserialize<Ping>(packetData);
        Ping ping = new Ping { timestamp = response.timestamp };

        var bufferWriter = new ArrayBufferWriter<byte>();
        Packets.Serialize(bufferWriter, ping);
        byte[] data = bufferWriter.WrittenSpan.ToArray();
        byte[] packet = CreatePingPacket(data);

        await Task.Delay(GameManager.instance.latency);
        stream.Write(packet, 0, packet.Length);
    }

    /// <summary>
    /// Ping 응답 패킷 생성
    /// </summary>
    private byte[] CreatePingPacket(byte[] data)
    {
        byte[] header = CreatePacketHeader(data.Length, Packets.PacketType.Ping);
        byte[] packet = new byte[header.Length + data.Length];
        Array.Copy(header, 0, packet, 0, header.Length);
        Array.Copy(data, 0, packet, header.Length, data.Length);
        return packet;
    }

    /// <summary>
    /// 일반 패킷 처리
    /// </summary>
    private void HandleNormalPacket(byte[] packetData)
    {
        var response = Packets.Deserialize<Response>(packetData);

        if (response.responseCode != 0 && !uiNotice.activeSelf)
        {
            ShowErrorNotice(2);
            return;
        }

        if (response.data?.Length > 0)
        {
            ProcessResponseData(response.data);
        }
    }

    /// <summary>
    /// 초기화 응답 패킷 처리
    /// </summary>
    private void HandleInitialResponsePacket(byte[] data)
    {
        try
        {
            if (data.Length == 0)
            {
                GameManager.instance.GameRetry();
                return;
            }

            InitialResponse response = Packets.Deserialize<InitialResponse>(data);
            Vector3 newPos = new Vector3(response.x, response.y, 0);
            GameManager.instance.player.transform.position = newPos;
            StartGame();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error HandleInitialResponsePacket: {e.Message}");
        }
    }

    /// <summary>
    /// 응답 데이터 처리
    /// </summary>
    private void ProcessResponseData(byte[] data)
    {
        try
        {
            string jsonString = Encoding.UTF8.GetString(data);
            Debug.Log($"Processed SpecificDataType: {jsonString}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing response data: {e.Message}");
        }
    }

    /// <summary>
    /// 위치 패킷 처리
    /// </summary>
    private void HandleLocationPacket(byte[] data)
    {
        try
        {
            LocationUpdate response;

            if (data.Length > 0)
            {
                response = Packets.Deserialize<LocationUpdate>(data);
            }
            else
            {
                response = new LocationUpdate { users = new List<LocationUpdate.UserLocation>() };
            }

            Spawner.instance.Spawn(response);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error HandleLocationPacket: {e.Message}");
        }
    }
    #endregion
}