using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using XFramework.Utils;

namespace XFramework.XAsset
{
    public sealed class AssetBundleCollectorWindow : EditorWindow
    {
        private readonly List<EditorWindow> _subWindows = new();
        private readonly List<AssetGroup> _groups = new();

        private bool _ignoreCase = false;
        string _packStrategyOption = "PackByGroup";

        private int _selectedGroupIndex = -1;
        private Vector2 _groupScrollPosition = Vector2.zero;
        Texture2D _collectorAssetsButtonBackground;
        GUIStyle _collectorAssetsButtonStyle;

        [MenuItem("XAsset/AssetBundle Collector")]
        public static void ShowWindow()
        {
            GetWindow<AssetBundleCollectorWindow>("AssetBundle Collector");
        }

        private void OnEnable()
        {
            // 收集器「查看资源」按钮的背景
            _collectorAssetsButtonBackground = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            var color = new Color(0f, 0f, 0f, 0f);
            _collectorAssetsButtonBackground.SetPixel(0, 0, color);
            _collectorAssetsButtonBackground.Apply();
        }

        private void OnGUI()
        {
            // 收集器「查看资源」按钮的样式
            if (_collectorAssetsButtonStyle == null)
            {
                _collectorAssetsButtonStyle = new GUIStyle(GUI.skin.button);
                _collectorAssetsButtonStyle.normal.background = _collectorAssetsButtonBackground;
                _collectorAssetsButtonStyle.normal.scaledBackgrounds = new Texture2D[] { _collectorAssetsButtonBackground };
                _collectorAssetsButtonStyle.hover.background = _collectorAssetsButtonBackground;
                _collectorAssetsButtonStyle.hover.scaledBackgrounds = new Texture2D[] { _collectorAssetsButtonBackground };
                _collectorAssetsButtonStyle.active.background = _collectorAssetsButtonBackground;
                _collectorAssetsButtonStyle.active.scaledBackgrounds = new Texture2D[] { _collectorAssetsButtonBackground };
                _collectorAssetsButtonStyle.alignment = TextAnchor.MiddleLeft;
                _collectorAssetsButtonStyle.fontSize -= 1;
            }

            using (new EditorGUILayout.HorizontalScope("box"))
            {
                Color originColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.green;

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Save", GUILayout.Width(100)))
                {
                    Save();
                }

                GUI.backgroundColor = originColor;
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {

                EditorGUILayout.LabelField("Global Settings", EditorStyles.boldLabel);

                _ignoreCase = EditorGUILayout.Toggle("Ignore Case", _ignoreCase);
            }

            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandHeight(true)))
            {
                using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(250), GUILayout.ExpandHeight(true)))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Groups", EditorStyles.boldLabel);

                        if (GUILayout.Button("+", GUILayout.Width(35)))
                        {
                            var window = AssetBundleCollectorAddGroupWindow.ShowWindow(this);
                            _subWindows.Add(window);
                        }

                        using (new EditorGUI.DisabledGroupScope(_selectedGroupIndex < 0 || _selectedGroupIndex >= _groups.Count))
                        {
                            if (GUILayout.Button("-", GUILayout.Width(35)))
                            {
                                RemoveGroup();
                            }
                        }
                    }

                    string[] groupFullNames = new string[_groups.Count];
                    for (int i = 0; i < _groups.Count; i++)
                    {
                        groupFullNames[i] = _groups[i].FullName;
                    }
                    using (var scrollVeiw = new EditorGUILayout.ScrollViewScope(_groupScrollPosition, GUILayout.ExpandHeight(true)))
                    {
                        _groupScrollPosition = scrollVeiw.scrollPosition;
                        _selectedGroupIndex = GUILayout.SelectionGrid(_selectedGroupIndex, groupFullNames, 1);
                    }
                }

                using (new EditorGUILayout.VerticalScope("box", GUILayout.ExpandHeight(true)))
                {
                    GUILayout.Label("Collectors", EditorStyles.boldLabel);

                    if (_selectedGroupIndex >= 0 && _selectedGroupIndex < _groups.Count)
                    {
                        _groups[_selectedGroupIndex].Name = EditorGUILayout.TextField("Group Name", _groups[_selectedGroupIndex].Name);
                        _groups[_selectedGroupIndex].Description = EditorGUILayout.TextField("Description", _groups[_selectedGroupIndex].Description);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("Pack Strategy", GUILayout.Width(150));
                            if (EditorGUILayout.DropdownButton(new GUIContent(_packStrategyOption), FocusType.Keyboard))
                            {
                                // 创建下拉菜单
                                var menu = new GenericMenu();
                                menu.AddItem(new GUIContent("PackByGroup"), _packStrategyOption == "PackByGroup", OnOptionSelected, PackStrategy.PackByGroup);
                                menu.AddItem(new GUIContent("PackByCollector"), _packStrategyOption == "PackByCollector", OnOptionSelected, PackStrategy.PackByCollector);
                                menu.AddItem(new GUIContent("PackSeparately"), _packStrategyOption == "PackSeparately", OnOptionSelected, PackStrategy.PackSeparately);
                                menu.ShowAsContext(); // 显示菜单
                            }
                        }

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("+", GUILayout.Width(35)))
                            {
                                AddCollector();
                            }
                        }

                        foreach (var collector in _groups[_selectedGroupIndex].Collectors)
                        {
                            using (new EditorGUILayout.VerticalScope("box"))
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    if (GUILayout.Button("-", GUILayout.Width(35)))
                                    {

                                    }

                                    EditorGUILayout.LabelField("Collector", GUILayout.Width(65));
                                }

                                using (new EditorGUILayout.VerticalScope())
                                {
                                    collector.Target = EditorGUILayout.ObjectField(collector.Target, typeof(UnityEngine.Object), false);
                                    EditorGUILayout.LabelField(collector.Target == null ? string.Empty : collector.TargetPath);
                                }

                                string arrow = collector.IsDropDownExpanded ? "▼" : "▶";
                                if (GUILayout.Button($"{arrow}  Assets", _collectorAssetsButtonStyle))
                                {
                                    collector.IsDropDownExpanded = !collector.IsDropDownExpanded;
                                    if (collector.Target != null && collector.IsDropDownExpanded)
                                    {
                                        string[] assetPaths = collector.GetAllAssetPaths();

                                        foreach (string path in assetPaths)
                                        {
                                            Log.Debug(path);
                                            EditorGUILayout.LabelField(path);
                                        }
                                    }
                                }
                            }

                            EditorGUILayout.Separator();
                        }
                    }
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var window in _subWindows)
            {
                window.Close();
            }
            _subWindows.Clear();

            Save();
        }

        public void AddGroup(string newGroupName, string newGroupDescription)
        {
            if (!string.IsNullOrEmpty(newGroupName))
            {
                foreach (AssetGroup group in _groups)
                {
                    if (group.Name == newGroupName)
                    {
                        Log.Warning($"[XAsset] AddGroup failed. Group {newGroupName} already exists.");
                        return;
                    }
                }

                Log.Info($"[XAsset] Add Group {newGroupName}.");
                var newGroup = new AssetGroup(newGroupName, newGroupDescription);
                _groups.Add(newGroup);
            }
        }

        private void RemoveGroup()
        {
        }

        private void AddCollector()
        {
            AssetGroup currentGroup = _groups[_selectedGroupIndex];
            currentGroup.Collectors.Add(new AssetCollector());
        }

        private void Save()
        {
            Log.Info("[XAsset] AssetBundle Collector Saved!");
        }

        private void OnOptionSelected(object option)
        {
            PackStrategy strategy = (PackStrategy)option;
            _groups[_selectedGroupIndex].PackStrategy = strategy;
            _packStrategyOption = strategy.ToString();
            Log.Info($"[XAsset] Pack Strategy changed to {strategy}.");
        }
    }

    public class AssetBundleCollectorAddGroupWindow : EditorWindow
    {
        private string _groupName = string.Empty;
        private string _groupDescription = string.Empty;
        private AssetBundleCollectorWindow _mainWindow;

        public static AssetBundleCollectorAddGroupWindow ShowWindow(AssetBundleCollectorWindow collectorWindow)
        {
            AssetBundleCollectorAddGroupWindow window = GetWindow<AssetBundleCollectorAddGroupWindow>("Add Group");
            window._mainWindow = collectorWindow;
            window.position = new Rect(collectorWindow.position.center.x - 200, collectorWindow.position.center.y - 60, 400, 120);
            return window;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Group Name");
            _groupName = GUILayout.TextField(_groupName);

            EditorGUILayout.LabelField("Group Description");
            _groupDescription = GUILayout.TextField(_groupDescription);

            EditorGUILayout.Separator();

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledGroupScope(string.IsNullOrEmpty(_groupName)))
                {
                    if (GUILayout.Button("OK", "LargeButton"))
                    {
                        _mainWindow.AddGroup(_groupName, _groupDescription);
                        Close();
                    }
                }
                if (GUILayout.Button("Cancel", "LargeButton"))
                {
                    Close();
                }
            }
        }
    }
}