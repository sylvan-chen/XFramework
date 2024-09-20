using UnityEngine;

namespace XFramework.Unity
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
            ShutdownFramework();
        }

        private void BootGame()
        {
            XLog.Info("[XFramework.Unity] [GameController] Boot game...");
        }

        public void ShutdownGame()
        {
            XLog.Info("[XFramework.Unity] [GameController] Quit game...");
            ShutdownFramework();
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        public void ShutdownFramework()
        {
            XLog.Info("[XFramework.Unity] [GameController] Shutdown XFramework...");
            Destroy(gameObject);
        }
    }
}