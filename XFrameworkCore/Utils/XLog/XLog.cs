using System;
using System.Diagnostics;

namespace XFramework
{
    /// <summary>
    /// 日志工具类
    /// </summary>
    public static class XLog
    {
        private static ILogDriver _logDriver;

        public static void RegisterDriver(ILogDriver driver)
        {
            _logDriver = driver;
        }

        private static bool CheckDriverRegistered()
        {
            if (_logDriver == null)
            {
                throw new NullReferenceException("No LogDriver registered.");
            }
            return true;
        }

        /// <summary>
        /// 打印 Debug 级别日志
        /// </summary>
        /// <param name="message"></param>
        [Conditional("LOG_LEVEL_ALL")]
        [Conditional("LOG_LEVEL_DEBUG")]
        public static void Debug(string message)
        {
            if (CheckDriverRegistered())
            {
                _logDriver.Debug(message);
            }
        }

        /// <summary>
        /// 打印 Info 级别日志
        /// </summary>
        /// <param name="message"></param>
        [Conditional("LOG_LEVEL_ALL")]
        [Conditional("LOG_LEVEL_DEBUG")]
        [Conditional("LOG_LEVEL_INFO")]
        public static void Info(string message)
        {
            if (CheckDriverRegistered())
            {
                _logDriver.Info(message);
            }
        }

        /// <summary>
        /// 打印 Warning 级别日志
        /// </summary>
        /// <param name="message"></param>
        [Conditional("LOG_LEVEL_ALL")]
        [Conditional("LOG_LEVEL_DEBUG")]
        [Conditional("LOG_LEVEL_INFO")]
        [Conditional("LOG_LEVEL_WARNING")]
        public static void Warning(string message)
        {
            if (CheckDriverRegistered())
            {
                _logDriver.Warning(message);
            }
        }

        /// <summary>
        /// 打印 Error 级别日志
        /// </summary>
        /// <param name="message"></param>
        [Conditional("LOG_LEVEL_ALL")]
        [Conditional("LOG_LEVEL_DEBUG")]
        [Conditional("LOG_LEVEL_INFO")]
        [Conditional("LOG_LEVEL_WARNING")]
        [Conditional("LOG_LEVEL_ERROR")]
        public static void Error(string message)
        {
            if (CheckDriverRegistered())
            {
                _logDriver.Error(message);
            }
        }

        /// <summary>
        /// 打印 Fatal 级别日志
        /// </summary>
        /// <param name="message"></param>
        /// <remarks>
        /// 建议在发生致命错误，即可能导致游戏崩溃的情况时调用此方法，可以尝试重启进程或游戏框架来修复。
        /// </remarks>
        [Conditional("LOG_LEVEL_ALL")]
        [Conditional("LOG_LEVEL_DEBUG")]
        [Conditional("LOG_LEVEL_INFO")]
        [Conditional("LOG_LEVEL_WARNING")]
        [Conditional("LOG_LEVEL_ERROR")]
        [Conditional("LOG_LEVEL_FATAL")]
        public static void Fatal(string message)
        {
            if (CheckDriverRegistered())
            {
                _logDriver.Fatal(message);
            }
        }
    }
}