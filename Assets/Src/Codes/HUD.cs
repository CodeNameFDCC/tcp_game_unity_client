using UnityEngine; // Unity 관련 네임스페이스
using UnityEngine.UI; // UI 관련 네임스페이스

public class HUD : MonoBehaviour
{
    // HUD에서 표시할 정보의 유형 정의
    public enum InfoType { DeviceId, Time }
    public InfoType type; // 현재 HUD에서 표시할 정보 유형

    Text myText; // Text 컴포넌트를 저장할 변수

    void Awake()
    {
        // HUD 초기화 시 Text 컴포넌트를 가져옴
        myText = GetComponent<Text>();
    }

    // 매 프레임 업데이트 후 호출되는 메서드
    void LateUpdate()
    {
        // 현재 표시할 정보 유형에 따라 텍스트 업데이트
        switch (type)
        {
            case InfoType.DeviceId:
                // 장치 ID를 텍스트로 설정
                myText.text = string.Format("{0}", GameManager.instance.deviceId);
                break;
            case InfoType.Time:
                // 게임 시간을 분과 초로 변환
                int min = Mathf.FloorToInt(GameManager.instance.gameTime / 60); // 분 계산
                int sec = Mathf.FloorToInt(GameManager.instance.gameTime % 60); // 초 계산
                // 텍스트 형식 설정: mm:ss
                myText.text = string.Format("{0:D2}:{1:D2}", min, sec);
                break;
        }
    }
}
