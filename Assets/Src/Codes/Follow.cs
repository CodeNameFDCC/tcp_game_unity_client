using UnityEngine; // Unity 관련 네임스페이스

/// <summary>
// 따라오시오 UI 
/// </summary>
public class Follow : MonoBehaviour
{
    RectTransform rect; // UI 요소의 RectTransform을 저장할 변수

    void Awake()
    {
        // 컴포넌트 초기화
        rect = GetComponent<RectTransform>(); // 현재 오브젝트의 RectTransform 컴포넌트를 가져옴
    }

    void FixedUpdate()
    {
        // 플레이어의 월드 좌표를 스크린 좌표로 변환하여 RectTransform의 위치 업데이트
        rect.position = Camera.main.WorldToScreenPoint(GameManager.instance.player.transform.position);
    }
}
