using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;
using Button = UnityEngine.UI.Button;

namespace BetterMapMarker
{
    public class LootboxMarkerUI : MonoBehaviour
    {
        private ModBehaviour _modBehaviour;
        private RectTransform _panel;

        // UI控件
        private Toggle _toggleAll;
        private Toggle _toggleJLabOnly;

        // 面板状态
        private bool _panelVisible = true;

        public void Initialize(ModBehaviour modBehaviour)
        {
            _modBehaviour = modBehaviour;
            BuildUI();
        }

        private void BuildUI()
        {
            // 获取画布
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            // 创建主面板
            var panelGO = new GameObject("ChestMarkerPanel", typeof(RectTransform));
            panelGO.transform.SetParent(canvas.transform, false);
            _panel = panelGO.GetComponent<RectTransform>();

            // 设置面板位置（屏幕右下角）
            _panel.anchorMin = new Vector2(1f, 0f);  // 右下角
            _panel.anchorMax = new Vector2(1f, 0f);
            _panel.pivot = new Vector2(1f, 0f);  // 轴心在右下角
            _panel.anchoredPosition = new Vector2(-1000f, 20f);  
            _panel.sizeDelta = new Vector2(240f, 180f);

            // 背景
            var bg = panelGO.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // 垂直布局
            var layout = panelGO.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childAlignment = TextAnchor.MiddleLeft;

            // 标题
            var titleGO = new GameObject("Title", typeof(RectTransform));
            titleGO.transform.SetParent(panelGO.transform, false);
            var titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = "箱子标记显示";
            titleText.fontSize = 20;
            titleText.color = Color.yellow;
            titleText.alignment = TextAlignmentOptions.TopJustified;

            var titleLayout = titleGO.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 25f;

            // 创建单选按钮
            CreateRadioButtons(panelGO.transform);

            // 设置默认选择
            _toggleAll.isOn = true;
            _toggleJLabOnly.isOn = false;

        }

        private void CreateRadioButtons(Transform parent)
        {
            // 创建一个容器用于放置单选按钮
            var containerGO = new GameObject("RadioContainer", typeof(RectTransform));
            containerGO.transform.SetParent(parent, false);

            var layout = containerGO.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5f;
            layout.childAlignment = TextAnchor.MiddleLeft;

            // 创建Toggle组实现单选
            var toggleGroup = containerGO.AddComponent<ToggleGroup>();
            toggleGroup.allowSwitchOff = false;

            // 创建"显示所有箱子"选项
            _toggleAll = CreateRadioToggle(containerGO.transform, "显示所有箱子", toggleGroup);
            _toggleAll.onValueChanged.AddListener(isOn =>
            {
                if (isOn && _modBehaviour != null)
                {
                    _modBehaviour.SetShowAll(true);
                }
            });

            // 创建"只显示Lab箱子"选项
            _toggleJLabOnly = CreateRadioToggle(containerGO.transform, "只显示高价值箱子", toggleGroup);
            _toggleJLabOnly.onValueChanged.AddListener(isOn =>
            {
                if (isOn && _modBehaviour != null)
                {
                    _modBehaviour.SetShowAll(false);
                }
            });

        }

        private Toggle CreateRadioToggle(Transform parent, string label, ToggleGroup toggleGroup)
        {
            var toggleGO = new GameObject("Toggle_" + label, typeof(RectTransform));
            toggleGO.transform.SetParent(parent, false);

            var toggle = toggleGO.AddComponent<Toggle>();
            toggle.group = toggleGroup;

            var toggleLayoutElem = toggleGO.AddComponent<LayoutElement>();
            toggleLayoutElem.minHeight = 25f;

            // 水平布局
            var layout = toggleGO.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5f;
            layout.childControlWidth = false;  // 不强制控制子物体宽度
            layout.childControlHeight = false; // 不强制控制子物体高度
            layout.childAlignment = TextAnchor.MiddleLeft;  // 左中对齐

            // 单选框背景
            var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(toggleGO.transform, false);

            var bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.sizeDelta = new Vector2(20f, 20f);

            var bgImage = bgGO.GetComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // 勾选标记
            var checkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkGO.transform.SetParent(bgGO.transform, false);

            var checkRT = checkGO.GetComponent<RectTransform>();
            checkRT.sizeDelta = new Vector2(16f, 16f);
            checkRT.anchorMin = new Vector2(0.5f, 0.5f);
            checkRT.anchorMax = new Vector2(0.5f, 0.5f);
            checkRT.anchoredPosition = Vector2.zero;

            var checkImage = checkGO.GetComponent<Image>();
            checkImage.color = Color.green;

            // 标签
            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(toggleGO.transform, false);

            // 标签布局控制
            var labelLayout = labelGO.AddComponent<LayoutElement>();
            labelLayout.minHeight = 24f;  // 与背景高度匹配

            var labelText = labelGO.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 20;
            labelText.alignment= TextAlignmentOptions.MidlineLeft;
            labelText.color = Color.white;
            labelText.enableAutoSizing = false;

            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;

            return toggle;
        }

        public void SetVisible(bool visible)
        {
            _panelVisible = visible;
            if (_panel != null)
            {
                _panel.gameObject.SetActive(visible);
            }
        }
    }
}