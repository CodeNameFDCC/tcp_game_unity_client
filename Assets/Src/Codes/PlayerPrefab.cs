// src/Codes/PlayerPrefab.cs

/**
 * 플레이어 프리팹의 애니메이션과 위치 업데이트를 관리하는 클래스입니다.
 * 서버로부터 위치 정보를 수신하고, 애니메이션의 속성을 업데이트합니다.
 * 디바이스 ID를 텍스트로 표시합니다.
 */

using TMPro; // TextMeshPro를 사용하기 위한 네임스페이스
using UnityEngine; // Unity 관련 기능을 사용하기 위한 네임스페이스

public class PlayerPrefab : MonoBehaviour
{
    public RuntimeAnimatorController[] animCon; // 애니메이터 컨트롤러 배열
    private Animator anim; // 애니메이터 컴포넌트
    private SpriteRenderer spriter; // 스프라이트 렌더러
    private Vector3 lastPosition; // 이전 위치
    private Vector3 currentPosition; // 현재 위치
    private Vector3 targetPosition; // 목표 위치
    private Vector3 currentVelocity; // 현재 속도
    private uint playerId; // 플레이어 ID
    TextMeshPro myText; // 텍스트 메쉬 프로

    [SerializeField] private float smoothTime = 0.1f; // 부드러운 이동을 위한 시간
    private bool hasTarget = false; // 목표 위치가 설정되었는지 여부
    private float lastUpdateTime; // 마지막 업데이트 시간
    private const float UPDATE_THRESHOLD = 0.01f; // 업데이트 간격 기준
    private const float MIN_MOVE_THRESHOLD = 0.001f; // 최소 이동 기준

    // Awake 메소드, 객체가 생성될 때 호출
    void Awake()
    {
        anim = GetComponent<Animator>(); // 애니메이터 컴포넌트 가져오기
        spriter = GetComponent<SpriteRenderer>(); // 스프라이트 렌더러 가져오기
        myText = GetComponentInChildren<TextMeshPro>(); // 자식에서 텍스트 메쉬 프로 가져오기
    }

    // 플레이어 초기화 메소드
    public void Init(uint playerId, string id)
    {
        anim.runtimeAnimatorController = animCon[playerId]; // 애니메이터 컨트롤러 설정
        lastPosition = transform.position; // 현재 위치를 이전 위치로 설정
        currentPosition = transform.position; // 현재 위치 초기화
        targetPosition = transform.position; // 목표 위치 초기화
        this.playerId = playerId; // 플레이어 ID 설정
        lastUpdateTime = Time.time; // 마지막 업데이트 시간 초기화

        // 디바이스 ID를 텍스트로 설정
        if (id.Length > 5)
        {
            myText.text = id[..5]; // ID가 5자 이상이면 잘라서 표시
        }
        else
        {
            myText.text = id; // ID가 5자 이하이면 그대로 표시
        }

        // 스프라이트의 색상 초기화
        if (spriter != null)
        {
            spriter.color = new Color(1f, 1f, 1f, 1f); // 완전 불투명
        }

        myText.GetComponent<MeshRenderer>().sortingOrder = 6; // 텍스트의 정렬 순서 설정
    }

    // 활성화될 때 호출되는 메소드
    void OnEnable()
    {
        // 애니메이터 컨트롤러 설정
        if (anim != null && playerId < animCon.Length)
        {
            anim.runtimeAnimatorController = animCon[playerId];
        }

        // 스프라이트 색상 초기화
        if (spriter != null)
        {
            spriter.color = new Color(1f, 1f, 1f, 1f); // 완전 불투명
        }
    }

    // 위치 업데이트 메소드
    public void UpdatePosition(float x, float y)
    {
        Vector3 newTargetPos = new Vector3(x, y); // 새로운 목표 위치 생성

        // 너무 작은 움직임은 무시
        if (hasTarget && Vector3.Distance(targetPosition, newTargetPos) < MIN_MOVE_THRESHOLD)
        {
            return; // 목표 위치가 너무 가까우면 종료
        }

        // 업데이트 간격이 너무 짧으면 스킵
        float currentTime = Time.time; // 현재 시간 저장
        if (currentTime - lastUpdateTime < UPDATE_THRESHOLD)
        {
            return; // 업데이트 간격이 짧으면 종료
        }

        lastPosition = currentPosition; // 이전 위치 업데이트
        targetPosition = newTargetPos; // 목표 위치 업데이트
        lastUpdateTime = currentTime; // 마지막 업데이트 시간 갱신
        hasTarget = true; // 목표 위치가 설정되었음을 표시
    }

    // 매 프레임 호출되는 Update 메소드
    void Update()
    {
        // 게임이 진행 중이지 않거나 목표가 없으면 종료
        if (!GameManager.instance.isLive || !hasTarget)
            return;

        float deltaTime = Time.deltaTime; // 프레임 간 시간 차이 저장

        // 부드러운 이동 처리
        currentPosition = Vector3.SmoothDamp(
            currentPosition, // 현재 위치
            targetPosition, // 목표 위치
            ref currentVelocity, // 현재 속도 참조
            smoothTime, // 부드러운 이동 시간
            Mathf.Infinity, // 최대 속도
            deltaTime // 프레임 간 시간 차이
        );

        // 위치가 충분히 변경되었을 때만 업데이트
        if (Vector3.Distance(transform.position, currentPosition) > MIN_MOVE_THRESHOLD)
        {
            transform.position = currentPosition; // 오브젝트 위치 업데이트
            UpdateAnimation(deltaTime); // 애니메이션 업데이트
        }
    }

    // LateUpdate 메소드, Update 이후 호출
    void LateUpdate()
    {
        // 게임이 진행 중이지 않으면 종료
        if (!GameManager.instance.isLive)
            return;

        // 목표 위치에 충분히 가까워졌을 때
        if (hasTarget && Vector3.Distance(currentPosition, targetPosition) < MIN_MOVE_THRESHOLD)
        {
            currentPosition = targetPosition; // 현재 위치를 목표 위치로 설정
            transform.position = currentPosition; // 오브젝트 위치 업데이트
            currentVelocity = Vector3.zero; // 속도 초기화
            anim.SetFloat("Speed", 0); // 애니메이션 속도 초기화
        }
    }

    // 애니메이션 업데이트 메소드
    private void UpdateAnimation(float deltaTime)
    {
        if (anim == null || spriter == null) return; // 애니메이터나 스프라이트가 없으면 종료

        Vector2 moveDirection = targetPosition - lastPosition; // 이동 방향 계산
        float speed = currentVelocity.magnitude; // 현재 속도 계산

        // 부드러운 애니메이션 전환
        float currentSpeed = anim.GetFloat("Speed"); // 현재 애니메이션 속도 가져오기
        float targetSpeed = speed; // 목표 애니메이션 속도 설정
        float smoothSpeed = Mathf.Lerp(currentSpeed, targetSpeed, deltaTime * 10f); // 부드러운 속도 전환

        anim.SetFloat("Speed", smoothSpeed); // 애니메이션 속도 설정

        // 이동 방향에 따라 스프라이트 반전
        if (Mathf.Abs(moveDirection.x) > MIN_MOVE_THRESHOLD)
        {
            spriter.flipX = moveDirection.x < 0; // 왼쪽으로 이동할 경우 스프라이트 반전
        }
    }

    // 비활성화될 때 호출되는 메소드
    void OnDisable()
    {
        hasTarget = false; // 목표가 없음을 표시
        currentVelocity = Vector3.zero; // 현재 속도 초기화
    }
}
