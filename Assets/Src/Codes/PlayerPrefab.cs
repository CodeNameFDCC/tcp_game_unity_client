using TMPro; // TextMeshPro 관련 네임스페이스
using UnityEngine; // Unity 관련 네임스페이스

public class PlayerPrefab : MonoBehaviour
{
    // 애니메이터 컨트롤러 배열
    public RuntimeAnimatorController[] animCon;

    // 필요한 컴포넌트 선언
    private Animator anim; // 애니메이터
    private SpriteRenderer spriter; // 스프라이트 렌더러
    private Vector3 lastPosition; // 이전 위치
    private Vector3 currentPosition; // 현재 위치
    private uint playerId; // 플레이어 ID
    TextMeshPro myText; // TextMeshPro 텍스트

    private float FLIP_THRESHOLD = 0.1f; // 스프라이트 뒤집기 임계값

    void Awake()
    {
        // 필요한 컴포넌트를 가져옴
        anim = GetComponent<Animator>();
        spriter = GetComponent<SpriteRenderer>();
        myText = GetComponentInChildren<TextMeshPro>();
    }

    // 플레이어 초기화 메서드
    public void Init(uint playerId, string id)
    {
        // 애니메이터 컨트롤러 설정
        anim.runtimeAnimatorController = animCon[playerId];

        // 위치 초기화
        lastPosition = Vector3.zero;
        currentPosition = Vector3.zero;
        this.playerId = playerId;

        // 장치 ID가 5자 이상일 경우 잘라서 표시
        if (id.Length > 5)
        {
            myText.text = id[..5];
        }
        else
        {
            myText.text = id; // 장치 ID를 텍스트로 설정
        }

        // 텍스트의 정렬 순서 설정
        myText.GetComponent<MeshRenderer>().sortingOrder = 6;
    }

    void OnEnable()
    {
        // 활성화 시 애니메이터 컨트롤러 설정
        anim.runtimeAnimatorController = animCon[playerId];
    }

    // 서버로부터 위치 업데이트를 수신할 때 호출될 메서드
    public void UpdatePosition(float x, float y)
    {
        // 이전 위치를 현재 위치로 업데이트
        lastPosition = currentPosition; // 현재 위치를 이전 위치로 설정
        currentPosition = new Vector3(x, y); // 새로운 위치 설정

        // 보간을 사용하여 부드럽게 이동
        Vector3 nextPos = Vector3.Lerp(lastPosition, currentPosition, 0.1f);
        transform.position = nextPos; // 오브젝트의 위치 업데이트

        UpdateAnimation(); // 애니메이션 업데이트
    }

    void LateUpdate()
    {
        // 게임이 활성화되지 않은 경우 조기 종료
        if (!GameManager.instance.isLive)
        {
            return;
        }

        UpdateAnimation(); // 애니메이션 업데이트
    }

    private void UpdateAnimation()
    {
        // 현재 위치와 이전 위치를 비교하여 이동 벡터 계산
        Vector2 inputVec = currentPosition - lastPosition;

        // 애니메이션 속도 설정
        anim.SetFloat("Speed", inputVec.magnitude);

        // 스프라이트 방향 설정
        if (Mathf.Abs(inputVec.x) > FLIP_THRESHOLD)
        {
            spriter.flipX = inputVec.x < 0; // 왼쪽 방향이면 스프라이트 뒤집기
        }
    }

    // 충돌이 발생하고 있는 동안 호출됨
    void OnCollisionStay2D(Collision2D collision)
    {
        // 게임이 활성화되지 않은 경우 조기 종료
        if (!GameManager.instance.isLive)
        {
            return;
        }
        // 충돌 처리 로직을 여기에 추가할 수 있음
    }
}
