namespace XFramework.Resource
{
    /// <summary>
    /// 内置文件系统参数
    /// </summary>
    public readonly struct BuiltinFileSystemParameter
    {
        /// <summary>
        /// 根目录
        /// </summary>
        public readonly string RootDirectory;

        public BuiltinFileSystemParameter(string rootDirectory)
        {
            RootDirectory = rootDirectory;
        }
    }
}