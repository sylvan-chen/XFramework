namespace XGame.Core
{
    public class IapPurchaseResult
    {
        public bool Success;
        public string ProductId;
        public string TransactionId;
        public string Receipt;
        public string ErrorMessage;


        public static IapPurchaseResult Succeed(bool success, string productId, string transactionId = null, string receipt = null, string errorMessage = null)
        {
            return new IapPurchaseResult
            {
                Success = success,
                ProductId = productId,
                TransactionId = transactionId,
                Receipt = receipt,
                ErrorMessage = errorMessage
            };
        }
    }
}