using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine.Purchasing;

namespace XGame.Core
{
    public class IapProvider : IIapProvider
    {
        private enum ProviderStatus
        {
            Uninitialized,
            Preparing,
            ProductsFetched,
            PurchasesFetched,
            Initialized,
        }

        private const float DEFAULT_TIME_OUT = 60f;

        private ProviderStatus _status = ProviderStatus.Uninitialized;
        private StoreController _storeController;

        public void Initialize()
        {
            if (_status == ProviderStatus.Uninitialized)
            {
                InitializeAsync().Forget();
            }
        }

        public async UniTaskVoid InitializeAsync()
        {
            Log.Debug("[IapProvider] IapProvider start initializing...");
            _status = ProviderStatus.Preparing;

            _storeController = UnityIAPServices.StoreController();

            RegisterIapCallbacks();

            await ConnectStoreAsync();

            FetchStoreProducts(LoadLocalProductDefinitionsFromCatalog());
            await UniTask.WaitUntil(() => _status == ProviderStatus.ProductsFetched).Timeout(TimeSpan.FromSeconds(DEFAULT_TIME_OUT));

            FetchStorePurchases();
            await UniTask.WaitUntil(() => _status == ProviderStatus.PurchasesFetched).Timeout(TimeSpan.FromSeconds(DEFAULT_TIME_OUT));

            _status = ProviderStatus.Initialized;
            Log.Debug("[IapProvider] IapProvider initialization complete");
        }

        public void Dispose()
        {
            UnregisterIapCallbacks();
            _storeController = null;
            _status = ProviderStatus.Uninitialized;

            Log.Debug("[IapProvider] IapProvider disposed");
        }

        private async UniTask ConnectStoreAsync()
        {
            await _storeController.Connect().AsUniTask();
            Log.Debug("[IapProvider] Store connected");
        }

        private List<ProductDefinition> LoadLocalProductDefinitionsFromCatalog()
        {
            var catalog = ProductCatalog.LoadDefaultCatalog();
            if (catalog == null)
            {
                Log.Error("[IapProvider] Load product catalog failed.");
                return null;
            }

            Log.Debug($"[IapProvider] Load product catalog succeeded, product count: {catalog.allProducts.Count}, valid product count: {catalog.allValidProducts.Count}");

            var result = new List<ProductDefinition>();
            foreach (var product in catalog.allValidProducts)
            {
                result.Add(new ProductDefinition(product.id, product.type));
            }

            return result;
        }

        private void FetchStoreProducts(List<ProductDefinition> productDefinitions)
        {
            Log.Debug($"[IapProvider] Fetching {productDefinitions.Count} products from store...");
            _storeController.FetchProducts(productDefinitions);
        }

        private void FetchStorePurchases()
        {
            Log.Debug("[IapProvider] Fetching purchases from store...");
            _storeController.FetchPurchases();
        }

        #region 公共接口

        public void PurchaseProduct(string productId)
        {
            if (_status != ProviderStatus.Initialized)
            {
                Log.Error("[IapProvider] Purchase failed, IAP provider is not initialized.");
                return;
            }

            var product = _storeController.GetProductById(productId);
            if (product == null)
            {
                Log.Error($"[IapProvider] Purchase failed, product not found. Product ID: {productId}");
                return;
            }
            if (!product.availableToPurchase)
            {
                Log.Error($"[IapProvider] Purchase failed, product is not available for purchase. Product ID: {productId}");
                return;
            }

            _storeController.PurchaseProduct(product);
        }

        public void RestorePurchases()
        {
            if (_status != ProviderStatus.Initialized)
            {
                Log.Error("[IapProvider] Restore purchases failed, IAP provider is not initialized.");
                return;
            }

            _storeController.RestoreTransactions((succeed, error) =>
            {
                if (succeed)
                {
                    Log.Debug("[IapProvider] Restore purchases succeed.");
                }
                else
                {
                    Log.Error($"[IapProvider] Restore purchases failed. Error: {error}");
                }
            });
        }

        private readonly Dictionary<string, List<Action<bool>>> _checkProductOwnedCallbacksMap = new();
        private readonly HashSet<string> _checkingProductOwnedSet = new();

        public void CheckProductOwned(string productId, Action<bool> callback)
        {
            var product = _storeController.GetProductById(productId);
            if (product == null)
            {
                Log.Error($"[IapProvider] Check product owned failed, product not found. Product ID: {productId}");
                callback?.Invoke(false);
                return;
            }

            if (callback != null)
            {
                if (!_checkProductOwnedCallbacksMap.TryGetValue(productId, out var callbacks))
                {
                    _checkProductOwnedCallbacksMap[productId] = new();
                }
                callbacks.Add(callback);
            }

            // 正在检查的商品避免重复检查
            if (!_checkingProductOwnedSet.Contains(productId))
            {
                _checkingProductOwnedSet.Add(productId);
                _storeController.CheckEntitlement(product);
            }
        }

        #endregion

        #region 回调方法

        private void RegisterIapCallbacks()
        {
            _storeController.OnStoreDisconnected += OnStoreDisconnected;

            _storeController.OnProductsFetched += OnProductsFetched;
            _storeController.OnProductsFetchFailed += OnProductsFetchedFailed;
            _storeController.OnPurchasesFetched += OnPurchasesFetched;
            _storeController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;

            _storeController.OnPurchasePending += OnPurchasePending;
            _storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            _storeController.OnPurchaseFailed += OnPurchaseFailed;
            _storeController.OnPurchaseDeferred += OnPurchaseDeferred;

            _storeController.OnCheckEntitlement += OnCheckEntitlement;
        }

        private void UnregisterIapCallbacks()
        {
            _storeController.OnStoreDisconnected -= OnStoreDisconnected;

            _storeController.OnProductsFetched -= OnProductsFetched;
            _storeController.OnProductsFetchFailed -= OnProductsFetchedFailed;
            _storeController.OnPurchasesFetched -= OnPurchasesFetched;
            _storeController.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;

            _storeController.OnPurchasePending -= OnPurchasePending;
            _storeController.OnPurchaseConfirmed -= OnPurchaseConfirmed;
            _storeController.OnPurchaseFailed -= OnPurchaseFailed;
            _storeController.OnPurchaseDeferred -= OnPurchaseDeferred;

            _storeController.OnCheckEntitlement -= OnCheckEntitlement;
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription description)
        {
            Log.Debug($"[IapProvider] Store Disconnected. Message: {description.message}");
        }

        private void OnProductsFetched(List<Product> products)
        {
            Log.Debug($"[IapProvider] Successfully fetched {products.Count} products.");
            if (_status != ProviderStatus.Initialized)
                _status = ProviderStatus.ProductsFetched;
        }

        private void OnProductsFetchedFailed(ProductFetchFailed failure)
        {
            Log.Error($"[IapProvider] Failed to fetch products. Count of failed products: {failure.FailedFetchProducts.Count}, Reason: {failure.FailureReason}");
        }

        private void OnPurchasesFetched(Orders orders)
        {
            Log.Debug($"[IapProvider] Fetch purchases succeed. Pending: {orders.PendingOrders.Count}, " +
                      $"Confirmed: {orders.ConfirmedOrders.Count}, " +
                      $"Deferred: {orders.DeferredOrders.Count}.");

            foreach (var pendingOrder in orders.PendingOrders)
            {
                OnPurchasePending(pendingOrder);
            }

            if (_status != ProviderStatus.Initialized)
                _status = ProviderStatus.PurchasesFetched;
        }

        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription description)
        {
            Log.Error($"[IapProvider] Fetch purchases failed. Message: {description.message}， FailureReason: {description.FailureReason}");
        }

        private void OnPurchasePending(PendingOrder order)
        {
            if (ValidatePendingOrder(order))
            {
                var productDefinition = GetFirstProductInOrder(order).definition;
                GrantReward(productDefinition.id);
            }

            _storeController.ConfirmPurchase(order);
        }

        private void OnPurchaseConfirmed(Order order)
        {
            switch (order)
            {
                case ConfirmedOrder confirmedOrder:
                    HandleConfirmedOrder(confirmedOrder);
                    break;
                case FailedOrder failedOrder:
                    HandleFailedOrder(failedOrder);
                    break;
            }
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            HandleFailedOrder(order);
        }

        private void HandleConfirmedOrder(ConfirmedOrder confirmedOrder)
        {
            var product = GetFirstProductInOrder(confirmedOrder);
            Log.Debug($"[IapProvider] Purchase succeeded. ProductID: {product?.definition.id}");
        }

        private void HandleFailedOrder(FailedOrder failedOrder)
        {
            var product = GetFirstProductInOrder(failedOrder);
            var reason = failedOrder.FailureReason;
            Log.Error($"[IapProvider] Purchase failed. ProductID: {product?.definition.id}, FailureReason: {reason}");
        }

        private void OnPurchaseDeferred(DeferredOrder order)
        {
            var product = GetFirstProductInOrder(order);
            Log.Debug($"[IapProvider] 购买被延迟，商品ID: {product?.definition.id}");
        }

        private void OnCheckEntitlement(Entitlement entitlement)
        {
            var productId = entitlement?.Product?.definition.id;
            if (string.IsNullOrEmpty(productId))
            {
                Log.Error("[IapProvider] Check entitlement failed, product ID is null or empty.");
                return;
            }

            Log.Debug($"[IapProvider] Check entitlement for product ID: {productId}, status: {entitlement.Status}");

            var owned = entitlement.Status switch
            {
                EntitlementStatus.FullyEntitled => true,
                EntitlementStatus.EntitledButNotFinished => true,
                EntitlementStatus.EntitledUntilConsumed => true,
                EntitlementStatus.NotEntitled => false,
                EntitlementStatus.Unknown => false,
                _ => false
            };

            _checkingProductOwnedSet.Remove(productId);
            if (_checkProductOwnedCallbacksMap.TryGetValue(productId, out var callbacks))
            {
                foreach (var callback in callbacks)
                {
                    callback?.Invoke(owned);
                }
                _checkProductOwnedCallbacksMap.Remove(productId);
            }
        }

        #endregion

        #region 辅助方法

        private Product GetFirstProductInOrder(Order order)
        {
            return order.CartOrdered.Items().FirstOrDefault()?.Product;
        }

        private bool ValidatePendingOrder(PendingOrder order)
        {
            // TODO: 实现订单验证逻辑

            var product = GetFirstProductInOrder(order);
            if (product == null)
            {
                Log.Error($"[IapProvider] Order validation failed. TransactionID: {order.Info.TransactionID}, FailureReason: Product not found.");
                return false;
            }

            return true;
        }

        public void GrantReward(string productId)
        {
            // TODO: 分发发放奖励事件
        }

        #endregion
    }
}