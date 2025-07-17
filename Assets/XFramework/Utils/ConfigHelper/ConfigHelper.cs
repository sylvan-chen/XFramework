using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace XFramework.Utils
{
    /// <summary>
    /// 配置表助手类，需要先预加载才能获取配置表类
    /// </summary>
    public static class ConfigTableHelper
    {
        private static readonly Dictionary<Type, string> _tableCache = new();
        private static readonly string _configTableDirectory = Path.Combine(Application.streamingAssetsPath, "GameConfigs");

        /// <summary>
        /// 获取配置表类
        /// </summary>
        /// <typeparam name="T">配置表类型</typeparam>
        /// <returns>配置文件实例</returns>
        public static T GetTable<T>() where T : ConfigTableBase
        {
            Type tableType = typeof(T);

            if (_tableCache.TryGetValue(tableType, out var jsonContent))
            {
                return JsonConvert.DeserializeObject<T>(jsonContent);
            }
            else
            {
                Log.Error($"[XFramework] [ConfigLoader] Config not found in cache: {tableType}");
                return null;
            }
        }

        /// <summary>
        /// 异步预加载配置文件
        /// </summary>
        /// <param name="configType">配置文件类型</param>
        /// <param name="fileName">配置文件名（包含扩展名）</param>
        public static async UniTask PreloadConfigAsync(Type configType, string fileName)
        {
            // 检查配置类型是否为 ConfigTableBase 的子类
            if (!typeof(ConfigTableBase).IsAssignableFrom(configType))
            {
                Log.Error($"[XFramework] [ConfigLoader] Preload config failed (Type: {configType}). Config type must be a subclass of <ConfigTableBase>. ");
                return;
            }

            if (_tableCache.TryGetValue(configType, out var _)) // 检查缓存
            {
                Log.Warning($"[XFramework] [ConfigLoader] Config already loaded but still trying to preload:" +
                    $"Type: {configType}, File: {fileName}");
            }
            else
            {
                await ReadConfigTableFileAsync(fileName, configType);
            }
        }

        /// <summary>
        /// 异步预加载配置文件
        /// </summary>
        /// <typeparam name="T">配置文件类型</typeparam>
        /// <param name="fileName">配置文件名（包含扩展名）</param>
        public static async UniTask PreloadConfigAsync<T>(string fileName) where T : ConfigTableBase
        {
            await PreloadConfigAsync(typeof(T), fileName);
        }

        /// <summary>
        /// 清除所有配置文件缓存
        /// </summary>
        public static void ClearAllConfigCache()
        {
            _tableCache.Clear();
            Log.Debug("[XFramework] [ConfigLoader] All config caches cleared.");
        }

        private static async UniTask<string> ReadConfigTableFileAsync(string fileName, Type tableType)
        {
            string jsonContent;
            string filePath = Path.Combine(_configTableDirectory, $"{fileName}");
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

            _tableCache[tableType] = jsonContent; // 缓存配置内容
            Log.Debug($"[XFramework] [ConfigLoader] Config file cached: {fileName}");
            return jsonContent;
        }
    }
}