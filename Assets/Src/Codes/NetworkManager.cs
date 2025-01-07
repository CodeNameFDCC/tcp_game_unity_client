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

    void Awake()
    {
        instance = this; // 인스턴스 초기화
        wait = new WaitForSecondsRealtime(5); // 5초 대기 시간 설정
    }
    public void OnStartButtonClicked()
    {
        string ip = ipInputField.text; // 입력된 IP
        string port = portInputField.text; // 입력된 포트

        if (IsValidPort(port)) // 포트 유효성 검사
        {
            int portNumber = int.Parse(port); // 포트 번호로 변환

            if (deviceIdInputField.text != "") // 디바이스 ID가 입력된 경우
            {
                GameManager.instance.deviceId = deviceIdInputField.text; // 입력된 ID 사용
            }
            else
            {
                if (GameManager.instance.deviceId == "") // 디바이스 ID가 비어있는 경우
                {
                    GameManager.instance.deviceId = GenerateUniqueID(); // 고유 ID 생성
                }
            }

            if (ConnectToServer(ip, portNumber)) // 서버에 연결 성공
            {
                StartGame(); // 게임 시작
            }
            else
            {
                AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp); // 실패 사운드 재생
                StartCoroutine(NoticeRoutine(1)); // 알림 표시
            }

        }
        else // 포트가 유효하지 않은 경우
        {
            AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp); // 실패 사운드 재생
            StartCoroutine(NoticeRoutine(0)); // 알림 표시
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
            Debug.LogError($"SocketException: {e}"); // 예외 발생 시 에러 메시지
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

    byte[] CreatePacketHeader(int dataLength, Packets.PacketType packetType)
    {
        int packetLength = 4 + 1 + dataLength; // 전체 패킷 길이 (헤더 포함)
        byte[] header = new byte[5]; // 4바이트 길이 + 1바이트 타입

        // 첫 4바이트: 패킷 전체 길이
        byte[] lengthBytes = BitConverter.GetBytes(packetLength);
        lengthBytes = ToBigEndian(lengthBytes); // 빅 엔디안으로 변환
        Array.Copy(lengthBytes, 0, header, 0, 4); // 길이 복사

        // 다음 1바이트: 패킷 타입
        header[4] = (byte)packetType; // 타입 설정

        return header; // 헤더 반환
    }

    // 공통 패킷 생성 함수
    async void SendPacket<T>(T payload, uint handlerId)
    {
        // ArrayBufferWriter<byte>를 사용하여 직렬화
        var payloadWriter = new ArrayBufferWriter<byte>();
        Packets.Serialize(payloadWriter, payload); // 페이로드 직렬화
        byte[] payloadData = payloadWriter.WrittenSpan.ToArray(); // 직렬화된 데이터 배열

        CommonPacket commonPacket = new CommonPacket
        {
            handlerId = handlerId, // 핸들러 ID 설정
            userId = GameManager.instance.deviceId, // 사용자 ID 설정
            version = GameManager.instance.version, // 버전 설정
            payload = payloadData, // 페이로드 설정
        };

        // ArrayBufferWriter<byte>를 사용하여 직렬화
        var commonPacketWriter = new ArrayBufferWriter<byte>();
        Packets.Serialize(commonPacketWriter, commonPacket); // 공통 패킷 직렬화
        byte[] data = commonPacketWriter.WrittenSpan.ToArray(); // 직렬화된 데이터 배열

        // 헤더 생성
        byte[] header = CreatePacketHeader(data.Length, Packets.PacketType.Normal); // 헤더 생성

        // 패킷 생성
        byte[] packet = new byte[header.Length + data.Length]; // 패킷 배열 생성
        Array.Copy(header, 0, packet, 0, header.Length); // 헤더 복사
        Array.Copy(data, 0, packet, header.Length, data.Length); // 데이터 복사

        await Task.Delay(GameManager.instance.latency); // 지연 시간 대기

        // 패킷 전송
        stream.Write(packet, 0, packet.Length); // 패킷 전송
    }

    void SendInitialPacket()
    {
        InitialPayload initialPayload = new InitialPayload
        {
            deviceId = GameManager.instance.deviceId, // 디바이스 ID 설정
            playerId = GameManager.instance.playerId, // 플레이어 ID 설정
            latency = GameManager.instance.latency, // 지연 시간 설정
        };

        // handlerId는 0으로 가정
        SendPacket(initialPayload, (uint)Packets.HandlerIds.Init); // 초기 패킷 전송
    }

    public void SendLocationUpdatePacket(float x, float y)
    {
        LocationUpdatePayload locationUpdatePayload = new LocationUpdatePayload
        {
            x = x, // x 좌표 설정
            y = y, // y 좌표 설정
        };

        SendPacket(locationUpdatePayload, (uint)Packets.HandlerIds.LocationUpdate); // 위치 업데이트 패킷 전송
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
                Debug.LogError($"Receive error: {e.Message}"); // 수신 오류 메시지
                break; // 루프 종료
            }
        }
    }

    void ProcessReceivedData(byte[] data, int length)
    {
        incompleteData.AddRange(data.AsSpan(0, length).ToArray()); // 수신된 데이터를 불완전한 데이터 리스트에 추가

        while (incompleteData.Count >= 5) // 불완전한 데이터가 5바이트 이상인 경우
        {
            // 패킷 길이와 타입 읽기
            byte[] lengthBytes = incompleteData.GetRange(0, 4).ToArray(); // 첫 4바이트로 길이 읽기
            int packetLength = BitConverter.ToInt32(ToBigEndian(lengthBytes), 0); // 패킷 길이 변환
            Packets.PacketType packetType = (Packets.PacketType)incompleteData[4]; // 패킷 타입 읽기

            if (incompleteData.Count < packetLength) // 데이터가 충분하지 않으면
            {
                // 데이터가 충분하지 않으면 반환
                return; // 함수 종료
            }

            // 패킷 데이터 추출
            byte[] packetData = incompleteData.GetRange(5, packetLength - 5).ToArray(); // 패킷 데이터 추출
            incompleteData.RemoveRange(0, packetLength); // 처리한 데이터 제거

            // Debug.Log($"Received packet: Length = {packetLength}, Type = {packetType}");

            switch (packetType) // 패킷 타입에 따라 처리
            {
                case Packets.PacketType.Normal:
                    HandleNormalPacket(packetData); // 일반 패킷 처리
                    break;
                case Packets.PacketType.Location:
                    HandleLocationPacket(packetData); // 위치 패킷 처리
                    break;
            }
        }
    }

    void HandleNormalPacket(byte[] packetData)
    {
        // 패킷 데이터 처리
        var response = Packets.Deserialize<Response>(packetData); // 응답 데이터 역직렬화
        // Debug.Log($"HandlerId: {response.handlerId}, responseCode: {response.responseCode}, timestamp: {response.timestamp}");

        if (response.responseCode != 0 && !uiNotice.activeSelf) // 응답 코드가 0이 아니고 알림 UI가 활성화되어 있지 않은 경우
        {
            AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp); // 실패 사운드 재생
            StartCoroutine(NoticeRoutine(2)); // 알림 표시
            return; // 함수 종료
        }

        if (response.data != null && response.data.Length > 0) // 응답 데이터가 있는 경우
        {
            if (response.handlerId == 0) // 핸들러 ID가 0인 경우
            {
                GameManager.instance.GameStart(); // 게임 시작
            }
            ProcessResponseData(response.data); // 응답 데이터 처리
        }
    }

    void ProcessResponseData(byte[] data)
    {
        try
        {
            // var specificData = Packets.Deserialize<SpecificDataType>(data);
            string jsonString = Encoding.UTF8.GetString(data); // 데이터 UTF-8 문자열로 변환
            Debug.Log($"Processed SpecificDataType: {jsonString}"); // 처리된 데이터 로그
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing response data: {e.Message}"); // 처리 오류 메시지
        }
    }

    void HandleLocationPacket(byte[] data)
    {
        try
        {
            LocationUpdate response;

            if (data.Length > 0) // 데이터가 있는 경우
            {
                // 패킷 데이터 처리
                response = Packets.Deserialize<LocationUpdate>(data); // 위치 업데이트 데이터 역직렬화
            }
            else // 데이터가 비어있는 경우
            {
                // data가 비어있을 경우 빈 배열을 전달
                response = new LocationUpdate { users = new List<LocationUpdate.UserLocation>() }; // 빈 사용자 위치 리스트 생성
            }

            Spawner.instance.Spawn(response); // 스폰 처리
        }
        catch (Exception e)
        {
            Debug.LogError($"Error HandleLocationPacket: {e.Message}"); // 위치 패킷 처리 오류 메시지
        }
    }
}

