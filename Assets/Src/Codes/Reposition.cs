/**
 * 오브젝트의 위치를 재조정하는 클래스입니다.
 * 플레이어가 특정 영역을 벗어날 때, 오브젝트의 위치를 변경합니다.
 * 태그에 따라 다른 방식으로 오브젝트를 이동시킵니다.
 */

using UnityEngine;

public class Reposition : MonoBehaviour
{
    Collider2D coll; // Collider2D 컴포넌트

    void Awake()
    {
        coll = GetComponent<Collider2D>(); // Collider2D 컴포넌트 초기화
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        // 충돌한 오브젝트가 "area" 태그가 아니면 종료
        if (!collision.CompareTag("area"))
        {
            return;
        }

        Vector3 playerPos = GameManager.instance.player.transform.position; // 플레이어 위치
        Vector3 myPos = transform.position; // 현재 오브젝트 위치
        Vector3 playerDir = GameManager.instance.player.inputVec; // 플레이어 방향 입력

        // 태그에 따라 다른 위치 조정
        switch (transform.tag)
        {
            case "ground":
                float diffX = playerPos.x - myPos.x; // x축 거리 차이
                float diffY = playerPos.y - myPos.y; // y축 거리 차이

                float dirX = diffX < 0 ? -1 : 1; // x 방향 결정
                float dirY = diffY < 0 ? -1 : 1; // y 방향 결정

                diffX = Mathf.Abs(diffX); // 절대값으로 변환
                diffY = Mathf.Abs(diffY); // 절대값으로 변환

                // x축과 y축의 차이를 비교하여 이동 방향 결정
                if (diffX > diffY)
                {
                    transform.Translate(Vector3.right * dirX * 40); // x축으로 이동
                }
                else if (diffX < diffY)
                {
                    transform.Translate(Vector3.up * dirY * 40); // y축으로 이동
                }
                break;
            case "Enemy":
                if (coll.enabled)
                { // Collider가 활성화된 경우
                    Vector3 dist = playerPos - myPos; // 플레이어와의 거리 계산
                    Vector3 ran = new Vector3(Random.Range(-3, 3), Random.Range(-3, 3), 0); // 랜덤 이동 벡터
                    transform.Translate(ran + dist * 2); // 랜덤 벡터와 거리 벡터를 합쳐 이동
                }
                break;
        }
    }
}
