namespace XFramework.Utils
{
    /// <summary>
    /// 引用，可被引用池管理的对象接口
    /// </summary>
    public interface IReference
    {
        /// <summary>
        /// 回收时的处理
        /// </summary>
        /// <remarks>
        /// 一般用于清理脏数据，保证下次使用时可以正常初始化。
        /// </remarks>
        public void OnRelease();
    }
}