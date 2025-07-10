using YooAsset;
using UnityEngine;
using XFramework.Utils;
using System;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

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
        private InitResult _initResult;

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

        #region 资源包初始化

        async public UniTask<InitResult> InitPackageAsync()
        {
            _initResult = new();

            DateTime startTime = DateTime.Now;

            await InitPackageInternal();

            TimeSpan duration = DateTime.Now - startTime;
            _initResult.InitDuration = duration;

            OnInitializeFinishedEvent?.Invoke();
            if (_initResult.Succeed)
            {
                OnInitializeSucceedEvent?.Invoke();
            }
            else
            {
                OnInitializeFailedEvent?.Invoke();
            }

            return _initResult;
        }

        /// <summary>
        /// 销毁资源包
        /// </summary>
        async public UniTask DestroyPackageAsync()
        {
            if (_package == null)
            {
                Log.Warning("[XFramework] [AssetManager] DestroyPackageAsync: Package is null, nothing to destroy.");
                return;
            }

            string packageName = _package.PackageName;
            DestroyOperation operation = _package.DestroyAsync();
            await operation.ToUniTask();

            if (operation.Status == EOperationStatus.Succeed)
            {
                Log.Debug($"[XFramework] [AssetManager] Destroy package ({packageName}) succeed.");
            }
            else
            {
                Log.Error($"[XFramework] [AssetManager] Destroy package ({packageName}) failed. {operation.Error}");
            }

            if (YooAssets.RemovePackage(_package))
            {
                Log.Debug($"[XFramework] [AssetManager] Remove package ({packageName}) from YooAssets succeed.");
            }
            else
            {
                Log.Warning($"[XFramework] [AssetManager] Remove package ({packageName}) from YooAssets failed, it may not exist.");
            }
        }

        /// <summary>
        /// 初始化资源包
        /// </summary>
        async private UniTask InitPackageInternal()
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
            await operation.ToUniTask();

            if (operation.Status == EOperationStatus.Succeed)
            {
                Log.Debug($"[XFramework] [AssetManager] Initialize package succeed. ({_buildMode})");
                await RequestPackageVersion();
            }
            else
            {
                Log.Error($"[XFramework] [AssetManager] Initialize package failed. ({_buildMode}) {operation.Error}");
                _initResult.Succeed = false;
                _initResult.ErrorMessage = operation.Error;
            }
        }

        /// <summary>
        /// 获取资源版本
        /// </summary>
        async private UniTask RequestPackageVersion()
        {
            var operation = _package.RequestPackageVersionAsync();
            await operation.ToUniTask();

            if (operation.Status == EOperationStatus.Succeed)
            {
                _packageVersion = operation.PackageVersion;
                Log.Debug($"[XFramework] [AssetManager] Request package version succeed. {_packageVersion}");
                await UpdatePackageManifest();
            }
            else
            {
                Log.Error($"[XFramework] [AssetManager] Request package version failed. {operation.Error}");
                _initResult.Succeed = false;
                _initResult.ErrorMessage = operation.Error;
            }
        }

        /// <summary>
        /// 根据版本号更新资源清单
        /// </summary>
        async private UniTask UpdatePackageManifest()
        {
            var operation = _package.UpdatePackageManifestAsync(_packageVersion);
            await operation.ToUniTask();

            if (operation.Status == EOperationStatus.Succeed)
            {
                Log.Debug($"[XFramework] [AssetManager] Update package manifest succeed. Latest version: {_packageVersion}");
                await UpdatePackageFiles();
            }
            else
            {
                Log.Error($"[XFramework] [AssetManager] Update package manifest failed. (Latest version: {_packageVersion}) {operation.Error}");
                _initResult.Succeed = false;
                _initResult.ErrorMessage = operation.Error;
            }
        }

        /// <summary>
        /// 根据资源清单更新资源文件（下载到缓存资源）
        /// </summary>
        async private UniTask UpdatePackageFiles()
        {
            var downloader = _package.CreateResourceDownloader(_maxConcurrentDownloadCount, _failedDownloadRetryCount);

            if (downloader.TotalDownloadCount == 0)
            {
                Log.Debug("[XFramework] [AssetManager] No package files need to update.");
                _initResult.Succeed = true;
                _initResult.ErrorMessage = string.Empty;
                _initResult.DownloadCount = 0;
                _initResult.DownloadBytes = 0;
                _initResult.DownloadDuration = TimeSpan.Zero;
                return;
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

            DateTime startTime = DateTime.Now;

            downloader.BeginDownload();
            await downloader.ToUniTask();

            TimeSpan duration = DateTime.Now - startTime;

            if (downloader.Status == EOperationStatus.Succeed)
            {
                Log.Debug($"[XFramework] [AssetManager] Update package files succeed. Total download count: {totalDownloadCount}, Total download bytes: {totalDownloadBytes}");
                _initResult.Succeed = true;
                _initResult.ErrorMessage = string.Empty;
                _initResult.DownloadCount = totalDownloadCount;
                _initResult.DownloadBytes = totalDownloadBytes;
                _initResult.DownloadDuration = duration;
            }
            else
            {
                Log.Error($"[XFramework] [AssetManager] Update package files failed. {downloader.Error}");
                _initResult.Succeed = false;
                _initResult.ErrorMessage = downloader.Error;
            }
        }

        #endregion

        #region 资源缓存清理

        /// <summary>
        /// 清理所有缓存资源文件
        /// </summary>
        async public UniTask ClearAllCacheBundleFiles()
        {
            var operation = _package.ClearCacheFilesAsync(EFileClearMode.ClearAllBundleFiles);
            await operation.ToUniTask();

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
        async public UniTask ClearUnusedCacheBundleFiles()
        {
            var operation = _package.ClearCacheFilesAsync(EFileClearMode.ClearUnusedBundleFiles);
            await operation.ToUniTask();

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
        async public UniTask ClearAllCacheManifestFiles()
        {
            var operation = _package.ClearCacheFilesAsync(EFileClearMode.ClearAllManifestFiles);
            await operation.ToUniTask();

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
        async public UniTask ClearUnusedCacheManifestFiles()
        {
            var operation = _package.ClearCacheFilesAsync(EFileClearMode.ClearUnusedManifestFiles);
            await operation.ToUniTask();

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


        #region 统计信息

        /// <summary>
        /// 获取当前资源使用统计信息
        /// </summary>
        /// <returns>资源使用统计</returns>
        public ResourceUsageStats GetResourceUsageStats()
        {
            return new ResourceUsageStats(
                _assetHandleCache.Count,
                _sceneInfoCache.Count,
                _handleRefCount.Values.Sum(),
                _handleRefCount.Where(kvp => kvp.Value <= 0).Count()
            );
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
            await handle.ToUniTask();

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
            await handle.ToUniTask();

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


        #region 资源卸载

        /// <summary>
        /// 尝试卸载指定资源
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <remarks>
        /// 如果该资源还在被使用（存在句柄引用），该方法会无效
        /// </remarks>
        /// <returns>是否成功卸载</returns>
        public bool TryUnloadUnusedAsset(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                Log.Warning("[XFramework] [AssetManager] TryUnloadUnusedAsset: address is null or empty.");
                return false;
            }

            // 检查是否在自己的缓存中
            bool wasInCache = _assetHandleCache.ContainsKey(address);

            // 调用 YooAsset 的卸载方法
            _package.TryUnloadUnusedAsset(address);

            // 如果在缓存中且引用计数为0，则从缓存中移除
            if (wasInCache && _handleRefCount.TryGetValue(address, out int refCount) && refCount <= 0)
            {
                _assetHandleCache.Remove(address);
                _handleRefCount.Remove(address);
                Log.Debug($"[XFramework] [AssetManager] Removed unused asset from cache: {address}");
            }
            return true;
        }

        /// <summary>
        /// 卸载所有引用句柄为零的资源
        /// </summary>
        /// <remarks>
        /// 可以在切换场景之后调用资源释放方法或者写定时器间隔时间去释放
        /// </remarks>
        /// <returns>卸载操作的结果信息</returns>
        public async UniTask<UnloadResult> UnloadUnusedAssetsAsync()
        {
            Log.Debug($"[XFramework] [AssetManager] Starting to unload unused assets...");

            var startTime = DateTime.Now;

            var operation = _package.UnloadUnusedAssetsAsync();
            await operation.ToUniTask();

            TimeSpan duration = DateTime.Now - startTime;
            bool succeed = operation.Status == EOperationStatus.Succeed;
            string errorMessage = string.Empty;

            if (succeed)
            {
                Log.Debug($"[XFramework] [AssetManager] Unload unused assets completed successfully. " +
                         $"Duration: {duration.TotalMilliseconds:F2}ms");
            }
            else
            {
                Log.Error($"[XFramework] [AssetManager] Unload unused assets failed: {operation.Error}");
                errorMessage = operation.Error;
            }

            var result = new UnloadResult(succeed, errorMessage, duration);
            return result;
        }

        /// <summary>
        /// 强制卸载所有资源
        /// </summary>
        /// <remarks>
        /// Package 在销毁的时候也会自动调用该方法
        /// </remarks>
        /// <returns>卸载操作的结果信息</returns>
        public async UniTask<UnloadResult> ForceUnloadAllAssetsAsync()
        {
            Log.Debug($"[XFramework] [AssetManager] Starting to force unload all assets...");

            var startTime = DateTime.Now;

            // 清理所有缓存
            ClearAssetHandleCache();
            ClearSceneHandleCache();

            var operation = _package.UnloadAllAssetsAsync();
            await operation.ToUniTask();

            TimeSpan duration = DateTime.Now - startTime;
            bool succeed = operation.Status == EOperationStatus.Succeed;
            string errorMessage = string.Empty;

            if (succeed)
            {
                Log.Debug($"[XFramework] [AssetManager] Force unload all assets completed successfully. " +
                         $"Duration: {duration.TotalMilliseconds:F2}ms");
            }
            else
            {
                Log.Error($"[XFramework] [AssetManager] Force unload all assets failed: {operation.Error}");
                errorMessage = operation.Error;
            }

            var result = new UnloadResult(succeed, errorMessage, duration);
            return result;
        }

        #endregion


        #region 场景资源管理

        private readonly Dictionary<string, SceneInfo> _sceneInfoCache = new();

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
            // 检查当前状态
            var currentState = GetSceneState(address);
            if (currentState == SceneState.Loading)
            {
                Log.Warning($"[XFramework] [AssetManager] Scene ({address}) is already loading.");
                return false;
            }
            else if (currentState == SceneState.LoadedActive || currentState == SceneState.LoadedInactive)
            {
                Log.Warning($"[XFramework] [AssetManager] Scene ({address}) is already loaded.");
                return true;
            }

            SceneHandle handle;
            SceneInfo sceneInfo;

            // 开始加载
            handle = _package.LoadSceneAsync(address, sceneMode, physicsMode, suspendLoad, priority);
            sceneInfo = new SceneInfo(handle, SceneState.Loading, sceneMode);
            _sceneInfoCache[address] = sceneInfo;

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

            await handle.ToUniTask();

            if (handle.Status == EOperationStatus.Succeed)
            {
                // 更新状态
                sceneInfo.State = suspendLoad ? SceneState.LoadedInactive : SceneState.LoadedActive;
                Log.Debug($"[XFramework] [AssetManager] Load scene ({handle.SceneName}) succeed. State: {sceneInfo.State}");
                return true;
            }
            else
            {
                Log.Error($"[XFramework] [AssetManager] Load scene ({address}) failed: {handle.LastError}");
                _sceneInfoCache.Remove(address);
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
            if (_sceneInfoCache.TryGetValue(address, out SceneInfo sceneInfo))
            {
                if (sceneInfo.State != SceneState.LoadedInactive)
                {
                    Log.Warning($"[XFramework] [AssetManager] Scene ({address}) is not in preloaded state. Current state: {sceneInfo.State}");
                    return false;
                }

                sceneInfo.Handle.ActivateScene();
                sceneInfo.State = SceneState.LoadedActive;
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
            if (_sceneInfoCache.TryGetValue(address, out SceneInfo sceneInfo))
            {
                if (sceneInfo.State == SceneState.Unloading)
                {
                    Log.Warning($"[XFramework] [AssetManager] Scene ({address}) is already unloading.");
                    return false;
                }

                // 更新状态为卸载中
                sceneInfo.State = SceneState.Unloading;

                var unloadOperation = sceneInfo.Handle.UnloadAsync();
                await unloadOperation.ToUniTask();

                if (unloadOperation.Status == EOperationStatus.Succeed)
                {
                    _sceneInfoCache.Remove(address);
                    Log.Debug($"[XFramework] [AssetManager] Unload scene ({address}) succeed.");
                    return true;
                }
                else
                {
                    // 卸载失败，恢复状态
                    sceneInfo.State = SceneState.LoadedActive; // 或之前的状态
                    Log.Error($"[XFramework] [AssetManager] Unload scene ({address}) failed: {unloadOperation.Error}");
                    return false;
                }
            }
            else
            {
                Log.Warning($"[XFramework] [AssetManager] Try to unload scene ({address}) that is not loaded.");
                return false;
            }
        }

        /// <summary>
        /// 批量卸载场景，请注意至少保留一个场景
        /// </summary>
        /// <param name="addresses">要卸载的场景地址列表</param>
        /// <returns>卸载结果，包含成功和失败的场景列表</returns>
        public async UniTask<SceneUnloadResult> UnloadScenesAsync(IEnumerable<string> addresses)
        {
            var successList = new List<string>();
            var failedList = new List<string>();

            foreach (var address in addresses)
            {
                bool success = await UnloadSceneAsync(address);
                if (success)
                    successList.Add(address);
                else
                    failedList.Add(address);
            }

            return new SceneUnloadResult(successList, failedList);
        }

        /// <summary>
        /// 卸载所有场景（除了指定的保留场景）
        /// </summary>
        /// <param name="exceptAddresses">要保留的场景地址列表</param>
        /// <returns>卸载结果</returns>
        public async UniTask<SceneUnloadResult> UnloadAllScenesExceptAsync(params string[] exceptAddresses)
        {
            var toUnload = _sceneInfoCache.Keys
                .Where(address => !exceptAddresses.Contains(address))
                .ToList();

            return await UnloadScenesAsync(toUnload);
        }

        /// <summary>
        /// 获取场景诊断信息
        /// </summary>
        /// <returns>场景诊断信息</returns>
        public SceneDiagnosticInfo GetSceneDiagnosticInfo()
        {
            var scenesByState = _sceneInfoCache.Values
                .GroupBy(info => info.State)
                .ToDictionary(g => g.Key, g => g.Count());

            var scenesByMode = _sceneInfoCache.Values
                .GroupBy(info => info.LoadMode)
                .ToDictionary(g => g.Key, g => g.Count());

            return new SceneDiagnosticInfo(
                totalScenes: _sceneInfoCache.Count,
                scenesByState: scenesByState,
                scenesByMode: scenesByMode,
                oldestLoadTime: _sceneInfoCache.Values.Any() ? _sceneInfoCache.Values.Min(info => info.LoadTime) : DateTime.Now,
                newestLoadTime: _sceneInfoCache.Values.Any() ? _sceneInfoCache.Values.Max(info => info.LoadTime) : DateTime.Now
            );
        }

        private void ClearSceneHandleCache()
        {
            foreach (var sceneInfo in _sceneInfoCache.Values)
            {
                sceneInfo.Handle?.Release();
            }
            _sceneInfoCache.Clear();
        }

        #endregion


        #region 场景状态管理

        /// <summary>
        /// 场景状态枚举
        /// </summary>
        public enum SceneState
        {
            NotLoaded,      // 未加载
            Loading,        // 加载中
            LoadedInactive, // 已加载但未激活（预加载状态）
            LoadedActive,   // 已加载且激活
            Unloading      // 卸载中
        }

        /// <summary>
        /// 场景信息
        /// </summary>
        public class SceneInfo
        {
            public SceneHandle Handle { get; set; }
            public SceneState State { get; set; }
            public LoadSceneMode LoadMode { get; set; }
            public DateTime LoadTime { get; set; }

            public SceneInfo(SceneHandle handle, SceneState state, LoadSceneMode mode)
            {
                Handle = handle;
                State = state;
                LoadMode = mode;
                LoadTime = DateTime.Now;
            }
        }

        /// <summary>
        /// 获取场景状态
        /// </summary>
        /// <param name="address">场景地址</param>
        /// <returns>场景状态</returns>
        public SceneState GetSceneState(string address)
        {
            if (_sceneInfoCache.TryGetValue(address, out SceneInfo info))
            {
                return info.State;
            }
            return SceneState.NotLoaded;
        }

        /// <summary>
        /// 获取所有场景信息
        /// </summary>
        /// <returns>场景信息字典</returns>
        public Dictionary<string, SceneInfo> GetAllScenesInfo()
        {
            return new Dictionary<string, SceneInfo>(_sceneInfoCache);
        }

        /// <summary>
        /// 检查场景是否可以安全卸载
        /// </summary>
        /// <param name="address">场景地址</param>
        /// <returns>是否可以安全卸载</returns>
        public bool CanUnloadScene(string address)
        {
            if (!_sceneInfoCache.TryGetValue(address, out SceneInfo sceneInfo))
                return false;

            // 正在卸载中或未加载的场景不能再次卸载
            return sceneInfo.State != SceneState.Unloading && sceneInfo.State != SceneState.NotLoaded;
        }

        /// <summary>
        /// 获取活跃场景数量
        /// </summary>
        /// <returns>活跃场景数量</returns>
        public int GetActiveSceneCount()
        {
            return _sceneInfoCache.Values.Count(info => info.State == SceneState.LoadedActive);
        }

        /// <summary>
        /// 获取所有已加载场景的地址
        /// </summary>
        /// <returns>已加载场景地址列表</returns>
        public List<string> GetLoadedSceneAddresses()
        {
            return _sceneInfoCache
                .Where(kvp => kvp.Value.State == SceneState.LoadedActive || kvp.Value.State == SceneState.LoadedInactive)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        #endregion


        #region 数据结构定义

        public class InitResult
        {
            /// <summary>
            /// 是否初始化成功
            /// </summary>
            public bool Succeed;

            /// <summary>
            /// 错误消息（如果失败）
            /// </summary>
            public string ErrorMessage;

            /// <summary>
            /// 下载的资源数量
            /// </summary>
            public int DownloadCount;

            /// <summary>
            /// 下载的总字节数
            /// </summary>
            public long DownloadBytes;

            /// <summary>
            /// 初始化操作总耗时
            /// </summary>
            public TimeSpan InitDuration;

            /// <summary>
            /// 下载操作总耗时
            /// </summary>
            public TimeSpan DownloadDuration;

            public override string ToString()
            {
                return $"Success: {Succeed}, " +
                       (Succeed ? "" : $", Error: {ErrorMessage}") +
                       (DownloadCount > 0 ? $", Downloaded: {DownloadCount} files, {DownloadBytes / 1024.0:F2} KB" : "") +
                       (InitDuration != default ? $", Init Duration: {InitDuration.TotalMilliseconds:F2}ms" : "") +
                       (DownloadDuration != default ? $", Download Duration: {DownloadDuration.TotalMilliseconds:F2}ms" : "");
            }
        }

        /// <summary>
        /// 资源卸载操作结果
        /// </summary>
        public readonly struct UnloadResult
        {
            /// <summary>
            /// 操作是否成功
            /// </summary>
            public readonly bool Succeed;

            /// <summary>
            /// 错误消息（如果失败）
            /// </summary>
            public readonly string ErrorMessage;

            /// <summary>
            /// 操作耗时
            /// </summary>
            public readonly TimeSpan Duration;

            public UnloadResult(bool succeed, string errorMessage, TimeSpan duration)
            {
                Succeed = succeed;
                ErrorMessage = errorMessage;
                Duration = duration;
            }

            public override readonly string ToString()
            {
                return $"Success: {Succeed}, Duration: {Duration.TotalMilliseconds:F2}ms, " +
                       (Succeed ? "" : $", Error: {ErrorMessage}");
            }
        }

        /// <summary>
        /// 资源使用统计信息
        /// </summary>
        public readonly struct ResourceUsageStats
        {
            /// <summary>
            /// 缓存的资源数量
            /// </summary>
            public readonly int CachedAssetsCount;

            /// <summary>
            /// 缓存的场景数量
            /// </summary>
            public readonly int CachedScenesCount;

            /// <summary>
            /// 总引用计数
            /// </summary>
            public readonly int TotalReferenceCount;

            /// <summary>
            /// 引用计数为0的资源数量
            /// </summary>
            public readonly int ZeroRefAssetsCount;

            public ResourceUsageStats(int cachedAssetsCount, int cachedScenesCount, int totalReferenceCount, int zeroRefAssetsCount)
            {
                CachedAssetsCount = cachedAssetsCount;
                CachedScenesCount = cachedScenesCount;
                TotalReferenceCount = totalReferenceCount;
                ZeroRefAssetsCount = zeroRefAssetsCount;
            }

            public override readonly string ToString()
            {
                return $"Cached Assets: {CachedAssetsCount}, Cached Scenes: {CachedScenesCount}, " +
                       $"Total Refs: {TotalReferenceCount}, Zero Refs: {ZeroRefAssetsCount}";
            }
        }

        /// <summary>
        /// 场景批量卸载结果
        /// </summary>
        public readonly struct SceneUnloadResult
        {
            /// <summary>
            /// 成功卸载的场景列表
            /// </summary>
            public readonly List<string> SuccessfulScenes;

            /// <summary>
            /// 卸载失败的场景列表
            /// </summary>
            public readonly List<string> FailedScenes;

            /// <summary>
            /// 是否全部成功
            /// </summary>
            public readonly bool AllSuccessful => FailedScenes.Count == 0;

            /// <summary>
            /// 成功数量
            /// </summary>
            public readonly int SuccessCount => SuccessfulScenes.Count;

            /// <summary>
            /// 失败数量
            /// </summary>
            public readonly int FailureCount => FailedScenes.Count;

            public SceneUnloadResult(List<string> successfulScenes, List<string> failedScenes)
            {
                SuccessfulScenes = successfulScenes ?? new List<string>();
                FailedScenes = failedScenes ?? new List<string>();
            }

            public override readonly string ToString()
            {
                return $"Scene Unload Result: {SuccessCount} successful, {FailureCount} failed";
            }
        }

        /// <summary>
        /// 场景诊断信息
        /// </summary>
        public readonly struct SceneDiagnosticInfo
        {
            /// <summary>
            /// 总场景数量
            /// </summary>
            public readonly int TotalScenes;

            /// <summary>
            /// 按状态分组的场景数量
            /// </summary>
            public readonly Dictionary<SceneState, int> ScenesByState;

            /// <summary>
            /// 按加载模式分组的场景数量
            /// </summary>
            public readonly Dictionary<LoadSceneMode, int> ScenesByMode;

            /// <summary>
            /// 最早加载时间
            /// </summary>
            public readonly DateTime OldestLoadTime;

            /// <summary>
            /// 最新加载时间
            /// </summary>
            public readonly DateTime NewestLoadTime;

            public SceneDiagnosticInfo(int totalScenes, Dictionary<SceneState, int> scenesByState,
                Dictionary<LoadSceneMode, int> scenesByMode, DateTime oldestLoadTime, DateTime newestLoadTime)
            {
                TotalScenes = totalScenes;
                ScenesByState = scenesByState ?? new Dictionary<SceneState, int>();
                ScenesByMode = scenesByMode ?? new Dictionary<LoadSceneMode, int>();
                OldestLoadTime = oldestLoadTime;
                NewestLoadTime = newestLoadTime;
            }

            public override readonly string ToString()
            {
                var stateInfo = string.Join(", ", ScenesByState.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                var modeInfo = string.Join(", ", ScenesByMode.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                return $"Total: {TotalScenes}, States: [{stateInfo}], Modes: [{modeInfo}]";
            }
        }

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
        #endregion
    }
}