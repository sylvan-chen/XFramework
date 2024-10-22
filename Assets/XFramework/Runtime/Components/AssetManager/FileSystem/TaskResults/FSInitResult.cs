namespace XFramework.Resource
{
    public readonly struct FSInitResult
    {
        public static FSInitResult Sucess()
        {
            return new FSInitResult(TaskResultStatus.Success, null);
        }

        public static FSInitResult Failure(string error)
        {
            return new FSInitResult(TaskResultStatus.Failure, error);
        }

        private readonly TaskResultStatus _status;
        private readonly string _error;

        private FSInitResult(TaskResultStatus status, string error)
        {
            _status = status;
            _error = error;
        }

        public TaskResultStatus Status
        {
            get => _status;
        }

        public string Error
        {
            get => _error;
        }
    }
}