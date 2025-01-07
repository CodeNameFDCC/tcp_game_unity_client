
/* 게임 오브젝트가 플레이어의 위치를 따라가도록 하는 스크립트입니다.
플레이어의 월드 좌표를 스크린 좌표로 변환하여 UI 요소의 위치를 업데이트합니다. */

using UnityEngine;

public class Follow : MonoBehaviour
{
    RectTransform rect; // RectTransform 변수 선언

    void Awake()
    {
        rect = GetComponent<RectTransform>(); // RectTransform 컴포넌트 가져오기
    }

    void FixedUpdate()
    {
        // 플레이어의 월드 좌표를 스크린 좌표로 변환하여 UI 요소 위치 업데이트
        rect.position = Camera.main.WorldToScreenPoint(GameManager.instance.player.transform.position);
    }
}
