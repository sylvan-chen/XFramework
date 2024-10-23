namespace XFramework.Resource
{
    /// <summary>
    /// 编辑器文件系统参数
    /// </summary>
    public readonly struct EditorFileSystemParameter
    {
        /// <summary>
        /// 根目录
        /// </summary>
        public readonly string RootDirectory;

        public EditorFileSystemParameter(string rootDirectory)
        {
            RootDirectory = rootDirectory;
        }
    }
}