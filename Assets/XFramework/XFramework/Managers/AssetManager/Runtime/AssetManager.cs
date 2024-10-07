using YooAsset;
using UnityEngine;
using System.Collections;
using XFramework.Utils;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;

namespace XFramework
{
    public sealed class AssetManager : Manager
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

        private readonly List<ResourcePackage> _resourcePackages = new();
        private readonly List<ResourceDownloaderOperation> _downloaders = new();
        private bool _isInitialized = false;
        private float _checkUpdateProgress = 0f;
        private int _totalDownloadCount = 0;
        private long _totalDownloadBytes = 0;

        private const string DEFAULT_PACKAGE_NAME = "DefaultPackage";

        public int PackageCount
        {
            get => _resourcePackages.Count;
        }

        public bool IsInitialized
        {
            get => _isInitialized;
        }

        public float CheckUpdateProgress
        {
            get => _checkUpdateProgress;
        }

        public int TotalDownloadCount
        {
            get => _totalDownloadCount;
        }

        public long TotalDownloadBytes
        {
            get => _totalDownloadBytes;
        }

        private void Awake()
        {
            YooAssets.Initialize();
        }

        public void InitAsync(Action onSuccess, Action onFailed)
        {
            // 尝试获取资源包，如果资源包不存在，则创建资源包
            ResourcePackage package = YooAssets.TryGetPackage(DEFAULT_PACKAGE_NAME) ?? YooAssets.CreatePackage(DEFAULT_PACKAGE_NAME);
            YooAssets.SetDefaultPackage(package);
            // 初始化资源包
            StartCoroutine(InitPackageInternal(package, onSuccess, onFailed));
        }

        private IEnumerator CheckUpdateInternal(ResourcePackage package)
        {
            // 先请求资源包版本
            var requstPackageVersionOperation = package.RequestPackageVersionAsync();
            yield return requstPackageVersionOperation;

            string packageVersion;
            if (requstPackageVersionOperation.Status == EOperationStatus.Succeed)
            {
                packageVersion = requstPackageVersionOperation.PackageVersion;
                Log.Debug($"[XFramework] [AssetManager] Current Package Version: {packageVersion}");
            }
            else
            {
                Debug.LogError($"[XFramework] [AssetManager] Request Package Version Failed: {requstPackageVersionOperation.Error}");
                // TODO: 跳出错误提示弹窗
                yield break;
            }

            // 再更新资源清单，确保本地和服务器一致
            var updatePackageManifestOperation = package.UpdatePackageManifestAsync(packageVersion);
            yield return updatePackageManifestOperation;

            if (updatePackageManifestOperation.Status == EOperationStatus.Succeed)
            {
                Debug.Log("[XFramework] [AssetManager] Update Package Manifest Succeed!");
            }
            else
            {
                Debug.LogError($"[XFramework] [AssetManager] Update Package Manifest Failed: {updatePackageManifestOperation.Error}");
                // TODO: 跳出错误提示弹窗
                yield break;
            }

            // 创建下载器，根据资源清单下载资源，如果没有要下载的资源，说明不需要更新
            ResourceDownloaderOperation downloader = package.CreateResourceDownloader(_maxConcurrentDownloadCount, _failedDownloadRetryCount);
            if (downloader != null && downloader.TotalDownloadCount > 0)
            {
                _downloaders.Add(downloader);
            }

            _checkUpdateProgress += 1f / PackageCount;
        }

        /// <summary>
        /// 检查是否需要更新
        /// </summary>
        /// <returns>是否需要更新</returns>
        public bool CheckNeedUpdate()
        {
            _downloaders.Clear();

            StartCoroutine(CheckUpdateInternal());
            return true;
        }

        /// <summary>
        /// 检查磁盘空间是否足够用于下载更新
        /// </summary>
        /// <returns></returns>
        public bool CheckDiskSpaceIsEnoughForDownload()
        {
            // 获取当前驱动器信息
            DriveInfo drive = new(Path.GetPathRoot(Application.dataPath));
            if (drive.IsReady)
            {
                long availableFreeSpace = drive.AvailableFreeSpace;
            }
            return true;
        }

        private IEnumerator InitPackageInternal(ResourcePackage package, Action onSuccess, Action onFailed, EDefaultBuildPipeline buildPipelineInEditorMode = EDefaultBuildPipeline.ScriptableBuildPipeline)
        {
            InitializationOperation initOperation = null;
            switch (_buildMode)
            {
                case BuildMode.Editor:
                    SimulateBuildResult simulateBuildResult = EditorSimulateModeHelper.SimulateBuild(buildPipelineInEditorMode, "DefaultPackage");
                    var initParametersEditor = new EditorSimulateModeParameters()
                    {
                        EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(simulateBuildResult)
                    };
                    initOperation = package.InitializeAsync(initParametersEditor);
                    break;
                case BuildMode.Standalone:
                    var initParametersStandalone = new OfflinePlayModeParameters
                    {
                        BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters()
                    };
                    initOperation = package.InitializeAsync(initParametersStandalone);
                    break;
                case BuildMode.Online:
                    IRemoteServices remoteServices = new RemoteServices(_defaultHostServer, _fallbackHostServer);
                    var initParametersRemote = new HostPlayModeParameters
                    {
                        BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters(),
                        CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices)
                    };
                    initOperation = package.InitializeAsync(initParametersRemote);
                    break;
                case BuildMode.WebGL:
                    var initParametersWebGL = new WebPlayModeParameters
                    {
                        WebFileSystemParameters = FileSystemParameters.CreateDefaultWebFileSystemParameters()
                    };
                    initOperation = package.InitializeAsync(initParametersWebGL);
                    break;
                default:
                    Log.Error($"[XFramework] [AssetManager] Invalid package mode: {_buildMode}");
                    break;
            }
            yield return initOperation;

            if (initOperation.Status == EOperationStatus.Succeed)
            {
                Log.Debug($"[XFramework] [AssetManager] Initialize package succeed. ({_buildMode})");
                onSuccess?.Invoke();
            }
            else
            {
                Log.Error($"[XFramework] [AssetManager] Initialize package failed. ({_buildMode}) {initOperation.Error}");
                onFailed?.Invoke();
            }
        }

        private IEnumerator CheckUpdateInternal()
        {
            foreach (ResourcePackage package in _resourcePackages)
            {
                // 先请求资源包版本
                RequestPackageVersionOperation requstPackageVersionOperation = package.RequestPackageVersionAsync();
                yield return requstPackageVersionOperation;

                string packageVersion;
                if (requstPackageVersionOperation.Status == EOperationStatus.Succeed)
                {
                    packageVersion = requstPackageVersionOperation.PackageVersion;
                    Log.Debug($"[XFramework] [AssetManager] Current Package Version: {packageVersion}");
                }
                else
                {
                    Log.Error($"[XFramework] [AssetManager] Request Package Version Failed: {requstPackageVersionOperation.Error}");
                    // TODO: 跳出错误提示弹窗
                    yield break;
                }

                // 再更新资源清单，确保本地和服务器一致
                UpdatePackageManifestOperation updatePackageManifestOperation = package.UpdatePackageManifestAsync(packageVersion);
                yield return updatePackageManifestOperation;

                if (updatePackageManifestOperation.Status == EOperationStatus.Succeed)
                {
                    Log.Debug("Update Package Manifest Succeed!");
                }
                else
                {
                    Log.Error($"Update Package Manifest Failed: {updatePackageManifestOperation.Error}");
                    // TODO: 跳出错误提示弹窗
                    yield break;
                }

                // 创建下载器，根据资源清单下载资源，如果没有要下载的资源，说明不需要更新
                ResourceDownloaderOperation downloader = package.CreateResourceDownloader(_maxConcurrentDownloadCount, _failedDownloadRetryCount);
                if (downloader != null && downloader.TotalDownloadCount > 0)
                {
                    _downloaders.Add(downloader);
                }

                _checkUpdateProgress += 1f / PackageCount;
            }

            _checkUpdateProgress = 1f;

            if (_downloaders.Count > 0)
            {
                _totalDownloadCount = _downloaders.Sum(d => d.TotalDownloadCount);
                _totalDownloadBytes = _downloaders.Sum(d => d.TotalDownloadBytes);
            }
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