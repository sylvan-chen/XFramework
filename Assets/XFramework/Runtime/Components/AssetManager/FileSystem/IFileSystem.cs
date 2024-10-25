using Cysharp.Threading.Tasks;

namespace XFramework.Resource
{
    public interface IFileSystem
    {
        /// <summary>
        /// 文件系统的根目录
        /// </summary>
        public string RootDirectory { get; }

        /// <summary>
        /// 文件数量
        /// </summary>
        public int FileCount { get; }

        public UniTask InitAsync();

        public UniTask<string> LoadResourceVersionAsync();

        public UniTask<Manifest> LoadManifestAsync();
    }
}