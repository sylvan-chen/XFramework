using System.Collections.Generic;

namespace XFramework
{
    public interface IFsmManager : IManager
    {
        public IFsm<T> CreateFsm<T>(T owner, IFsmState<T>[] states) where T : class;
        public IFsm<T> CreateFsm<T>(string name, T owner, IFsmState<T>[] states) where T : class;
        public IFsm<T> CreateFsm<T>(T owner, List<IFsmState<T>> states) where T : class;
        public IFsm<T> CreateFsm<T>(string name, T owner, List<IFsmState<T>> states) where T : class;

        public IFsm<T> GetFsm<T>() where T : class;
        public IFsm<T> GetFsm<T>(string name) where T : class;

        public void DestroyFsm<T>() where T : class;
        public void DestroyFsm<T>(string name) where T : class;
    }
}