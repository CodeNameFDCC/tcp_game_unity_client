using System.Collections;
using System.Collections.Generic;
using UnityEngine; // Unity 관련 네임스페이스

public class Reposition : MonoBehaviour
{
    Collider2D coll; // 콜라이더 변수 선언

    void Awake()
    {
        // 컴포넌트 초기화
        coll = GetComponent<Collider2D>(); // 현재 오브젝트의 Collider2D 컴포넌트를 가져옴
    }

    // 다른 콜라이더와의 충돌이 종료될 때 호출되는 메서드
    void OnTriggerExit2D(Collider2D collision)
    {
        // 충돌한 오브젝트가 "area" 태그가 아닐 경우 조기 종료
        if (!collision.CompareTag("area"))
        {
            return;
        }

        // 플레이어의 위치와 현재 오브젝트의 위치 가져오기
        Vector3 playerPos = GameManager.instance.player.transform.position; // 플레이어 위치
        Vector3 myPos = transform.position; // 현재 오브젝트 위치
        Vector3 playerDir = GameManager.instance.player.inputVec; // 플레이어의 입력 방향

        // 현재 오브젝트의 태그에 따라 다르게 처리
        switch (transform.tag)
        {
            case "ground":
                // 플레이어와 현재 오브젝트 간의 위치 차이 계산
                float diffX = playerPos.x - myPos.x; // x축 차이
                float diffY = playerPos.y - myPos.y; // y축 차이

                // 방향 결정
                float dirX = diffX < 0 ? -1 : 1; // x축 방향
                float dirY = diffY < 0 ? -1 : 1; // y축 방향

                // 차이의 절대값 계산
                diffX = Mathf.Abs(diffX);
                diffY = Mathf.Abs(diffY);

                // x축 차이가 y축 차이보다 크면 수평으로 이동, 그렇지 않으면 수직으로 이동
                if (diffX > diffY)
                {
                    transform.Translate(Vector3.right * dirX * 40); // 오른쪽 또는 왼쪽으로 이동
                }
                else if (diffX < diffY)
                {
                    transform.Translate(Vector3.up * dirY * 40); // 위쪽 또는 아래쪽으로 이동
                }
                break;
            case "Enemy":
                // 콜라이더가 활성화된 경우
                if (coll.enabled)
                {
                    // 플레이어와 현재 오브젝트 간의 거리 계산
                    Vector3 dist = playerPos - myPos; // 거리 벡터
                    // 무작위 위치 벡터 생성
                    Vector3 ran = new Vector3(Random.Range(-3, 3), Random.Range(-3, 3), 0);
                    // 두 벡터를 더하여 새로운 위치로 이동
                    transform.Translate(ran + dist * 2); // 플레이어 방향으로 두 배 이동 후 랜덤 이동
                }
                break;
        }
    }
}
