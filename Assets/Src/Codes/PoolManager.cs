using System.Collections;
using System.Collections.Generic;
using UnityEngine; // Unity 관련 네임스페이스

public class PoolManager : MonoBehaviour
{
    // 프리팹을 보관할 배열 변수
    public GameObject[] prefabs;

    // 풀을 담당하는 리스트
    List<GameObject> pool;

    // 유저를 관리할 딕셔너리 (ID와 GameObject 매핑)
    Dictionary<string, GameObject> userDictionary = new Dictionary<string, GameObject>();

    void Awake()
    {
        // 풀 리스트 초기화
        pool = new List<GameObject>();
    }

    // 유저 위치 업데이트 데이터를 기반으로 GameObject를 가져오는 메서드
    public GameObject Get(LocationUpdate.UserLocation data)
    {
        // 유저가 이미 존재하면 해당 유저의 GameObject 반환
        if (userDictionary.TryGetValue(data.id, out GameObject existingUser))
        {
            return existingUser;
        }

        GameObject select = null;

        // 비활성화된 게임 오브젝트를 선택하기 위한 반복문
        foreach (GameObject item in pool)
        {
            if (!item.activeSelf)
            {
                // 비활성화된 오브젝트 발견 시 선택
                select = item;
                // PlayerPrefab 초기화
                select.GetComponent<PlayerPrefab>().Init(data.playerId, data.id);
                select.SetActive(true); // 오브젝트 활성화
                userDictionary[data.id] = select; // 딕셔너리에 추가
                break;
            }
        }

        // 비활성화된 오브젝트를 못 찾은 경우
        if (select == null)
        {
            // 새롭게 생성하고 select 변수에 할당
            select = Instantiate(prefabs[0], transform); // 프리팹 인스턴스화
            pool.Add(select); // 풀에 추가
            // PlayerPrefab 초기화
            select.GetComponent<PlayerPrefab>().Init(data.playerId, data.id);
            userDictionary[data.id] = select; // 딕셔너리에 추가
        }

        return select; // 선택된 GameObject 반환
    }

    // 유저를 제거하는 메서드
    public void Remove(string userId)
    {
        // 딕셔너리에서 유저를 찾고 제거
        if (userDictionary.TryGetValue(userId, out GameObject userObject))
        {
            Debug.Log($"Removing user: {userId}"); // 제거 로그 출력
            userObject.SetActive(false); // 오브젝트 비활성화
            userDictionary.Remove(userId); // 딕셔너리에서 제거
        }
        else
        {
            Debug.Log($"User {userId} not found in dictionary"); // 유저가 없을 경우 로그 출력
        }
    }
}
