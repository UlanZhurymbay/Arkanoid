using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Ракетка игрока. Управление: стрелки / A-D или мышь.
/// Двигается через кинематический Rigidbody2D, чтобы мяч корректно от неё отскакивал.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Paddle : MonoBehaviour
{
    public float Speed = 14f;
    public float Width = 2.2f;

    private Rigidbody2D _rb;
    private float _keyInput;
    private bool _useMouse;
    private float _mouseX;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        _keyInput = 0f;

        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) _keyInput -= 1f;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) _keyInput += 1f;
        }

        var mouse = Mouse.current;
        if (mouse != null && Camera.main != null)
        {
            // Мышь сдвинулась — управляем мышью; нажали клавишу — переключаемся на клавиатуру
            if (mouse.delta.ReadValue().sqrMagnitude > 0.01f) _useMouse = true;
            Vector3 world = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());
            _mouseX = world.x;
        }
        if (_keyInput != 0f) _useMouse = false;
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance == null || !GameManager.Instance.PaddleCanMove) return;

        float x = _rb.position.x;
        float newX = _useMouse
            ? Mathf.MoveTowards(x, _mouseX, Speed * 1.5f * Time.fixedDeltaTime)
            : x + _keyInput * Speed * Time.fixedDeltaTime;

        float limit = GameManager.FieldHalfWidth - Width / 2f;
        newX = Mathf.Clamp(newX, -limit, limit);

        _rb.MovePosition(new Vector2(newX, _rb.position.y));
    }

    public void ResetPosition()
    {
        var pos = new Vector2(0f, transform.position.y);
        _rb.position = pos;
        transform.position = pos;
    }
}
