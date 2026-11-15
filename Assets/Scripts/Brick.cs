using UnityEngine;

/// <summary>
/// Кирпич. У крепких кирпичей несколько «жизней»: с каждым ударом они меняют цвет.
/// </summary>
public class Brick : MonoBehaviour
{
    public int HitPoints = 1;
    public int Points = 10;

    private SpriteRenderer _sr;
    private Color _baseColor;

    public void Init(int hitPoints, Color baseColor)
    {
        HitPoints = hitPoints;
        Points = 10 * hitPoints;
        _baseColor = baseColor;
        _sr = GetComponent<SpriteRenderer>();
        UpdateColor();
    }

    public void Hit()
    {
        HitPoints--;
        if (HitPoints <= 0)
        {
            GameManager.Instance.BrickDestroyed(this);
            Destroy(gameObject);
        }
        else
        {
            UpdateColor();
        }
    }

    private void UpdateColor()
    {
        if (_sr == null) return;
        _sr.color = HitPoints switch
        {
            1 => _baseColor,
            2 => new Color(0.75f, 0.78f, 0.82f), // серебряный
            _ => new Color(0.95f, 0.78f, 0.25f), // золотой
        };
    }
}
