using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using XFramework.Utils;
using YooAsset;

namespace XFramework
{
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/Scene Manager")]
    public class SceneManager : XFrameworkComponent
    {
        private readonly Dictionary<string, SceneHandle> _sceneHandleDict = new();

        internal override int Priority => XFrameworkConstant.ComponentPriority.SceneManager;

        internal override void Init()
        {
            base.Init();

            ReleaseAllHandles();
        }

        internal override void Clear()
        {
            base.Clear();

            ReleaseAllHandles();
        }

        public async UniTask ChangeSceneAsync(string sceneName)
        {
            await LoadSceneAsync(sceneName);
        }

        public async UniTask AddSceneAsync(string sceneName)
        {
            await LoadAdditiveSceneAsync(sceneName);
        }

        public async UniTask RemoveSceneAsync(string sceneName)
        {
            await UnloadSceneAsync(sceneName);
        }

        public async UniTask RemoveAllScenesAsync(string exceptSceneName)
        {
            await UnloadAllScenesAsync(exceptSceneName);
        }

        private async UniTask LoadSceneAsync(string sceneName)
        {
            SceneHandle handle = await Global.AssetManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            ReleaseAllHandles();
            _sceneHandleDict.Add(sceneName, handle);
        }

        private async UniTask LoadAdditiveSceneAsync(string sceneName)
        {
            SceneHandle handle = await Global.AssetManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (_sceneHandleDict.ContainsKey(sceneName))
            {
                _sceneHandleDict[sceneName].Release();
                _sceneHandleDict[sceneName] = handle;
            }
            else
            {
                _sceneHandleDict.Add(sceneName, handle);
            }
        }

        private async UniTask UnloadSceneAsync(string sceneName)
        {
            if (!_sceneHandleDict.ContainsKey(sceneName))
            {
                Log.Warning($"[XFramework] [SceneManager] Cannot unload scene ({sceneName}) because scene is not loaded.");
                return;
            }

            SceneHandle handle = _sceneHandleDict[sceneName];
            await handle.UnloadAsync().Task.AsUniTask();
            handle.Release();
            _sceneHandleDict.Remove(sceneName);
        }

        private async UniTask UnloadAllScenesAsync(string exceptSceneName)
        {
            if (exceptSceneName == null || !_sceneHandleDict.ContainsKey(exceptSceneName))
            {
                Log.Error($"[XFramework] [SceneManager] You must remain at least one scene!");
                return;
            }
            SceneHandle exceptSceneHandle = null;
            foreach (var pair in _sceneHandleDict)
            {
                string sceneName = pair.Key;
                SceneHandle handle = pair.Value;
                if (sceneName == exceptSceneName)
                {
                    exceptSceneHandle = handle;
                    continue;
                }
                await handle.UnloadAsync().Task.AsUniTask();
                handle.Release();
            }
            _sceneHandleDict.Clear();
            if (exceptSceneHandle != null)
            {
                _sceneHandleDict.Add(exceptSceneName, exceptSceneHandle);
            }
        }

        private void ReleaseAllHandles()
        {
            foreach (SceneHandle handle in _sceneHandleDict.Values)
            {
                handle.Release();
            }
            _sceneHandleDict.Clear();
        }
    }
}