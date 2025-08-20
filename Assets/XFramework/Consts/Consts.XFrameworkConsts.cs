using XFramework;

public static partial class Consts
{
    public static class XFrameworkConsts
    {
        /// <summary>
        /// 组件优先级
        /// </summary>
        public static class ComponentPriority
        {
            internal const int CachePool = -5000;
            internal const int AssetManager = -5000;
            internal const int EventManager = -4000;
            internal const int StateMachineManager = -4000;
            internal const int GameSetting = -4000;
            internal const int PoolManager = -4000;
            internal const int UIManager = -1000;
            internal const int ProcedureManager = 0;
        }

        /// <summary>
        /// 资源管理器属性
        /// </summary>
        public static class AssetManagerProperty
        {
            public const AssetManager.BuildMode BuildMode = AssetManager.BuildMode.Editor;
            public const string MainPackageName = "DefaultPackage";
            public const string DefaultHostServer = "http://<Server>/CDN/<Platform>/<Version>";
            public const string FallbackHostServer = "http://<Server>/CDN/<Platform>/Fallback";
            public const int MaxConcurrentDownloadCount = 10;
            public const int FailedDownloadRetryCount = 3;
        }

        /// <summary>
        /// 流程管理器属性
        /// </summary>
        public static class ProcedureManagerProperty
        {
            public const string StartupProcedureTypeName = "ProcedureStartup";
            public static readonly string[] AvailableProcedureTypeNames = new[]
            {
                "ProcedureStartup",
                "ProcedureSplash",
                "ProcedureInitAssets",
                "ProcedurePreload",
                "ProcedureEnterScene"
            };
        }
    }
}
