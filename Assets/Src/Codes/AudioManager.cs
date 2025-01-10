//Src/Codes/AudioManager.cs

/* 오디오 관리 클래스이며, 배경음악(BGM)과 효과음(SFX)을 재생하고 관리합니다.
BGM과 SFX 플레이어를 초기화하며, SFX는 여러 채널에서 동시에 재생할 수 있습니다.
각 음원의 볼륨과 효과를 조절하는 기능을 제공합니다. */

using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance; // 오디오 매니저 인스턴스

    [Header("#BGM")]
    public AudioClip bgmClip; // 배경음악 클립
    public float bgmVolume; // 배경음악 볼륨
    AudioSource bgmPlayer; // 배경음악 플레이어
    AudioHighPassFilter bgmEffect; // 배경음악 효과

    [Header("#SFX")]
    public AudioClip[] sfxClips; // 효과음 클립 배열
    public float sfxVolume; // 효과음 볼륨
    public int channels; // 효과음 채널 수
    AudioSource[] sfxPlayers; // 효과음 플레이어 배열
    int channelIndex; // 현재 채널 인덱스

    public enum Sfx { Dead, Hit, LevelUp = 3, Lose, Melee, Range = 7, Select, Win } // 효과음 종류 열거형

    void Awake()
    {
        instance = this; // 인스턴스 초기화
        Init(); // 초기화 메서드 호출
    }

    void Init()
    {
        // 배경음 플레이어 초기화
        GameObject bgmObject = new GameObject("BgmPlayer"); // 배경음악 플레이어 오브젝트 생성
        bgmObject.transform.parent = transform; // 현재 오브젝트의 자식으로 설정
        bgmPlayer = bgmObject.AddComponent<AudioSource>(); // AudioSource 컴포넌트 추가
        bgmPlayer.playOnAwake = false; // 시작 시 자동재생 비활성화
        bgmPlayer.loop = true; // 반복 재생 설정
        bgmPlayer.volume = bgmVolume; // 볼륨 설정
        bgmPlayer.clip = bgmClip; // 배경음악 클립 설정
        bgmEffect = Camera.main.GetComponent<AudioHighPassFilter>(); // 카메라에서 고역 통과 필터 가져오기

        // 효과음 플레이어 초기화
        GameObject sfxObject = new GameObject("SfxPlayer"); // 효과음 플레이어 오브젝트 생성
        sfxObject.transform.parent = transform; // 현재 오브젝트의 자식으로 설정
        sfxPlayers = new AudioSource[channels]; // 효과음 플레이어 배열 초기화

        for (int i = 0; i < sfxPlayers.Length; i++)
        {
            sfxPlayers[i] = sfxObject.AddComponent<AudioSource>(); // 각 채널에 AudioSource 추가
            sfxPlayers[i].playOnAwake = false; // 시작 시 자동재생 비활성화
            sfxPlayers[i].bypassListenerEffects = true; // 리스너 효과 우회 설정
            sfxPlayers[i].volume = sfxVolume; // 볼륨 설정
        }
    }

    public void PlayBgm(bool isPlay)
    {
        if (isPlay)
        {
            bgmPlayer.Play(); // BGM 재생
        }
        else
        {
            bgmPlayer.Stop(); // BGM 정지
        }
    }

    public void EffectBgm(bool isPlay)
    {
        bgmEffect.enabled = isPlay; // 고역 통과 필터 효과 활성화 또는 비활성화
    }

    public void PlaySfx(Sfx sfx)
    {
        for (int i = 0; i < sfxPlayers.Length; i++)
        {
            int loopIndex = (i + channelIndex) % sfxPlayers.Length; // 채널 인덱스 계산

            if (sfxPlayers[loopIndex].isPlaying)
            {
                continue; // 현재 채널이 재생 중이면 다음 채널로 이동
            }

            int ranIndex = 0; // 랜덤 인덱스 초기화
            if (sfx == Sfx.Hit || sfx == Sfx.Melee)
            { // 특정 효과음인 경우
                ranIndex = Random.Range(0, 2); // 랜덤 인덱스 설정
            }

            channelIndex = loopIndex; // 현재 채널 인덱스 업데이트
            sfxPlayers[loopIndex].clip = sfxClips[(int)sfx + ranIndex]; // 효과음 클립 설정
            sfxPlayers[loopIndex].Play(); // 효과음 재생
            break; // 루프 종료
        }
    }
}
