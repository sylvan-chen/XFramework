using System;
using System.IO;
using Cysharp.Threading.Tasks;
using XFramework.Utils;

namespace XFramework.Resource
{
    /// <summary>
    /// 内置文件系统，包含内置在游戏本体中的文件
    /// </summary>
    internal sealed class BuiltinFileSystem : IFileSystem
    {
        public string RootDirectory { get; private set; }

        public int FileCount { get; private set; }

        public UniTask<FSInitResult> InitAsync()
        {
            return UniTask.FromResult(FSInitResult.Sucess());
        }

        public async UniTask<string> LoadResourceVersionAsync(float timeout)
        {
            string versionFilePath = PathHelper.Combine(RootDirectory, ResourceManagerConfig.ResourceVersionFileName);
            return await FileHelper.ReadAllTextAsync(versionFilePath);
        }

        public async UniTask<FSLoadManifestResult> LoadManifestAsync(string resourceVersion, float timeout)
        {
            string hashFilePath = PathHelper.Combine(RootDirectory, ResourceManagerConfig.ManifestHashFileName);
            string binaryFilePath = PathHelper.Combine(RootDirectory, ResourceManagerConfig.ManifestBinaryFileName);
            if (!File.Exists(hashFilePath))
            {
                return FSLoadManifestResult.Failure($"{hashFilePath} not found.");
            }
            if (!File.Exists(binaryFilePath))
            {
                return FSLoadManifestResult.Failure($"{binaryFilePath} not found.");
            }

            // 先验证清单文件的 MD5 哈希值
            string targetHash = await FileHelper.ReadAllTextAsync(hashFilePath);
            byte[] binaryFileData = await FileHelper.ReadAllBytesAsync(binaryFilePath);
            string actualHash = HashHelper.BytesMD5(binaryFileData);
            if (actualHash != targetHash)
            {
                return FSLoadManifestResult.Failure($"The manifest file '{binaryFilePath}' is corrupted. The MD5 checksum verification has failed.");
            }

            // 反序列化清单文件
            Manifest manifest = ManifestSerilizer.DeserializeFromBytes(binaryFileData);

            return FSLoadManifestResult.Success(manifest);
        }

        public UniTask ClearAllBundleFilesAsync()
        {
            throw new System.NotImplementedException();
        }

        public UniTask ClearAllUnusedBundleFilesAsync(Manifest manifestInfo)
        {
            throw new System.NotImplementedException();
        }
    }
}