using System;
using UnityEngine;

namespace XFramework.Unity
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
                    _instance = FindObjectOfType<T>() ?? throw new NullReferenceException("MonoSingleton<" + typeof(T).Name + "> is not initialized, please check if there is an instance in the scene.");
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
