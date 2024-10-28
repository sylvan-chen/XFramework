using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using XFramework.Utils;

namespace XFramework.Resource
{
    /// <summary>
    /// 内置文件系统，用于访问内置资源（首包资源）
    /// </summary>
    internal sealed partial class BuiltinFileSystem
    {
        private readonly string _rootDirectory;

        public BuiltinFileSystem(string rootDirectory)
        {
            _rootDirectory = rootDirectory;
        }

        public UniTask InitAsync()
        {
            return UniTask.CompletedTask;
        }

        public async UniTask<AssetBundle> LoadBundleAsync(ManifestBundle bundle)
        {
            string bunldeFilePath = GetBuiltinFilePath(bundle.FileName);
            if (!FileHelper.Exists(bunldeFilePath))
            {
                throw new FileNotFoundException($"Cannot find bundle file {bunldeFilePath}", bunldeFilePath);
            }

            return await AssetBundle.LoadFromFileAsync(bunldeFilePath);
        }

        private string GetBuiltinFilePath(string fileName)
        {
            return PathHelper.Combine(_rootDirectory, fileName);
        }
    }
}