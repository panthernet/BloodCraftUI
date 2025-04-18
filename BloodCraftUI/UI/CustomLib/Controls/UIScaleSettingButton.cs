using System.Collections.Generic;
using BloodCraftUI.UI.UniverseLib.UI;
using UnityEngine;

namespace BloodCraftUI.UI.CustomLib.Controls;

public class UIScaleSettingButton : SettingsButtonBase
{
    private readonly List<(string, Vector2)> scales = new()
    {
        ("tiny", new(3840, 2160)),
        ("small", new(2560, 1440)),
        ("normal", new(1920, 1080)),
        ("medium", new(1600, 900))
    };

    private int scaleIndex;

    public UIScaleSettingButton() : base("UIScale", "normal")
    {
        scaleIndex = State switch
        {
            "tiny" => 0,
            "small" => 1,
            "normal" => 2,
            "medium" => 3,
            _ => 2
        };

        ApplyScale(scales[scaleIndex].Item2);
    }

    public override string PerformAction()
    {
        scaleIndex = (scaleIndex + 1) % scales.Count;
        ApplyScale(scales[scaleIndex].Item2);

        Setting.Value = scales[scaleIndex].Item1;
        return scales[scaleIndex].Item1;
    }

    protected override string Label()
    {
        return $"Toggle screen size [{scales[scaleIndex].Item1}]";
    }

    private void ApplyScale(Vector2 newScale)
    {
        UniversalUI.CanvasDimensions = newScale;
        foreach (var uiBase in UniversalUI.uiBases)
        {
            uiBase.Scaler.referenceResolution = newScale;
            uiBase.Panels.ValidatePanels();
        }
    }
}