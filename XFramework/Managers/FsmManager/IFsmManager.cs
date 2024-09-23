namespace XFramework
{
    public interface IFsmManager
    {
        public IFsm<T> CreateFsm<T>(T owner, IFsmState<T> states) where T : class;
        public IFsm<T> CreateFsm<T>(string id, T owner, IFsmState<T> states) where T : class;

        public IFsm<T> GetFsm<T>() where T : class;
        public IFsm<T> GetFsm<T>(string id) where T : class;

        public void DestroyFsm<T>() where T : class;
        public void DestroyFsm<T>(string id) where T : class;
    }
}