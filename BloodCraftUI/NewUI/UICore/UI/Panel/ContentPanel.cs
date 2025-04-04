﻿using BloodCraftUI.NewUI.UICore.UI.Panel.Base;
using BloodCraftUI.NewUI.UICore.UniverseLib.UI;
using BloodCraftUI.NewUI.UICore.UniverseLib.UI.Panels;
using UnityEngine;
using UIBase = BloodCraftUI.NewUI.UICore.UniverseLib.UI.UIBase;

namespace BloodCraftUI.NewUI.UICore.UI.Panel
{
    public class ContentPanel : ResizeablePanelBase
    {
        public override string PanelId => "CorePanel";
        public override int MinWidth => 340;
        public override int MinHeight => 25;
        public override Vector2 DefaultAnchorMin => new Vector2(0.5f, 0.5f);
        public override Vector2 DefaultAnchorMax => new Vector2(0.5f, 0.5f);
        public override Vector2 DefaultPivot => new Vector2(0.5f, 0.5f);
        public override Vector2 DefaultPosition => new Vector2(0f, Owner.Scaler.m_ReferenceResolution.y);
        public override bool CanDrag => true;
        public override PanelDragger.ResizeTypes CanResize => PanelDragger.ResizeTypes.None;
        public override BCUIManager.Panels PanelType => BCUIManager.Panels.Base;
        private GameObject _uiAnchor;

        public ContentPanel(UIBase owner) : base(owner)
        {
        }

        protected override void ConstructPanelContent()
        {
            TitleBar.SetActive(false);
            _uiAnchor = UIFactory.CreateHorizontalGroup(ContentRoot, "UIAnchor", true, true, true, true);
            Dragger.DraggableArea = Rect;
            Dragger.OnEndResize();

            var text = UIFactory.CreateLabel(_uiAnchor, "UIAnchorText", $"BCUI {PluginInfo.PLUGIN_VERSION}");
            UIFactory.SetLayoutElement(text.gameObject, 0, 25, 1, 1);

            var boxListButton = UIFactory.CreateButton(_uiAnchor, "BoxListButton", "Box List");
            UIFactory.SetLayoutElement(boxListButton.GameObject, ignoreLayout: false, minWidth: 80, minHeight: 25);
            boxListButton.OnClick = () =>
            {
                BCUIManager.AddPanel(BCUIManager.Panels.BoxList);
            };
            var famStatsButton = UIFactory.CreateButton(_uiAnchor, "FamStatsButton", "Fam Stats");
            UIFactory.SetLayoutElement(famStatsButton.GameObject, ignoreLayout: false, minWidth: 80, minHeight: 25);
            famStatsButton.OnClick = () =>
            {
                BCUIManager.AddPanel(BCUIManager.Panels.FamStats);
            };

            SetDefaultSizeAndPosition();
        }

        protected override void LateConstructUI()
        {
            base.LateConstructUI();
        }

        internal override void Reset()
        {
        }

        public override void Update()
        {
            base.Update();
            // Call update on the panels that need it
        }
    }
}