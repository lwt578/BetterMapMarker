using BetterMapMarker;
using Duckov.MiniMaps;
using Duckov.MiniMaps.UI;
using Duckov.Scenes;
using Duckov.UI;
using Duckov.UI.MainMenu;
using ItemStatsSystem;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;
using UnityEngine.Timeline;
using UnityEngine.UIElements;


namespace BetterMapMarker
{

    #region 设置标记图标和颜色

    public enum LootboxState
    {
        Opened,
        Closed
    }

    public static class MarkerVisuals
    {

        public static Sprite SetMarkerIcon(InteractableLootbox Lootbox)
        {
            var icon = MapMarkerManager.Icons[6];//默认游戏自带箱子图标

            if (Lootbox.name.Contains("Hidden", StringComparison.OrdinalIgnoreCase))
                icon = MapMarkerManager.Icons[9];
            if (Lootbox.name.Contains("Enemy", StringComparison.OrdinalIgnoreCase))
                icon = MapMarkerManager.Icons[10];
            if (Lootbox.name.Contains("Clone", StringComparison.OrdinalIgnoreCase) &&
                !Lootbox.name.Contains("Enemy", StringComparison.OrdinalIgnoreCase))
                icon = MapMarkerManager.Icons[5];
            if (Lootbox.name.Contains("Formula", StringComparison.OrdinalIgnoreCase))
                icon = MapMarkerManager.Icons[7];
            if (Lootbox.name.Contains("Lab", StringComparison.OrdinalIgnoreCase))
                icon = MapMarkerManager.Icons[12];//自定义图标（要先添加）

            return icon;
        }


        //箱子是黄色，打开后颜色变成白色
        public static Color SetMarkerColor(LootboxState State)
        {
            if (State == LootboxState.Closed)
                return Color.yellow;
            else
                return Color.white;
        }

    }

    #endregion


    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private sealed class LootboxMarker
        {
            public InteractableLootbox? Lootbox;
            public GameObject? MarkerObject;
            public SimplePointOfInterest? Poi;
            public string? DisplayName;
            public LootboxState State;
            public Color Color;
        }


        /// <summary>
        /// Map a character to its marker.
        /// </summary>
        private readonly Dictionary<InteractableLootbox, LootboxMarker> _boxmarkers =
            new Dictionary<InteractableLootbox, LootboxMarker>();


        private bool _showAll = true;  // 默认显示所有箱子
        private bool _showOnlyJLab = false;  // 只显示JLab箱等高价值箱子
        private bool _showNone = false;  // 不显示任何标记

        // 新增：类型过滤相关字段
        private bool _useTypeFilter = false;
        private string _selectedFilterType = null;

        internal static bool HasSelectedUpdate { get; private set; }

        private LootboxMarkerUI _LootboxMarkerUI;  // UI实例
        private bool _isUIVisible = true;  // UI是否可见

        // 高价值箱子目录
        private readonly List<string> _specialLootboxNames = new List<string> {
           "Starter", "Lab","Lux","Cash","Hang","Formula","Weapon","Clone","Hidden","Technical","Bullet"
        };

        private bool _mapActive;
        private float _scanCooldown;
        private const float ScanIntervalSeconds = 1f;


        // 修改 ShouldShow 以支持类型过滤
        private bool ShouldShow(InteractableLootbox lootbox)
        {

            if (_showNone) return false;

            // 类型过滤模式：只看类型名是否匹配
            if (_useTypeFilter)
            {
                string type = GetLootboxType(lootbox);
                return type.Equals(_selectedFilterType, StringComparison.OrdinalIgnoreCase);
            }

            // 高价值模式
            if (_showOnlyJLab)
            {
                foreach (var specialName in _specialLootboxNames)
                {
                    if (lootbox.name.Contains(specialName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                return false;
            }

            // 全部显示模式
            return _showAll;
        }

        // --- 新增：根据箱子名称获取类型 ---
        private string GetLootboxType(InteractableLootbox lootbox)
        {
            return string.IsNullOrEmpty(lootbox.InteractName) ? "未知" : lootbox.InteractName;
        }

        // --- 新增：获取当前场景中所有存在的箱子类型（去重排序）---
        public List<string> GetAllLootboxTypes()
        {

            HashSet<string> types = new HashSet<string>();
            var lootboxes = UnityEngine.Object.FindObjectsOfType<InteractableLootbox>();
            foreach (var lootbox in lootboxes)
            {
                if (lootbox == null) continue;
                // 过滤不需要标记的箱子（同 ScanLootboxes 逻辑）
                if (lootbox.name.Contains("PetProxy", StringComparison.OrdinalIgnoreCase) ||
                    lootbox.name.Contains("PlayerStorage", StringComparison.OrdinalIgnoreCase) ||
                    lootbox.Inventory.GetItemCount() == 0)
                    continue;

                string type = GetLootboxType(lootbox);
                if (!string.IsNullOrEmpty(type))
                    types.Add(type);
            }
            List<string> sorted = new List<string>(types);
            sorted.Sort();
            return sorted;
        }



        // --- 新增：设置类型过滤 ---
        public void SetTypeFilter(string type)
        {
            // 【必须添加】当用户通过下拉菜单选择特定类型时，强制关闭“不显示”状态
            _showNone = false;
            _showAll = false;
            _showOnlyJLab = false;

            if (string.IsNullOrEmpty(type))
            {
                _useTypeFilter = false;
                _selectedFilterType = null;
            }
            else
            {
                _useTypeFilter = true;
                _selectedFilterType = type;
            }

            HasSelectedUpdate = true;

        }



        // 修改原有的显示模式设置，清除类型过滤
        public void SetShowAll()
        {
            _showAll = true;
            _showOnlyJLab = false;
            _showNone = false;
            _useTypeFilter = false;
            _selectedFilterType = null;

            HasSelectedUpdate = true;
        }

        public void SetShowJLab()
        {
            _showAll = false;
            _showOnlyJLab = true;
            _showNone = false;
            _useTypeFilter = false;
            _selectedFilterType = null;

            HasSelectedUpdate = true;
        }

        public void SetShowNone()
        {
            _showAll = false;
            _showOnlyJLab = false;
            _showNone = true;
            _useTypeFilter = false;
            _selectedFilterType = null;

            HasSelectedUpdate = true;
            ResetMarkers();
        }

        internal static void ApplySelectedChanges()
        {
            if (!HasSelectedUpdate)
                return;

            HasSelectedUpdate = false;
        }

        private void CreateSimpleUI()
        {
            if (_LootboxMarkerUI != null) return;

            try
            {
                var mapView = MiniMapView.Instance;
                if (mapView == null) return;

                // 创建UI
                var uiGO = new GameObject("SimpleLootboxUI", typeof(RectTransform));
                uiGO.transform.SetParent(mapView.transform, false);
                _LootboxMarkerUI = uiGO.AddComponent<LootboxMarkerUI>();
                _LootboxMarkerUI.Initialize(this);

                Debug.Log("箱子标记UI已创建");
            }
            catch (Exception ex)
            {
                Debug.LogError($"创建箱子标记UI失败: {ex.Message}");
            }
        }

        private void DestroySimpleUI()
        {
            try
            {
                if (_LootboxMarkerUI != null)
                {
                    Destroy(_LootboxMarkerUI.gameObject);
                    _LootboxMarkerUI = null;
                }
            }
            catch { }
        }

        // 切换UI可见性
        public void ToggleUIVisibility()
        {
            _isUIVisible = !_isUIVisible;
            if (_LootboxMarkerUI != null)
            {
                _LootboxMarkerUI.SetVisible(_isUIVisible);
            }
        }

        void OnEnable()
        {

            Debug.Log("Mod启用");

            LevelManager.OnAfterLevelInitialized += AddSelfIconOnMaker;

            View.OnActiveViewChanged += OnActiveViewChanged;
            SceneLoader.onStartedLoadingScene += OnSceneStartedLoading;
            SceneLoader.onFinishedLoadingScene += OnSceneFinishedLoading;

            if (IsMapOpen())
            {
                BeginTracking();
                CreateSimpleUI();

            }

        }


        void OnDisable()
        {
            LevelManager.OnAfterLevelInitialized -= AddSelfIconOnMaker;

            View.OnActiveViewChanged -= OnActiveViewChanged;
            SceneLoader.onStartedLoadingScene -= OnSceneStartedLoading;
            SceneLoader.onFinishedLoadingScene -= OnSceneFinishedLoading;

            EndTracking();
            DestroySimpleUI();

        }

        private void OnSceneStartedLoading(SceneLoadingContext context)
        {
            // Clear markers when leaving the current scene
            ResetMarkers();
        }

        // 在场景加载完成后刷新 UI 下拉菜单
        private void OnSceneFinishedLoading(SceneLoadingContext context)
        {
            StartCoroutine(DelayedScan());
            if (_LootboxMarkerUI != null)
            {
                _LootboxMarkerUI.RefreshTypeDropdown(); // 刷新类型列表
            }

        }

        private System.Collections.IEnumerator DelayedScan()
        {
            yield return new WaitForSeconds(0.5f);
            if ((_mapActive || IsMapOpen()) && !_showNone)
                ScanLootboxes();
        }

        private static bool IsMapOpen()
        {
            var view = MiniMapView.Instance;
            return view != null && View.ActiveView == view;
        }

        private void OnActiveViewChanged()
        {
            if (IsMapOpen())
            {
                BeginTracking();

                if (_LootboxMarkerUI != null)
                {
                    _LootboxMarkerUI.SetVisible(_isUIVisible);
                }
            }

            else
            {
                EndTracking();
                // 隐藏UI，但保持对象
                if (_LootboxMarkerUI != null)
                {
                    _LootboxMarkerUI.SetVisible(false);
                }
            }
        }
        // 在地图打开时刷新下拉菜单
        private void BeginTracking()
        {
            _mapActive = true;
            CreateSimpleUI();
            if (_LootboxMarkerUI != null)
            {
                _LootboxMarkerUI.RefreshTypeDropdown();
                _LootboxMarkerUI.SetVisible(_isUIVisible);
            }

            RemoveLootboxMarkers();
            ScanLootboxes();
            _scanCooldown = ScanIntervalSeconds;
            Debug.Log("开始追踪箱子位置");

        }

        private void EndTracking()
        {
            if (!_mapActive)
                return;
            _mapActive = false;
            Debug.Log("停止追踪");
            // ResetMarkers();

        }

        private static bool IsLootboxValid(InteractableLootbox lootbox)
        {

            if (lootbox == null)
                return false;

            var go = lootbox.gameObject;
            if (!go.scene.IsValid() || !go.scene.isLoaded)
                return false;

            return true;
        }

        private void RemoveLootboxMarkers()
        {
            List<InteractableLootbox> toRemove = new List<InteractableLootbox>();
            foreach (var kvp in _boxmarkers)
            {
                var box = kvp.Key;
                // 如果：箱子本身被销毁了 OR 箱子空了 OR UI筛选不匹配
                if (box == null || IsLootboxEmpty(box) || !ShouldShow(box))
                {
                    toRemove.Add(box);
                }
            }
            foreach (var box in toRemove)
            {
                DestroyMarker(box);
            }
        }

        private void ScanLootboxes()
        {
            

            // 1. 如果处于“不显示任何标记”模式，销毁标记并返回
            
            if (_showNone)
            {
                ResetMarkers();
                return;
            }


            // 2. 正常处理符合条件的箱子
            var lootboxes = UnityEngine.Object.FindObjectsOfType<InteractableLootbox>();
            Debug.Log($"扫描到 {lootboxes.Length} 个箱子");

            GetAllLootboxTypes();
            RemoveLootboxMarkers();

            foreach (var lootbox in lootboxes)
            {
                if (lootbox == null || lootbox.Inventory == null) continue;

                // 屏蔽无关容器
                if (lootbox.name.Contains("PetProxy", StringComparison.OrdinalIgnoreCase) ||
                    lootbox.name.Contains("PlayerStorage", StringComparison.OrdinalIgnoreCase)||
                    IsLootboxEmpty(lootbox))
                    continue;


                AddOrUpdateMarker(lootbox);

            }
        }



        private void AddOrUpdateMarker(InteractableLootbox lootbox)
        {

            if (!IsLootboxValid(lootbox))  return;

            var displayName = GetDisplayName(lootbox);
            var currentState = GetLootboxState(lootbox);


            if (_boxmarkers.TryGetValue(lootbox, out var marker))
            {
                // 检查箱子是否被打开了或者空了，或者UI筛选不匹配了，如果是则销毁标记
                if (IsLootboxEmpty(marker.Lootbox) || !ShouldShow(marker.Lootbox))
                {
                    DestroyMarker(lootbox);
                    return;
                }
                else
                {
                    if (marker.State != currentState)
                        UpdateMarker(marker, displayName);
                    else
                        return;
                }
            }

            if (IsLootboxEmpty(lootbox) || !ShouldShow(lootbox))
            {
                return;
            }
            else
            {
                var markerObject = new GameObject($"{displayName}");
                markerObject.transform.position = lootbox.transform.position;

                if (MultiSceneCore.MainScene.HasValue)
                {
                    SceneManager.MoveGameObjectToScene(markerObject, MultiSceneCore.MainScene.Value);
                }

                var poi = markerObject.AddComponent<SimplePointOfInterest>();
                var state = GetLootboxState(lootbox);
                var color = MarkerVisuals.SetMarkerColor(state);

                marker = new LootboxMarker
                {
                    Lootbox = lootbox, // 保存引用
                    MarkerObject = markerObject,
                    Poi = poi,
                    DisplayName = displayName,
                    State = state,
                    Color = color
                };

                _boxmarkers[lootbox] = marker;

                //if (marker.Poi == null) return;

                var icon = MarkerVisuals.SetMarkerIcon(lootbox);

                if (icon != null)
                {

                    marker.Poi.Color = marker.Color;
                    marker.Poi.ShadowColor = Color.clear;
                    marker.Poi.Setup(icon, displayName, followActiveScene: true);
                    marker.Poi.HideIcon = false;

                }
                Debug.Log($"创建箱子标记: {marker.DisplayName} 位置: {lootbox.transform.position} 状态: {marker.State} 是否为空: {{IsLootboxEmpty(lootbox)}} ShouldShow: {ShouldShow(lootbox)}" );

            }

        }

        private void UpdateMarker(LootboxMarker marker, string displayName)
        {

             if (marker?.MarkerObject == null || marker.Poi == null)  return;

            //marker.MarkerObject.transform.position = marker.Lootbox.transform.position;

            if (IsLootboxEmpty(marker.Lootbox) || !ShouldShow(marker.Lootbox))
            {
                //Debug.Log("检查箱子是否为空（updatemarker）");
                DestroyMarker(marker.Lootbox);
                return;
            }

            var currentState = GetLootboxState(marker.Lootbox);

            marker.State = currentState;
            marker.Color = MarkerVisuals.SetMarkerColor(marker.State);
            marker.Poi.Color = marker.Color;
            marker.Poi.Setup(MarkerVisuals.SetMarkerIcon(marker.Lootbox), displayName, followActiveScene: true);
            marker.Poi.HideIcon = false;
            Debug.Log("更新箱子标记（预设）{marker.DisplayName} 位置: {lootbox.transform.position} 状态: {marker.State} 是否为空: {IsLootboxEmpty(lootbox)} ShouldShow: {ShouldShow(lootbox)}");

            return;
        }

        private static string GetDisplayName(InteractableLootbox lootbox)
        {
            string name = lootbox.name;//show box name(InteractName)
            if (name.Contains("Formula", StringComparison.OrdinalIgnoreCase))
            {
                string FormulaName = string.Concat(lootbox.InteractName, name.Substring(16));
                return FormulaName;
            }
            else
                return lootbox.InteractName;

        }

        //check if lootbox is opened or closed
        private LootboxState GetLootboxState(InteractableLootbox Lootbox)
        {
            var interactMarker = Lootbox.GetComponentInChildren<InteractMarker>();
            if (interactMarker != null)
            {
                //Debug.Log("interactMarker不为空");
                // 如果showIfUsedObject存在且处于激活状态（或hideIfUsedObject存在且处于未激活状态），则箱子被打开
                if ((interactMarker.showIfUsedObject != null && interactMarker.showIfUsedObject.activeInHierarchy) ||
                    (interactMarker.hideIfUsedObject != null && !interactMarker.hideIfUsedObject.activeInHierarchy))
                {
                    //Debug.Log("箱子已打开");
                    return LootboxState.Opened;
                }

            }

            return LootboxState.Closed;

        }

        private bool IsLootboxEmpty(InteractableLootbox lootbox)
        {
            // check if lootbox inventory is empty
            if (lootbox.Inventory.GetItemCount() == 0)
                return true;
            return false;
        }

        /// <summary>
        /// Check for configuration changes and only apply changes when config is changed.
        /// </summary>
        private void Update()
        {
            if (!_mapActive)
            {
                return;
            }

            // 如果不显示任何标记，跳过扫描
            if (_showNone)
                return;

            // 简单的计时器逻辑
            _scanCooldown -= Time.deltaTime;
            if (_scanCooldown <= 0)
            {
                ScanLootboxes();
                _scanCooldown = ScanIntervalSeconds;
            }

            if (HasSelectedUpdate)
            {
                
                ResetMarkers();                           // 销毁所有现有标记
                ScanLootboxes();                          // 根据新配置重新扫描并创建标记
                _scanCooldown = ScanIntervalSeconds;
                ApplySelectedChanges();                //重置选项切换
            }
        }

        #region 重置和删除标记

        private void ResetMarkers()
        {
            foreach (var marker in _boxmarkers.Values)
            {
                if (marker.Poi != null)
                {
                    PointsOfInterests.Unregister(marker.Poi);
                }

                DestroySafely(marker.MarkerObject);
            }
            _boxmarkers.Clear();

        }

        private static void DestroySafely(GameObject go)
        {
            if (go != null)
            {
                UnityEngine.Object.Destroy(go);
            }
        }

        private void DestroyMarker(InteractableLootbox lootbox)
        {
            if (lootbox == null)
                return;

            if (!_boxmarkers.TryGetValue(lootbox, out var marker))
                return;

            if (marker.Poi != null)
            {
                PointsOfInterests.Unregister(marker.Poi);
            }

            Destroy(marker.MarkerObject);

            Debug.Log($"销毁箱子标记: {marker.DisplayName} 位置: {lootbox.transform.position} 状态: {marker.State} 是否为空: {{IsLootboxEmpty(lootbox)}} ShouldShow: {ShouldShow(lootbox)}");

            _boxmarkers.Remove(lootbox);


        }
        #endregion

        #region 新增一个自定义图标（j-lab箱）

        Sprite? selfSp;
        int spIndex = -1;


        public void AddSelfIconOnMaker()
        {

            //加载一次
            if (selfSp == null)
            {
                string ModDir = GetFileDirByClass(this.GetType());
                selfSp = LoadLocalImageAsSprite(Path.Combine(ModDir, "j.png"));
                selfSp.name = "j";

            }

            if (LevelManager.Instance != null)
            {
                //拿到maker脚本
                MapMarkerSettingsPanel tempmarkersetting = LevelManager.Instance.transform.GetComponentInChildren<MapMarkerSettingsPanel>(true);

                //icon是保存在这个上边的
                MapMarkerManager mapMakerManager = MapMarkerManager.Instance;
                if (mapMakerManager != null)
                {
                    List<Sprite> tempSpriteList = GetPrivateList<Sprite>(mapMakerManager, "icons");

                    //加入自己的图标
                    tempSpriteList.Add(selfSp);
                    spIndex = tempSpriteList.Count - 1;
                }
            }

        }

        public static string GetFileDirByClass(Type BC)
        {

            string directory = Path.GetDirectoryName(BC.Assembly.Location);
            if (string.IsNullOrEmpty(directory))
            {
                directory = AppContext.BaseDirectory;
            }

            Debug.Log("======================>Mod地址：" + directory);
            return directory;
        }

        public Sprite? LoadLocalImageAsSprite(string filePath)
        {
            try
            {
                // 1. 检查文件是否存在
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"图片文件不存在！路径：{filePath}\nEXE 所在目录：{Application.dataPath}");
                    return null;
                }

                // 2. 读取图片字节流
                byte[] imageBytes = File.ReadAllBytes(filePath);

                // 3. 创建 Texture2D 并加载字节流
                Texture2D texture = new Texture2D(2, 2);

                if (!texture.LoadImage(imageBytes)) // 自动识别图片格式（PNG/JPG/TGA 等）
                {
                    Debug.LogError("图片加载失败！可能是格式不支持或文件损坏");
                    Destroy(texture); // 销毁无效 Texture
                    return null;
                }

                // 4. 转为 Sprite（适配 UI）
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height), // 完整图片区域
                    new Vector2(0.5f, 0.5f) // 锚点居中（UI 常用）
                );

                Debug.Log($"图片加载成功！尺寸：{texture.width}x{texture.height}");
                return sprite;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"加载图片异常：{e.Message}");
                return null;
            }
        }

        //反射获取
        public static List<T> GetPrivateList<T>(object instance, string fieldName)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance), "目标实例不能为空");
            }

            // 获取目标类型（MonoBehaviour 直接取实例的类型）
            Type targetType = instance.GetType();

            // 查找私有实例字段（BindingFlags.NonPublic + BindingFlags.Instance）
            FieldInfo field = targetType.GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (field == null)
            {
                throw new ArgumentException($"未找到私有字段：{fieldName}（检查字段名是否正确）");
            }

            // 验证字段类型是否为 List<T>
            if (field.FieldType != typeof(List<T>))
            {
                throw new InvalidCastException($"字段 {fieldName} 不是 List<{typeof(T).Name}> 类型");
            }

            // 读取字段值并转换为 List<T>
            return (List<T>)field.GetValue(instance);
        }

        #endregion

    }
}

