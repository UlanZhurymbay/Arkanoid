using UnityEngine;

/// <summary>
/// Мяч. Пока «прилеплен» к ракетке — едет вместе с ней.
/// После запуска летит с постоянной скоростью; угол отскока от ракетки
/// зависит от того, куда мяч попал (край — острее, центр — вертикально).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Ball : MonoBehaviour
{
    public float Speed = 8f;

    private const float MaxBounceAngle = 60f;   // градусов от вертикали
    private const float MinVerticalShare = 0.3f; // чтобы мяч не летал почти горизонтально

    private Rigidbody2D _rb;
    private Paddle _paddle;
    private bool _attached = true;

    public bool IsAttached => _attached;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Init(Paddle paddle)
    {
        _paddle = paddle;
        Attach();
    }

    /// <summary>Прилепить мяч к ракетке (в начале и после потери жизни).</summary>
    public void Attach()
    {
        _attached = true;
        _rb.simulated = false;
        _rb.linearVelocity = Vector2.zero;
        FollowPaddle();
    }

    public void Launch()
    {
        if (!_attached) return;
        _attached = false;
        _rb.position = transform.position;
        _rb.simulated = true;

        var dir = new Vector2(Random.Range(-0.5f, 0.5f), 1f).normalized;
        _rb.linearVelocity = dir * Speed;
    }

    private void LateUpdate()
    {
        if (_attached)
        {
            FollowPaddle();
            return;
        }

        if (transform.position.y < GameManager.FieldBottom)
            GameManager.Instance.BallLost();
    }

    private void FollowPaddle()
    {
        if (_paddle == null) return;
        transform.position = _paddle.transform.position + new Vector3(0f, 0.45f, 0f);
    }

    private void FixedUpdate()
    {
        if (!_attached) _rb.linearVelocity = FixDirection(_rb.linearVelocity);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        var paddle = collision.collider.GetComponent<Paddle>();
        if (paddle != null)
        {
            // -1 — левый край ракетки, 0 — центр, +1 — правый край
            float offset = (transform.position.x - paddle.transform.position.x) / (paddle.Width / 2f);
            offset = Mathf.Clamp(offset, -1f, 1f);

            float angle = offset * MaxBounceAngle * Mathf.Deg2Rad;
            _rb.linearVelocity = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * Speed;
            return;
        }

        var brick = collision.collider.GetComponent<Brick>();
        if (brick != null) brick.Hit();

        _rb.linearVelocity = FixDirection(_rb.linearVelocity);
    }

    /// <summary>Держит скорость постоянной и не даёт мячу лететь почти горизонтально.</summary>
    private Vector2 FixDirection(Vector2 v)
    {
        if (v.sqrMagnitude < 0.0001f) v = Vector2.up;
        v = v.normalized;

        if (Mathf.Abs(v.y) < MinVerticalShare)
        {
            float sign = v.y >= 0f ? 1f : -1f;
            v.y = MinVerticalShare * sign;
            v = v.normalized;
        }
        return v * Speed;
    }
}
