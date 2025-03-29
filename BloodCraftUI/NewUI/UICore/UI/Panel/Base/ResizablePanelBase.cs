using System;
using BloodCraftUI.NewUI.UICore.UniverseLib.UI.Panels;
using BloodCraftUI.Utils;
using Panels_PanelBase = BloodCraftUI.NewUI.UICore.UniverseLib.UI.Panels.PanelBase;
using UIBase = BloodCraftUI.NewUI.UICore.UniverseLib.UI.UIBase;

namespace BloodCraftUI.NewUI.UICore.UI.Panel.Base;

public abstract class ResizeablePanelBase : Panels_PanelBase
{
    protected ResizeablePanelBase(UIBase owner) : base(owner) { }

    public override PanelDragger.ResizeTypes CanResize => PanelDragger.ResizeTypes.All;

    private bool ApplyingSaveData { get; set; } = true;

    protected override void ConstructPanelContent()
    {
        // Disable the title bar, but still enable the draggable box area (this now being set to the whole panel)
        TitleBar.SetActive(false);
        Dragger.DraggableArea = Rect;
        // Update resizer elements
        Dragger.OnEndResize();
    }

    /// <summary>
    /// Intended to be called when leaving a server to ensure joining the next can build up the UI correctly again
    /// </summary>
    internal abstract void Reset();

    protected override void OnClosePanelClicked()
    {
        // Do nothing for now
    }

    public override void OnFinishDrag()
    {
        base.OnFinishDrag();
        SaveInternalData();
    }

    public override void OnFinishResize()
    {
        base.OnFinishResize();
        SaveInternalData();
    }

    public void SaveInternalData()
    {
        if (ApplyingSaveData) return;

        SetSaveDataToConfigValue();
    }

    private void SetSaveDataToConfigValue()
    {
        Plugin.Instance.Config.Bind("Panels", $"{PanelType}", "", "Serialised panel data").Value = ToSaveData();
    }

    private string ToSaveData()
    {
        try
        {
            return string.Join("|", new string[]
            {
                Rect.RectAnchorsToString(),
                Rect.RectPositionToString()
            });
        }
        catch (Exception ex)
        {
            LogUtils.LogWarning($"Exception generating Panel save data: {ex}");
            return "";
        }
    }

    private void ApplySaveData()
    {
        var data = Plugin.Instance.Config.Bind("Panels", $"{PanelType}", "", "Serialised panel data").Value;
        ApplySaveData(data);
    }

    private void ApplySaveData(string data)
    {
        if (string.IsNullOrEmpty(data))
            return;
        string[] split = data.Split('|');

        try
        {
            Rect.SetAnchorsFromString(split[0]);
            Rect.SetPositionFromString(split[1]);
            EnsureValidSize();
            EnsureValidPosition();
        }
        catch
        {
            LogUtils.LogWarning("Invalid or corrupt panel save data! Restoring to default.");
            SetDefaultSizeAndPosition();
            SetSaveDataToConfigValue();
        }
    }

    protected override void LateConstructUI()
    {
        ApplyingSaveData = true;

        base.LateConstructUI();

        // apply panel save data or revert to default
        try
        {
            ApplySaveData();
        }
        catch (Exception ex)
        {
            LogUtils.LogError($"Exception loading panel save data: {ex}");
            SetDefaultSizeAndPosition();
        }

        ApplyingSaveData = false;

        Dragger.OnEndResize();
    }
}