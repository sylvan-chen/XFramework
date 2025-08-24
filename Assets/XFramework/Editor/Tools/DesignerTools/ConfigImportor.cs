using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 策划工具 - 配表数据类生成工具
/// </summary>
public class ConfigClassGenerator : EditorWindow
{
    private const string JSON_DIR_KEY = "ConfigGenerator_JsonDir";
    private const string OUTPUT_DIR_KEY = "ConfigGenerator_OutputDir";
    private const string NAMESPACE_KEY = "ConfigGenerator_Namespace";
    private const string SEARCH_OPTION_KEY = "ConfigGenerator_SearchOption";

    private SearchOption _searchOption = SearchOption.TopDirectoryOnly;
    private string _jsonDirectory = "Assets/StreamingAssets/Schemes";
    private string _outputDirectory = "Assets/Configs/GeneratedClasses";
    private string _namespaceName = "GameConfig";

    [MenuItem("Tools/策划工具/配表数据类生成工具")]
    private static void ShowWindow()
    {
        var window = GetWindow<ConfigClassGenerator>();
        window.titleContent = new GUIContent("配表数据类生成工具");
        window.minSize = new Vector2(600, 600);
        window.Show();
    }

    private void OnEnable()
    {
        // 加载设置
        _jsonDirectory = EditorPrefs.GetString(JSON_DIR_KEY, "Assets/StreamingAssets/Schemes");
        _outputDirectory = EditorPrefs.GetString(OUTPUT_DIR_KEY, "Assets/Configs/GeneratedClasses");
        _namespaceName = EditorPrefs.GetString(NAMESPACE_KEY, "GameConfig");
        _searchOption = (SearchOption)EditorPrefs.GetInt(SEARCH_OPTION_KEY, (int)SearchOption.TopDirectoryOnly);
    }

    private void OnDisable()
    {
        // 保存设置
        EditorPrefs.SetString(JSON_DIR_KEY, _jsonDirectory);
        EditorPrefs.SetString(OUTPUT_DIR_KEY, _outputDirectory);
        EditorPrefs.SetString(NAMESPACE_KEY, _namespaceName);
        EditorPrefs.SetInt(SEARCH_OPTION_KEY, (int)_searchOption);
    }

    void OnGUI()
    {
        GUILayout.Label("基本设置", EditorStyles.boldLabel);

        EditorGUILayout.Space(5);

        using (new EditorGUILayout.HorizontalScope())
        {
            _jsonDirectory = EditorGUILayout.TextField("JSON目录:", _jsonDirectory);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("选择JSON目录", _jsonDirectory, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // Assets下的目录转换成相对路径
                    if (selectedPath.StartsWith(Application.dataPath))
                        _jsonDirectory = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    else
                        _jsonDirectory = selectedPath;
                }
            }
        }

        EditorGUILayout.Space(5);

        _searchOption = (SearchOption)EditorGUILayout.EnumPopup("搜索选项:", _searchOption);

        EditorGUILayout.Space(5);

        using (new EditorGUILayout.HorizontalScope())
        {
            _outputDirectory = EditorGUILayout.TextField("输出目录:", _outputDirectory);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("选择输出目录", _outputDirectory, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // Assets下的目录转换成相对路径
                    if (selectedPath.StartsWith(Application.dataPath))
                        _outputDirectory = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    else
                        _outputDirectory = selectedPath;
                }
            }
        }

        EditorGUILayout.Space(5);

        _namespaceName = EditorGUILayout.TextField("生成类的命名空间:", _namespaceName);

        EditorGUILayout.Space(10);

        // 信息检查
        bool jsonDirExists = Directory.Exists(_jsonDirectory);
        if (!jsonDirExists)
        {
            EditorGUILayout.HelpBox($"JSON目录不存在: {_jsonDirectory}", MessageType.Warning);
        }
        else
        {
            string[] jsonFiles = Directory.GetFiles(_jsonDirectory, "*.json", _searchOption);
            string searchInfo = _searchOption == SearchOption.TopDirectoryOnly ? "（仅顶层目录）" : "（包含子目录）";
            EditorGUILayout.HelpBox($"找到 {jsonFiles.Length} 个JSON配置文件 {searchInfo}", MessageType.Info);
        }

        bool namespaceNameValid = !string.IsNullOrWhiteSpace(_namespaceName);
        if (!namespaceNameValid)
        {
            EditorGUILayout.HelpBox("命名空间不能为空", MessageType.Warning);
        }

        EditorGUILayout.Space(10);

        GUI.enabled = jsonDirExists && namespaceNameValid;
        if (GUILayout.Button("生成数据类", GUILayout.Height(35)))
        {
            Debug.Log($"[ConfigClassGenerator] 生成数据类: JSON目录={_jsonDirectory}, 输出目录={_outputDirectory}, 命名空间={_namespaceName}");
            GenerateClasses();
        }
        GUI.enabled = true;

        EditorGUILayout.Space(10);

        if (GUILayout.Button("重置为默认设置"))
        {
            _jsonDirectory = "Assets/StreamingAssets/Schemes";
            _outputDirectory = "Assets/Configs/GeneratedClasses";
            _namespaceName = "GameConfig";
            _searchOption = SearchOption.TopDirectoryOnly;
        }
    }

    private void GenerateClasses()
    {
        if (!Directory.Exists(_jsonDirectory))
        {
            Debug.LogError("[ConfigClassGenerator] JSON目录不存在: " + _jsonDirectory);
            return;
        }
        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }

        string[] files = Directory.GetFiles(_jsonDirectory, "*.json", _searchOption);

        if (files.Length == 0)
        {
            Debug.LogWarning($"[XFramework] [ConfigClassGenerator] 在目录 {_jsonDirectory} 中未找到JSON文件（搜索选项: {_searchOption}）");
            return;
        }

        int successCount = 0;
        float progressStep = 1.0f / files.Length;

        // Show progress bar
        EditorUtility.DisplayProgressBar("生成配表数据类", "开始生成...", 0f);

        try
        {
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                float progress = i * progressStep;
                string fileName = Path.GetFileNameWithoutExtension(file);

                EditorUtility.DisplayProgressBar("生成配表数据类", $"处理文件: {fileName} ({i + 1}/{files.Length})", progress);

                string jsonContent = File.ReadAllText(file);
                try
                {
                    JObject rootObj = JObject.Parse(jsonContent);
                    JArray array = rootObj["configs"] as JArray;
                    if (array.Count == 0)
                    {
                        Debug.LogWarning($"[ConfigClassGenerator] JSON文件为空数组: {file}");
                        continue;
                    }

                    JObject firstObj = array[0] as JObject;
                    if (firstObj == null)
                    {
                        Debug.LogWarning($"[ConfigClassGenerator] JSON文件未读取到有效对象: {file}");
                        continue;
                    }

                    string className = ToPascalCase(fileName);
                    string code = GenerateClassCode(className, firstObj);

                    string outputPath = Path.Combine(_outputDirectory, $"{className}.cs");
                    File.WriteAllText(outputPath, code, Encoding.UTF8);

                    Debug.Log($"[ConfigClassGenerator] 生成数据类: {className} -> {outputPath}");
                    successCount++;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[ConfigClassGenerator] 解析JSON文件失败: {file}\n{ex}");
                    continue;
                }
            }

            EditorUtility.DisplayProgressBar("生成配表数据类", "刷新资源数据库...", 1.0f);
            AssetDatabase.Refresh();

            Debug.Log($"[XFramework] [ConfigClassGenerator] 数据类生成完成，成功生成 {successCount}/{files.Length} 个类文件");
            EditorUtility.DisplayDialog("生成完成", $"成功生成 {successCount}/{files.Length} 个数据类\n输出目录: {_outputDirectory}", "确定");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ConfigClassGenerator] 生成数据类失败: {ex}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();
        Debug.Log("[ConfigClassGenerator] 数据类生成结束");
    }

    private string GenerateClassCode(string className, JObject jsonObj)
    {
        var fields = new List<string>();

        foreach (var property in jsonObj.Properties())
        {
            string propertyType = InferTypeName(property.Value);
            string propertyName = ToPascalCase(property.Name);
            fields.Add($"public {propertyType} {propertyName};");
        }

        var sb = new StringBuilder();

        sb.AppendLine("/// ------------------------------------------------------------------------------");
        sb.AppendLine("/// <auto-generated>");
        sb.AppendLine("/// Generated by ConfigClassGenerator, don't modify it manually.");
        sb.AppendLine("/// </auto-generated>");
        sb.AppendLine("/// ------------------------------------------------------------------------------");
        sb.AppendLine();
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        sb.AppendLine($"namespace {_namespaceName}");
        sb.AppendLine("{");
        sb.AppendLine("    [System.Serializable]");
        sb.AppendLine($"    public class {className}");
        sb.AppendLine("    {");
        sb.AppendLine($"        {string.Join("\n        ", fields)}");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    [System.Serializable]");
        sb.AppendLine($"    public class {className}Table");
        sb.AppendLine("    {");
        sb.AppendLine($"        public Dictionary<int, {className}> AllConfigs;");
        sb.AppendLine();
        sb.AppendLine($"        public {className} GetConfigById(int id)");
        sb.AppendLine("         {");
        sb.AppendLine("             return AllConfigs.TryGetValue(id, out var config) ? config : null;");
        sb.AppendLine("         }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string InferTypeName(JToken value)
    {
        return value.Type switch
        {
            JTokenType.Integer => "int",
            JTokenType.Float => "float",
            JTokenType.Boolean => "bool",
            JTokenType.String => "string",
            _ => "string"
        };
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
