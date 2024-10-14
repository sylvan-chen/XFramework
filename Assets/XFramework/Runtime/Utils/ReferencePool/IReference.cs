namespace XFramework.Utils
{
    /// <summary>
    /// 引用，可被引用池管理的对象接口
    /// </summary>
    /// <remarks>
    /// 要被引用池管理的对象必须实现此接口，该接口能够保证对象必须实现一个清空自身的方法，
    /// 引用池将自动调用清空方法，以防外部放回到引用池时忘记清空。
    /// </remarks>
    public interface IReference
    {
        /// <summary>
        /// 清空对象（置为初始状态）
        /// </summary>
        public void Clear();
    }
}