using UnityEngine;

namespace XFrameworkUnity
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        protected static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                    // 如果场景中没有找到实例，则创建一个
                    if (_instance == null)
                    {
                        var obj = new GameObject(name: typeof(T).Name);
                        _instance = obj.AddComponent<T>();
                    }
                }
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            // 确保实例唯一
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this as T;
        }

        protected virtual void OnApplicationQuit()
        {
            _instance = null;
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 持久化单例，在场景切换时，实例不会被销毁
    /// </summary>
    public class MonoSingletonPersistent<T> : MonoSingleton<T> where T : MonoBehaviour
    {
        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
        }
    }
}
