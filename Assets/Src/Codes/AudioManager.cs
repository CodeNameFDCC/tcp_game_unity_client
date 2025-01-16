using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임의 모든 오디오를 관리하는 매니저 클래스
/// BGM과 효과음을 재생, 정지, 효과 적용 등을 담당
/// </summary>
public class AudioManager : MonoBehaviour
{
    // 싱글톤 패턴을 위한 정적 인스턴스
    public static AudioManager instance;

    [Header("#BGM")]
    public AudioClip bgmClip;        // 배경음악 클립
    public float bgmVolume;          // 배경음악 볼륨
    private AudioSource bgmPlayer;    // 배경음악 재생기
    private AudioHighPassFilter bgmEffect;    // 배경음악 필터 효과

    [Header("#SFX")]
    public AudioClip[] sfxClips;     // 효과음 클립 배열
    public float sfxVolume;          // 효과음 볼륨
    public int channels;             // 동시 재생 가능한 효과음 채널 수
    private AudioSource[] sfxPlayers; // 효과음 재생기 배열
    private int channelIndex;        // 현재 사용할 채널 인덱스

    // 효과음 종류를 열거형으로 정의
    public enum Sfx
    {
        Dead,       // 0: 사망 효과음
        Hit,        // 1: 피격 효과음
        LevelUp = 3,// 3: 레벨업 효과음
        Lose,       // 4: 패배 효과음
        Melee,      // 5: 근접 공격 효과음
        Range = 7,  // 7: 원거리 공격 효과음
        Select,     // 8: 선택 효과음
        Win         // 9: 승리 효과음
    }

    /// <summary>
    /// Unity 초기화 단계에서 실행되는 메서드
    /// 싱글톤 인스턴스 설정 및 초기화 수행
    /// </summary>
    private void Awake()
    {
        instance = this;
        InitializeAudio();
    }

    /// <summary>
    /// 오디오 시스템 초기화 메서드
    /// BGM과 효과음 플레이어를 설정
    /// </summary>
    private void InitializeAudio()
    {
        // 배경음악 플레이어 초기화
        InitializeBGMPlayer();

        // 효과음 플레이어 초기화
        InitializeSFXPlayers();
    }

    /// <summary>
    /// 배경음악 플레이어 초기화 및 설정
    /// </summary>
    private void InitializeBGMPlayer()
    {
        // BGM 플레이어 오브젝트 생성 및 설정
        GameObject bgmObject = new GameObject("BgmPlayer");
        bgmObject.transform.parent = transform;

        // 오디오 소스 컴포넌트 추가 및 설정
        bgmPlayer = bgmObject.AddComponent<AudioSource>();
        bgmPlayer.playOnAwake = false;
        bgmPlayer.loop = true;
        bgmPlayer.volume = bgmVolume;
        bgmPlayer.clip = bgmClip;

        // 카메라의 하이패스 필터 효과 가져오기
        bgmEffect = Camera.main.GetComponent<AudioHighPassFilter>();
    }

    /// <summary>
    /// 효과음 플레이어 배열 초기화 및 설정
    /// </summary>
    private void InitializeSFXPlayers()
    {
        // SFX 플레이어 오브젝트 생성
        GameObject sfxObject = new GameObject("SfxPlayer");
        sfxObject.transform.parent = transform;

        // 채널 수만큼 오디오 소스 생성
        sfxPlayers = new AudioSource[channels];
        for (int i = 0; i < sfxPlayers.Length; i++)
        {
            sfxPlayers[i] = sfxObject.AddComponent<AudioSource>();
            sfxPlayers[i].playOnAwake = false;
            sfxPlayers[i].bypassListenerEffects = true;
            sfxPlayers[i].volume = sfxVolume;
        }
    }

    /// <summary>
    /// 배경음악 재생/정지를 제어하는 메서드
    /// </summary>
    /// <param name="isPlay">true면 재생, false면 정지</param>
    public void PlayBgm(bool isPlay)
    {
        if (isPlay)
        {
            bgmPlayer.Play();
        }
        else
        {
            bgmPlayer.Stop();
        }
    }

    /// <summary>
    /// 배경음악의 하이패스 필터 효과를 켜고 끄는 메서드
    /// </summary>
    /// <param name="isPlay">true면 효과 켜기, false면 끄기</param>
    public void EffectBgm(bool isPlay)
    {
        bgmEffect.enabled = isPlay;
    }

    /// <summary>
    /// 효과음을 재생하는 메서드
    /// 여러 채널을 순환하며 비어있는 채널에서 효과음 재생
    /// </summary>
    /// <param name="sfx">재생할 효과음 종류</param>
    public void PlaySfx(Sfx sfx)
    {
        // 모든 채널을 순회하며 재생 가능한 채널 찾기
        for (int i = 0; i < sfxPlayers.Length; i++)
        {
            int loopIndex = (i + channelIndex) % sfxPlayers.Length;

            // 현재 채널이 사용 중이면 다음 채널 확인
            if (sfxPlayers[loopIndex].isPlaying)
            {
                continue;
            }

            // Hit나 Melee 효과음의 경우 랜덤 변형음 사용
            int ranIndex = 0;
            if (sfx == Sfx.Hit || sfx == Sfx.Melee)
            {
                ranIndex = Random.Range(0, 2);
            }

            // 채널 인덱스 업데이트 및 효과음 재생
            channelIndex = loopIndex;
            sfxPlayers[loopIndex].clip = sfxClips[(int)sfx + ranIndex];
            sfxPlayers[loopIndex].Play();
            break;
        }
    }
}