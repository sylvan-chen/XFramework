namespace XFramework
{
    /// <summary>
    /// 引用池对象接口
    /// </summary>
    public interface IReference
    {
        /// <summary>
        /// 清空引用数据，防止回收到引用池还存在脏数据
        /// </summary>
        public void Clear();
    }
}