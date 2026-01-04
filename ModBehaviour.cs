using Duckov.MiniMaps;
using Duckov.MiniMaps.UI;
using Duckov.Scenes;
using Duckov.UI;
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
            if (Lootbox.name.Contains("Clone", StringComparison.OrdinalIgnoreCase)&& 
                !Lootbox.name.Contains("Enemy", StringComparison.OrdinalIgnoreCase))
                icon = MapMarkerManager.Icons[5];
            if (Lootbox.name.Contains("Formula", StringComparison.OrdinalIgnoreCase))
                icon = MapMarkerManager.Icons[7];
            if (Lootbox.name.Contains("Lab", StringComparison.OrdinalIgnoreCase))
                icon= MapMarkerManager.Icons[12];//自定义图标（要先添加）

            return icon;
        }


        //箱子是黄色，打开后颜色变成白色
        public static Color SetMarkerColor(LootboxState State)
        {
            if (State==LootboxState.Closed)
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
            public bool HasPreexistingPoi; // Cached flag to avoid GetComponent calls
            public Color Color;
        }

        /// <summary>
        /// Map a character to its marker.
        /// </summary>
        private readonly Dictionary<InteractableLootbox, LootboxMarker> _markers =
            new Dictionary<InteractableLootbox, LootboxMarker>();


        private bool _mapActive;
        private float _scanCooldown;
        private const float ScanIntervalSeconds = 1f;

        // Special preset names loaded from text file (one name per line). Comparisons are case-insensitive.
        private static DateTime _specialPresetsLastWriteUtc = DateTime.MinValue;




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
            }


        }

        void OnDisable()
        {
            LevelManager.OnAfterLevelInitialized -= AddSelfIconOnMaker;

            View.OnActiveViewChanged -= OnActiveViewChanged;
            SceneLoader.onStartedLoadingScene -= OnSceneStartedLoading;
            SceneLoader.onFinishedLoadingScene -= OnSceneFinishedLoading;
            EndTracking();

        }

        private void OnSceneStartedLoading(SceneLoadingContext context)
        {
            // Clear markers when leaving the current scene
            ResetMarkers();
        }

        private void OnSceneFinishedLoading(SceneLoadingContext context)
        {
            // 延迟扫描，确保所有对象加载完成
            StartCoroutine(DelayedScan());
        }

        private System.Collections.IEnumerator DelayedScan()
        {
            yield return new WaitForSeconds(0.5f);
            if (_mapActive || IsMapOpen())
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
                BeginTracking();
            else
                EndTracking();
        }
        private void BeginTracking()
        {
            // Don't reset markers on map open - preserve last known positions when Live is OFF
            // ResetMarkers();
            _mapActive = true;

            ScanLootboxes();
            Debug.Log("开始追踪箱子位置");
            _scanCooldown = ScanIntervalSeconds;
        }

        private void EndTracking()
        {
            if (!_mapActive)
                return;
            _mapActive = false;
            Debug.Log("停止追踪箱子位置");
            // Don't reset markers on map close - preserve last known positions when Live is OFF
            // ResetMarkers();

        }

        private static bool IsLootboxValid(InteractableLootbox lootbox, out bool hasPreexistingPoi)
        {
            hasPreexistingPoi = false;

            if (lootbox == null)
                return false;

            var go = lootbox.gameObject;
            if (!go.scene.IsValid() || !go.scene.isLoaded)
                return false;

            // Only check GetComponent during initial scan, cache the result
            hasPreexistingPoi = lootbox.GetComponent<SimplePointOfInterest>() != null;

            return true;
        }


        private void ScanLootboxes()
        {
            var lootboxes = UnityEngine.Object.FindObjectsOfType<InteractableLootbox>();
            Debug.Log($"扫描到 {lootboxes.Length} 个箱子");

            foreach (var lootbox in lootboxes)
            {
                if (lootbox == null || lootbox.Inventory == null) continue;

                // 过滤不需要的箱子
                if (lootbox.name.Contains("PetProxy", StringComparison.OrdinalIgnoreCase) ||
                    lootbox.name.Contains("PlayerStorage", StringComparison.OrdinalIgnoreCase))
                    continue;

                AddOrUpdateMarker(lootbox);

            }
        }

        private void AddOrUpdateMarker(InteractableLootbox lootbox)
        {

            if (!IsLootboxValid(lootbox, out bool hasPreexistingPoi))
                return;

            // Don't create markers for characters that already have a POI component
            if (hasPreexistingPoi)
                return;

            var displayName = GetDisplayName(lootbox);

            if (_markers.TryGetValue(lootbox, out var marker))
            {
                // check if lootbox is empty, if so remove marker
                if (IsLootboxEmpty(lootbox))
                {
                    DestroyMarker(lootbox);
                    return;
                }
                else
                    UpdateMarker(marker, displayName);

                return;
            }

            var markerObject = new GameObject($"CharacterMarker:{displayName}");
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
                HasPreexistingPoi = hasPreexistingPoi,
                State = state,
                Color = color
            };

            _markers[lootbox] = marker;

            if (marker.Poi == null) return;

            var icon = MarkerVisuals.SetMarkerIcon(lootbox);

            if (icon != null)
            {
                marker.Poi.Setup(icon, displayName, followActiveScene: true);

                // 设置颜色
                try
                {
                    marker.Poi.Color = marker.Color;
                    marker.Poi.ShadowColor = Color.clear;
                }
                catch { }

                marker.Poi.HideIcon = false;
            }
            //Debug.Log($"创建箱子标记: {marker.DisplayName} 位置: {lootbox.transform.position}");

            UpdateMarker(marker, displayName);
        }

        private void UpdateMarker(LootboxMarker marker,string displayName)
        {

            if (marker?.MarkerObject == null || marker.Poi == null)
                return;

            //marker.MarkerObject.transform.position = marker.Lootbox.transform.position;

            if (IsLootboxEmpty(marker.Lootbox))
            {
                //Debug.Log("检查箱子是否为空（update）");
                DestroyMarker(marker.Lootbox);
                return;
            } 
                

            //Change marker color to white if lootbox was opened and lootbox not empty
            if (GetLootboxState(marker.Lootbox)==LootboxState.Opened)
            {
                marker.State = GetLootboxState(marker.Lootbox);
                marker.Color = MarkerVisuals.SetMarkerColor(marker.State);
                marker.Poi.Color = marker.Color;
                marker.Poi.Setup(MarkerVisuals.SetMarkerIcon(marker.Lootbox), displayName, followActiveScene: true);
                marker.Poi.HideIcon = false;
                //Debug.Log("更新箱子标记");
            }

        }

        private static string GetDisplayName(InteractableLootbox lootbox)
        {
            string name = lootbox.name;//show box name(InteractName)
            if(name.Contains("Formula", StringComparison.OrdinalIgnoreCase))
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
                if ((interactMarker.showIfUsedObject != null && interactMarker.showIfUsedObject.activeInHierarchy)||
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
            if (lootbox.Inventory.GetItemCount()==0)
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
            // 简单的计时器逻辑
            _scanCooldown -= Time.deltaTime;
            if (_scanCooldown <= 0)
            {
                ScanLootboxes();
                _scanCooldown = ScanIntervalSeconds;
            }
        }

        //reset or destroy marker

        private void DestroyMarker(InteractableLootbox lootbox)
        {
            if (lootbox == null)
                return;

            if (!_markers.TryGetValue(lootbox, out var marker))
                return;

            _markers.Remove(lootbox);

            if (marker.Poi != null)
            {
                PointsOfInterests.Unregister(marker.Poi);
                marker.Poi = null;
            }
            //Destroy lootbox marker
                      
        }


        private void ResetMarkers()
        {
            foreach (var marker in _markers.Values)
            {
                if (marker.Poi != null)
                {
                    PointsOfInterests.Unregister(marker.Poi);
                }
                DestroySafely(marker.MarkerObject);
            }
            _markers.Clear();

        }

        private static void DestroySafely(GameObject go)
        {
            if (go != null)
            {
                UnityEngine.Object.Destroy(go);
            }
        }


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

