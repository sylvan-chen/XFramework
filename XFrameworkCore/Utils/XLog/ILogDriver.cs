namespace XFramework
{
    public interface ILogDriver
    {
        public void Debug(string message);

        public void Info(string message);

        public void Warning(string message);

        public void Error(string message);

        public void Fatal(string message);
    }
}