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

        public readonly TaskResultStatus Status;
        public readonly string Error;

        public FSInitResult(TaskResultStatus status, string error)
        {
            Status = status;
            Error = error;
        }
    }
}