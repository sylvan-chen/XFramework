namespace XFramework.Utils
{
    public enum WebRequestStatus
    {
        Success,
        ConnectionError,
        ProtocolError,
        DataProcessingError,
        TimeoutError,
        UnknownError,
    }

    public readonly struct WebRequestResult
    {
        public readonly WebRequestStatus Status;
        public readonly string Error;
        public readonly byte[] Data;
        public readonly string Text;

        public WebRequestResult(WebRequestStatus status, string error, byte[] data, string text)
        {
            Status = status;
            Error = error;
            Data = data;
            Text = text;
        }
    }
}