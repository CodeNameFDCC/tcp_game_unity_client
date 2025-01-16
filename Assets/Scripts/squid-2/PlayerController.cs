// PlayerController.cs
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private GameClient gameClient;
    private float moveSpeed = 5f;
    private float rotateSpeed = 100f;
    private Vector3 lastPosition;
    private Quaternion lastRotation;

    private void Start()
    {
        gameClient = FindObjectOfType<GameClient>();
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
        SyncTransform();
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(horizontal, 0f, vertical) * moveSpeed * Time.deltaTime;
        transform.Translate(movement, Space.Self);
    }

    private void HandleRotation()
    {
        if (Input.GetKey(KeyCode.Q))
            transform.Rotate(Vector3.up, -rotateSpeed * Time.deltaTime);
        if (Input.GetKey(KeyCode.E))
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }

    private void SyncTransform()
    {
        if (Vector3.Distance(lastPosition, transform.position) > 0.01f)
        {
            gameClient.SendPositionUpdate(transform.position);
            lastPosition = transform.position;
        }

        if (Quaternion.Angle(lastRotation, transform.rotation) > 1f)
        {
            gameClient.SendRotationUpdate(transform.rotation);
            lastRotation = transform.rotation;
        }
    }
}