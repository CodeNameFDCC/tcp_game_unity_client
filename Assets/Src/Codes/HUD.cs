/* HUD(Heads-Up Display) 클래스로, 게임 정보(디바이스 ID 또는 게임 시간)를 화면에 표시합니다.
정보 유형에 따라 텍스트를 업데이트하며, 매 프레임마다 갱신됩니다. */

using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public enum InfoType { DeviceId, Time } // 표시할 정보 유형 열거형
    public InfoType type; // 현재 정보 유형

    Text myText; // 텍스트 컴포넌트

    void Awake()
    {
        myText = GetComponent<Text>(); // 텍스트 컴포넌트 가져오기
    }

    void LateUpdate()
    {
        switch (type)
        { // 현재 정보 유형에 따라
            case InfoType.DeviceId:
                myText.text = string.Format("{0}", GameManager.instance.deviceId); // 디바이스 ID 표시
                break;
            case InfoType.Time:
                int min = Mathf.FloorToInt(GameManager.instance.gameTime / 60); // 게임 시간 분 단위로 변환
                int sec = Mathf.FloorToInt(GameManager.instance.gameTime % 60); // 게임 시간 초 단위로 변환
                myText.text = string.Format("{0:D2}:{1:D2}", min, sec); // 포맷에 맞춰 시간 표시
                break;
        }
    }
}
