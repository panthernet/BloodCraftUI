﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BloodCraftUI.NewUI.UICore.UI.Panel.Base;
using BloodCraftUI.NewUI.UICore.UniverseLib.UI;
using BloodCraftUI.NewUI.UICore.UniverseLib.UI.Panels;
using BloodCraftUI.Services;
using System.Timers;
using BloodCraftUI.Config;
using BloodCraftUI.NewUI.UICore.UI.Controls;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BloodCraftUI.NewUI.UICore.UI.Util;

namespace BloodCraftUI.NewUI.UICore.UI.Panel
{
    internal class FamStatsPanel : ResizeablePanelBase
    {
        public override string PanelId => "FamStatsPanel";
        public override int MinWidth => 340;
        public override int MinHeight => 300;
        public override Vector2 DefaultAnchorMin => new Vector2(0.5f, 0.5f);
        public override Vector2 DefaultAnchorMax => new Vector2(0.5f, 0.5f);
        public override Vector2 DefaultPivot => new Vector2(0.5f, 0.5f);
        public override Vector2 DefaultPosition => new Vector2(Owner.Scaler.m_ReferenceResolution.x - 150,
            Owner.Scaler.m_ReferenceResolution.y * 0.5f);
        public override bool CanDrag => true;
        private readonly Color _pbColor = new Color(1f, 50f, 32f, 255f);

        // Allow vertical resizing only
        public override PanelDragger.ResizeTypes CanResize => PanelDragger.ResizeTypes.None;

        public override BCUIManager.Panels PanelType => BCUIManager.Panels.FamStats;
        private GameObject _uiAnchor;
        private Timer _queryTimer;
        private FamStats _data = new();

        // Controls for an update
        private TextMeshProUGUI _nameLabel;
        private TextMeshProUGUI _levelLabel;
        private GameObject _statsContainer;
        private ProgressBar _progressBar;

        private readonly Dictionary<string, GameObject> _statRowPool = new();

        public FamStatsPanel(UIBase owner) : base(owner)
        {
        }

        private void RecalculateHeight()
        {
            if (_uiAnchor == null) return;

            Canvas.ForceUpdateCanvases();

            // Force a recursive layout update
            foreach (var child in _uiAnchor.transform)
            {
                var childRect = child as RectTransform;
                if (childRect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(childRect);
                }
            }

            // Get VerticalLayoutGroup to account for its spacing and padding
            var vlg = _uiAnchor.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) return;

            // Count active children and calculate height
            float contentHeight = 0;
            int activeChildCount = 0;

            foreach (Transform child in _uiAnchor.transform)
            {
                if (child.gameObject.activeSelf)
                {
                    RectTransform childRect = child as RectTransform;
                    if (childRect != null)
                    {
                        contentHeight += LayoutUtility.GetPreferredHeight(childRect);
                        activeChildCount++;
                    }
                }
            }

            // Add spacing between children
            if (activeChildCount > 1)
                contentHeight += (activeChildCount - 1) * vlg.spacing;

            // Add padding
            contentHeight += vlg.padding.top + vlg.padding.bottom + 10f;

            // Set panel height
            if (Rect != null)
            {
                Rect.sizeDelta = new Vector2(Rect.sizeDelta.x, contentHeight);
            }
        }

        public void UpdateData(FamStats data)
        {
            if (data == null) return;

            var doFlash = _data != null && _data.ExperiencePercent != data.ExperiencePercent;
            _data = data;

            // Ensure we have a name to display
            string nameToShow = !string.IsNullOrEmpty(data.Name) ? data.Name : "Unknown Familiar";

            // Update name with school if available
            var schoolText = string.IsNullOrEmpty(data.School) ? "" : $" - {data.School}";
            if (_nameLabel != null)
                _nameLabel.text = $"{nameToShow}{schoolText}";

            // Update level info
            if (_levelLabel != null)
                _levelLabel.text =
                    $"Level: {data.Level}{(data.PrestigeLevel == 0 ? null : $"   Prestige: {data.PrestigeLevel}")}";

            // Track which rows we've used in this update
            var usedKeys = new HashSet<string>();

            // First, update the fixed stats
            UpdateStatRow("Health", data.MaxHealth, usedKeys);
            UpdateStatRow("Physical Power", data.PhysicalPower, usedKeys);
            UpdateStatRow("Spell Power", data.SpellPower, usedKeys);

            // Then update dynamic stats
            if (data.Stats != null)
            {
                foreach (var kvp in data.Stats)
                {
                    UpdateStatRow(kvp.Key, kvp.Value, usedKeys);
                }
            }

            // Hide any rows that aren't being used in this update
            foreach (var key in _statRowPool.Keys.ToList())
            {
                if (!usedKeys.Contains(key) && _statRowPool.TryGetValue(key, out GameObject row))
                {
                    row.SetActive(false);
                }
            }

            // Update progress bar
            if (_progressBar != null)
            {
                _progressBar.SetProgress(
                    data.ExperiencePercent / 100f,
                    "",
                    $"XP: {data.ExperienceValue} ({data.ExperiencePercent}%)",
                    ActiveState.Active,
                    _pbColor,
                    data.ExperienceValue.ToString(),
                    doFlash
                );
            }

            // Schedule layout rebuild
            CoroutineUtility.StartCoroutine(DelayedLayoutRebuild());
        }

        private IEnumerator DelayedLayoutRebuild()
        {
            // Wait for the end of the frame
            yield return new WaitForEndOfFrame();

            // Then force layout rebuild
            if (_uiAnchor != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(_uiAnchor.GetComponent<RectTransform>());
                RecalculateHeight();
            }
        }

        private void UpdateStatRow(string label, string value, HashSet<string> usedKeys)
        {
            if (string.IsNullOrEmpty(label)) return;
            usedKeys.Add(label);

            GameObject row;
            TextMeshProUGUI valueText;

            // Get or create the row
            if (_statRowPool.TryGetValue(label, out row))
            {
                // Row exists, just activate it
                row.SetActive(true);
                valueText = row.transform.Find($"{label}Value")?.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                // Create new row
                CreateStatRow(_statsContainer, label, out row, out valueText);
                _statRowPool[label] = row;
            }

            // Update the value
            if (valueText != null)
            {
                valueText.text = value ?? "0";
            }
        }

        protected override void ConstructPanelContent()
        {
            // Hide the title bar and set up the panel
            TitleBar.SetActive(false);
            Dragger.DraggableArea = Rect;
            Dragger.OnEndResize();

            // Modify ContentRoot to ensure it has no extra padding
            RectTransform contentRootRect = ContentRoot.GetComponent<RectTransform>();
            contentRootRect.anchorMin = Vector2.zero;
            contentRootRect.anchorMax = Vector2.one;
            contentRootRect.offsetMin = Vector2.zero;
            contentRootRect.offsetMax = Vector2.zero;

            // Remove any layout group on ContentRoot that might add spacing
            VerticalLayoutGroup existingVLG = ContentRoot.GetComponent<VerticalLayoutGroup>();
            if (existingVLG != null)
            {
                UnityEngine.Object.Destroy(existingVLG);
            }

            // Set ContentRoot layout element to fill available space
            UIFactory.SetLayoutElement(ContentRoot, flexibleWidth: 9999, flexibleHeight: 9999);

            var color = Colour.PanelBackground;
            color.a = 200f;

            // Create main container with explicit settings to eliminate bottom space
            _uiAnchor = UIFactory.CreateUIObject("UIAnchor", ContentRoot);
            _uiAnchor.AddComponent<Image>().color = color;

            // Set _uiAnchor to fill ContentRoot exactly
            RectTransform anchorRect = _uiAnchor.GetComponent<RectTransform>();
            anchorRect.anchorMin = Vector2.zero;
            anchorRect.anchorMax = Vector2.one;
            anchorRect.offsetMin = Vector2.zero;
            anchorRect.offsetMax = Vector2.zero;

            // Add a vertical layout group with explicit settings
            VerticalLayoutGroup vlg = _uiAnchor.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter; // Align to top
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false; // Critical to prevent extra space
            vlg.childForceExpandWidth = true;
            vlg.spacing = 2;
            vlg.padding.left = 8; 
            vlg.padding.right = 8;
            vlg.padding.top = 4;
            vlg.padding.bottom = 4;

            // Create header section
            CreateHeaderSection();

            // Create stats container
            CreateStatsSection();

            // Create XP progress bar at the bottom
            CreateProgressBarSection();

            // Set default position
            SetDefaultSizeAndPosition();
        }

        private void CreateHeaderSection()
        {
            // Create container with reduced height and spacing
            var headerContainer = UIFactory.CreateVerticalGroup(_uiAnchor, "HeaderContainer", false, false, true, true, 2,
                default, new Color(0.15f, 0.15f, 0.15f));
            UIFactory.SetLayoutElement(headerContainer, minHeight: 60, preferredHeight: 60, flexibleHeight: 0, flexibleWidth: 9999);

            // Familiar name with larger font
            _nameLabel = UIFactory.CreateLabel(headerContainer, "FamNameText", BloodCraftStateService.CurrentFamName ?? "Unknown",
                TextAlignmentOptions.Center, null, 18);
            UIFactory.SetLayoutElement(_nameLabel.gameObject, minHeight: 25, preferredHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);
            _nameLabel.fontStyle = FontStyles.Bold;

            // Level info - reduced height
            _levelLabel = UIFactory.CreateLabel(headerContainer, "FamLevelText", "Level: Unknown   Prestige: Unknown",
                TextAlignmentOptions.Center, null, 16);
            UIFactory.SetLayoutElement(_levelLabel.gameObject, minHeight: 25, preferredHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);
        }

        private void CreateStatsSection()
        {
            // Stats container with reduced height and tighter spacing
            _statsContainer = UIFactory.CreateVerticalGroup(_uiAnchor, "StatsContainer", true, false, true, true, 2,
                new Vector4(4, 2, 4, 2), new Color(0.12f, 0.12f, 0.12f));
            UIFactory.SetLayoutElement(_statsContainer, minHeight: 20, preferredHeight: 120, flexibleHeight: 0, flexibleWidth: 9999);
        }

        private void CreateStatRow(GameObject parent, string label, out GameObject rowObj, out TextMeshProUGUI valueText)
        {
            // Create a horizontal row with reduced height
            rowObj = UIFactory.CreateHorizontalGroup(parent, $"{label}Row", false, false, true, true, 5,
                default, new Color(0.18f, 0.18f, 0.18f));
            UIFactory.SetLayoutElement(rowObj, minHeight: 28, preferredHeight: 28, flexibleHeight: 0, flexibleWidth: 9999);

            // Stat label - reduced height
            var statLabel = UIFactory.CreateLabel(rowObj, $"{label}Label", label, TextAlignmentOptions.Left, null, 15);
            UIFactory.SetLayoutElement(statLabel.gameObject, minWidth: 150, flexibleWidth: 0, minHeight: 28, flexibleHeight: 0);

            // Value display - reduced height
            valueText = UIFactory.CreateLabel(rowObj, $"{label}Value", "0", TextAlignmentOptions.Right, Color.white, 15);
            UIFactory.SetLayoutElement(valueText.gameObject, minWidth: 100, flexibleWidth: 9999, minHeight: 28, flexibleHeight: 0);
            valueText.fontStyle = FontStyles.Bold;
        }

        private void CreateProgressBarSection()
        {
            // Create bare container without layout elements
            var progressBarHolder = UIFactory.CreateUIObject("ProgressBarContent", _uiAnchor);

            // Set fixed height with no flexibility
            UIFactory.SetLayoutElement(progressBarHolder, minHeight: 25, preferredHeight: 25,
                flexibleHeight: 0, flexibleWidth: 9999);

            // Create the progress bar
            _progressBar = new ProgressBar(progressBarHolder, Colour.DefaultBar);

            // Set initial progress
            _progressBar.SetProgress(0f, "", "XP: 0 (0%)", ActiveState.Active, Colour.DefaultBar, "", false);
        }

        internal override void Reset()
        {
            // Clean up timer if needed
            if (_queryTimer != null)
            {
                _queryTimer.Stop();
                _queryTimer.Dispose();
                _queryTimer = null;
            }

            // Reset progress bar if needed
            _progressBar?.Reset();

            // Clear the stat row pool on panel reset (when leaving the server, etc.)
            if (_statRowPool != null)
            {
                foreach (var kvp in _statRowPool)
                {
                    if (kvp.Value != null)
                    {
                        kvp.Value.SetActive(false);
                    }
                }
            }
        }

        public override void OnFinishResize()
        {
            base.OnFinishResize();

            // After manual resize, make sure content still fits
            LayoutRebuilder.ForceRebuildLayoutImmediate(ContentRoot.GetComponent<RectTransform>());
        }

        protected override void LateConstructUI()
        {
            base.LateConstructUI();

            if (Plugin.IS_TESTING)
            {
                UpdateData(new FamStats
                {
                    Name = "TestFamiliar",
                    Level = 99,
                    PrestigeLevel = 5,
                    ExperienceValue = 6500,
                    ExperiencePercent = 65,
                    MaxHealth = "5000",
                    PhysicalPower = "450",
                    SpellPower = "575",
                    School = "Unholy",
                    Stats = new System.Collections.Generic.Dictionary<string, string>()
                    {
                        {"Stat1", "Value1"},
                        {"Stat2", "Value2"},
                        {"Stat3", "Value3"},
                        {"Stat4", "Value4"},
                        {"Stat5", "Value5"},
                    }
                });
            }

            // Delay the initial height calculation
            CoroutineUtility.StartCoroutine(DelayedHeightCalculation());

            // Start querying for updates
            SendUpdateStatsCommand();
            _queryTimer = new Timer(Settings.FamStatsQueryIntervalInSeconds * 1000);
            _queryTimer.AutoReset = true;
            _queryTimer.Elapsed += (_, _) =>
            {
                SendUpdateStatsCommand();
                if (Plugin.IS_TESTING)
                {
                    _data.ExperiencePercent += 10;
                    if (_data.ExperiencePercent > 100)
                        _data.ExperiencePercent = 0;
                    UpdateData(_data);
                }
            };
            _queryTimer.Start();
        }

        private IEnumerator DelayedHeightCalculation()
        {
            // Wait a few frames for layout to stabilize
            yield return null;
            yield return null;
            yield return null;

            // Calculate the height based on content
            RecalculateHeight();
        }

        private void SendUpdateStatsCommand()
        {
            MessageService.EnqueueMessage(MessageService.BCCOM_FAMSTATS);
        }
    }
}