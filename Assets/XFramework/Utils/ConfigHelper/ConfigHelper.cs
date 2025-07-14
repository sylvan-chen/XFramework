using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace XFramework.Utils
{
    public static class ConfigHelper
    {
        private static readonly Dictionary<string, string> _configCache = new();
        private static readonly string _configDirectory = Path.Combine(Application.streamingAssetsPath, "GameConfig");
        private static readonly string _configFileExtension = ".json";

        /// <summary>
        /// 异步加载配置文件
        /// </summary>
        /// <typeparam name="T">配置文件类型</typeparam>
        /// <param name="fileName">配置文件名（不包含扩展名）</param>
        /// <returns>配置文件数据</returns>
        public static async UniTask<T> LoadConfigAsync<T>(string fileName) where T : class, new()
        {
            string jsonContent;

            if (_configCache.TryGetValue(fileName, out var cachedConfig)) // 检查缓存
            {
                Log.Debug($"[XFramework] [ConfigLoader] Config loaded from cache: {fileName}");
                jsonContent = cachedConfig;
            }
            else // 未缓存则读取文件
            {
                jsonContent = await ReadConfigFileAsync(fileName);
                if (string.IsNullOrEmpty(jsonContent))
                {
                    Log.Error($"[XFramework] [ConfigLoader] Failed to load config file: {fileName}");
                    return null;
                }
            }

            return JsonConvert.DeserializeObject<T>(jsonContent);
        }

        /// <summary>
        /// 预加载配置文件
        /// </summary>
        public static async UniTask PreloadConfigsAsync(string[] fileNames)
        {
            var tasks = new List<UniTask<string>>();
            foreach (var fileName in fileNames)
            {
                tasks.Add(ReadConfigFileAsync(fileName));
            }

            await UniTask.WhenAll(tasks);

            Log.Debug($"[XFramework] [ConfigLoader] All specified config files (count: {tasks.Count}) preloaded.");
        }

        /// <summary>
        /// 清除配置文件缓存
        /// </summary>
        /// <param name="fileName">要清空的配置文件名</param>
        public static void ClearConfigCache(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                Log.Error("[XFramework] [ConfigLoader] ClearConfigCache called with empty or null fileName.");
                return;
            }

            if (_configCache.Remove(fileName))
            {
                Log.Debug($"[XFramework] [ConfigLoader] Config cache cleared: {fileName}");
            }
            else
            {
                Log.Warning($"[XFramework] [ConfigLoader] Config cache not found for: {fileName}");
            }
        }

        /// <summary>
        /// 清除所有配置文件缓存
        /// </summary>
        public static void ClearAllConfigCache()
        {
            _configCache.Clear();
            Log.Debug("[XFramework] [ConfigLoader] All config caches cleared.");
        }

        private static async UniTask<string> ReadConfigFileAsync(string fileName)
        {
            string jsonContent;
            string filePath = Path.Combine(_configDirectory, $"{fileName}{_configFileExtension}");
            if (!FileHelper.Exists(filePath))
            {
                Log.Error($"[XFramework] [ConfigLoader] Config file not found: {filePath}");
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
                    Log.Error($"[XFramework] [ConfigLoader] Failed to read config file from web request: {result.Error}");
                    return null;
                }
            }
            else
            {
                // 其他平台直接读取文件
                jsonContent = await FileHelper.ReadAllTextAsync(filePath);
            }

            _configCache[fileName] = jsonContent; // 缓存配置内容
            Log.Debug($"[XFramework] [ConfigLoader] Config file cached: {fileName}");
            return jsonContent;
        }
    }
}