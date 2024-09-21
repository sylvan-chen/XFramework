using UnityEngine;

namespace XFramework
{
    public class GameController : MonoSingletonPersistent<GameController>
    {
        protected override void Awake()
        {
            base.Awake();
            if (gameObject == null)
            {
                return;
            }
            BootGame();
        }

        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            XLog.Info("[XFramework.Unity] [GameController] Force quit game!");
            ShutdownFramework();
        }

        /// <summary>
        /// 关闭游戏
        /// </summary>
        public void ShutdownGame()
        {
            XLog.Info("[XFramework.Unity] [GameController] Quit game...");
            ShutdownFramework();
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void BootGame()
        {
            XLog.Info("[XFramework.Unity] [GameController] Boot game...");
        }

        private void ShutdownFramework()
        {
            XLog.Info("[XFramework.Unity] [GameController] Shutdown XFramework...");
            Destroy(gameObject);
        }
    }
}