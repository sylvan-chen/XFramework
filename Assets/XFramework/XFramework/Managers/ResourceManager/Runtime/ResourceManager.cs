using YooAsset;
using UnityEngine;
using System.Collections;
using XFramework.Utils;
using System;
using System.IO;

namespace XFramework
{
    public sealed class ResourceManager : Manager
    {
        [SerializeField]
        BuildMode _buildMode;

        [SerializeField]
        private string _defaultHostServer = "http://<Server>/CDN/<Platform>/<Version>";

        [SerializeField]
        private string _fallbackHostServer = "http://<Server>/CDN/<Platform>/<Version>";

        [SerializeField]
        private int _maxConcurrentDownloadCount = 10;

        [SerializeField]
        private int _failedDownloadRetryCount = 3;

        private ResourcePackage _package;
        private ResourceDownloaderOperation _downloader;
        private long _totalDownloadBytes;

        private const string DEFAULT_PACKAGE_NAME = "DefaultPackage";

        private void Awake()
        {
            YooAssets.Initialize();
        }

        /// <summary>
        /// 初始化资源
        /// </summary>
        /// <param name="onSucceed">初始化成功回调</param>
        /// <param name="onFail">初始化失败回调，参数为错误信息</param>
        public void InitAsync(Action onSucceed, Action<string> onFail)
        {
            // 尝试获取资源包，如果资源包不存在，则创建资源包
            // 注意：这里需要先在 Collector 创建同名 Package
            _package = YooAssets.TryGetPackage(DEFAULT_PACKAGE_NAME) ?? YooAssets.CreatePackage(DEFAULT_PACKAGE_NAME);
            // 设置默认资源包，之后可以直接使用 YooAssets.XXX 接口来加载改资源包内容
            YooAssets.SetDefaultPackage(_package);
            // 初始化资源包
            StartCoroutine(InitPackageInternal(onSucceed, onFail));
        }

        /// <summary>
        /// 检查更新
        /// </summary>
        /// <param name="onSucceed">检查成功回调，参数为是否有更新</param>
        /// <param name="onFail">检查失败回调，参数为错误信息</param>
        public void CheckUpdateAsync(Action<bool> onSucceed, Action<string> onFail)
        {
            StartCoroutine(CheckUpdateInternal(onSucceed, onFail));
        }

        /// <summary>
        /// 下载更新
        /// </summary>
        /// <param name="onSucceed">下载成功回调</param>
        /// <param name="onDownloading">下载进度变动回调，参数为总下载数量，当前下载数量，总下载字节数，当前下载字节数</param>
        /// <param name="onFail">下载失败回调，参数为错误信息</param>
        public void DwonloadUpdateAsync(Action onSucceed, Action<int, int, long, long> onDownloading, Action<string> onFail)
        {
            StartCoroutine(DownloadUpdateInternal(onSucceed, onDownloading, onFail));
        }

        /// <summary>
        /// 检查磁盘空间是否足够用于下载更新
        /// </summary>
        public bool CheckDiskSpaceEnough()
        {
            DriveInfo driveInfo = new DriveInfo(Application.persistentDataPath);
            long freeSpace = driveInfo.AvailableFreeSpace;
            return freeSpace > _totalDownloadBytes;
        }

        private IEnumerator InitPackageInternal(Action onSuccess, Action<string> onFail, EDefaultBuildPipeline buildPipelineInEditorMode = EDefaultBuildPipeline.ScriptableBuildPipeline)
        {
            InitializationOperation operation = null;
            switch (_buildMode)
            {
                case BuildMode.Editor:
                    SimulateBuildResult simulateBuildResult = EditorSimulateModeHelper.SimulateBuild(buildPipelineInEditorMode, "DefaultPackage");
                    var initParametersEditor = new EditorSimulateModeParameters()
                    {
                        EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(simulateBuildResult)
                    };
                    operation = _package.InitializeAsync(initParametersEditor);
                    break;
                case BuildMode.Standalone:
                    var initParametersStandalone = new OfflinePlayModeParameters
                    {
                        BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters()
                    };
                    operation = _package.InitializeAsync(initParametersStandalone);
                    break;
                case BuildMode.Online:
                    IRemoteServices remoteServices = new RemoteServices(_defaultHostServer, _fallbackHostServer);
                    var initParametersRemote = new HostPlayModeParameters
                    {
                        BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters(),
                        CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices)
                    };
                    operation = _package.InitializeAsync(initParametersRemote);
                    break;
                case BuildMode.WebGL:
                    var initParametersWebGL = new WebPlayModeParameters
                    {
                        WebFileSystemParameters = FileSystemParameters.CreateDefaultWebFileSystemParameters()
                    };
                    operation = _package.InitializeAsync(initParametersWebGL);
                    break;
                default:
                    Log.Error($"[XFramework] [AssetManager] Invalid package mode: {_buildMode}");
                    break;
            }
            yield return operation;

            if (operation.Status != EOperationStatus.Succeed)
            {
                Log.Error($"[XFramework] [AssetManager] Initialize package failed. ({_buildMode}) {operation.Error}");
                onFail?.Invoke(operation.Error);
            }
            Log.Debug($"[XFramework] [AssetManager] Initialize package succeed. ({_buildMode})");
            onSuccess?.Invoke();
        }

        private IEnumerator CheckUpdateInternal(Action<bool> onSucceed, Action<string> onFail)
        {
            // 获取资源包版本
            // 单机模式下，直接获取本地资源包版本
            // 联机模式下，请求服务器资源包版本
            var requestVersionOperation = _package.RequestPackageVersionAsync();
            yield return requestVersionOperation;
            if (requestVersionOperation.Status != EOperationStatus.Succeed)
            {
                Log.Error($"[XFramework] [AssetManager] Request Package Version Failed: {requestVersionOperation.Error}");
                onFail?.Invoke(requestVersionOperation.Error);
                yield break;
            }
            string packageVersion = requestVersionOperation.PackageVersion;
            Log.Debug($"[XFramework] [AssetManager] Current Package Version: {packageVersion}");

            // 更新资源清单到本地
            var updateManifestOperation = _package.UpdatePackageManifestAsync(packageVersion);
            yield return updateManifestOperation;
            if (updateManifestOperation.Status != EOperationStatus.Succeed)
            {
                Log.Error($"[XFramework] [AssetManager] Update Package Manifest Failed: {updateManifestOperation.Error}");
                onFail?.Invoke(updateManifestOperation.Error);
                yield break;
            }
            Log.Debug("[XFramework] [AssetManager] Update Package Manifest Succeed.");

            // 创建下载器，如果没有要下载的资源，说明不需要更新
            _downloader = _package.CreateResourceDownloader(_maxConcurrentDownloadCount, _failedDownloadRetryCount);
            if (_downloader.TotalDownloadCount == 0)
            {
                Log.Debug("[XFramework] [AssetManager] No need to download update.");
                _downloader = null;
                onSucceed?.Invoke(false);
            }
            else
            {
                _totalDownloadBytes = _downloader.TotalDownloadBytes;
                onSucceed?.Invoke(true);
            }
        }

        private IEnumerator DownloadUpdateInternal(Action onSucceed, Action<int, int, long, long> onDownloading, Action<string> onFail)
        {
            if (_downloader == null)
            {
                onFail?.Invoke("Downloader is null. Please check update first.");
                yield break;
            }
            if (_downloader.TotalDownloadCount == 0)
            {
                onSucceed?.Invoke();
                yield break;
            }

            _downloader.OnDownloadErrorCallback += (fileName, error) =>
            {
                Log.Error($"[XFramework] [AssetManager] Download {fileName} failed. {error}");
                onFail?.Invoke(error);
            };
            _downloader.OnDownloadProgressCallback += (totalDownloadCount, currentDownloadCount, totalDownloadBytes, currentDownloadBytes) =>
            {
                onDownloading?.Invoke(totalDownloadCount, currentDownloadCount, totalDownloadBytes, currentDownloadBytes);
            };

            _downloader.BeginDownload();
            yield return _downloader;

            if (_downloader.Status != EOperationStatus.Succeed)
            {
                Log.Error($"[XFramework] [AssetManager] Download update failed. {_downloader.Error}");
                onFail?.Invoke(_downloader.Error);
            }
            Log.Debug("[XFramework] [AssetManager] Download update succeed.");
            onSucceed?.Invoke();
        }

        internal enum BuildMode
        {
            Editor,
            Standalone,
            Online,
            WebGL,
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
    }
}