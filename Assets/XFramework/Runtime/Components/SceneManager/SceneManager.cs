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

        /// <summary>
        /// 加载场景，会卸载所有已加载的其他场景
        /// </summary>
        /// <param name="sceneName">需要加载的场景名称</param>
        public async UniTask LoadSceneAsync(string sceneName)
        {
            SceneHandle handle = await Global.AssetManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            ReleaseAllHandles();
            _sceneHandleDict.Add(sceneName, handle);
        }

        /// <summary>
        /// 加载附加场景，不会卸载其他已加载的场景
        /// </summary>
        /// <param name="sceneName">需要加载的附加场景名称</param>
        public async UniTask LoadAdditiveSceneAsync(string sceneName)
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

        /// <summary>
        /// 卸载指定场景
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        public async UniTask UnloadSceneAsync(string sceneName)
        {
            if (!_sceneHandleDict.ContainsKey(sceneName))
            {
                Log.Warning($"[XFramework] [SceneManager] Cannot unload scene ({sceneName}) because scene is not loaded.");
                return;
            }

            SceneHandle handle = _sceneHandleDict[sceneName];
            // handle.UnloadAsync() 会自动释放 SceneHandle，不需要手动调用 Release()
            var operation = handle.UnloadAsync();
            await operation.Task.AsUniTask();
            if (operation.Status == EOperationStatus.Succeed)
            {
                _sceneHandleDict.Remove(sceneName);
            }
            else if (operation.Status == EOperationStatus.Processing)
            {
                Log.Warning($"[XFramework] [SceneManager] Unload scene ({sceneName}) is not completed yet.");
            }
            else if (operation.Status == EOperationStatus.Failed)
            {
                Log.Error($"[XFramework] [SceneManager] Unload scene ({sceneName}) failed: {operation.Error}");
            }
        }

        /// <summary>
        /// 卸载所有场景，保留指定的场景
        /// </summary>
        /// <param name="exceptSceneName">需要保留的场景</param>
        public async UniTask UnloadAllScenesAsync(string exceptSceneName)
        {
            if (exceptSceneName == null)
            {
                Log.Error($"[XFramework] [SceneManager] You must remain at least one scene!");
                return;
            }
            if (!_sceneHandleDict.ContainsKey(exceptSceneName))
            {
                Log.Error($"[XFramework] [SceneManager] Cannot unload all scenes except ({exceptSceneName}) because scene is not loaded.");
                return;
            }
            foreach (var pair in _sceneHandleDict)
            {
                string sceneName = pair.Key;
                SceneHandle handle = pair.Value;
                if (sceneName == exceptSceneName)
                {
                    continue;
                }
                var operation = handle.UnloadAsync();
                await operation.Task.AsUniTask();
                if (operation.Status == EOperationStatus.Succeed)
                {
                    _sceneHandleDict.Remove(sceneName);
                }
                else if (operation.Status == EOperationStatus.Processing)
                {
                    Log.Warning($"[XFramework] [SceneManager] Unload scene ({sceneName}) is not completed yet.");
                }
                else if (operation.Status == EOperationStatus.Failed)
                {
                    Log.Error($"[XFramework] [SceneManager] Unload scene ({sceneName}) failed: {operation.Error}");
                }
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