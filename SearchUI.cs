using Duckov.Modding;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace BetterMapMarker
{
    public class SearchUI : MonoBehaviour
    {
        private ModBehaviour _modBehaviour;
        private RectTransform _panel;

        private Toggle _toggleAll;
        private Toggle _toggleJLabOnly;
        private Toggle _toggleNone;
        private ToggleGroup _radioGroup; // 新增：保存组引用以进行批量操作
        private Button _lootboxdropdownButton;
        private GameObject _lootboxdropdownList;

        private Button _pickupDropdownButton;
        private GameObject _pickupDropdownList;
        private bool _isPickupDropdownOpen = false;

        private bool _panelVisible = true;
        private bool _ignoreButtonChange = false;
        private bool _isLootboxDropdownOpen = false;

        public void Initialize(ModBehaviour modBehaviour)
        {
            _modBehaviour = modBehaviour;
            BuildUI();
        }

        private void BuildUI()
        {
            try
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas == null) return;

                var panelGO = new GameObject("LootboxMarkerPanel", typeof(RectTransform));
                panelGO.transform.SetParent(canvas.transform, false);
                _panel = panelGO.GetComponent<RectTransform>();

                _panel.anchorMin = new Vector2(1f, 0f);
                _panel.anchorMax = new Vector2(1f, 0f);
                _panel.pivot = new Vector2(1f, 0f);
                _panel.anchoredPosition = new Vector2(-800f, 20f);
                _panel.sizeDelta = new Vector2(600f, 270f);

                var bg = panelGO.AddComponent<Image>();
                bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

                CreateRadioButtons(panelGO.transform);

                var separatorGO = new GameObject("LootboxSeparator", typeof(RectTransform));
                separatorGO.transform.SetParent(panelGO.transform, false);
                var sepRect = separatorGO.GetComponent<RectTransform>();
                sepRect.anchorMin = new Vector2(1, 1);
                sepRect.anchorMax = new Vector2(1, 1);
                sepRect.pivot = new Vector2(1, 1);
                sepRect.anchoredPosition = new Vector2(-20f, -20f);
                sepRect.sizeDelta = new Vector2(280f, 30f);

                var separatorText = separatorGO.AddComponent<TextMeshProUGUI>();
                separatorText.text = "或选择特定类型：";
                separatorText.fontSize = 18;
                separatorText.color = Color.gray;

                CreateLootboxDropdown(panelGO.transform);

                // 散落物下拉菜单分隔线
                var pickupSeparatorGO = new GameObject("PickupSeparator", typeof(RectTransform));
                pickupSeparatorGO.transform.SetParent(panelGO.transform, false);
                var sepRect2 = pickupSeparatorGO.GetComponent<RectTransform>();
                sepRect2.anchorMin = new Vector2(1, 1);
                sepRect2.anchorMax = new Vector2(1, 1);
                sepRect2.pivot = new Vector2(1, 1);
                sepRect2.anchoredPosition = new Vector2(-20f, -105f); // 位于箱子下拉菜单下方
                sepRect2.sizeDelta = new Vector2(280f, 30f);

                var sepText2 = pickupSeparatorGO.AddComponent<TextMeshProUGUI>();
                sepText2.text = "筛选散落物：";
                sepText2.fontSize = 18;
                sepText2.color = Color.gray;

                // 创建散落物下拉菜单
                CreatePickupDropdown(panelGO.transform);
            }
            catch (System.Exception ex) { Debug.LogError($"BuildUI 异常: {ex.Message}"); }
        }

        private void CreateRadioButtons(Transform parent)
        {
            var containerGO = new GameObject("RadioContainer", typeof(RectTransform));
            containerGO.transform.SetParent(parent, false);
            var rect = containerGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(20f, -20f);
            rect.sizeDelta = new Vector2(280f, 200f);

            var layout = containerGO.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;

            // 【关键修改】允许组内所有开关都关闭，用于支持下拉菜单的互斥
            _radioGroup = containerGO.AddComponent<ToggleGroup>();
            _radioGroup.allowSwitchOff = true;

            _toggleAll = CreateRadioToggle(containerGO.transform, "显示所有箱子", _radioGroup, true);
            _toggleJLabOnly = CreateRadioToggle(containerGO.transform, "只显示高价值箱子", _radioGroup, false);
            _toggleNone = CreateRadioToggle(containerGO.transform, "不显示箱子标记", _radioGroup, false);
        }

        private Toggle CreateRadioToggle(Transform parent, string label, ToggleGroup group, bool isOn)
        {
            var toggleGO = new GameObject("Toggle_" + label, typeof(RectTransform));
            toggleGO.transform.SetParent(parent, false);

            var toggle = toggleGO.AddComponent<Toggle>();
            toggle.group = group;
            toggle.isOn = isOn;

            var layoutElement = toggleGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 28f;

            var horizontalLayout = toggleGO.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.spacing = 8f;
            horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayout.childControlWidth = false;
            horizontalLayout.childControlHeight = false;

            var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(toggleGO.transform, false);
            bgGO.GetComponent<RectTransform>().sizeDelta = new Vector2(22f, 22f);
            bgGO.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 0.9f);

            var checkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkGO.transform.SetParent(bgGO.transform, false);
            var checkRect = checkGO.GetComponent<RectTransform>();
            checkRect.sizeDelta = new Vector2(18f, 18f);
            checkRect.anchorMin = new Vector2(0.5f, 0.5f);
            checkRect.anchorMax = new Vector2(0.5f, 0.5f);
            checkRect.anchoredPosition = Vector2.zero;
            checkGO.GetComponent<Image>().color = Color.green;

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(toggleGO.transform, false);
            var labelText = labelGO.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 20;
            labelText.color = Color.white;

            toggle.targetGraphic = bgGO.GetComponent<Image>();
            toggle.graphic = checkGO.GetComponent<Image>();

            toggle.onValueChanged.AddListener(isOnVal =>
            {
                if (_ignoreButtonChange) return;
                if (!isOnVal && toggle.group.AnyTogglesOn() == false)
                {
                    // 用户试图取消唯一选中的按钮，强制重新选中
                    _ignoreButtonChange = true;
                    toggle.isOn = true;
                    _ignoreButtonChange = false;
                    return;
                }

                if (isOnVal && _modBehaviour != null && !_ignoreButtonChange)
                {
                    // 【互斥逻辑】点击单选按钮时，重置下拉菜单文字并关闭列表
                    var btnText = _lootboxdropdownButton?.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null) btnText.text = "选择类型...";
                    if (_lootboxdropdownList != null) _lootboxdropdownList.SetActive(false);
                    _isLootboxDropdownOpen = false;

                    if (label == "显示所有箱子") _modBehaviour.SetShowAll();
                    else if (label == "只显示高价值箱子") _modBehaviour.SetShowJLab();
                    else if (label == "不显示箱子标记") _modBehaviour.SetShowNone();
                }


            });

            return toggle;
        }

        private void CreateLootboxDropdown(Transform parent)
        {
            var buttonGO = new GameObject("DropdownButton", typeof(RectTransform));
            buttonGO.transform.SetParent(parent, false);
            var rect = buttonGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-20f, -60f);
            rect.sizeDelta = new Vector2(280f, 40f);

            _lootboxdropdownButton = buttonGO.AddComponent<Button>();
            var img = buttonGO.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.25f, 1f);

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(buttonGO.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 0f);
            textRect.offsetMax = new Vector2(-30f, 0f);

            var btnText = textGO.AddComponent<TextMeshProUGUI>();
            btnText.text = "选择类型...";
            btnText.fontSize = 18;
            btnText.color = Color.white;
            btnText.alignment = TextAlignmentOptions.MidlineLeft;

            var arrowGO = new GameObject("Arrow", typeof(RectTransform));
            arrowGO.transform.SetParent(buttonGO.transform, false);
            var arrowRect = arrowGO.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0.5f);
            arrowRect.anchorMax = new Vector2(1, 0.5f);
            arrowRect.sizeDelta = new Vector2(20, 20);
            arrowRect.anchoredPosition = new Vector2(-15f, 0f);
            var arrowText = arrowGO.AddComponent<TextMeshProUGUI>();
            arrowText.text = "▼";
            arrowText.fontSize = 14;
            arrowText.alignment = TextAlignmentOptions.Center;

            CreateLootboxDropdownList(parent);
            _lootboxdropdownButton.onClick.AddListener(ToggleLootboxDropdown);
        }

        private void CreateLootboxDropdownList(Transform parent)
        {
            _lootboxdropdownList = new GameObject("DropdownList", typeof(RectTransform));
            _lootboxdropdownList.transform.SetParent(parent, false);

            var listRect = _lootboxdropdownList.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(1, 1);
            listRect.anchorMax = new Vector2(1, 1);
            listRect.pivot = new Vector2(1, 1);
            listRect.anchoredPosition = new Vector2(-20f, -105f);
            listRect.sizeDelta = new Vector2(280f, 160f);

            var listImage = _lootboxdropdownList.AddComponent<Image>();
            listImage.color = new Color(0.15f, 0.15f, 0.15f, 0.98f);

            var scrollRect = _lootboxdropdownList.AddComponent<ScrollRect>();

            var viewportGO = new GameObject("Viewport", typeof(RectTransform));
            viewportGO.transform.SetParent(_lootboxdropdownList.transform, false);
            var viewportRect = viewportGO.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.offsetMin = new Vector2(2f, 2f);
            viewportRect.offsetMax = new Vector2(-2f, -2f);

            var viewportImage = viewportGO.AddComponent<Image>();
            viewportImage.color = new Color(1, 1, 1, 0.01f);
            var mask = viewportGO.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRect = contentGO.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0, 1);

            var contentLayout = contentGO.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 2f;
            contentLayout.padding = new RectOffset(2, 2, 2, 2);
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;

            var contentFitter = contentGO.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            _lootboxdropdownList.transform.SetAsLastSibling();
            _lootboxdropdownList.SetActive(false);
        }

        private void ToggleLootboxDropdown()
        {
            if (_isPickupDropdownOpen)
            {
                _pickupDropdownList.SetActive(false);
                _isPickupDropdownOpen = false;
            }

            _isLootboxDropdownOpen = !_isLootboxDropdownOpen;
            _lootboxdropdownList.SetActive(_isLootboxDropdownOpen);
            if (_isLootboxDropdownOpen)
            {
                _lootboxdropdownList.transform.SetAsLastSibling(); // 移到最前
                RefreshLootboxDropdownOptions();
            }
        }


        private void RefreshLootboxDropdownOptions()
        {
            if (_modBehaviour == null) return;
            try
            {
                var types = _modBehaviour.GetAllLootboxTypes();
                var contentTransform = _lootboxdropdownList.transform.Find("Viewport/Content");
                if (contentTransform == null) return;

                foreach (Transform child in contentTransform) Destroy(child.gameObject);

                CreateLootboxDropdownOption(contentTransform, "选择类型...", true);
                foreach (var type in types) CreateLootboxDropdownOption(contentTransform, type, false);

                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentTransform.GetComponent<RectTransform>());
            }
            catch (System.Exception ex) { Debug.LogError($"RefreshDropdownOptions 异常: {ex.Message}"); }
        }

        private void CreateLootboxDropdownOption(Transform parent, string text, bool isPlaceholder)
        {
            var optionGO = new GameObject("Option_" + text, typeof(RectTransform));
            optionGO.transform.SetParent(parent, false);

            var layoutElement = optionGO.AddComponent<LayoutElement>();
            layoutElement.minHeight = 35f;
            layoutElement.preferredHeight = 35f;

            var optionImage = optionGO.AddComponent<Image>();
            optionImage.color = isPlaceholder ? new Color(0.4f, 0.4f, 0.4f, 1f) : new Color(0.25f, 0.25f, 0.25f, 1f);
            optionImage.raycastTarget = true;

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(optionGO.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 2f);
            textRect.offsetMax = new Vector2(-10f, -2f);

            var optionText = textGO.AddComponent<TextMeshProUGUI>();
            optionText.text = text;
            optionText.fontSize = 18;
            optionText.color = Color.white;
            optionText.alignment = TextAlignmentOptions.MidlineLeft;

            var button = optionGO.AddComponent<Button>();
            button.targetGraphic = optionImage;
            button.onClick.AddListener(() => OnOptionSelected(text));
        }

        private void OnOptionSelected(string selectedText)
        {
            var buttonText = _lootboxdropdownButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) buttonText.text = selectedText;

            _lootboxdropdownList.SetActive(false);
            _isLootboxDropdownOpen = false;

            // 【互斥逻辑】选择下拉菜单项时，清空左侧单选按钮的选中状态
            _ignoreButtonChange = true;
            if (_radioGroup != null) _radioGroup.SetAllTogglesOff();
            _ignoreButtonChange = false;

            if (_modBehaviour != null)
            {
                if (selectedText == "选择类型...")
                    _modBehaviour.SetTypeFilter(null);
                else
                    _modBehaviour.SetTypeFilter(selectedText);
            }
        }

        public void RefreshLootboxTypeDropdown()
        {
            if (_lootboxdropdownList != null && _lootboxdropdownList.activeSelf) RefreshLootboxDropdownOptions();
        }

        private void CreatePickupDropdown(Transform parent)
        {
            var buttonGO = new GameObject("PickupDropdownButton", typeof(RectTransform));
            buttonGO.transform.SetParent(parent, false);
            var rect = buttonGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-20f, -140f); // 位于箱子下拉菜单下方
            rect.sizeDelta = new Vector2(280f, 40f);

            _pickupDropdownButton = buttonGO.AddComponent<Button>();
            var img = buttonGO.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.25f, 1f);

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(buttonGO.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 0f);
            textRect.offsetMax = new Vector2(-30f, 0f);

            var btnText = textGO.AddComponent<TextMeshProUGUI>();
            btnText.text = "选择散落物...";
            btnText.fontSize = 18;
            btnText.color = Color.white;
            btnText.alignment = TextAlignmentOptions.MidlineLeft;

            var arrowGO = new GameObject("Arrow", typeof(RectTransform));
            arrowGO.transform.SetParent(buttonGO.transform, false);
            var arrowRect = arrowGO.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0.5f);
            arrowRect.anchorMax = new Vector2(1, 0.5f);
            arrowRect.sizeDelta = new Vector2(20, 20);
            arrowRect.anchoredPosition = new Vector2(-15f, 0f);
            var arrowText = arrowGO.AddComponent<TextMeshProUGUI>();
            arrowText.text = "▼";
            arrowText.fontSize = 14;
            arrowText.alignment = TextAlignmentOptions.Center;

            CreatePickupDropdownList(parent);
            _pickupDropdownButton.onClick.AddListener(TogglePickupDropdown);
        }

        private void CreatePickupDropdownList(Transform parent)
        {
            _pickupDropdownList = new GameObject("PickupDropdownList", typeof(RectTransform));
            _pickupDropdownList.transform.SetParent(parent, false);

            var listRect = _pickupDropdownList.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(1, 1);
            listRect.anchorMax = new Vector2(1, 1);
            listRect.pivot = new Vector2(1, 1);
            listRect.anchoredPosition = new Vector2(-20f, -185f); // 与按钮对齐下方
            listRect.sizeDelta = new Vector2(280f, 80f);

            var listImage = _pickupDropdownList.AddComponent<Image>();
            listImage.color = new Color(0.15f, 0.15f, 0.15f, 0.98f);

            var scrollRect = _pickupDropdownList.AddComponent<ScrollRect>();

            var viewportGO = new GameObject("Viewport", typeof(RectTransform));
            viewportGO.transform.SetParent(_pickupDropdownList.transform, false);
            var viewportRect = viewportGO.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.offsetMin = new Vector2(2f, 2f);
            viewportRect.offsetMax = new Vector2(-2f, -2f);

            var viewportImage = viewportGO.AddComponent<Image>();
            viewportImage.color = new Color(1, 1, 1, 0.01f);
            var mask = viewportGO.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRect = contentGO.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0, 1);

            var contentLayout = contentGO.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 2f;
            contentLayout.padding = new RectOffset(2, 2, 2, 2);
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;

            var contentFitter = contentGO.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            _pickupDropdownList.transform.SetAsLastSibling();
            _pickupDropdownList.SetActive(false);
        }

        private void TogglePickupDropdown()
        {
            if (_isLootboxDropdownOpen)
            {
                _lootboxdropdownList.SetActive(false);
                _isLootboxDropdownOpen = false;
            }

            _isPickupDropdownOpen = !_isPickupDropdownOpen;
            _pickupDropdownList.SetActive(_isPickupDropdownOpen);
            if (_isPickupDropdownOpen)
            {
                _pickupDropdownList.transform.SetAsLastSibling(); // 移到最前
                RefreshPickupDropdownOptions();
            }
        }

        private void RefreshPickupDropdownOptions()
        {
            if (_modBehaviour == null) return;
            try
            {
                var names = _modBehaviour.GetAllPickupNames();
                var contentTransform = _pickupDropdownList.transform.Find("Viewport/Content");
                if (contentTransform == null) return;

                foreach (Transform child in contentTransform) Destroy(child.gameObject);

                CreatePickupDropdownOption(contentTransform, "全部散落物", true);
                foreach (var name in names) CreatePickupDropdownOption(contentTransform, name, false);

                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentTransform.GetComponent<RectTransform>());
            }
            catch (System.Exception ex) { Debug.LogError($"RefreshPickupDropdownOptions 异常: {ex.Message}"); }
        }

        private void CreatePickupDropdownOption(Transform parent, string text, bool isPlaceholder)
        {
            var optionGO = new GameObject("Option_" + text, typeof(RectTransform));
            optionGO.transform.SetParent(parent, false);

            var layoutElement = optionGO.AddComponent<LayoutElement>();
            layoutElement.minHeight = 35f;
            layoutElement.preferredHeight = 35f;

            var optionImage = optionGO.AddComponent<Image>();
            optionImage.color = isPlaceholder ? new Color(0.4f, 0.4f, 0.4f, 1f) : new Color(0.25f, 0.25f, 0.25f, 1f);
            optionImage.raycastTarget = true;

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(optionGO.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 2f);
            textRect.offsetMax = new Vector2(-10f, -2f);

            var optionText = textGO.AddComponent<TextMeshProUGUI>();
            optionText.text = text;
            optionText.fontSize = 18;
            optionText.color = Color.white;
            optionText.alignment = TextAlignmentOptions.MidlineLeft;

            var button = optionGO.AddComponent<Button>();
            button.targetGraphic = optionImage;
            button.onClick.AddListener(() => OnPickupOptionSelected(text));
        }

        private void OnPickupOptionSelected(string selectedText)
        {
            var buttonText = _pickupDropdownButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) buttonText.text = selectedText;

            _pickupDropdownList.SetActive(false);
            _isPickupDropdownOpen = false;

            if (_modBehaviour != null)
            {
                if (selectedText == "全部散落物")
                    _modBehaviour.SetPickupFilter(null);
                else
                    _modBehaviour.SetPickupFilter(selectedText);
            }
        }

        public void SetVisible(bool visible)
        {
            _panelVisible = visible;
            if (_panel != null)
            {
                _panel.gameObject.SetActive(visible);
                if (!visible)
                {
                    if (_lootboxdropdownList != null)
                    {
                        _lootboxdropdownList.SetActive(false);
                        _isLootboxDropdownOpen = false;
                    }
                    if (_pickupDropdownList != null)
                    {
                        _pickupDropdownList.SetActive(false);
                        _isPickupDropdownOpen = false;
                    }
                }
            }
        }

        // 可选：添加刷新散落物下拉菜单的公开方法（供外部调用）
        public void RefreshPickupDropdown()
        {
            if (_pickupDropdownList != null && _pickupDropdownList.activeSelf)
                RefreshPickupDropdownOptions();
        }
    }
}