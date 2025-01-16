using TMPro; // TextMeshPro 관련 네임스페이스
using UnityEngine; // Unity 관련 네임스페이스

public class Player : MonoBehaviour
{
    // 입력 벡터 (x, y 방향)
    public Vector2 inputVec;

    // 이동 속도
    public float speed;

    // 장치 ID
    public string deviceId;

    // 애니메이터 컨트롤러 배열
    public RuntimeAnimatorController[] animCon;

    // 필요한 컴포넌트 선언
    Rigidbody2D rigid; // 2D 물리체
    SpriteRenderer spriter; // 스프라이트 렌더러
    Animator anim; // 애니메이터
    TextMeshPro myText; // TextMeshPro 텍스트

    void Awake()
    {
        // 필요한 컴포넌트를 가져옴
        rigid = GetComponent<Rigidbody2D>();
        spriter = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        myText = GetComponentInChildren<TextMeshPro>();
    }

    void OnEnable()
    {
        // 장치 ID가 5자 이상일 경우 잘라서 표시
        if (deviceId.Length > 5)
        {
            myText.text = deviceId[..5];
        }
        else
        {
            myText.text = deviceId; // 장치 ID를 텍스트로 설정
        }

        // 텍스트의 정렬 순서 설정
        myText.GetComponent<MeshRenderer>().sortingOrder = 6;

        // 애니메이터 컨트롤러 설정
        anim.runtimeAnimatorController = animCon[GameManager.instance.playerId];
    }

    // Update는 매 프레임 호출됨
    void Update()
    {
        // 게임이 활성화되지 않은 경우 조기 종료
        if (!GameManager.instance.isLive)
        {
            return;
        }

        // 입력 값을 가져옴
        inputVec.x = Input.GetAxisRaw("Horizontal"); // 수평 입력
        inputVec.y = Input.GetAxisRaw("Vertical"); // 수직 입력

        // 위치 이동 패킷 전송 -> 서버로
        NetworkManager.instance.SendLocationUpdatePacket(rigid.position.x, rigid.position.y);
    }

    // 다음 위치로 이동하는 메서드
    public void MoveToNextPosition(Vector2 nextVec)
    {
        // 기존의 위치 이동 코드 주석 처리
        // rigid.MovePosition(nextVec);

        // 보간을 사용하여 부드럽게 이동
        Vector2 newPos = Vector2.Lerp(rigid.position, nextVec, Time.fixedDeltaTime);
        rigid.MovePosition(newPos); // 새로운 위치로 이동
    }

    // FixedUpdate는 물리 업데이트에 사용됨
    void FixedUpdate()
    {
        // 게임이 활성화되지 않은 경우 조기 종료
        if (!GameManager.instance.isLive)
        {
            return;
        }

        // 위치 이동
        Vector2 nextVec = inputVec * speed * Time.fixedDeltaTime; // 이동할 벡터 계산
        rigid.MovePosition(rigid.position + nextVec); // 현재 위치에 이동 벡터를 더함
    }

    // Update가 끝난 후 애니메이션 관련 작업 수행
    void LateUpdate()
    {
        // 게임이 활성화되지 않은 경우 조기 종료
        if (!GameManager.instance.isLive)
        {
            return;
        }

        // 애니메이션의 속도 설정
        anim.SetFloat("Speed", inputVec.magnitude);

        // 방향에 따라 스프라이트를 뒤집음
        if (inputVec.x != 0)
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
