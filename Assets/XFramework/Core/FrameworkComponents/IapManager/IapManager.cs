namespace XGame.Core
{
    public class IapManager : FrameworkComponent
    {
        private readonly IapManagerSetting _setting;
        private readonly IIapProvider _iapProvider;

        public IapManager(IapManagerSetting setting)
        {
            _setting = setting;

            _iapProvider = _setting.ProviderType switch
            {
                IapProviderType.GooglePlay => new IapProvider(),
                IapProviderType.AppleAppStore => new IapProvider(),
                _ => throw new System.NotImplementedException()
            };
        }

        internal override void Initialize()
        {
            base.Initialize();
            _iapProvider.Initialize();
        }

        internal override void Dispose()
        {
            base.Dispose();
            _iapProvider.Dispose();
        }

        /// <summary>
        /// 购买商品
        /// </summary>
        /// <param name="productId">商品ID</param>
        public void PurchaseProduct(string productId)
        {
            _iapProvider.PurchaseProduct(productId);
        }

        /// <summary>
        /// 恢复购买（仅iOS有效）
        /// </summary>
        public void RestorePurchases()
        {
            _iapProvider.RestorePurchases();
        }

        /// <summary>
        /// 检查商品是否已拥有（仅针对非消耗品和订阅）
        /// </summary>
        /// <param name="productId">商品ID</param>
        /// <param name="callback">回调函数，参数表示是否拥有该商品</param>
        public void CheckProductOwned(string productId, System.Action<bool> callback)
        {
            _iapProvider.CheckProductOwned(productId, callback);
        }
    }
}