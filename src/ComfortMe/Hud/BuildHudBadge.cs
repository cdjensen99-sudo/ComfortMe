using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ComfortMe;

/// <summary>
/// Corner +N chip on hammer icons. Must not tint the artwork (vanilla greys unaffordable pieces)
/// and must not use blue (selection cursor).
/// </summary>
internal static class BuildHudBadge
{
    private const string BadgeName = "ComfortMeBadge";

    private static readonly Color ChipBackground = new Color(0.05f, 0.07f, 0.06f, 0.88f);
    private static readonly Color UpgradeGreen = new Color(0.35f, 0.85f, 0.55f, 1f);
    private static readonly Color EqualGrey = new Color(0.72f, 0.72f, 0.72f, 1f);
    private static readonly Color LowerRed = new Color(0.85f, 0.36f, 0.36f, 1f);

    private static readonly FieldInfo PieceIconsField = AccessTools.Field(typeof(Hud), "m_pieceIcons");
    private static readonly FieldInfo IconGoField = AccessTools.Field(AccessTools.Inner(typeof(Hud), "PieceIconData"), "m_go");

    internal static void Clear()
    {
    }

    internal static void Refresh(Hud hud, ComfortSnapshot snapshot)
    {
        if (hud == null || snapshot == null || !ModConfig.ShowValueBadge.Value || hud.m_buildHud == null || !hud.m_buildHud.activeInHierarchy)
        {
            HideAll(hud);
            return;
        }

        Player player = Player.m_localPlayer;
        if (player == null)
        {
            HideAll(hud);
            return;
        }

        List<Piece> pieces = player.GetBuildPieces();
        List<GameObject> iconGos = GetIconObjects(hud);
        if (pieces == null || iconGos.Count == 0)
        {
            return;
        }

        TMP_FontAsset font = HudUi.FontFrom(hud);
        Material fontMaterial = HudUi.FontMaterialFrom(hud);
        int limit = Mathf.Min(pieces.Count, iconGos.Count);
        for (int i = 0; i < iconGos.Count; i++)
        {
            GameObject go = iconGos[i];
            if (go == null)
            {
                continue;
            }

            if (i >= limit)
            {
                SetBadge(go, font, fontMaterial, visible: false, string.Empty, UpgradeGreen);
                continue;
            }

            Piece piece = pieces[i];
            ComfortClassification classification = UpgradeClassifier.Classify(piece, snapshot);
            bool show;
            Color color;
            if (ModConfig.BadgeUpgradesOnly.Value)
            {
                show = UpgradeClassifier.ShouldBadge(player, piece, classification, snapshot);
                color = UpgradeGreen;
            }
            else
            {
                show = classification.Verdict != ComfortVerdict.Skip;
                if (ModConfig.RequireMaterials.Value
                    && !player.HaveRequirements(piece, Player.RequirementMode.CanBuild))
                {
                    show = false;
                }

                if (ModConfig.Policy.Value == HighlightPolicy.UpgradeOnly
                    && classification.Verdict == ComfortVerdict.NewGroup)
                {
                    show = false;
                }

                color = ColorFor(classification.Verdict);
            }

            string label = show ? "+" + piece.m_comfort : string.Empty;
            SetBadge(go, font, fontMaterial, show, label, color);
        }
    }

    private static Color ColorFor(ComfortVerdict verdict)
    {
        switch (verdict)
        {
            case ComfortVerdict.Upgrade:
            case ComfortVerdict.NewGroup:
                return UpgradeGreen;
            case ComfortVerdict.Downgrade:
                return LowerRed;
            default:
                return EqualGrey;
        }
    }

    private static void HideAll(Hud hud)
    {
        List<GameObject> iconGos = GetIconObjects(hud);
        for (int i = 0; i < iconGos.Count; i++)
        {
            GameObject go = iconGos[i];
            if (go == null)
            {
                continue;
            }

            Transform existing = go.transform.Find(BadgeName);
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
            }
        }
    }

    private static List<GameObject> GetIconObjects(Hud hud)
    {
        List<GameObject> result = new List<GameObject>();
        IList icons = PieceIconsField?.GetValue(hud) as IList;
        if (icons != null && IconGoField != null)
        {
            for (int i = 0; i < icons.Count; i++)
            {
                if (IconGoField.GetValue(icons[i]) is GameObject go)
                {
                    result.Add(go);
                }
            }

            return result;
        }

        if (hud != null && hud.m_pieceListRoot != null)
        {
            foreach (Transform child in hud.m_pieceListRoot)
            {
                if (child != null && child.name != BadgeName)
                {
                    result.Add(child.gameObject);
                }
            }
        }

        return result;
    }

    private static void SetBadge(GameObject iconGo, TMP_FontAsset font, Material fontMaterial, bool visible, string label, Color color)
    {
        Transform existing = iconGo.transform.Find(BadgeName);
        if (!visible)
        {
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
            }

            return;
        }

        Image chip = existing != null
            ? existing.GetComponent<Image>()
            : CreateBadge(iconGo, font, fontMaterial);
        if (chip == null)
        {
            return;
        }

        chip.gameObject.SetActive(true);
        TextMeshProUGUI text = chip.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
        {
            text.text = label;
            text.color = color;
        }
    }

    private static Image CreateBadge(GameObject iconGo, TMP_FontAsset font, Material fontMaterial)
    {
        Image chip = HudUi.CreatePanel(iconGo.transform, BadgeName, ChipBackground);
        RectTransform rt = chip.rectTransform;
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(28f, 16f);
        rt.anchoredPosition = new Vector2(-2f, -2f);
        rt.SetAsLastSibling();

        TextMeshProUGUI text = HudUi.CreateText(chip.transform, "Text", font, fontMaterial, 11f);
        text.alignment = TextAlignmentOptions.Center;
        text.color = UpgradeGreen;
        RectTransform textRt = text.rectTransform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        return chip;
    }
}
