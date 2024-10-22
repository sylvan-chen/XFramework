namespace XFramework.Resource
{
    /// <summary>
    /// 远程文件系统参数
    /// </summary>
    public readonly struct RemoteFileSystemParameter
    {
        /// <summary>
        /// 根目录
        /// </summary>
        public readonly string RootDirectory;

        /// <summary>
        /// 远程服务类，用于获取文件远程 URL
        /// </summary>
        public readonly IRemoteService RemoteService;

        /// <summary>
        /// 文件解密服务类
        /// </summary>
        public readonly IDecryptionService DecryptionService;

        public RemoteFileSystemParameter(string rootDirectory, IRemoteService remoteService, IDecryptionService decryptionService)
        {
            RootDirectory = rootDirectory;
            RemoteService = remoteService;
            DecryptionService = decryptionService;
        }
    }
}