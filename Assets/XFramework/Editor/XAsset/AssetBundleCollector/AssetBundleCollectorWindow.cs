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
        private int _selectedGroupIndex = -1;
        private Vector2 _groupScrollPosition = Vector2.zero;
        private Vector2 _collectorScrollPosition = Vector2.zero;
        Texture2D _groupButtonBackground;
        Texture2D _groupSelectedButtonBackground;
        GUIStyle _groupButtonStyle;
        Texture2D _collectorAssetsButtonBackground;
        GUIStyle _collectorAssetsButtonStyle;
        GUIStyle _collectorAssetsLabelStyle;

        [MenuItem("XAsset/AssetBundle Collector")]
        public static void ShowWindow()
        {
            GetWindow<AssetBundleCollectorWindow>("AssetBundle Collector");
        }

        private void OnEnable()
        {
            // 「组」按钮的背景
            _groupButtonBackground = new Texture2D(20, 100, TextureFormat.ARGB32, false);
            var color = new Color(0f, 0f, 0f, 0f);
            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j < 100; j++)
                {
                    _groupButtonBackground.SetPixel(i, j, color);
                }
            }
            _groupButtonBackground.Apply();

            // 选中「组」按钮的背景
            _groupSelectedButtonBackground = new Texture2D(20, 100, TextureFormat.ARGB32, false);
            color = new Color(0.5f, 0.5f, 0.5f, 1f);
            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j < 100; j++)
                {
                    _groupSelectedButtonBackground.SetPixel(i, j, color);
                }
            }
            _groupSelectedButtonBackground.Apply();

            // 收集器「查看资源」按钮的背景
            _collectorAssetsButtonBackground = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            color = new Color(0f, 0f, 0f, 0f);
            _collectorAssetsButtonBackground.SetPixel(0, 0, color);
            _collectorAssetsButtonBackground.Apply();
        }

        private void OnGUI()
        {
            // 「组」按钮的样式
            if (_groupButtonStyle == null)
            {
                _groupButtonStyle = new GUIStyle(GUI.skin.button);
                _groupButtonStyle.normal.background = _groupButtonBackground;
                _groupButtonStyle.normal.scaledBackgrounds = new Texture2D[] { _groupButtonBackground };
                _groupButtonStyle.onNormal.background = _groupSelectedButtonBackground;
                _groupButtonStyle.onNormal.scaledBackgrounds = new Texture2D[] { _groupSelectedButtonBackground };
                _groupButtonStyle.hover.background = _groupButtonBackground;
                _groupButtonStyle.hover.scaledBackgrounds = new Texture2D[] { _groupButtonBackground };
                _groupButtonStyle.active.background = _groupSelectedButtonBackground;
                _groupButtonStyle.active.scaledBackgrounds = new Texture2D[] { _groupSelectedButtonBackground };
                _groupButtonStyle.focused.background = _groupSelectedButtonBackground;
                _groupButtonStyle.focused.scaledBackgrounds = new Texture2D[] { _groupSelectedButtonBackground };
                _groupButtonStyle.alignment = TextAnchor.MiddleCenter;
            }

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
            }

            // 收集器「查看资源」标签的样式
            if (_collectorAssetsLabelStyle == null)
            {
                _collectorAssetsLabelStyle = new GUIStyle(GUI.skin.label);
                _collectorAssetsLabelStyle.alignment = TextAnchor.MiddleLeft;
                _collectorAssetsLabelStyle.fontSize -= 1;
                _collectorAssetsLabelStyle.padding.left = 24;
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
                        _selectedGroupIndex = GUILayout.SelectionGrid(_selectedGroupIndex, groupFullNames, 1, _groupButtonStyle);
                    }
                }

                using (new EditorGUILayout.VerticalScope("box", GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true)))
                {
                    if (_selectedGroupIndex >= 0 && _selectedGroupIndex < _groups.Count)
                    {
                        GUILayout.Label("Group Settings", EditorStyles.boldLabel);

                        _groups[_selectedGroupIndex].Name = EditorGUILayout.TextField("Group Name", _groups[_selectedGroupIndex].Name);
                        _groups[_selectedGroupIndex].Description = EditorGUILayout.TextField("Description", _groups[_selectedGroupIndex].Description);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("Pack Strategy", GUILayout.Width(150));
                            string packStrategyOption = _groups[_selectedGroupIndex].PackStrategy.ToString();
                            if (EditorGUILayout.DropdownButton(new GUIContent(packStrategyOption), FocusType.Keyboard))
                            {
                                var menu = new GenericMenu();
                                menu.AddItem(new GUIContent("PackByGroup"), packStrategyOption == "PackByGroup", OnPackStrategySelected, PackStrategy.PackByGroup);
                                menu.AddItem(new GUIContent("PackByCollector"), packStrategyOption == "PackByCollector", OnPackStrategySelected, PackStrategy.PackByCollector);
                                menu.AddItem(new GUIContent("PackSeparately"), packStrategyOption == "PackSeparately", OnPackStrategySelected, PackStrategy.PackSeparately);
                                menu.ShowAsContext();
                            }
                        }

                        EditorGUILayout.Separator();
                        EditorGUILayout.Separator();

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Collectors", EditorStyles.boldLabel);
                            if (GUILayout.Button("+", GUILayout.Width(35)))
                            {
                                AddCollector();
                            }
                            GUILayout.FlexibleSpace();
                        }

                        using (var scrollView = new EditorGUILayout.ScrollViewScope(_collectorScrollPosition, GUILayout.ExpandHeight(true)))
                        {
                            _collectorScrollPosition = scrollView.scrollPosition;
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

                                        EditorGUILayout.LabelField(collector.Target == null ? string.Empty : $"({collector.TargetPath})");

                                        GUILayout.FlexibleSpace();
                                    }

                                    collector.Target = EditorGUILayout.ObjectField(collector.Target, typeof(UnityEngine.Object), false);

                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        string addressStrategyOption = collector.AddressStrategy.ToString();
                                        if (EditorGUILayout.DropdownButton(new GUIContent(addressStrategyOption), FocusType.Keyboard, GUILayout.Width(200)))
                                        {
                                            var menu = new GenericMenu();
                                            menu.AddItem(new GUIContent("AddressByFileName"), addressStrategyOption == "AddressByFileName", OnAddressStrategySelected, new AddressStrategyOptionData(AddressStrategy.AddressByFileName, collector));
                                            menu.AddItem(new GUIContent("AddressByGroupAndFileName"), addressStrategyOption == "AddressByGroupAndFileName", OnAddressStrategySelected, new AddressStrategyOptionData(AddressStrategy.AddressByGroupAndFileName, collector));
                                            menu.ShowAsContext();
                                        }
                                    }

                                    string arrow = collector.IsDropDownExpanded ? "▼" : "▶";
                                    if (GUILayout.Button($"{arrow}  Assets", _collectorAssetsButtonStyle))
                                    {
                                        collector.IsDropDownExpanded = !collector.IsDropDownExpanded;
                                    }
                                    if (collector.Target != null && collector.IsDropDownExpanded)
                                    {
                                        string[] assetPaths = collector.GetAllAssetPaths();

                                        foreach (string path in assetPaths)
                                        {
                                            EditorGUILayout.LabelField(path, _collectorAssetsLabelStyle);
                                        }
                                    }
                                }

                                EditorGUILayout.Separator();
                            }
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

        private void OnPackStrategySelected(object option)
        {
            PackStrategy strategy = (PackStrategy)option;
            _groups[_selectedGroupIndex].PackStrategy = strategy;
        }

        public void OnAddressStrategySelected(object option)
        {
            AddressStrategyOptionData data = (AddressStrategyOptionData)option;
            data.Collector.AddressStrategy = data.Strategy;
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

    public readonly struct AddressStrategyOptionData
    {
        public readonly AddressStrategy Strategy;
        public readonly AssetCollector Collector;

        public AddressStrategyOptionData(AddressStrategy strategy, AssetCollector collector)
        {
            Strategy = strategy;
            Collector = collector;
        }
    }
}