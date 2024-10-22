namespace XFramework.Resource
{
    public readonly struct DecryptFileInfo
    {
        public readonly string BundleName;
        public readonly string FileLoadPath;
        public readonly uint FileLoadCRC;
    }

    public interface IDecryptionService
    {
        public byte[] DecryptFileStream(DecryptFileInfo fileInfo);

        public string DecryptFileText(DecryptFileInfo fileInfo);
    }
}