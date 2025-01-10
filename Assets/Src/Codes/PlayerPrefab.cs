//src/Codes/PlayerPrefab.cs


/**
 * 플레이어 프리팹의 애니메이션과 위치 업데이트를 관리하는 클래스입니다.
 * 서버로부터 위치 정보를 수신하고, 애니메이션의 속성을 업데이트합니다.
 * 디바이스 ID를 텍스트로 표시합니다.
 */

using TMPro;
using UnityEngine;



// public class PlayerPrefab : MonoBehaviour
// {
//     public RuntimeAnimatorController[] animCon; // 애니메이터 컨트롤러 배열
//     private Animator anim; // 애니메이터
//     private SpriteRenderer spriter; // 스프라이트 렌더러
//     private Vector3 lastPosition; // 이전 위치
//     private Vector3 currentPosition; // 현재 위치
//     private uint playerId; // 플레이어 ID
//     TextMeshPro myText; // 텍스트 메쉬 프로

//     void Awake()
//     {
//         anim = GetComponent<Animator>(); // 애니메이터 컴포넌트 가져오기
//         spriter = GetComponent<SpriteRenderer>(); // 스프라이트 렌더러 가져오기
//         myText = GetComponentInChildren<TextMeshPro>(); // 자식에서 텍스트 메쉬 프로 가져오기
//     }

//     public void Init(uint playerId, string id)
//     {
//         anim.runtimeAnimatorController = animCon[playerId]; // 애니메이터 컨트롤러 설정
//         lastPosition = Vector3.zero; // 초기 이전 위치
//         currentPosition = Vector3.zero; // 초기 현재 위치
//         this.playerId = playerId; // 플레이어 ID 설정

//         // 디바이스 ID를 텍스트로 설정
//         if (id.Length > 5)
//         {
//             myText.text = id[..5]; // ID가 5자 이상이면 잘라서 표시
//         }
//         else
//         {
//             myText.text = id; // ID가 5자 이하이면 그대로 표시
//         }
//         myText.GetComponent<MeshRenderer>().sortingOrder = 6; // 텍스트의 정렬 순서 설정
//     }

//     void OnEnable()
//     {
//         anim.runtimeAnimatorController = animCon[playerId]; // 애니메이터 컨트롤러 설정
//     }

//     // 서버로부터 위치 업데이트를 수신할 때 호출될 메서드
//     public void UpdatePosition(float x, float y)
//     {
//         lastPosition = currentPosition; // 이전 위치 업데이트
//         currentPosition = new Vector3(x, y); // 현재 위치 업데이트
//         transform.position = currentPosition; // 오브젝트 위치 변경

//         UpdateAnimation(); // 애니메이션 업데이트
//     }

//     void LateUpdate()
//     {
//         if (!GameManager.instance.isLive)
//         {
//             return; // 게임이 진행 중이지 않으면 종료
//         }

//         UpdateAnimation(); // 애니메이션 업데이트
//     }

//     private void UpdateAnimation()
//     {
//         // 현재 위치와 이전 위치를 비교하여 이동 벡터 계산
//         Vector2 inputVec = currentPosition - lastPosition;

//         anim.SetFloat("Speed", inputVec.magnitude); // 애니메이션 속도 설정

//         if (inputVec.x != 0)
//         {
//             spriter.flipX = inputVec.x < 0; // 방향에 따라 스프라이트 반전
//         }
//     }

//     void OnCollisionStay2D(Collision2D collision)
//     {
//         if (!GameManager.instance.isLive)
//         {
//             return; // 게임이 진행 중이지 않으면 종료
//         }
//     }


// }



public class PlayerPrefab : MonoBehaviour
{
    public RuntimeAnimatorController[] animCon;
    private Animator anim;
    private SpriteRenderer spriter;
    private Vector3 lastPosition;
    private Vector3 currentPosition;
    private Vector3 targetPosition;  // 목표 위치
    private Vector3 currentVelocity; // 현재 속도
    private uint playerId;
    TextMeshPro myText;

    [SerializeField] private float smoothTime = 0.1f; // 보간 시간
    private bool hasTarget = false; // 목표 위치 존재 여부

    void Awake()
    {
        anim = GetComponent<Animator>();
        spriter = GetComponent<SpriteRenderer>();
        myText = GetComponentInChildren<TextMeshPro>();
    }

    public void Init(uint playerId, string id)
    {
        anim.runtimeAnimatorController = animCon[playerId];
        lastPosition = transform.position;
        currentPosition = transform.position;
        targetPosition = transform.position;
        this.playerId = playerId;

        if (id.Length > 5)
        {
            myText.text = id[..5];
        }
        else
        {
            myText.text = id;
        }
        myText.GetComponent<MeshRenderer>().sortingOrder = 6;
    }

    void OnEnable()
    {
        anim.runtimeAnimatorController = animCon[playerId];
    }

    public void UpdatePosition(float x, float y)
    {
        lastPosition = currentPosition;
        targetPosition = new Vector3(x, y);
        hasTarget = true;
    }

    void Update()
    {
        if (!GameManager.instance.isLive || !hasTarget)
            return;

        // 현재 위치에서 목표 위치로 부드럽게 이동
        currentPosition = Vector3.SmoothDamp(
            currentPosition,
            targetPosition,
            ref currentVelocity,
            smoothTime
        );

        transform.position = currentPosition;
        UpdateAnimation();
    }

    void LateUpdate()
    {
        if (!GameManager.instance.isLive)
            return;

        // 목표 위치에 거의 도달했는지 확인
        if (hasTarget && Vector3.Distance(currentPosition, targetPosition) < 0.01f)
        {
            currentPosition = targetPosition;
            transform.position = currentPosition;
            currentVelocity = Vector3.zero;
        }
    }

    private void UpdateAnimation()
    {
        // 이동 방향과 속도 계산
        Vector2 moveDirection = targetPosition - lastPosition;
        float speed = currentVelocity.magnitude;

        anim.SetFloat("Speed", speed);
        if (moveDirection.x != 0)
        {
            spriter.flipX = moveDirection.x < 0;
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (!GameManager.instance.isLive)
            return;
    }
}