// TestManager.cs
using UnityEngine;

public class TestManager : MonoBehaviour
{
    public GameClient gameClient;

    private void Start()
    {
        gameClient = GetComponent<GameClient>();
        SetupEventHandlers();
        Debug.Log("Test Manager Started - Press keys to test:");
        Debug.Log("1-6: Request data from server");
        Debug.Log("Q-Y: Send data to server");
        PrintHelp();
    }

    private void PrintHelp()
    {
        Debug.Log(@"
Key mappings:
Request from server:
1 - Request Int
2 - Request Float
3 - Request Bool
4 - Request String
5 - Request Position
6 - Request Quaternion

Send to server:
Q - Send Int
W - Send Float
E - Send Bool
R - Send String
T - Send Position
Y - Send Quaternion

H - Show this help
");
    }

    private void Update()
    {
        // Request tests
        if (Input.GetKeyDown(KeyCode.Alpha1)) TestInt8Request();
        if (Input.GetKeyDown(KeyCode.Alpha2)) TestInt16Request();
        if (Input.GetKeyDown(KeyCode.Alpha3)) TestInt32Request();
        if (Input.GetKeyDown(KeyCode.Alpha4)) TestFloatRequest();
        if (Input.GetKeyDown(KeyCode.Alpha5)) TestBoolRequest();
        if (Input.GetKeyDown(KeyCode.Alpha6)) TestStringRequest();
        if (Input.GetKeyDown(KeyCode.Alpha7)) TestPositionRequest();
        if (Input.GetKeyDown(KeyCode.Alpha8)) TestQuaternionRequest();

        // Send tests
        if (Input.GetKeyDown(KeyCode.Q)) TestInt8Send();
        if (Input.GetKeyDown(KeyCode.W)) TestInt16Send();
        if (Input.GetKeyDown(KeyCode.E)) TestInt32Send();
        if (Input.GetKeyDown(KeyCode.R)) TestFloatSend();
        if (Input.GetKeyDown(KeyCode.T)) TestBoolSend();
        if (Input.GetKeyDown(KeyCode.Y)) TestStringSend();
        if (Input.GetKeyDown(KeyCode.U)) TestPositionSend();
        if (Input.GetKeyDown(KeyCode.I)) TestQuaternionSend();

        // Help
        if (Input.GetKeyDown(KeyCode.H)) PrintHelp();
    }

    private void SetupEventHandlers()
    {
        gameClient.handler.OnInt8Received += (value) =>
            Debug.Log($"Received Int8 from server: {value}");

        gameClient.handler.OnInt16Received += (value) =>
            Debug.Log($"Received Int16 from server: {value}");

        gameClient.handler.OnInt32Received += (value) =>
            Debug.Log($"Received Int32 from server: {value}");

        gameClient.handler.OnFloatReceived += (value) =>
            Debug.Log($"Received Float from server: {value}");

        gameClient.handler.OnBoolReceived += (value) =>
            Debug.Log($"Received Bool from server: {value}");

        gameClient.handler.OnStringReceived += (value) =>
            Debug.Log($"Received String from server: {value}");

        gameClient.handler.OnPositionReceived += (value) =>
            Debug.Log($"Received Position from server: {value}");

        gameClient.handler.OnQuaternionReceived += (value) =>
            Debug.Log($"Received Quaternion from server: {value}");
    }

    // Request Test Methods
    private void TestInt8Request()
    {
        Debug.Log("Requesting Int8 (8-bit) from server...");
        gameClient.RequestInt8();
    }

    private void TestInt16Request()
    {
        Debug.Log("Requesting Int16 (16-bit) from server...");
        gameClient.RequestInt16();
    }

    private void TestInt32Request()
    {
        Debug.Log("Requesting Int32 (32-bit) from server...");
        gameClient.RequestInt32();
    }

    private void TestFloatRequest()
    {
        Debug.Log("Requesting Float from server...");
        gameClient.RequestFloat();
    }

    private void TestBoolRequest()
    {
        Debug.Log("Requesting Bool from server...");
        gameClient.RequestBool();
    }

    private void TestStringRequest()
    {
        Debug.Log("Requesting String from server...");
        gameClient.RequestString();
    }

    private void TestPositionRequest()
    {
        Debug.Log("Requesting Position from server...");
        gameClient.RequestPosition();
    }

    private void TestQuaternionRequest()
    {
        Debug.Log("Requesting Quaternion from server...");
        gameClient.RequestQuaternion();
    }

    // Send Test Methods

    private void TestInt8Send()
    {
        sbyte testValue = 42; // -128 ~ 127 사이의 값
        Debug.Log($"Sending Int8 to server: {testValue}");
        gameClient.SendInt8Response(testValue);
    }

    private void TestInt16Send()
    {
        int testValue = 255;
        Debug.Log($"Sending Int to server: {testValue}");
        gameClient.SendInt16Response(testValue);
    }
    private void TestInt32Send()
    {
        int testValue = 12345;
        Debug.Log($"Sending Int to server: {testValue}");
        gameClient.SendInt32Response(testValue);
    }

    private void TestFloatSend()
    {
        float testValue = 123.456f;
        Debug.Log($"Sending Float to server: {testValue}");
        gameClient.SendFloatResponse(testValue);
    }

    private void TestBoolSend()
    {
        bool testValue = true;
        Debug.Log($"Sending Bool to server: {testValue}");
        gameClient.SendBoolResponse(testValue);
    }

    private void TestStringSend()
    {
        string testValue = "Hello from Unity Client!";
        Debug.Log($"Sending String to server: {testValue}");
        gameClient.SendStringResponse(testValue);
    }

    private void TestPositionSend()
    {
        Vector3 testValue = new Vector3(1.1f, 2.2f, 3.3f);
        Debug.Log($"Sending Position to server: {testValue}");
        gameClient.SendPositionResponse(testValue);
    }

    private void TestQuaternionSend()
    {
        Quaternion testValue = Quaternion.Euler(30, 45, 60);
        Debug.Log($"Sending Quaternion to server: {testValue}");
        gameClient.SendQuaternionResponse(testValue);
    }
}