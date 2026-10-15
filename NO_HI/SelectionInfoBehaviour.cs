using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NO_HI;

public sealed class SelectionInfoBehaviour : MonoBehaviour
{
    private const string OverlayName = "NO_HP_HardpointStats";
    private const string EmptyRcsRowName = "NO_HP_EmptyRCSLabel";
    private const float OverlayGap = 6f;
    private const float OverlayPadding = 6f;
    private const float RightPanelExtraHeight = 45f;
    private const float LabelToValueGap = 5f;
    private const float CheckInterval = 0.25f;
    private readonly Dictionary<WeaponSelector, Overlay> _overlays = new();
    private readonly List<WeaponSelector> _toRemove = [];
    private float _appliedInfoHeight;
    private float _appliedRightHeight;
    private GameObject? _emptyRcsRow;
    private TextMeshProUGUI? _emptyRcsValue;
    private RectTransform? _infoRect;
    private RectTransform? _loadoutRoot;
    private AircraftSelectionMenu? _menu;
    private float _nextCheck;
    private RectTransform? _rightPanel;
    
    private void LateUpdate()
    {
        if (_menu == null || Time.unscaledTime < _nextCheck)
            return;
        
        _nextCheck = Time.unscaledTime + CheckInterval;
        Refresh();
    }
    
    private void OnDestroy()
    {
        SetInfoRowExtraHeight(0f);
        SetRightPanelExtraHeight(0f);
        _overlays.Clear();
    }
    
    public void Initialise(AircraftSelectionMenu menu)
    {
        _menu = menu;
        _loadoutRoot = menu.transform.Find("LowRow/BottomPanel/LoadoutSelector") as RectTransform;
        _nextCheck = 0f;
        Refresh();
    }
    
    public void Refresh()
    {
        if (_menu == null)
            return;
        
        if (_loadoutRoot == null)
            _loadoutRoot = _menu.transform.Find("LowRow/BottomPanel/LoadoutSelector") as RectTransform;
        
        UpdateHardpointOverlays();
        UpdateEmptyRcsRow();
    }
    
    private static bool AnyStatTurnedOn() => Plugin.ShowEmptyCost.Value || Plugin.ShowEmptyMass.Value ||
                                             Plugin.ShowMass.Value || Plugin.ShowDrag.Value ||
                                             Plugin.ShowEmptyDrag.Value || Plugin.ShowRcs.Value ||
                                             Plugin.ShowEmptyRcs.Value;
    
    private void UpdateHardpointOverlays()
    {
        if (!Plugin.ShowHardpointInfo.Value || !AnyStatTurnedOn() || _loadoutRoot == null)
        {
            foreach (var overlay in _overlays.Values)
                if (overlay.Root != null)
                    overlay.Root.SetActive(false);
            return;
        }
        
        var selectors = _loadoutRoot.GetComponentsInChildren<WeaponSelector>(true);
        var existing = new HashSet<WeaponSelector>(selectors);
        
        foreach (var selector in selectors)
        {
            if (selector == null)
                continue;
            
            if (!_overlays.TryGetValue(selector, out var overlay) || overlay.Root == null)
            {
                overlay = CreateOverlay(selector);
                if (overlay == null)
                    continue;
                
                _overlays[selector] = overlay;
            }
            
            BuildRows(selector, out var labels, out var values, out var count);
            overlay.Root.SetActive(count != 0);
            if (count == 0)
                continue;
            
            PositionOverlayAboveSymmetry(selector, overlay.Rect);
            if (!string.Equals(overlay.Labels.text, labels, StringComparison.Ordinal))
                overlay.Labels.text = labels;
            
            if (!string.Equals(overlay.Values.text, values, StringComparison.Ordinal))
                overlay.Values.text = values;
            
            var preferredWidth = overlay.Values.GetPreferredValues(values).x + 2f;
            var maxWidth = Mathf.Max(22f, overlay.Rect.rect.width * 0.6f);
            var valueWidth = Mathf.Min(Mathf.Max(34f, preferredWidth), maxWidth);
            if (Mathf.Abs(overlay.LastValueWidth - valueWidth) > 0.5f)
            {
                overlay.LastValueWidth = valueWidth;
                overlay.Values.rectTransform.sizeDelta = new Vector2(valueWidth, -4f);
                overlay.Labels.rectTransform.offsetMax = new Vector2(-valueWidth - LabelToValueGap - 4f, -2f);
            }
            var contentHeight = Mathf.Max(overlay.Labels.GetPreferredValues(labels).y, overlay.Values.GetPreferredValues(values).y);
            var height = Mathf.Ceil(contentHeight + OverlayPadding);
            if (Mathf.Abs(overlay.Rect.sizeDelta.y - height) > 0.5f)
                overlay.Rect.sizeDelta = new Vector2(-4f, height);
        }
        
        _toRemove.Clear();
        foreach (var entry in _overlays)
        {
            if (entry.Key != null && existing.Contains(entry.Key))
                continue;
            
            if (entry.Value.Root != null)
                Destroy(entry.Value.Root);
            
            _toRemove.Add(entry.Key!);
        }
        
        foreach (var selector in _toRemove)
            _overlays.Remove(selector);
        _toRemove.Clear();
    }
    
    private static void PositionOverlayAboveSymmetry(WeaponSelector selector, RectTransform overlayRect)
    {
        var bottomY = OverlayGap;
        if (selector.transform.Find("Symmetry") is RectTransform { gameObject.activeSelf: true } symmetry)
        {
            var symmetryTop = symmetry.anchoredPosition.y + symmetry.rect.height * (1f - symmetry.pivot.y);
            bottomY = Mathf.Max(bottomY, symmetryTop + OverlayGap);
        }
        
        if (Mathf.Abs(overlayRect.anchoredPosition.y - bottomY) > 0.1f)
            overlayRect.anchoredPosition = new Vector2(overlayRect.anchoredPosition.x, bottomY);
    }
    
    private static Overlay? CreateOverlay(WeaponSelector selector)
    {
        var selectorRect = selector.transform as RectTransform;
        var sourceText = selector.transform.Find("HardpointSetName")?.GetComponent<TextMeshProUGUI>();
        if (selectorRect == null || sourceText == null)
            return null;
        
        var panel = new GameObject(OverlayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
            typeof(LayoutElement))
        {
            layer = selector.gameObject.layer
        };
        panel.transform.SetParent(selectorRect, false);
        var rect = (RectTransform)panel.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, OverlayGap);
        rect.sizeDelta = new Vector2(-4f, 44f);
        panel.GetComponent<LayoutElement>().ignoreLayout = true;
        var image = panel.GetComponent<Image>();
        var sourceImage = selector.GetComponent<Image>();
        if (sourceImage != null)
        {
            image.sprite = sourceImage.sprite;
            image.type = sourceImage.type;
            image.material = sourceImage.material;
            var color = sourceImage.color;
            image.color = new Color(color.r * 0.3f, color.g * 0.3f, color.b * 0.3f, Mathf.Clamp01(color.a * 1.5f));
        }
        else
        {
            image.color = new Color(0.20f, 0.22f, 0.24f, 0.5f);
        }
        
        image.raycastTarget = false;
        var labels = Instantiate(sourceText, panel.transform);
        ConfigureText(labels, "StatLabels", TextAlignmentOptions.MidlineLeft);
        var labelRect = labels.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(4f, 2f);
        labelRect.offsetMax = new Vector2(-72f, -2f);
        var values = Instantiate(sourceText, panel.transform);
        ConfigureText(values, "StatValues", TextAlignmentOptions.MidlineRight);
        var valueRect = values.rectTransform;
        valueRect.anchorMin = new Vector2(1f, 0f);
        valueRect.anchorMax = new Vector2(1f, 1f);
        valueRect.pivot = new Vector2(1f, 0.5f);
        valueRect.sizeDelta = new Vector2(64f, -4f);
        valueRect.anchoredPosition = new Vector2(-4f, 0f);
        return new Overlay(panel, rect, labels, values);
    }
    
    private static void ConfigureText(TextMeshProUGUI text, string name, TextAlignmentOptions alignment)
    {
        text.name = name;
        text.text = string.Empty;
        text.fontSize = 14f;
        text.fontSizeMin = 9f;
        text.fontSizeMax = 14f;
        text.enableAutoSizing = true;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Truncate;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.rectTransform.localScale = Vector3.one;
    }
    
    private static void BuildRows(WeaponSelector selector, out string labels, out string values, out int count)
    {
        labels = values = string.Empty;
        count = 0;
        var primary = selector.hardpointSet;
        if (primary == null)
            return;
        
        WeaponMount? mount;
        try
        {
            mount = selector.GetValue();
        }
        catch (ArgumentOutOfRangeException)
        {
            return;
        }
        catch (IndexOutOfRangeException)
        {
            return;
        }
        
        var physicalCount = primary.hardpoints?.Count ?? 0;
        var sibling = selector.symmetryWeaponSelector;
        if (sibling != null && !sibling.gameObject.activeSelf && sibling.hardpointSet != null &&
            sibling.hardpointSet != primary)
            physicalCount += sibling.hardpointSet.hardpoints?.Count ?? 0;
        
        var n = mount != null ? physicalCount : 0f;
        var labelBuilder = new StringBuilder(96);
        var valueBuilder = new StringBuilder(96);
        var skipEmptyMassConfigCheck = false;
        var skipEmptyDragConfigCheck = false;
        var skipEmptyRcsConfigCheck = false;
        
        if (Plugin.ShowRcs.Value)
        {
            var number = (mount?.RCS ?? 0f) * n;
            skipEmptyRcsConfigCheck =
                AppendRow(labelBuilder, valueBuilder, "RCS", number.ToString("F4"), number, ref count);
        }
        
        if (Plugin.ShowEmptyRcs.Value)
        {
            var number = (mount?.emptyRCS ?? 0f) * n;
            AppendRow(labelBuilder, valueBuilder, "Empty RCS", number.ToString("F4"), number, ref count,
                skipEmptyRcsConfigCheck);
        }
        
        if (Plugin.ShowMass.Value)
        {
            var number = (mount?.mass ?? 0f) * n;
            skipEmptyMassConfigCheck = AppendRow(labelBuilder, valueBuilder, "Mass",
                UnitConverter.WeightReading(number) ?? "0", number, ref count);
        }
        
        if (Plugin.ShowEmptyMass.Value)
        {
            var number = (mount?.emptyMass ?? 0f) * n;
            AppendRow(labelBuilder, valueBuilder, "Empty mass", UnitConverter.WeightReading(number) ?? "0", number,
                ref count, skipEmptyMassConfigCheck);
        }
        
        if (Plugin.ShowDrag.Value)
        {
            var number = (mount?.drag ?? 0f) * n;
            skipEmptyDragConfigCheck =
                AppendRow(labelBuilder, valueBuilder, "Drag", number.ToString("F3"), number, ref count);
        }
        
        if (Plugin.ShowEmptyDrag.Value)
        {
            var number = (mount?.emptyDrag ?? 0f) * n;
            AppendRow(labelBuilder, valueBuilder, "Empty drag", number.ToString("F3"), number, ref count,
                skipEmptyDragConfigCheck);
        }
        
        if (Plugin.ShowEmptyCost.Value)
        {
            var number = (mount?.emptyCost ?? 0f) * n;
            AppendRow(labelBuilder, valueBuilder, "Empty cost", UnitConverter.ValueReading(number) ?? "0", number,
                ref count);
        }
        
        labels = labelBuilder.ToString();
        values = valueBuilder.ToString();
    }
    
    private static bool AppendRow(StringBuilder labelBuilder, StringBuilder valueBuilder,
        string name, string formatted, float numeric, ref int count, bool skipEmptyConfigCheck = false)
    {
        if (Plugin.HideZeroValues.Value && !skipEmptyConfigCheck && Mathf.Abs(numeric) < 0.0000001f)
            return false;
        
        if (count > 0)
        {
            labelBuilder.Append('\n');
            valueBuilder.Append('\n');
        }
        
        labelBuilder.Append(name);
        valueBuilder.Append(formatted);
        count++;
        return true;
    }
    
    private void UpdateEmptyRcsRow()
    {
        if (_menu == null || !Plugin.ShowAircraftEmptyRcs.Value)
        {
            if (_emptyRcsRow != null)
                _emptyRcsRow.SetActive(false);
            
            SetInfoRowExtraHeight(0f);
            SetRightPanelExtraHeight(0f);
            return;
        }
        
        if (_emptyRcsRow == null && !TryCreateEmptyRcsRow())
            return;
        
        _emptyRcsRow!.SetActive(true);
        PositionEmptyRcsRowBelowExistingLabels();
        
        var extraInfo = CalculateRequiredInfoExtraHeight();
        SetInfoRowExtraHeight(extraInfo);
        SetRightPanelExtraHeight(RightPanelExtraHeight);
        var aircraft = _menu.previewAircraft;
        if (aircraft == null || aircraft.definition == null || aircraft.weaponManager == null || _emptyRcsValue == null)
            return;
        
        var rcs = aircraft.definition.radarSize;
        foreach (var set in aircraft.weaponManager.hardpointSets)
        {
            if (set?.weaponMount == null)
                continue;
            
            rcs += set.weaponMount.emptyRCS * (set.hardpoints?.Count ?? 0);
        }
        
        var text = rcs.ToString("F4");
        if (_emptyRcsValue.text != text)
            _emptyRcsValue.text = text;
    }
    
    private bool TryCreateEmptyRcsRow()
    {
        var infoPanel = _menu?.infoPanel;
        if (infoPanel == null)
            return false;
        
        var info = infoPanel.Find("Container/Info") as RectTransform;
        var source = info?.Find("RCSLabel") as RectTransform;
        if (info == null || source == null)
            return false;
        
        _infoRect = info;
        _rightPanel = infoPanel.parent as RectTransform;
        
        var row = Instantiate(source.gameObject, info);
        row.name = EmptyRcsRowName;
        var label = row.GetComponent<TextMeshProUGUI>();
        if (label != null)
        {
            label.text = "Empty RCS :";
            label.raycastTarget = false;
        }
        
        var value = row.transform.Find("RCS")?.GetComponent<TextMeshProUGUI>();
        if (value == null)
        {
            Destroy(row);
            return false;
        }
        
        value.name = "EmptyRCS";
        value.text = "0.0000";
        value.raycastTarget = false;
        _emptyRcsRow = row;
        _emptyRcsValue = value;
        return true;
    }
    
    private float GetLowestExistingInfoLabelBottom()
    {
        if (_infoRect == null)
            return -50f;
        var original = _infoRect.Find("RCSLabel") as RectTransform;
        var lowestBottom = original != null
            ? original.anchoredPosition.y - original.rect.height * original.pivot.y
            : -50f;
        
        foreach (Transform child in _infoRect)
        {
            if (child.gameObject == _emptyRcsRow || child is not RectTransform candidate || candidate.GetComponent<TextMeshProUGUI>() == null || !candidate.gameObject.activeSelf || candidate.anchorMin.y < 0.9f || candidate.anchorMax.y < 0.9f)
                continue;
            
            lowestBottom = Mathf.Min(lowestBottom,
                candidate.anchoredPosition.y - candidate.rect.height * candidate.pivot.y);
        }
        
        return lowestBottom;
    }
    
    private void PositionEmptyRcsRowBelowExistingLabels()
    {
        if (_infoRect == null || _emptyRcsRow == null)
            return;
        
        var row = _emptyRcsRow.transform as RectTransform;
        if (row == null)
            return;
        
        var y = GetLowestExistingInfoLabelBottom() - 10f - row.rect.height * (1f - row.pivot.y);
        if (Mathf.Abs(row.anchoredPosition.y - y) > 0.1f)
            row.anchoredPosition = new Vector2(row.anchoredPosition.x, y);
    }
    
    private float CalculateRequiredInfoExtraHeight()
    {

        if (_emptyRcsRow == null || _emptyRcsRow.transform is not RectTransform row)
            return 0f;
        
        var newBottom = row.anchoredPosition.y - row.rect.height * row.pivot.y;
        return Mathf.Max(0f, GetLowestExistingInfoLabelBottom() - newBottom);
    }
    
    private void SetRightPanelExtraHeight(float desired)
    {
        if (_rightPanel == null)
            return;
        
        var delta = desired - _appliedRightHeight;
        if (Mathf.Abs(delta) < 0.01f)
            return;
        
        _rightPanel.sizeDelta = new Vector2(_rightPanel.sizeDelta.x, _rightPanel.sizeDelta.y + delta);
        _appliedRightHeight = desired;
        LayoutRebuilder.MarkLayoutForRebuild(_rightPanel);
    }
    
    private void SetInfoRowExtraHeight(float desired)
    {
        if (_infoRect == null)
            return;
        
        var delta = desired - _appliedInfoHeight;
        if (Mathf.Abs(delta) < 0.01f)
            return;
        
        _infoRect.sizeDelta = new Vector2(_infoRect.sizeDelta.x, _infoRect.sizeDelta.y + delta);
        _appliedInfoHeight = desired;
        if (_infoRect.parent is RectTransform parent)
            LayoutRebuilder.MarkLayoutForRebuild(parent);
    }
    
    private sealed class Overlay
    {
        internal readonly TextMeshProUGUI Labels;
        internal readonly RectTransform Rect;
        internal readonly GameObject Root;
        internal readonly TextMeshProUGUI Values;
        internal float LastValueWidth = -1f;
        
        internal Overlay(GameObject root, RectTransform rect, TextMeshProUGUI labels, TextMeshProUGUI values)
        {
            Root = root;
            Rect = rect;
            Labels = labels;
            Values = values;
        }
    }
}