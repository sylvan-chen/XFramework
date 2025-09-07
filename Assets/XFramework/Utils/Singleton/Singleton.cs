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

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_instance != null) return _instance;

                lock (_lock)
                {
                    if (_instance != null) return _instance;

                    _instance = new T();
                    (_instance as Singleton<T>)?.OnInit();
                    return _instance;
                }
            }
        }

        /// <summary>
        /// 销毁单例实例
        /// </summary>
        public static void DestroyInstance()
        {
            if (_instance == null) return;

            lock (_lock)
            {
                if (_instance == null) return;

                (_instance as Singleton<T>)?.OnDestroy();
                _instance = null;
            }
        }

        /// <summary>
        /// 单例初始化时调用
        /// 子类可重写此方法进行初始化操作
        /// </summary>
        protected virtual void OnInit()
        {
        }

        /// <summary>
        /// 单例销毁时调用
        /// 子类可重写此方法进行清理操作
        /// </summary>
        protected virtual void OnDestroy()
        {
        }

        /// <summary>
        /// 防止外部实例化
        /// </summary>
        protected Singleton()
        {
        }
    }
}