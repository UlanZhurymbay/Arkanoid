using UnityEngine;

/// <summary>
/// Запускает игру автоматически при нажатии Play в любой сцене:
/// если на сцене нет GameManager — создаёт его.
/// </summary>
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateGame()
    {
        if (Object.FindAnyObjectByType<GameManager>() != null) return;
        new GameObject("GameManager").AddComponent<GameManager>();
    }
}
