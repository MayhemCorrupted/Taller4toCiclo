using UnityEngine;
public static class GameLogger
{
    public static void LogInfo(string message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Rotbound][INFO] {message}");
#endif
    }
    public static void LogWarning(string message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogWarning($"[Rotbound][WARN] {message}");
#endif
    }
    public static void LogError(string message)
    {
        Debug.LogError($"[Rotbound][ERROR] {message}");
    }
}