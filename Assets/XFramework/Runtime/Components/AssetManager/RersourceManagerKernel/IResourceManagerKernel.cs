using Cysharp.Threading.Tasks;

namespace XFramework.Resource
{
    /// <summary>
    /// 资源管理器内核
    /// </summary>
    public interface IResourceManagerKernel
    {
        /// <summary>
        /// 初始化内核
        /// </summary>
        public UniTask InitAsync();
    }
}