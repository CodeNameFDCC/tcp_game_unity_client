using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    // Start is called before the first frame update
    Transform target;
    Rigidbody2D rb;

    public float speed = 15;
    void Start()
    {
        target = FindObjectOfType<Player>().transform;
        rb = GetComponent<Rigidbody2D>();
        StartCoroutine(CoRbZero());
    }

    // Update is called once per frame
    void Update()
    {
        if (target == null)
        {
            target = FindObjectOfType<Player>().transform;
            Debug.Log("플레이어 찾는중");
            return;
        }
        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
    }

    IEnumerator CoRbZero()
    {
        while (true)
        {
            yield return new WaitForSeconds(4f);
            rb.velocity = Vector2.zero;
        }
    }
}
