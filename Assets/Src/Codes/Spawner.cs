using System.Collections;
using System.Collections.Generic;
using UnityEngine; // Unity 관련 네임스페이스

public class Spawner : MonoBehaviour
{
    public static Spawner instance; // Spawner 클래스의 인스턴스를 전역으로 접근할 수 있게 함
    private HashSet<string> currentUsers = new HashSet<string>(); // 현재 활성화된 사용자 ID를 저장하는 해시셋

    void Awake()
    {
        // Spawner 인스턴스 초기화
        instance = this; // Singleton 패턴을 위해 인스턴스를 설정
    }

    // 위치 업데이트 데이터를 기반으로 사용자 스폰 및 위치 업데이트
    public void Spawn(LocationUpdate data)
    {
        // 게임이 비활성화된 경우 조기 종료
        if (!GameManager.instance.isLive)
        {
            return;
        }

        HashSet<string> newUsers = new HashSet<string>(); // 새로 활성화된 사용자 ID를 저장할 해시셋

        // 위치 업데이트 데이터의 사용자 목록 반복
        foreach (LocationUpdate.UserLocation user in data.users)
        {
            Player currentPlayer = GameManager.instance.player; // 현재 플레이어 인스턴스 가져오기
            if (user.id == currentPlayer.deviceId)
            {
                // 플레이어의 위치를 업데이트
                Vector2 nextVec = new Vector2(user.x, user.y); // 사용자 위치를 벡터로 변환
                GameManager.instance.player.MoveToNextPosition(nextVec); // 플레이어의 위치로 이동
                continue; // 다음 사용자로 넘어감
            }
            newUsers.Add(user.id); // 새 사용자 목록에 추가

            // 사용자에 대한 게임 오브젝트를 가져옴
            GameObject player = GameManager.instance.pool.Get(user);
            PlayerPrefab playerScript = player.GetComponent<PlayerPrefab>(); // PlayerPrefab 스크립트 가져오기
            playerScript.UpdatePosition(user.x, user.y); // 사용자 위치 업데이트
        }

        // 현재 사용자 목록과 새 사용자 목록 비교
        foreach (string userId in currentUsers)
        {
            // 새 목록에 없는 사용자는 제거
            if (!newUsers.Contains(userId))
            {
                GameManager.instance.pool.Remove(userId); // 풀에서 사용자 제거
            }
        }

        // 현재 사용자 목록을 새 사용자 목록으로 업데이트
        currentUsers = newUsers;
    }
}
