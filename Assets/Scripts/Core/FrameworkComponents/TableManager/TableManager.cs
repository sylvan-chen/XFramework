using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using XFramework.Utils;
using XGame.Utils;

namespace XGame.Core
{
    /// <summary>
    /// 配置表加载器
    /// </summary>
    public class TableManager : FrameworkComponent
    {
        private readonly TableManagerSetting _setting;

        // 所有配置表缓存: typeof(T) -> T类实例
        private readonly Dictionary<Type, object> _cachedTables = new();

        public TableManager(TableManagerSetting setting)
        {
            _setting = setting;
        }

        internal override void Init()
        {
            base.Init();

            if (_setting.PreloadOnInit)
            {
                PreloadTables().Forget();
            }
        }

        /// <summary>
        /// 预加载配置表
        /// </summary>
        public async UniTaskVoid PreloadTables()
        {
            Log.Debug("[ConfigManager] Start Preload tables...");

            foreach (var folderName in _setting.PreloadTableFolderNames)
            {
                var tableDirectory = Path.Combine(Application.streamingAssetsPath, folderName);

                Log.Debug($"[ConfigManager] Preloading tables from directory: {tableDirectory}");

                if (!Directory.Exists(tableDirectory))
                {
                    Log.Error($"[ConfigManager] Table directory not found: {tableDirectory}");
                    return;
                }

                var jsonPaths = Directory.GetFiles(tableDirectory, "*.json", SearchOption.AllDirectories);
                if (jsonPaths == null || jsonPaths.Length == 0)
                {
                    Log.Warning($"[ConfigManager] No config JSON files found in directory: {tableDirectory}");
                    return;
                }

                foreach (var jsonPath in jsonPaths)
                {
                    string fileName = PathHelper.GetFileNameWithoutExtension(jsonPath);
                    fileName = StringHelper.ToPascalCase(fileName);
                    string typeFullName = $"XGame.Table.Table{fileName}";
                    Type tableType = TypeHelper.GetTypeDeeply(typeFullName);
                    if (tableType == null)
                    {
                        Log.Error($"[ConfigManager] Config type {typeFullName} not found.");
                        continue;
                    }
                    await LoadTableAsync(jsonPath, tableType);
                }
            }

            Log.Debug("[ConfigManager] Preload tables finished.");
        }

        /// <summary>
        /// 获取配置表实例
        /// </summary>
        /// <typeparam name="T">配置表类型</typeparam>
        /// <returns>配置文件实例</returns>
        public T GetTable<T>() where T : class
        {
            Type tableType = typeof(T);

            if (_cachedTables.TryGetValue(tableType, out var table))
            {
                return table as T;
            }

            Log.Error($"[ConfigManager] Table not found: {tableType}");
            return null;
        }

        /// <summary>
        /// 异步加载配置表实例
        /// </summary>
        /// <typeparam name="T">配置表类型</typeparam>
        /// <param name="filePath">配置文件路径</param>
        /// <param name="isOverride">是否覆盖已加载的配置</param>
        public async UniTask LoadTableAsync<T>(string filePath, bool isOverride = false) where T : class
        {
            await LoadTableAsync(filePath, typeof(T), isOverride);
        }

        /// <summary>
        /// 异步加载配置表实例
        /// </summary>
        /// <param name="filePath">配置表路径</param>
        /// <param name="tableType">配置表类型</param>
        /// <param name="isOverride">是否覆盖已加载的配置</param>
        public async UniTask LoadTableAsync(string filePath, Type tableType, bool isOverride = false)
        {
            if (tableType == null)
            {
                throw new ArgumentNullException(nameof(tableType), "Config type cannot be null.");
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("Config file path cannot be null or empty.", nameof(filePath));
            }

            if (_cachedTables.TryGetValue(tableType, out var _))
            {
                if (isOverride)
                {
                    Log.Debug($"[ConfigManager] Duplicate config load attempt, covering it:" +
                        $"Type: {tableType}, File: {filePath}");
                }
                else
                {
                    Log.Warning($"[ConfigManager] Duplicate config load attempt, skip it:" +
                        $"Type: {tableType}, File: {filePath}");
                    return;
                }
            }

            string jsonContent = await FileHelper.ReadAllTextAsync(filePath);
            if (jsonContent == null)
            {
                Log.Error($"[ConfigManager] Failed to read config file: {filePath}");
                return;
            }

            var table = JsonConvert.DeserializeObject(jsonContent, tableType);

            _cachedTables[tableType] = table;
        }

        /// <summary>
        /// 清除所有配置文件缓存
        /// </summary>
        public void ClearAllConfigCache()
        {
            _cachedTables.Clear();
            Log.Debug("[ConfigManager] All config caches cleared.");
        }
    }
}