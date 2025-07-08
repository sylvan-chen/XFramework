using YooAsset;
using UnityEngine;
using System.Collections;
using XFramework.Utils;
using System;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace XFramework
{
    /// <summary>
    /// 资源加载管理器，依赖于 YooAsset
    /// </summary>
    /// <remarks>目前只支持单个默认资源包，后续可以扩展支持多个资源包</remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/Asset Manager")]
    public sealed class AssetManager : XFrameworkComponent
    {
        internal enum BuildMode
        {
            Editor,  // 编辑器模式，编辑器下模拟运行游戏，只在编辑器下有效
            Offline, // 单机运行模式，不需要热更新资源的游戏
            Online,  // 联机模式，需要热更新资源的游戏
            WebGL,   // 针对 WebGL 的特殊模式
        }

        [Header("资源构建模式")]
        [SerializeField]
        BuildMode _buildMode;

        [Header("主要 Package 名称")]
        [SerializeField]
        private string _mainPackageName = "DefaultPackage";

        [Header("资源下载配置")]
        [SerializeField]
        private string _defaultHostServer = "http://<Server>/CDN/<Platform>/<Version>";

        [SerializeField]
        private string _fallbackHostServer = "http://<Server>/CDN/<Platform>/<Version>";

        [SerializeField]
        private int _maxConcurrentDownloadCount = 10;

        [SerializeField]
        private int _failedDownloadRetryCount = 3;

        private ResourcePackage _package;
        private string _packageVersion;

        // 资源包初始化回调
        public event Action OnInitializeFinishedEvent;
        public event Action OnInitializeSucceedEvent;
        public event Action OnInitializeFailedEvent;

        // 资源下载回调
        private Action<DownloaderFinishData> _onDownloadFinishedEvent;
        private Action<DownloadErrorData> _onDownloadErrorEvent;
        private Action<DownloadUpdateData> _onDownloadUpdateEvent;
        private Action<DownloadFileData> _onDownloadFileBeginEvent;

        public delegate void ProgressCallBack(float progress);

        internal override int Priority
        {
            get => XFrameworkConstant.ComponentPriority.AssetManager;
        }

        internal override void Init()
        {
            base.Init();

            YooAssets.Initialize();
            // 获取资源包对象，如果资源包不存在，则创建资源包
            // 注意：需要先在 Collector 创建同名 Package
            _package = YooAssets.TryGetPackage(_mainPackageName);
            if (_package == null)
            {
                _package = YooAssets.CreatePackage(_mainPackageName);
            }
            // 设置默认资源包，之后可以直接使用 YooAssets.XXX 接口来加载该资源包内容
            YooAssets.SetDefaultPackage(_package);
        }

        internal override void Clear()
        {
            base.Clear();

            ClearAssetHandleCache();
            ClearSceneHandleCache();

            _package = null;
            _packageVersion = null;

            OnInitializeFinishedEvent = null;
            OnInitializeFailedEvent = null;
            OnInitializeSucceedEvent = null;
            _onDownloadFinishedEvent = null;
            _onDownloadErrorEvent = null;
            _onDownloadUpdateEvent = null;
            _onDownloadFileBeginEvent = null;
        }

        #region 资源包初始化和销毁

        public void InitPackageAsync()
        {
            // 初始化资源包
            StartCoroutine(InitPackageInternal());
        }

        /// <summary>
        /// 初始化资源包
        /// </summary>
        private IEnumerator InitPackageInternal()
        {
            InitializationOperation operation = null;
            switch (_buildMode)
            {
                case BuildMode.Editor:
                    var simulateBuildResult = EditorSimulateModeHelper.SimulateBuild(_mainPackageName);
                    var initParametersEditor = new EditorSimulateModeParameters()
                    {
                        EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(simulateBuildResult.PackageRootDirectory)
                    };
                    operation = _package.InitializeAsync(initParametersEditor);
                    break;
                case BuildMode.Offline:
                    var initParametersOffline = new OfflinePlayModeParameters
                    {
                        BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters()
                    };
                    operation = _package.InitializeAsync(initParametersOffline);
                    break;
                case BuildMode.Online:
                    IRemoteServices remoteServicesOnline = new RemoteServices(_defaultHostServer, _fallbackHostServer);
                    var initParametersOnline = new HostPlayModeParameters
                    {
                        BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters(),
                        CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServicesOnline)
                    };
                    operation = _package.InitializeAsync(initParametersOnline);
                    break;
                case BuildMode.WebGL:
                    IRemoteServices remoteServicesWebGL = new RemoteServices(_defaultHostServer, _fallbackHostServer);
                    var initParametersWebGL = new WebPlayModeParameters
                    {
                        WebServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters(),
                        WebRemoteFileSystemParameters = FileSystemParameters.CreateDefaultWebRemoteFileSystemParameters(remoteServicesWebGL)
                    };
                    operation = _package.InitializeAsync(initParametersWebGL);
                    break;
                default:
                    Log.Error($"[XFramework] [AssetManager] Invalid package mode: {_buildMode}");
                    break;
            }
            yield return operation;

            if (operation.Status == EOperationStatus.Succeed)
            {
                Log.Debug($"[XFramework] [AssetManager] Initialize package succeed. ({_buildMode})");
                StartCoroutine(RequestPackageVersion());
            }
            else
            {
                Log.Error($"[XFramework] [AssetManager] Initialize package failed. ({_buildMode}) {operation.Error}");
            }
        }

        /// <summary>
        /// 销毁资源包
        /// </summary>
        private IEnumerator DestroyPackageInternal()
        {
            if (_package == null)
            {
                yield break;
            }
            string packageName = _package.PackageName;
            DestroyOperation destroyOperation = _package.DestroyAsync();
            yield return destroyOperation;

            if (YooAssets.RemovePackage(_package))
            {
                Log.Debug($"[XFramework] [AssetManager] Destroy package ({packageName}) succeed.");
            }
        }

        #endregion

        #region 资源更新

        /// <summary>
        /// 获取资源版本
        /// </summary>
        private IEnumerator RequestPackageVersion()
        {
            var operation = _package.RequestPackageVersionAsync();
            yield return operation;

            if (operation.Status == EOperationStatus.Succeed)
            {
                _packageVersion = operation.PackageVersion;
                Log.Debug($"[XFramework] [AssetManager] Request package version succeed. {_packageVersion}");
                StartCoroutine(UpdatePackageManifest());
            }
            else
            {
                Log.Error($"[XFramework] [AssetManager] Request package version failed. {operation.Error}");
            }
        }

        /// <summary>
        /// 根据版本号更新资源清单
        /// </summary>
        private IEnumerator UpdatePackageManifest()
        {
            var operation = _package.UpdatePackageManifestAsync(_packageVersion);
            yield return operation;

            if (operation.Status == EOperationStatus.Succeed)
            {
                Log.Debug($"[XFramework] [AssetManager] Update package manifest succeed. Latest version: {_packageVersion}");
                StartCoroutine(UpdatePackageFiles());
            }
            else
            {
                Log.Error($"[XFramework] [AssetManager] Update package manifest failed. (Latest version: {_packageVersion}) {operation.Error}");
            }
        }

        /// <summary>
        /// 根据资源清单更新资源文件（下载到缓存资源）
        /// </summary>
        private IEnumerator UpdatePackageFiles()
        {
            var downloader = _package.CreateResourceDownloader(_maxConcurrentDownloadCount, _failedDownloadRetryCount);

            if (downloader.TotalDownloadCount == 0)
            {
                Log.Debug("[XFramework] [AssetManager] No package files need to update.");
                OnInitializeFinishedEvent?.Invoke();
                OnInitializeSucceedEvent?.Invoke();
                yield break;
            }

            int totalDownloadCount = downloader.TotalDownloadCount;
            long totalDownloadBytes = downloader.TotalDownloadBytes;

            downloader.DownloadFinishCallback = (finishData) =>
            {
                _onDownloadFinishedEvent?.Invoke(finishData);
            };
            downloader.DownloadErrorCallback = (errorData) =>
            {
                _onDownloadErrorEvent?.Invoke(errorData);
            };
            downloader.DownloadUpdateCallback = (updateData) =>
            {
                _onDownloadUpdateEvent?.Invoke(updateData);
            };
            downloader.DownloadFileBeginCallback = (fileData) =>
            {
                _onDownloadFileBeginEvent?.Invoke(fileData);
            };

            downloader.BeginDownload();
            yield return downloader;

            if (downloader.Status == EOperationStatus.Succeed)
            {
                Log.Debug($"[XFramework] [AssetManager] Update package files succeed. Total download count: {totalDownloadCount}, Total download bytes: {totalDownloadBytes}");
                OnInitializeFinishedEvent?.Invoke();
                OnInitializeSucceedEvent?.Invoke();
            }
            else
            {
                Log.Error($"[XFramework] [AssetManager] Update package files failed. {downloader.Error}");
                OnInitializeFinishedEvent?.Invoke();
                OnInitializeFailedEvent?.Invoke();
            }
        }

        #endregion

        #region 资源缓存清理

        /// <summary>
        /// 清理所有缓存资源文件
        /// </summary>
        private IEnumerator ClearAllCacheBundleFiles()
        {
            var operation = _package.ClearCacheFilesAsync(EFileClearMode.ClearAllBundleFiles);
            yield return operation;

            if (operation.Status == EOperationStatus.Succeed)
            {
                Log.Debug($"[XFramework] [AssetManager] Clear all cache bundle files succeed.");
            }
            else
            {
                Log.Error($"[XFramework] [AssetManager] Clear all cache bundle files failed. {operation.Error}");
            }
        }

        /// <summary>
        /// 清理未使用的缓存资源文件
        /// </summary>
        private IEnumerator ClearUnusedCacheBundleFiles()
        {
            var operation = _package.ClearCacheFilesAsync(EFileClearMode.ClearUnusedBundleFiles);
            yield return operation;

            if (operation.Status == EOperationStatus.Succeed)
            {
                Log.Debug($"[XFramework] [AssetManager] Clear unused cache bundle files succeed.");
            }
            else
            {
                Log.Error($"[XFramework] [AssetManager] Clear unused cache bundle files failed. {operation.Error}");
            }
        }

        /// <summary>
        /// 清理所有缓存清单文件
        /// </summary>
        private IEnumerator ClearAllCacheManifestFiles()
        {
            var operation = _package.ClearCacheFilesAsync(EFileClearMode.ClearAllManifestFiles);
            yield return operation;

            if (operation.Status == EOperationStatus.Succeed)
            {
                Log.Debug($"[XFramework] [AssetManager] Clear all cache manifest files succeed.");
            }
            else
            {
                Log.Error($"[XFramework] [AssetManager] Clear all cache manifest files failed. {operation.Error}");
            }
        }

        /// <summary>
        /// 清理未使用的缓存清单文件
        /// </summary>
        private IEnumerator ClearUnusedCacheManifestFiles()
        {
            var operation = _package.ClearCacheFilesAsync(EFileClearMode.ClearUnusedManifestFiles);
            yield return operation;

            if (operation.Status == EOperationStatus.Succeed)
            {
                Log.Debug($"[XFramework] [AssetManager] Clear unused cache manifest files succeed.");
            }
            else
            {
                Log.Error($"[XFramework] [AssetManager] Clear unused cache manifest files failed. {operation.Error}");
            }
        }

        #endregion


        #region 资源卸载

        /// <summary>
        /// 尝试卸载指定资源
        /// </summary>
        /// <remarks>
        /// 如果该资源还在被使用（存在句柄引用），该方法会无效
        /// </remarks>
        public void TryUnloadUnusedAsset(string address)
        {
            _package.TryUnloadUnusedAsset(address);
        }

        /// <summary>
        /// 卸载所有引用句柄为零的资源
        /// </summary>
        /// <remarks>
        /// 可以在切换场景之后调用资源释放方法或者写定时器间隔时间去释放
        /// </remarks>
        public async UniTask UnloadUnusedAssetsAsync()
        {
            var operation = _package.UnloadUnusedAssetsAsync();
            await operation.Task.AsUniTask();
        }

        /// <summary>
        /// 强制卸载所有资源
        /// </summary>
        /// <remarks>
        /// Package 在销毁的时候也会自动调用该方法
        /// </remarks>
        public async UniTask ForceUnloadAllAssetsAsync()
        {
            var operation = _package.UnloadAllAssetsAsync();
            await operation.Task.AsUniTask();
        }

        #endregion

        #region 资源加载（无缓存策略）

        /// <summary>
        /// 加载资源（无缓存策略）
        /// </summary>
        /// <remarks>
        /// 每次都返回新的句柄，调用者必须自己Release
        /// </remarks>
        public async UniTask<AssetHandle> LoadAssetNoCacheAsync<T>(string address) where T : UnityEngine.Object
        {
            AssetHandle handle = _package.LoadAssetAsync<T>(address);
            await handle.Task.AsUniTask();

            if (handle.Status == EOperationStatus.Succeed)
            {
                Log.Debug($"[XFramework] [AssetManager] Load asset ({address}) succeed - direct handle.");
                return handle;
            }
            else
            {
                Log.Error($"[XFramework] [AssetManager] Failed to load asset: {address}. Error: {handle.LastError}");
                handle.Release(); // 释放失败的句柄
                return null;
            }
        }

        #endregion

        #region 资源加载（引用计数策略）

        private readonly Dictionary<string, int> _handleRefCount = new();
        private readonly Dictionary<string, AssetHandle> _assetHandleCache = new();

        /// <summary>
        /// 获取资源
        /// </summary>
        /// <remarks>
        /// AssetManager 负责引用计数管理，调用者使用完毕后调用 ReleaseAsset
        /// </remarks>
        public async UniTask<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
        {
            // 增加引用计数
            if (!_handleRefCount.ContainsKey(address))
            {
                _handleRefCount[address] = 0;
            }
            _handleRefCount[address]++;

            // 检查是否已经加载
            if (_assetHandleCache.TryGetValue(address, out AssetHandle cachedHandle))
            {
                Log.Debug($"[XFramework] [AssetManager] Reuse cached asset ({address}), ref count: {_handleRefCount[address]}");
                return cachedHandle.AssetObject as T;
            }

            // 首次加载
            AssetHandle handle = _package.LoadAssetAsync<T>(address);
            await handle.Task.AsUniTask();

            if (handle.Status == EOperationStatus.Succeed)
            {
                _assetHandleCache[address] = handle;
                Log.Debug($"[XFramework] [AssetManager] Load asset ({address}) succeed, ref count: {_handleRefCount[address]}");
                return handle.AssetObject as T;
            }
            else
            {
                // 加载失败，回退引用计数
                _handleRefCount[address]--;
                if (_handleRefCount[address] <= 0)
                {
                    _handleRefCount.Remove(address);
                }

                Log.Error($"[XFramework] [AssetManager] Failed to load asset: {address}. Error: {handle.LastError}");
                handle.Release();
                return null;
            }
        }

        /// <summary>
        /// 释放资源句柄
        /// </summary>
        /// <remarks>
        /// 与 LoadAssetAsync 配对使用
        /// </remarks>
        public void ReleaseAsset(string address)
        {
            if (!_handleRefCount.ContainsKey(address))
            {
                Log.Warning($"[XFramework] [AssetManager] Try to release asset ({address}) that was never loaded or with no cache.");
                return;
            }

            _handleRefCount[address]--;
            Log.Debug($"[XFramework] [AssetManager] Release asset ({address}), ref count: {_handleRefCount[address]}");

            // 引用计数归零时真正释放
            if (_handleRefCount[address] <= 0)
            {
                if (_assetHandleCache.TryGetValue(address, out AssetHandle handle))
                {
                    handle.Release();
                    _assetHandleCache.Remove(address);
                    Log.Debug($"[XFramework] [AssetManager] Actually released asset ({address})");
                }
                _handleRefCount.Remove(address);
            }
        }

        private void ClearAssetHandleCache()
        {
            foreach (var handle in _assetHandleCache.Values)
            {
                handle?.Release();
            }
            _assetHandleCache.Clear();
            _handleRefCount.Clear();
        }

        #endregion

        #region 场景资源管理

        private readonly Dictionary<string, SceneHandle> _sceneHandleCache = new();

        /// <summary>
        /// 加载场景
        /// </summary>
        internal async UniTask<SceneHandle> LoadSceneAsync(string address, LoadSceneMode mode = LoadSceneMode.Single)
        {
            SceneHandle handle = _package.LoadSceneAsync(address, mode);
            await handle.Task.AsUniTask();
            Log.Debug($"[XFramework] [AssetManager] Load scene ({handle.SceneName}) succeed.");
            return handle;
        }

        /// <summary>
        /// 加载场景
        /// </summary>
        /// <param name="address">场景地址</param>
        /// <param name="sceneMode">场景加载模式</param>
        /// <param name="physicsMode">物理模式</param>
        /// <param name="progressCallback">进度回调</param>
        /// <param name="suspendLoad">是否暂停加载</param>
        /// <param name="priority">加载优先级</param>
        /// <returns>加载是否成功</returns>
        public async UniTask<bool> LoadSceneAsync(string address, LoadSceneMode sceneMode = LoadSceneMode.Single,
            LocalPhysicsMode physicsMode = LocalPhysicsMode.None, ProgressCallBack progressCallback = null,
            bool suspendLoad = false, uint priority = 0)
        {
            SceneHandle handle;
            // 检查是否已在加载中
            if (_sceneHandleCache.ContainsKey(address))
            {
                Log.Warning($"[XFramework] [AssetManager] Scene ({address}) is already loaded or loading.");
                handle = _sceneHandleCache[address];
            }
            else
            {
                handle = _package.LoadSceneAsync(address, sceneMode, physicsMode, suspendLoad, priority);
                _sceneHandleCache[address] = handle;

                // 监听进度
                if (progressCallback != null)
                {
                    while (!handle.IsDone)
                    {
                        progressCallback?.Invoke(handle.Progress);
                        await UniTask.Delay(16); // 约 60 FPS 的更新频率
                    }
                    progressCallback?.Invoke(1.0f);
                }
            }

            await handle.Task.AsUniTask();

            if (handle.Status == EOperationStatus.Succeed)
            {
                Log.Debug($"[XFramework] [AssetManager] Load scene ({handle.SceneName}) succeed.");
                return true;
            }
            else
            {
                Log.Error($"[XFramework] [AssetManager] Load scene ({address}) failed: {handle.LastError}");
                _sceneHandleCache.Remove(address);
                return false;
            }
        }

        /// <summary>
        /// 预加载场景（不激活）
        /// </summary>
        /// <param name="address">场景地址</param>
        /// <param name="physicsMode">物理模式</param>
        /// <param name="progressCallback">进度回调</param>
        /// <param name="priority">加载优先级</param>
        /// <returns>预加载是否成功</returns>
        public async UniTask<bool> PreloadSceneAsync(string address, LocalPhysicsMode physicsMode = LocalPhysicsMode.None,
            ProgressCallBack progressCallback = null, uint priority = 0)
        {
            return await LoadSceneAsync(address, LoadSceneMode.Additive, physicsMode, progressCallback, true, priority);
        }

        /// <summary>
        /// 激活预加载的场景
        /// </summary>
        /// <param name="address">场景地址</param>
        /// <returns>是否激活成功</returns>
        public bool ActivatePreloadedScene(string address)
        {
            if (_sceneHandleCache.TryGetValue(address, out SceneHandle handle))
            {
                handle.ActivateScene();
                Log.Debug($"[XFramework] [AssetManager] Activate preloaded scene ({address}).");
                return true;
            }
            else
            {
                Log.Error($"[XFramework] [AssetManager] Scene ({address}) is not preloaded.");
                return false;
            }
        }

        /// <summary>
        /// 卸载场景
        /// </summary>
        /// <param name="address">场景地址</param>
        /// <returns>卸载是否成功</returns>
        public async UniTask<bool> UnloadSceneAsync(string address)
        {
            if (_sceneHandleCache.TryGetValue(address, out SceneHandle handle))
            {
                await handle.UnloadAsync().Task.AsUniTask();
                _sceneHandleCache.Remove(address);
                Log.Debug($"[XFramework] [AssetManager] Unload scene ({address}) succeed.");
                return true;
            }
            else
            {
                Log.Warning($"[XFramework] [AssetManager] Try to unload scene ({address}) that is not loaded.");
                return false;
            }
        }

        private void ClearSceneHandleCache()
        {
            foreach (var handle in _sceneHandleCache.Values)
            {
                handle?.Release();
            }
            _sceneHandleCache.Clear();
        }

        #endregion

        public class RemoteServices : IRemoteServices
        {
            public RemoteServices(string defaultHostServer, string fallbackHostServer)
            {
                DefaultHostServer = defaultHostServer;
                FallbackHostServer = fallbackHostServer;
            }

            public string DefaultHostServer { get; private set; }
            public string FallbackHostServer { get; private set; }

            public string GetRemoteFallbackURL(string fileName)
            {
                return $"{FallbackHostServer}/{fileName}";
            }

            public string GetRemoteMainURL(string fileName)
            {
                return $"{DefaultHostServer}/{fileName}";
            }
        }
    }
}