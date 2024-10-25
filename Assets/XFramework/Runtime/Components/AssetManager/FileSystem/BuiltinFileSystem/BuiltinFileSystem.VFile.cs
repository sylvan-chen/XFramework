namespace XFramework.Resource
{
    internal sealed partial class BuiltinFileSystem
    {
        public readonly struct VFile
        {
            public readonly string FileName;

            public VFile(string fileName)
            {
                FileName = fileName;
            }
        }
    }
}