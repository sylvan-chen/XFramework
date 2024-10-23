namespace XFramework.Resource
{
    public readonly struct KernelInitResult
    {
        public static KernelInitResult Success()
        {
            return new KernelInitResult(TaskResultStatus.Success, null);
        }

        public static KernelInitResult Failure(string error)
        {
            return new KernelInitResult(TaskResultStatus.Failure, error);
        }

        public readonly TaskResultStatus Status;
        public readonly string Error;

        public KernelInitResult(TaskResultStatus status, string error)
        {
            Status = status;
            Error = error;
        }
    }
}