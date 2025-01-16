// src/Codes/Spawner.cs

/**
 * 사용자 오브젝트를 생성하고 관리하는 클래스입니다.
 * 현재 활성화된 사용자 목록을 유지하며, 위치 업데이트를 처리합니다.
 * 비활성화된 사용자를 오브젝트 풀에서 제거합니다.
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public static Spawner instance; // Singleton 패턴을 위한 인스턴스

    void Awake() // MonoBehaviour의 Awake 메소드, 객체가 생성될 때 호출
    {
        instance = this; // 현재 인스턴스를 싱글톤 인스턴스로 설정
    }

    private Dictionary<string, PlayerPrefab> activeUsers = new Dictionary<string, PlayerPrefab>(); // 활성화된 사용자 목록
    private Dictionary<string, float> lastUpdateTime = new Dictionary<string, float>(); // 각 사용자의 마지막 업데이트 시간
    private const float USER_TIMEOUT = 5f; // 5초 동안 업데이트가 없으면 제거

    // 위치 업데이트 데이터를 기반으로 사용자 오브젝트를 생성하거나 업데이트하는 메소드
    public void Spawn(LocationUpdate data)
    {
        if (!GameManager.instance.isLive) // 게임이 활성화되어 있지 않으면 처리 중지
            return;

        float currentTime = Time.time; // 현재 시간 저장

        // 새로운 위치 업데이트 처리
        foreach (LocationUpdate.UserLocation user in data.users) // 위치 업데이트 데이터의 각 사용자에 대해 반복
        {
            if (user.id == GameManager.instance.deviceId) // 현재 디바이스의 사용자 ID는 무시
                continue;

            lastUpdateTime[user.id] = currentTime; // 해당 사용자의 마지막 업데이트 시간 갱신

            // 활성 사용자 목록에서 기존 플레이어를 찾음
            if (activeUsers.TryGetValue(user.id, out PlayerPrefab existingPlayer))
            {
                existingPlayer.UpdatePosition(user.x, user.y); // 기존 플레이어의 위치 업데이트
            }
            else // 새로운 플레이어인 경우
            {
                GameObject newPlayer = GameManager.instance.pool.Get(user); // 오브젝트 풀에서 사용자 오브젝트 요청
                if (newPlayer != null) // 요청한 오브젝트가 유효한 경우
                {
                    PlayerPrefab playerScript = newPlayer.GetComponent<PlayerPrefab>(); // PlayerPrefab 스크립트 가져오기
                    if (playerScript != null) // 스크립트가 유효한 경우
                    {
                        playerScript.UpdatePosition(user.x, user.y); // 새로운 플레이어의 위치 설정
                        activeUsers[user.id] = playerScript; // 활성 사용자 목록에 추가
                    }
                }
            }
        }

        // 일정 시간 동안 업데이트가 없는 유저만 제거
        List<string> usersToRemove = activeUsers.Keys // 활성 사용자 목록의 키를 가져옴
            .Where(id => currentTime - lastUpdateTime.GetValueOrDefault(id, 0) > USER_TIMEOUT) // 타임아웃 기준에 따라 필터링
            .ToList(); // 리스트로 변환

        // 타임아웃이 발생한 사용자 제거
        foreach (string userId in usersToRemove)
        {
            Debug.Log($"Removing user {userId} due to timeout"); // 제거되는 사용자 로그 출력
            if (activeUsers.TryGetValue(userId, out PlayerPrefab player)) // 사용자 목록에서 플레이어 찾기
            {
                GameManager.instance.pool.Remove(userId); // 오브젝트 풀에서 해당 사용자 제거
                activeUsers.Remove(userId); // 활성 사용자 목록에서 제거
                lastUpdateTime.Remove(userId); // 마지막 업데이트 시간 목록에서 제거
            }
        }
    }

    public List<GameObject> prefabList = new(); // 프리팹 리스트 등록 << 인스팩터 창에서 가능
    private Dictionary<string, Queue<GameObject>> prefabPoolDict = new(); // 딕셔너리 도서관 키값을 사용해서 원하는 데이터를 가져온다.


    public GameObject Spawn(string nameKey, Vector2 position = default, Quaternion rotation = default, Transform parent = null)
    {
        if (prefabPoolDict.ContainsKey(nameKey))
        {
            var origin = prefabPoolDict[nameKey].Dequeue();
            origin.transform.SetPositionAndRotation(position, rotation);
            origin.transform.SetParent(parent);
            return origin;
        }
        var prefab = prefabList.Find(x => x.name == nameKey);
        var copy = Instantiate<GameObject>(prefab, position, rotation, parent);
        copy.name = nameKey;
        return copy;
    }

    public void DeSpawn(GameObject go) // 돌려 보내기
    {
        var nameKey = go.name;
        if (prefabPoolDict.ContainsKey(nameKey))
        {
            prefabPoolDict[nameKey].Enqueue(go);
        }
        else
        {
            prefabPoolDict[nameKey] = new();
            prefabPoolDict[nameKey].Enqueue(go);
        }

    }

}
