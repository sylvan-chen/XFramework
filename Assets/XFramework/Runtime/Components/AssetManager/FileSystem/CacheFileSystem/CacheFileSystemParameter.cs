namespace XFramework.Resource
{
    /// <summary>
    /// 缓存文件系统参数
    /// </summary>
    public readonly struct CacheFileSystemParameter
    {
        /// <summary>
        /// 根目录
        /// </summary>
        public readonly string RootDirectory;

        /// <summary>
        /// 文件解密服务类
        /// </summary>
        public readonly IDecryptionService DecryptionService;

        public CacheFileSystemParameter(string rootDirectory, IDecryptionService decryptionService)
        {
            RootDirectory = rootDirectory;
            DecryptionService = decryptionService;
        }
    }
}