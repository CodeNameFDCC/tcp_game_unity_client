/**
 * 오브젝트 풀링을 관리하는 클래스입니다.
 * 비활성화된 게임 오브젝트를 재사용하여 성능을 최적화합니다.
 * 사용자 정보를 딕셔너리로 관리하여 빠른 접근을 제공합니다.
 */

using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    // 프리팹을 보관할 변수
    public GameObject[] prefabs;

    // 풀 담당 하는 리스트들
    List<GameObject> pool;

    // 유저를 관리할 딕셔너리
    Dictionary<string, GameObject> userDictionary = new Dictionary<string, GameObject>();

    void Awake()
    {
        pool = new List<GameObject>(); // 게임 오브젝트 풀 초기화
    }

    // 주어진 위치 업데이트 데이터를 기반으로 게임 오브젝트를 반환
    public GameObject Get(LocationUpdate.UserLocation data)
    {
        // 유저가 이미 존재하면 해당 유저 반환
        if (userDictionary.TryGetValue(data.id, out GameObject existingUser))
        {
            return existingUser; // 기존 유저 반환
        }

        GameObject select = null;

        // 선택한 풀의 놀고 있는(비활성화) 게임 오브젝트 접근
        foreach (GameObject item in pool)
        {
            if (!item.activeSelf)
            {
                // 발견하면 select에 할당
                select = item;
                select.GetComponent<PlayerPrefab>().Init(data.playerId, data.id); // 초기화
                select.SetActive(true); // 활성화
                userDictionary[data.id] = select; // 딕셔너리에 추가
                break;
            }
        }

        // 못 찾으면
        if (select == null)
        {
            // 새롭게 생성하고 select 변수에 할당
            select = Instantiate(prefabs[0], transform); // 프리팹 인스턴스화
            pool.Add(select); // 풀에 추가
            select.GetComponent<PlayerPrefab>().Init(data.playerId, data.id); // 초기화
            userDictionary[data.id] = select; // 딕셔너리에 추가
        }

        return select; // 선택된 게임 오브젝트 반환
    }

    // 주어진 사용자 ID에 해당하는 게임 오브젝트를 제거
    public void Remove(string userId)
    {
        if (userDictionary.TryGetValue(userId, out GameObject userObject))
        {
            Debug.Log($"Removing user: {userId}"); // 제거 로그
            userObject.SetActive(false); // 비활성화
            userDictionary.Remove(userId); // 딕셔너리에서 제거
        }
        else
        {
            Debug.Log($"User {userId} not found in dictionary"); // 유저 미발견 로그
        }
    }
}
