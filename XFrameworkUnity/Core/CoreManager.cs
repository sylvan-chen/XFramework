using UnityEngine;
using UnityEngine.SceneManagement;

namespace XFramework.Unity
{
    public class CoreManager : MonoBehaviour, ICoreManager
    {
        private void Awake()
        {
            Global.RegisterManager<ICoreManager>(this);
        }

        private void OnApplicationQuit()
        {
            ShutdownFramework();
        }

        public void QuitGame()
        {
            XLog.Info("[XFramework.Unity] [CoreManager] Quit game...");
            ShutdownFramework();
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        public void RestartGame()
        {
            ShutdownFramework();
            XLog.Info("[XFramework.Unity] [CoreManager] Restarting game...");
            SceneManager.LoadScene(0);
        }

        public void BootFramework()
        {
            XLog.Info("[XFramework.Unity] [CoreManager] Boot XFramework...");
            LoadDrivers();
        }

        public void ShutdownFramework()
        {
            XLog.Info("[XFramework.Unity] [CoreManager] Shutdown XFramework...");
            Destroy(gameObject);
        }

        /// <summary>
        /// 加载驱动
        /// </summary>
        private void LoadDrivers()
        {
            XLog.Info("[XFramework.Unity] [CoreManager] [BootFramework] Load drivers...");
            var logDriver = new LogDriver();
            XLog.RegisterDriver(logDriver);
        }
    }
}