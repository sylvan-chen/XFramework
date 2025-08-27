using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using XFramework.Utils;

namespace XFramework
{
    /// <summary>
    /// 配置表加载器
    /// </summary>
    public class ConfigManager : FrameworkComponent
    {
        private readonly ConfigManagerSetting _setting;

        // 所有配置表缓存: typeof(T) -> Dictionary<id, 对象>
        private readonly Dictionary<Type, object> _tableMap = new();
        // 配置表目录
        private readonly string _tableDirectory;

        public ConfigManager(ConfigManagerSetting setting)
        {
            _setting = setting;
            _tableDirectory = Path.Combine(Application.streamingAssetsPath, _setting.TableFolderName);
        }

        internal override void Init()
        {
            base.Init();

            if (_setting.AutoPreload)
            {
                PreloadTables().Forget();
            }
        }

        public async UniTaskVoid PreloadTables()
        {
            Log.Debug("[ConfigManager] Preload tables...");
            if (!Directory.Exists(_tableDirectory))
            {
                Log.Error($"[ConfigManager] Config table directory not found: {_tableDirectory}");
                return;
            }

            var jsonPaths = Directory.GetFiles(_tableDirectory, "*.json", SearchOption.AllDirectories);
            if (jsonPaths == null || jsonPaths.Length == 0)
            {
                Log.Warning("[ConfigManager] No config JSON files found.");
                return;
            }

            foreach (var jsonPath in jsonPaths)
            {
                string fileName = Path.GetFileNameWithoutExtension(jsonPath);
                fileName = ToPascalCase(fileName);
                fileName = $"GameConfig.{fileName}ConfigTable";
                Type configType = TypeHelper.GetType(fileName, "XFramework");
                if (configType == null)
                {
                    Log.Error($"[ConfigManager] Config type {fileName} not found.");
                    continue;
                }
                await LoadConfigAsync(jsonPath, configType);
            }
            Log.Debug("[ConfigManager] Preload tables finished.");
        }

        /// <summary>
        /// 根据ID获取配置表数据
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <param name="id">配置ID</param>
        /// <returns>配置对象</returns>
        public T GetConfig<T>(int id) where T : IConfig
        {
            if (_tableMap.TryGetValue(typeof(T), out var table))
            {
                if (table is Dictionary<int, T> t && t.TryGetValue(id, out var value))
                {
                    return value;
                }
            }

            Log.Error($"[ConfigManager] Config not found. Type: {typeof(T)}, Id: {id}");
            return default;
        }

        /// <summary>
        /// 获取整个配置表
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <returns>配置文件实例</returns>
        public Dictionary<int, T> GetTable<T>() where T : IConfig
        {
            Type tableType = typeof(T);

            if (_tableMap.TryGetValue(tableType, out var table))
            {
                return table as Dictionary<int, T>;
            }

            Log.Error($"[ConfigManager] Table not found: {tableType}");
            return null;
        }

        /// <summary>
        /// 异步加载配置文件
        /// </summary>
        /// <typeparam name="T">配置文件类型</typeparam>
        /// <param name="filePath">配置文件路径</param>
        /// <param name="isCover">是否覆盖已加载的配置</param>
        public async UniTask LoadConfigAsync<T>(string filePath, bool isCover = false) where T : IConfig
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("Config file path cannot be null or empty.", nameof(filePath));
            }

            Type configType = typeof(T);

            if (_tableMap.TryGetValue(configType, out var _))
            {
                if (isCover)
                {
                    Log.Debug($"[ConfigManager] Duplicate config load attempt, covering it:" +
                        $"Type: {configType}, File: {filePath}");
                }
                else
                {
                    Log.Warning($"[ConfigManager] Duplicate config load attempt, skip it:" +
                        $"Type: {configType}, File: {filePath}");
                    return;
                }
            }

            string jsonContent = await ReadJsonFileAsync(filePath);
            if (jsonContent == null)
            {
                Log.Error($"[ConfigManager] Failed to read config file: {filePath}");
                return;
            }

            List<T> configs = JsonConvert.DeserializeObject<List<T>>(jsonContent);

            var map = new Dictionary<int, T>();
            foreach (var config in configs)
            {
                map[config.Id] = config;
            }

            _tableMap[typeof(T)] = map; // 缓存配置对象
        }

        /// <summary>
        /// 异步加载配置文件
        /// </summary>
        /// <param name="filePath">配置文件路径</param>
        /// <param name="tableType">配置表类型</param>
        /// <param name="isCover">是否覆盖已加载的配置</param>
        public async UniTask LoadConfigAsync(string filePath, Type tableType, bool isCover = false)
        {
            if (tableType == null)
            {
                throw new ArgumentNullException(nameof(tableType), "Config type cannot be null.");
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("Config file path cannot be null or empty.", nameof(filePath));
            }

            if (_tableMap.TryGetValue(tableType, out var _))
            {
                if (isCover)
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

            string jsonContent = await ReadJsonFileAsync(filePath);
            if (jsonContent == null)
            {
                Log.Error($"[ConfigManager] Failed to read config file: {filePath}");
                return;
            }

            var listType = typeof(List<>).MakeGenericType(tableType);
            var configs = JsonConvert.DeserializeObject(jsonContent, listType);

            var mapType = typeof(Dictionary<,>).MakeGenericType(typeof(int), tableType);
            var map = Activator.CreateInstance(mapType);

            var addMethod = mapType.GetMethod("Add");
            var idProperty = tableType.GetProperty("Id");

            foreach (var config in (System.Collections.IEnumerable)configs)
            {
                var id = (int)idProperty.GetValue(config);
                addMethod.Invoke(map, new object[] { id, config });
            }

            _tableMap[tableType] = map; // 缓存配置对象
        }

        /// <summary>
        /// 清除所有配置文件缓存
        /// </summary>
        public void ClearAllConfigCache()
        {
            _tableMap.Clear();
            Log.Debug("[ConfigManager] All config caches cleared.");
        }

        private async UniTask<string> ReadJsonFileAsync(string filePath)
        {
            string jsonContent;
            if (!FileHelper.Exists(filePath))
            {
                Log.Error($"[ConfigManager] Config file not found: {filePath}");
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
                    Log.Error($"[ConfigManager] Failed to read config file from web request: {result.Error}");
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

        private string ToPascalCase(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            str = str.Replace("_", " ").Replace("-", " ");
            var words = str.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
            }
            return string.Join("", words);
        }
    }
}