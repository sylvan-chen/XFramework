namespace XFramework.Resource
{
    public interface IRemoteService
    {
        public string GetRemoteURL(string fileName);

        public string GetFallbackRemoteURL(string fileName);
    }
}