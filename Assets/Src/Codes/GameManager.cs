using System.Collections; // 코루틴 관련 네임스페이스
using UnityEngine; // Unity 관련 네임스페이스
using UnityEngine.SceneManagement; // 씬 관리 관련 네임스페이스

/// <summary>
/// 게임의 전반적인 기능들을 관리
/// </summary>
public class GameManager : MonoBehaviour
{
    // GameManager의 Singleton 인스턴스
    public static GameManager instance;

    [Header("# Game Control")]
    public bool isLive; // 게임이 활성화되어 있는지 여부
    public float gameTime; // 게임 시간이 경과된 시간
    public int targetFrameRate; // 목표 프레임 레이트
    public string version = "1.0.0"; // 게임 버전
    public int latency = 2; // 지연 시간 (초)

    [Header("# Player Info")]
    public uint playerId; // 플레이어 ID
    public string deviceId; // 장치 ID

    [Header("# Game Object")]
    public PoolManager pool; // 풀 매니저 인스턴스
    public Player player; // 플레이어 인스턴스
    public GameObject hud; // HUD 오브젝트
    public GameObject GameStartUI; // 게임 시작 UI 오브젝트

    void Awake()
    {
        // GameManager 인스턴스 초기화
        instance = this;
        // 목표 프레임 레이트 설정
        Application.targetFrameRate = targetFrameRate;
    }

    // 게임 시작 메서드
    public void GameStart()
    {
        // 플레이어의 장치 ID 설정
        player.deviceId = deviceId;
        player.gameObject.SetActive(true); // 플레이어 활성화
        hud.SetActive(true); // HUD 활성화
        GameStartUI.SetActive(false); // 게임 시작 UI 비활성화
        isLive = true; // 게임 활성화 상태로 설정

        // 배경 음악 및 효과음 재생
        AudioManager.instance.PlayBgm(true);
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Select);
    }

    // 게임 오버 메서드
    public void GameOver()
    {
        StartCoroutine(GameOverRoutine()); // 게임 오버 루틴 시작
    }

    // 게임 오버 처리 코루틴
    IEnumerator GameOverRoutine()
    {
        isLive = false; // 게임 비활성화 상태로 설정
        yield return new WaitForSeconds(0.5f); // 0.5초 대기

        // 게임 오버 음악 및 효과음 재생
        AudioManager.instance.PlayBgm(true);
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Lose);
    }

    // 게임 재시작 메서드
    public void GameRetry()
    {
        SceneManager.LoadScene(0); // 첫 번째 씬 로드
    }

    // 게임 종료 메서드
    public void GameQuit()
    {
        Application.Quit(); // 게임 애플리케이션 종료
    }

    void Update()
    {
        // 게임이 활성화되어 있지 않으면 조기 종료
        if (!isLive)
        {
            return;
        }
        // 경과된 게임 시간 업데이트
        gameTime += Time.deltaTime; // 프레임 간 경과 시간 추가
    }
}
