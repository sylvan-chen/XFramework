namespace XFramework.Utils
{
    /// <summary>
    /// 引用，可被引用池管理的对象接口
    /// </summary>
    public interface IReference
    {
        /// <summary>
        /// 清空引用对象，以便回收到引用池
        /// </summary>
        public void Clear();
    }
}