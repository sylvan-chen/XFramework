namespace XFramework
{
    public readonly struct LoadBytesResult
    {
        private readonly bool _isSuccess;
        private readonly byte[] _bytes;
        private readonly float _duration;
        private readonly string _error;

        public LoadBytesResult(bool isSuccess, byte[] bytes, float duration, string error)
        {
            _isSuccess = isSuccess;
            _bytes = bytes;
            _duration = duration;
            _error = error;
        }

        public bool IsSuccess => _isSuccess;

        public byte[] Bytes => _bytes;

        public float Duration => _duration;

        public string Error => _error;
    }
}