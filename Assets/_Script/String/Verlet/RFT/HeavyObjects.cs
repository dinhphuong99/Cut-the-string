using UnityEngine;

public class HeavyObjects : MonoBehaviour
{
    public Rigidbody2D bob;
    public float gravity = 9.81f;
    private Vector2 oldPos;      // vị trí trước khi tính

    Vector2 vel;

    void Awake()
    {
        bob.gravityScale = 0f; // sử dụng gravity thủ công
        oldPos = bob.position;
        if(bob != null)
        {
            vel = bob.linearVelocity;           // lấy velocity hiện tại làm khởi đầu
        }
    }

    void SetUpPreviousStatic()
    {
        Vector2 oldPos = bob.position;        // vị trí trước khi tính
        Vector2 pos = oldPos;
        Vector2 vel = bob.linearVelocity;           // lấy velocity hiện tại làm khởi đầu
    }
}
