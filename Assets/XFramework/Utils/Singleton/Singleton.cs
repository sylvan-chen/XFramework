namespace XGame.Core
{
    /// <summary>
    /// 普通C#类的单例基类 (线程安全实现)
    /// </summary>
    /// <typeparam name="T">继承此基类的具体类型</typeparam>
    public abstract class Singleton<T> where T : class, new()
    {
        private static T _instance;
        private static readonly object _lock = new();

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new T();
                    }
                }
                return _instance;
            }
        }
    }
}