using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Главный скрипт игры: строит поле, ракетку, мяч и кирпичи,
/// считает очки и жизни, переключает состояния игры и рисует интерфейс.
/// </summary>
public class GameManager : MonoBehaviour
{
    // Размеры игрового поля в единицах Unity
    public const float FieldHalfWidth = 7f;
    public const float FieldTop = 5.5f;
    public const float FieldBottom = -6.5f;
    private const float PaddleY = -5f;

    private const int StartLives = 3;
    private const string BestKey = "Arkanoid.Best";

    public static GameManager Instance { get; private set; }

    private enum State { Ready, Playing, Paused, LevelComplete, GameOver }

    private State _state = State.Ready;
    private int _score;
    private int _lives = StartLives;
    private int _level = 1;
    private int _bricksLeft;
    private int _best;

    private Paddle _paddle;
    private Ball _ball;
    private Transform _bricksRoot;

    private GUIStyle _hudStyle;
    private GUIStyle _bigStyle;
    private GUIStyle _hintStyle;

    // Цвета рядов кирпичей сверху вниз
    private static readonly Color[] RowColors =
    {
        new Color(0.94f, 0.33f, 0.31f), // красный
        new Color(0.98f, 0.60f, 0.25f), // оранжевый
        new Color(0.98f, 0.85f, 0.30f), // жёлтый
        new Color(0.45f, 0.82f, 0.40f), // зелёный
        new Color(0.30f, 0.75f, 0.85f), // голубой
        new Color(0.36f, 0.52f, 0.95f), // синий
        new Color(0.66f, 0.45f, 0.92f), // фиолетовый
        new Color(0.92f, 0.45f, 0.75f), // розовый
    };

    /// <summary>Может ли ракетка двигаться (до запуска мяча и во время игры).</summary>
    public bool PaddleCanMove => _state == State.Ready || _state == State.Playing;

    private void Awake()
    {
        Instance = this;
        _best = PlayerPrefs.GetInt(BestKey, 0);

        SetupCamera();
        BuildWalls();
        CreatePaddleAndBall();
        BuildLevel();
    }

    // ───────────────────────── Построение сцены ─────────────────────────

    private void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = go.AddComponent<Camera>();
        }
        cam.orthographic = true;
        cam.orthographicSize = 6.6f;
        cam.transform.position = new Vector3(0f, -0.3f, -10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.08f, 0.12f);
    }

    private void BuildWalls()
    {
        var root = new GameObject("Walls").transform;
        var color = new Color(0.35f, 0.38f, 0.45f);
        float height = FieldTop - FieldBottom + 2f;
        float midY = (FieldTop + FieldBottom) / 2f;

        CreateBlock("Left", root, new Vector2(-FieldHalfWidth - 0.25f, midY), new Vector2(0.5f, height), color);
        CreateBlock("Right", root, new Vector2(FieldHalfWidth + 0.25f, midY), new Vector2(0.5f, height), color);
        CreateBlock("Top", root, new Vector2(0f, FieldTop + 0.25f), new Vector2(FieldHalfWidth * 2f + 1f, 0.5f), color);
    }

    private static GameObject CreateBlock(string name, Transform parent, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteFactory.AddRenderer(go, SpriteFactory.Square, color);
        go.AddComponent<BoxCollider2D>();
        return go;
    }

    private void CreatePaddleAndBall()
    {
        // Ракетка
        var paddleGo = new GameObject("Paddle");
        paddleGo.transform.position = new Vector3(0f, PaddleY, 0f);
        SpriteFactory.AddRenderer(paddleGo, SpriteFactory.Square, new Color(0.9f, 0.92f, 0.96f), 1);

        var paddleRb = paddleGo.AddComponent<Rigidbody2D>();
        paddleRb.bodyType = RigidbodyType2D.Kinematic;
        paddleRb.interpolation = RigidbodyInterpolation2D.Interpolate;

        _paddle = paddleGo.AddComponent<Paddle>();
        paddleGo.transform.localScale = new Vector3(_paddle.Width, 0.3f, 1f);
        paddleGo.AddComponent<BoxCollider2D>();

        // Мяч
        var ballGo = new GameObject("Ball");
        ballGo.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
        SpriteFactory.AddRenderer(ballGo, SpriteFactory.Circle, Color.white, 2);

        var ballRb = ballGo.AddComponent<Rigidbody2D>();
        ballRb.gravityScale = 0f;
        ballRb.freezeRotation = true;
        ballRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        ballRb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var ballCollider = ballGo.AddComponent<CircleCollider2D>();
        ballCollider.sharedMaterial = new PhysicsMaterial2D("Bouncy") { bounciness = 1f, friction = 0f };

        _ball = ballGo.AddComponent<Ball>();
        _ball.Init(_paddle);
    }

    private void BuildLevel()
    {
        if (_bricksRoot != null) Destroy(_bricksRoot.gameObject);
        _bricksRoot = new GameObject("Bricks").transform;
        _bricksLeft = 0;

        const int columns = 12;
        const float brickW = 1.0f, brickH = 0.4f, gap = 0.1f;
        int rows = Mathf.Min(3 + _level, 8);

        float startX = -(columns * (brickW + gap) - gap) / 2f + brickW / 2f;
        float startY = FieldTop - 1.2f;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                // На каждом третьем уровне — шахматный узор
                if (_level % 3 == 2 && (row + col) % 2 == 1) continue;

                var pos = new Vector2(startX + col * (brickW + gap), startY - row * (brickH + gap));
                var go = CreateBlock($"Brick {row}-{col}", _bricksRoot, pos, new Vector2(brickW, brickH), Color.white);

                // Чем выше уровень, тем больше крепких кирпичей в верхних рядах
                int hp = 1;
                if (_level >= 2 && row == 0) hp = 2;
                if (_level >= 3 && row == 0) hp = 3;
                if (_level >= 3 && row == 1) hp = 2;

                go.AddComponent<Brick>().Init(hp, RowColors[row % RowColors.Length]);
                _bricksLeft++;
            }
        }

        _ball.Speed = Mathf.Min(7f + _level * 0.7f, 13f);
        ResetBall();
        _state = State.Ready;
    }

    private void ResetBall()
    {
        _paddle.ResetPosition();
        _ball.Attach();
    }

    // ───────────────────────── Игровая логика ─────────────────────────

    private bool _attachPending;

    private void Update()
    {
        if (_attachPending)
        {
            _attachPending = false;
            ResetBall();
        }

        var kb = Keyboard.current;
        var mouse = Mouse.current;
        bool action = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                      || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        bool pause = kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame);

        switch (_state)
        {
            case State.Ready:
                if (action)
                {
                    _ball.Launch();
                    _state = State.Playing;
                }
                else if (pause) SetPaused(true);
                break;

            case State.Playing:
                if (pause) SetPaused(true);
                break;

            case State.Paused:
                if (pause || action) SetPaused(false);
                break;

            case State.LevelComplete:
                if (action)
                {
                    _level++;
                    BuildLevel();
                }
                break;

            case State.GameOver:
                if (action) RestartGame();
                break;
        }
    }

    private State _stateBeforePause;

    private void SetPaused(bool paused)
    {
        if (paused)
        {
            _stateBeforePause = _state;
            _state = State.Paused;
            Time.timeScale = 0f;
        }
        else
        {
            _state = _stateBeforePause;
            Time.timeScale = 1f;
        }
    }

    public void BrickDestroyed(Brick brick)
    {
        _score += brick.Points;
        _bricksLeft--;

        if (_score > _best)
        {
            _best = _score;
            PlayerPrefs.SetInt(BestKey, _best);
        }

        if (_bricksLeft <= 0)
        {
            // Мяч прилепим в Update: внутри обработки столкновения физику лучше не трогать
            _state = State.LevelComplete;
            _attachPending = true;
        }
    }

    public void BallLost()
    {
        if (_state != State.Playing) return;

        _lives--;
        ResetBall();

        if (_lives <= 0)
        {
            _state = State.GameOver;
            PlayerPrefs.Save();
        }
        else
        {
            _state = State.Ready;
        }
    }

    private void RestartGame()
    {
        _score = 0;
        _lives = StartLives;
        _level = 1;
        BuildLevel();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        PlayerPrefs.Save();
    }

    // ───────────────────────── Интерфейс ─────────────────────────

    private void OnGUI()
    {
        if (_hudStyle == null)
        {
            _hudStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            _bigStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _hintStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        }

        int unit = Mathf.Max(12, Screen.height / 32);
        _hudStyle.fontSize = unit;
        _bigStyle.fontSize = unit * 2;
        _hintStyle.fontSize = unit;
        _hudStyle.normal.textColor = Color.white;
        _bigStyle.normal.textColor = Color.white;
        _hintStyle.normal.textColor = new Color(0.8f, 0.85f, 0.9f);

        float pad = unit * 0.6f;
        GUI.Label(new Rect(pad, pad, 400, unit * 1.5f), $"Счёт: {_score}", _hudStyle);
        GUI.Label(new Rect(pad, pad + unit * 1.4f, 400, unit * 1.5f), $"Рекорд: {_best}", _hudStyle);
        GUI.Label(new Rect(Screen.width - unit * 9, pad, unit * 9, unit * 1.5f), $"Жизни: {_lives}", _hudStyle);
        GUI.Label(new Rect(Screen.width - unit * 9, pad + unit * 1.4f, unit * 9, unit * 1.5f), $"Уровень: {_level}", _hudStyle);

        string title = null, hint = null;
        switch (_state)
        {
            case State.Ready:
                hint = "Пробел или клик — запустить мяч\n← → / A D / мышь — двигать ракетку";
                break;
            case State.Paused:
                title = "ПАУЗА";
                hint = "Esc / P — продолжить";
                break;
            case State.LevelComplete:
                title = $"Уровень {_level} пройден!";
                hint = "Пробел — следующий уровень";
                break;
            case State.GameOver:
                title = "ИГРА ОКОНЧЕНА";
                hint = $"Счёт: {_score}\nПробел — играть снова";
                break;
        }

        float cy = Screen.height * 0.55f;
        if (title != null)
            GUI.Label(new Rect(0, cy - unit * 3, Screen.width, unit * 3), title, _bigStyle);
        if (hint != null)
            GUI.Label(new Rect(0, cy, Screen.width, unit * 3), hint, _hintStyle);
    }
}
