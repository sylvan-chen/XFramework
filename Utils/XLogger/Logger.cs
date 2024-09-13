namespace XFramework
{
    public static class XLogger
    {
        public static void Info(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        public static void Warning(string message)
        {
            UnityEngine.Debug.LogWarning(message);
        }

        public static void Error(string message)
        {
            UnityEngine.Debug.LogError(message);
        }
    }
}