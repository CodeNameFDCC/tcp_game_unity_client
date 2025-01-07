/**
 * 사용자 오브젝트를 생성하고 관리하는 클래스입니다.
 * 현재 활성화된 사용자 목록을 유지하며, 위치 업데이트를 처리합니다.
 * 비활성화된 사용자를 오브젝트 풀에서 제거합니다.
 */

using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public static Spawner instance; // Spawner의 싱글턴 인스턴스
    private HashSet<string> currentUsers = new HashSet<string>(); // 현재 활성 사용자 목록

    void Awake()
    {
        instance = this; // 인스턴스 초기화
    }

    // 위치 업데이트 데이터를 기반으로 사용자 스폰
    public void Spawn(LocationUpdate data)
    {
        if (!GameManager.instance.isLive)
        {
            return; // 게임이 진행 중이지 않으면 종료
        }

        HashSet<string> newUsers = new HashSet<string>(); // 새로운 사용자 목록

        // 위치 업데이트 데이터에 있는 사용자 처리
        foreach (LocationUpdate.UserLocation user in data.users)
        {
            newUsers.Add(user.id); // 새로운 사용자 추가

            // 사용자 오브젝트를 풀에서 가져와 위치 업데이트
            GameObject player = GameManager.instance.pool.Get(user);
            PlayerPrefab playerScript = player.GetComponent<PlayerPrefab>();
            playerScript.UpdatePosition(user.x, user.y); // 위치 업데이트
        }

        // 현재 사용자 목록과 비교하여 비활성화할 사용자 제거
        foreach (string userId in currentUsers)
        {
            if (!newUsers.Contains(userId))
            {
                GameManager.instance.pool.Remove(userId); // 비활성화
            }
        }

        currentUsers = newUsers; // 현재 사용자 목록 업데이트
    }
}
