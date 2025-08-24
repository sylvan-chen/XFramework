using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace XFramework.Utils
{
    /// <summary>
    /// 配置表加载器
    /// </summary>
    public static class ConfigLoader
    {
        /// <summary>
        /// 所有配置表缓存: typeof(T) -> Dictionary<id, 对象>
        /// </summary>
        private static readonly Dictionary<Type, object> _configMap = new();
        private static readonly Dictionary<Type, string> _tableCache = new();
        private static readonly string _configTableDirectory = Path.Combine(Application.streamingAssetsPath, "Schemes");

        /// <summary>
        /// 根据ID获取配置
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <param name="id">配置ID</param>
        /// <returns>配置对象</returns>
        public static T GetConfig<T>(int id) where T : IConfig
        {
            if (_configMap.TryGetValue(typeof(T), out var config))
            {
                var table = config as Dictionary<int, T>;
                if (table != null && table.TryGetValue(id, out var value))
                {
                    return value;
                }
            }

            Log.Error($"[ConfigLoader] Config not found: {typeof(T)}, Id: {id}");
            return default;
        }

        /// <summary>
        /// 获取整个配置表
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <returns>配置文件实例</returns>
        public static Dictionary<int, T> GetTable<T>() where T : IConfig
        {
            Type tableType = typeof(T);

            if (_configMap.TryGetValue(tableType, out var config))
            {
                return config as Dictionary<int, T>;
            }

            Log.Error($"[ConfigLoader] Table not loaded: {tableType}");
            return null;
        }

        /// <summary>
        /// 异步加载配置文件
        /// </summary>
        /// <typeparam name="T">配置文件类型</typeparam>
        /// <param name="fileName">配置文件名（包含扩展名）</param>
        public static async UniTask LoadConfigAsync<T>(string fileName) where T : IConfig
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("Config file name cannot be null or empty.", nameof(fileName));
            }

            if (_tableCache.TryGetValue(typeof(T), out var _)) // 检查缓存
            {
                Log.Warning($"[ConfigLoader] Config already loaded but still trying to preload:" +
                    $"Type: {typeof(T)}, File: {fileName}");
                return;
            }

            string jsonContent = await ReadJsonFileAsync(fileName);
            if (jsonContent == null)
            {
                Log.Error($"[ConfigLoader] Failed to read config file: {fileName}");
                return;
            }

            List<T> configs = JsonConvert.DeserializeObject<List<T>>(jsonContent);

            var map = new Dictionary<int, T>();
            foreach (var config in configs)
            {
                map[config.Id] = config;
            }

            _configMap[typeof(T)] = map; // 缓存配置对象
        }

        /// <summary>
        /// 异步加载配置文件
        /// </summary>
        /// <param name="configType">配置文件类型</param>
        /// <param name="fileName">配置文件名（包含扩展名）</param>
        public static async UniTask LoadConfigAsync(string fileName, Type configType)
        {
            if (configType == null)
            {
                throw new ArgumentNullException(nameof(configType), "Config type cannot be null.");
            }

            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("Config file name cannot be null or empty.", nameof(fileName));
            }

            if (_tableCache.TryGetValue(configType, out var _)) // 检查缓存
            {
                Log.Warning($"[ConfigLoader] Config already loaded but still trying to preload:" +
                    $"Type: {configType}, File: {fileName}");
                return;
            }

            string jsonContent = await ReadJsonFileAsync(fileName);
            if (jsonContent == null)
            {
                Log.Error($"[ConfigLoader] Failed to read config file: {fileName}");
                return;
            }

            var listType = typeof(List<>).MakeGenericType(configType);
            var configs = JsonConvert.DeserializeObject(jsonContent, listType);

            var mapType = typeof(Dictionary<,>).MakeGenericType(typeof(int), configType);
            var map = Activator.CreateInstance(mapType);

            var addMethod = mapType.GetMethod("Add");
            var idProperty = configType.GetProperty("Id");

            foreach (var config in (System.Collections.IEnumerable)configs)
            {
                var id = (int)idProperty.GetValue(config);
                addMethod.Invoke(map, new object[] { id, config });
            }

            _configMap[configType] = map; // 缓存配置对象
        }

        /// <summary>
        /// 清除所有配置文件缓存
        /// </summary>
        public static void ClearAllConfigCache()
        {
            _tableCache.Clear();
            Log.Debug("[ConfigLoader] All config caches cleared.");
        }

        private static async UniTask<string> ReadJsonFileAsync(string fileName)
        {
            string jsonContent;
            string filePath = Path.Combine(_configTableDirectory, $"{fileName}");
            if (!FileHelper.Exists(filePath))
            {
                Log.Error($"[ConfigLoader] Config file not found: {filePath}");
                return null;
            }

            // 根据平台选择不同的读取方式
            if (Application.platform == RuntimePlatform.Android)
            {
                // Android 平台使用 UnityWebRequest 读取
                var result = await WebRequestHelper.WebGetBufferAsync(filePath);
                if (result.Status == WebRequestStatus.Success)
                {
                    jsonContent = result.DownloadBuffer.Text;
                }
                else
                {
                    Log.Error($"[ConfigLoader] Failed to read config file from web request: {result.Error}");
                    return null;
                }
            }
            else
            {
                // 其他平台直接读取文件
                jsonContent = await FileHelper.ReadAllTextAsync(filePath);
            }

            return jsonContent;
        }
    }
}