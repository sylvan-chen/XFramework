namespace XGame.Core
{
    public interface IIapProvider
    {
        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize();

        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose();

        /// <summary>
        /// 购买商品
        /// </summary>
        /// <param name="productId">商品ID</param>
        public void PurchaseProduct(string productId);

        /// <summary>
        /// 恢复购买（仅iOS有效）
        /// </summary>
        public void RestorePurchases();

        /// <summary>
        /// 检查商品是否已拥有（仅针对非消耗品和订阅）
        /// </summary>
        /// <param name="productId">商品ID</param>
        /// <param name="callback">回调函数，参数表示是否拥有该商品</param>
        public void CheckProductOwned(string productId, System.Action<bool> callback);
    }
}