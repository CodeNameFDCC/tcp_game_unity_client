using UnityEngine;

namespace squid_game
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField]
        private float moveSpeed = 5f;
        [SerializeField]
        private float positionSendInterval = 0.1f; // 위치 전송 간격 (100ms)

        private bool canMove = false;
        private bool isMoving = false;
        private float lastPositionSentTime;
        private Vector3 lastSentPosition;

        public bool IsMoving => isMoving;

        private Rigidbody2D rb;
        private Animator animator;
        private SpriteRenderer sr;

        private void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            sr = GetComponent<SpriteRenderer>();
            lastSentPosition = transform.position;
        }

        private void Update()
        {
            if (!canMove) return;

            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            // 속도 계산
            float speed = Mathf.Sqrt(horizontal * horizontal + vertical * vertical);
            // Animator에 속도 전달
            animator.SetFloat("Speed", speed);

            if (horizontal != 0 || vertical != 0)
            {
                Vector3 movement = new Vector3(horizontal, vertical, 0).normalized * moveSpeed;
                rb.velocity = movement;

                // 이동 방향으로 회전
                if (movement != Vector3.zero)
                {
                    sr.flipX = movement.x < 0;
                }
                isMoving = true;

                // 일정 간격으로 위치 전송
                if (Time.time - lastPositionSentTime >= positionSendInterval &&
                    Vector3.Distance(transform.position, lastSentPosition) > 0.01f)
                {
                    lastPositionSentTime = Time.time;
                    lastSentPosition = transform.position;

                    // 위치 정보 서버로 전송 (Fire-and-Forget)
                    NetworkManager.Instance.SendPosition(transform.position).ContinueWith(task =>
                    {
                        if (task.Exception != null)
                        {
                            Debug.LogError($"Failed to send position: {task.Exception.InnerException.Message}");
                        }
                    });
                }
            }
            else
            {
                rb.velocity = Vector2.zero;
                isMoving = false;

                // 정지했을 때도 마지막 위치 전송
                if (Vector3.Distance(transform.position, lastSentPosition) > 0.01f)
                {
                    lastSentPosition = transform.position;
                    NetworkManager.Instance.SendPosition(transform.position).ContinueWith(task =>
                    {
                        if (task.Exception != null)
                        {
                            Debug.LogError($"Failed to send position: {task.Exception.InnerException.Message}");
                        }
                    });
                }
            }
        }

        public void EnableMovement()
        {
            canMove = true;
        }

        public void DisableMovement()
        {
            canMove = false;
            rb.velocity = Vector2.zero;
            isMoving = false;
            animator.SetBool("IsMoving", false);
        }
    }
}