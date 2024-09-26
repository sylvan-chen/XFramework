using System;
using System.Collections;
using UnityEngine;
using XFramework.Utils;

namespace XFramework
{
    /// <summary>
    /// 根管理器
    /// </summary>
    /// <remarks>
    /// 管理各个管理器，并提供启动游戏和关闭游戏的方法。
    /// </remarks>
    public class RootManager : MonoSingletonPersistent<RootManager>
    {
        private bool _isBooted = false;

        protected override void Awake()
        {
            base.Awake();
        }

        private IEnumerator Start()
        {
            // 第一帧等待各管理器的初始化，帧末启动游戏
            yield return new WaitForEndOfFrame();
            // 由且仅由唯一单例来启动游戏
            RootManager.Instance.BootGame();
        }

        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            XLog.Info("[XFramework] [GameController] Force quit game!");
            ShutdownFramework();
        }

        /// <summary>
        /// 获取管理器
        /// </summary>
        /// <typeparam name="T">管理器类型（接口）</typeparam>
        /// <returns>要获取的管理器实例</returns>
        /// <exception cref="ArgumentException">T 必须是接口类型</exception>
        /// <exception cref="ArgumentException">找不到管理器</exception>
        public T GetManager<T>() where T : IManager
        {
            if (!typeof(T).IsInterface)
            {
                throw new ArgumentException("GetManager faild. T must be an interface.", nameof(T));
            }
            T manager = GetComponentInChildren<T>() ?? throw new ArgumentException($"Can not find manager: {nameof(T)}", nameof(T));
            return manager;
        }

        /// <summary>
        /// 关闭游戏
        /// </summary>
        public void ShutdownGame()
        {
            XLog.Info("[XFramework] [GameController] Quit game...");
            ShutdownFramework();
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        /// <summary>
        /// 启动游戏
        /// </summary>
        private void BootGame()
        {
            if (_isBooted)
            {
                return;
            }
            _isBooted = true;
            XLog.Info("[XFramework] [GameController] Boot game...");
        }

        private void ShutdownFramework()
        {
            XLog.Info("[XFramework] [GameController] Shutdown XFramework...");
            Destroy(gameObject);
        }
    }
}