/**
 * 플레이어의 이동 및 애니메이션을 관리하는 클래스입니다.
 * 입력을 받아 플레이어의 위치를 업데이트하고, 애니메이션을 제어합니다.
 * 서버에 위치 업데이트 패킷을 전송합니다.
 */

using TMPro;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Vector2 inputVec; // 입력 벡터
    public float speed; // 이동 속도
    public string deviceId; // 디바이스 ID
    public RuntimeAnimatorController[] animCon; // 애니메이터 컨트롤러 배열

    Rigidbody2D rigid; // 2D 리지드바디
    SpriteRenderer spriter; // 스프라이트 렌더러
    Animator anim; // 애니메이터
    TextMeshPro myText; // 텍스트 메쉬 프로

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>(); // 리지드바디 컴포넌트 가져오기
        spriter = GetComponent<SpriteRenderer>(); // 스프라이트 렌더러 가져오기
        anim = GetComponent<Animator>(); // 애니메이터 가져오기
        myText = GetComponentInChildren<TextMeshPro>(); // 자식에서 텍스트 메쉬 프로 가져오기
    }

    void OnEnable()
    {
        // 디바이스 ID를 텍스트로 설정
        if (deviceId.Length > 5)
        {
            myText.text = deviceId[..5]; // ID가 5자 이상이면 잘라서 표시
        }
        else
        {
            myText.text = deviceId; // ID가 5자 이하이면 그대로 표시
        }
        myText.GetComponent<MeshRenderer>().sortingOrder = 6; // 텍스트의 정렬 순서 설정

        // 애니메이터 컨트롤러 설정
        anim.runtimeAnimatorController = animCon[GameManager.instance.playerId];
    }

    // Update는 매 프레임 호출됩니다.
    void Update()
    {
        if (!GameManager.instance.isLive)
        {
            return; // 게임이 진행 중이지 않으면 종료
        }
        // 입력 벡터 업데이트
        inputVec.x = Input.GetAxisRaw("Horizontal");
        inputVec.y = Input.GetAxisRaw("Vertical");

        // 위치 이동 패킷 전송 -> 서버로
        NetworkManager.instance.SendLocationUpdatePacket(rigid.position.x, rigid.position.y);
    }

    void FixedUpdate()
    {
        if (!GameManager.instance.isLive)
        {
            return; // 게임이 진행 중이지 않으면 종료
        }

        // 위치 이동
        Vector2 nextVec = inputVec * speed * Time.fixedDeltaTime; // 다음 위치 계산
        rigid.MovePosition(rigid.position + nextVec); // 리지드바디 위치 이동
    }

    // Update가 끝난 후 적용
    void LateUpdate()
    {
        if (!GameManager.instance.isLive)
        {
            return; // 게임이 진행 중이지 않으면 종료
        }

        anim.SetFloat("Speed", inputVec.magnitude); // 애니메이션 속도 설정

        // 방향에 따라 스프라이트 반전
        if (inputVec.x != 0)
        {
            spriter.flipX = inputVec.x < 0; // 왼쪽으로 이동 시 스프라이트 반전
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (!GameManager.instance.isLive)
        {
            return; // 게임이 진행 중이지 않으면 종료
        }
    }
}
