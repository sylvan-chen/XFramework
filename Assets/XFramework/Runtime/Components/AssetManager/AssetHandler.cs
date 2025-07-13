using XFramework.Utils;
using YooAsset;

namespace XFramework
{
    /// <summary>
    /// 资源句柄
    /// </summary>
    /// <remarks>
    /// NOTE：不需要之后一定要调用 Release() 方法释放资源，否则会造成内存泄露！
    /// </remarks>
    public class AssetHandler
    {
        /// <summary>
        /// 实际资源对象
        /// </summary>
        public UnityEngine.Object AssetObject => _handle?.AssetObject;

        private AssetHandle _handle;
        private readonly string _address;

        internal int RefCount { get; set; }
        public string Address => _address;

        internal AssetHandler(AssetHandle handle, string address)
        {
            _handle = handle;
            _address = address;
            RefCount = 1; // 初始化引用计数为 1
        }

        /// <summary>
        /// 释放资源引用
        /// </summary>
        public void Release()
        {
            if (RefCount <= 0)
            {
                Log.Warning($"[XFramework] [AssetHandler] Attempting to release asset '{_address}' with non-positive ref count: {RefCount}");
            }
            else if (RefCount == 1)
            {
                Log.Debug($"[XFramework] [AssetHandler] Releasing asset '{_address}'");
                RefCount = 0; // 释放时将引用计数置为 0
                _handle?.Release();
                _handle = null;
            }
            else
            {
                Log.Debug($"[XFramework] [AssetHandler] Decreasing ref count for asset '{_address}', current count: {RefCount}");
                RefCount--; // 仅减少引用计数
            }
        }
    }
}