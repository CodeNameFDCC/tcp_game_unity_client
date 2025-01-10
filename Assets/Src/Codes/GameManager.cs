//Src/Codes/GameManager.cs


/* 게임의 상태와 플레이어 정보를 관리하는 클래스입니다.
게임 시작, 종료, 다시 시작 및 종료 기능을 포함하고 있으며, 게임 시간과 프레임 레이트를 관리합니다. */

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance; // 게임 매니저 인스턴스

    [Header("# Game Control")]
    public bool isLive; // 게임 진행 상태
    public float gameTime; // 게임 시간
    public int targetFrameRate; // 목표 프레임 레이트
    public string version = "1.0.0"; // 게임 버전
    public int latency = 2; // 지연 시간

    [Header("# Player Info")]
    public uint playerId; // 플레이어 ID
    public string deviceId; // 디바이스 ID

    [Header("# Game Object")]
    public PoolManager pool; // 풀 매니저
    public Player player; // 플레이어 객체
    public GameObject hud; // HUD 객체
    public GameObject GameStartUI; // 게임 시작 UI

    // 그리드 크기
    public float gridSize = 1f; // 기본값 1로 설정


    void Awake()
    {
        instance = this; // 인스턴스 초기화
        Application.targetFrameRate = targetFrameRate; // 목표 프레임 레이트 설정
        playerId = (uint)Random.Range(0, 4); // 랜덤 플레이어 ID 설정
    }

    // public void GameStart()
    // {
    //     player.deviceId = deviceId; // 플레이어의 디바이스 ID 설정
    //     player.gameObject.SetActive(true); // 플레이어 활성화
    //     hud.SetActive(true); // HUD 활성화
    //     GameStartUI.SetActive(false); // 게임 시작 UI 비활성화
    //     isLive = true; // 게임 진행 상태 업데이트

    //     AudioManager.instance.PlayBgm(true); // 배경음악 재생
    //     AudioManager.instance.PlaySfx(AudioManager.Sfx.Select); // 선택 효과음 재생
    // }

    public void GameStart()
    {
        player.deviceId = deviceId;  // 플레이어의 디바이스 ID 설정
        player.gameObject.SetActive(true);  // 플레이어 캐릭터 활성화
        hud.SetActive(true);  // HUD UI 활성화
        GameStartUI.SetActive(false);  // 게임 시작 UI 비활성화
        isLive = true;  // 게임 진행 상태 활성화
        AudioManager.instance.PlayBgm(true);  // 배경음악 재생
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Select);  // 선택 효과음 재생
    }

    public void GameOver()
    {
        StartCoroutine(GameOverRoutine()); // 게임 오버 루틴 시작
    }

    IEnumerator GameOverRoutine()
    {
        isLive = false; // 게임 진행 상태 비활성화
        yield return new WaitForSeconds(0.5f); // 0.5초 대기

        AudioManager.instance.PlayBgm(true); // 배경음악 재생
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Lose); // 패배 효과음 재생
    }

    public void GameRetry()
    {
        SceneManager.LoadScene(0); // 씬을 처음으로 로드하여 게임 재시작
    }

    public void GameQuit()
    {
        Application.Quit(); // 애플리케이션 종료
    }

    void Update()
    {
        if (!isLive)
        { // 게임이 진행 중이 아닐 경우
            return; // 함수 종료
        }
        gameTime += Time.deltaTime; // 게임 시간 업데이트
    }
}
