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
/// 1.0 shows the same piece on many BuildUi lists (usage tags, material, recent, favorites, serving tray).
/// </summary>
internal static class BuildHudBadge
{
    private const string BadgeName = "ComfortMeBadge";

    private static readonly Color ChipBackground = new Color(0.05f, 0.07f, 0.06f, 0.88f);
    private static readonly Color UpgradeGreen = new Color(0.35f, 0.85f, 0.55f, 1f);
    private static readonly Color EqualGrey = new Color(0.72f, 0.72f, 0.72f, 1f);
    private static readonly Color LowerRed = new Color(0.85f, 0.36f, 0.36f, 1f);
    private static readonly Color UnlitAmber = new Color(0.95f, 0.72f, 0.28f, 1f);

    private static readonly FieldInfo PieceIconsField = AccessTools.Field(typeof(Hud), "m_pieceIcons");
    private static readonly FieldInfo IconGoField = AccessTools.Field(AccessTools.Inner(typeof(Hud), "PieceIconData"), "m_go");
    private static readonly FieldInfo BuildUiButtonsField = AccessTools.Field(typeof(BuildUi), "m_pieceButtons");
    private static readonly FieldInfo BuildUiSpecialField = AccessTools.Field(typeof(BuildUi), "m_specialPieceButton");
    private static readonly FieldInfo PieceButtonIconField = AccessTools.Field(typeof(BuildUiPieceButton), "m_icon");
    private static bool loggedBadgeSweep;

    internal static void Clear()
    {
    }

    internal static void Refresh(Hud hud, ComfortSnapshot snapshot)
    {
        if (hud == null || snapshot == null || !ModConfig.ShowValueBadge.Value || !MenuOpen(hud))
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

        TMP_FontAsset font = HudUi.FontFrom(hud);
        Material fontMaterial = HudUi.FontMaterialFrom(hud);
        RefreshLegacyGrid(hud, snapshot, player, font, fontMaterial);
        RefreshBuildUi(hud, snapshot, player, font, fontMaterial);
    }

    internal static void OnPieceButtonsRebuilt(Hud hud, ComfortSnapshot snapshot)
    {
        if (hud != null && hud.m_buildUi != null)
        {
            CleanupStrayBadges(hud.m_buildUi);
        }

        Refresh(hud, snapshot);
    }

    internal static void ApplyToButton(BuildUiPieceButton button, ComfortSnapshot snapshot)
    {
        if (button == null)
        {
            return;
        }

        Hud hud = Hud.instance;
        Player player = Player.m_localPlayer;
        if (hud == null || snapshot == null || player == null || !ModConfig.ShowValueBadge.Value)
        {
            HideOn(BadgeHost(button));
            HideOn(button.gameObject);
            return;
        }

        Apply(
            BadgeHost(button),
            button.Piece,
            snapshot,
            player,
            HudUi.FontFrom(hud),
            HudUi.FontMaterialFrom(hud));
    }

    internal static void ApplyToImage(Image image, Piece piece, ComfortSnapshot snapshot)
    {
        Hud hud = Hud.instance;
        Player player = Player.m_localPlayer;
        ApplyToImage(image, piece, snapshot, player, HudUi.FontFrom(hud), HudUi.FontMaterialFrom(hud));
    }

    private static bool MenuOpen(Hud hud)
    {
        if (hud != null && hud.m_buildUi != null && hud.m_buildUi.gameObject.activeInHierarchy)
        {
            return true;
        }

        if (Hud.IsPieceSelectionVisible())
        {
            return true;
        }

        return hud != null && hud.m_buildHud != null && hud.m_buildHud.activeInHierarchy;
    }

    private static void RefreshBuildUi(
        Hud hud,
        ComfortSnapshot snapshot,
        Player player,
        TMP_FontAsset font,
        Material fontMaterial)
    {
        if (hud.m_buildUi == null)
        {
            return;
        }

        if (BuildUiButtonsField?.GetValue(hud.m_buildUi) is IList buttons)
        {
            int comfort = 0;
            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i] is BuildUiPieceButton button && button != null)
                {
                    if (button.Piece != null && button.Piece.m_comfort > 0)
                    {
                        comfort++;
                    }

                    Apply(BadgeHost(button), button.Piece, snapshot, player, font, fontMaterial);
                }
            }

            if (!loggedBadgeSweep)
            {
                loggedBadgeSweep = true;
                ComfortMePlugin.Log?.LogInfo($"BuildUi chips: {buttons.Count} icons, {comfort} comfort pieces.");
            }
        }

        if (BuildUiSpecialField?.GetValue(hud.m_buildUi) is BuildUiPieceButton special && special != null)
        {
            Apply(BadgeHost(special), special.Piece, snapshot, player, font, fontMaterial);
        }
    }

    private static void RefreshLegacyGrid(
        Hud hud,
        ComfortSnapshot snapshot,
        Player player,
        TMP_FontAsset font,
        Material fontMaterial)
    {
        if (hud.m_pieceListRoot != null && !hud.m_pieceListRoot.gameObject.activeInHierarchy)
        {
            return;
        }

        List<Piece> pieces = player.GetBuildPieces();
        List<GameObject> iconGos = GetIconObjects(hud);
        if (pieces == null || iconGos.Count == 0)
        {
            return;
        }

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
                HideOn(go);
                continue;
            }

            Apply(go, pieces[i], snapshot, player, font, fontMaterial);
        }
    }

    private static void ApplyToImage(
        Image image,
        Piece piece,
        ComfortSnapshot snapshot,
        Player player,
        TMP_FontAsset font,
        Material fontMaterial)
    {
        if (image == null)
        {
            return;
        }

        Apply(image.gameObject, piece, snapshot, player, font, fontMaterial);
    }

    private static void Apply(
        GameObject iconGo,
        Piece piece,
        ComfortSnapshot snapshot,
        Player player,
        TMP_FontAsset font,
        Material fontMaterial)
    {
        if (iconGo == null)
        {
            return;
        }

        ComfortClassification classification = UpgradeClassifier.Classify(piece, snapshot);
        bool show;
        Color color;
        if (!ModConfig.ShowValueBadge.Value || classification.Verdict == ComfortVerdict.Skip)
        {
            show = false;
            color = UpgradeGreen;
        }
        else if (ModConfig.BadgeUpgradesOnly.Value)
        {
            show = UpgradeClassifier.ShouldBadge(player, piece, classification, snapshot);
            color = UpgradeGreen;
        }
        else
        {
            show = true;
            if (ModConfig.RequireMaterials.Value
                && player != null
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

        string label = show && piece != null ? "+" + piece.m_comfort : string.Empty;
        SetBadge(iconGo, font, fontMaterial, show, label, color);
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
            case ComfortVerdict.Unlit:
                return UnlitAmber;
            default:
                return EqualGrey;
        }
    }

    private static void HideAll(Hud hud)
    {
        if (hud == null)
        {
            return;
        }

        List<GameObject> iconGos = GetIconObjects(hud);
        for (int i = 0; i < iconGos.Count; i++)
        {
            HideOn(iconGos[i]);
        }

        if (hud.m_buildUi != null && BuildUiButtonsField?.GetValue(hud.m_buildUi) is IList buttons)
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i] is BuildUiPieceButton button && button != null)
                {
                    HideOn(BadgeHost(button));
                    HideOn(button.gameObject);
                }
            }
        }

        if (hud.m_buildUi != null && BuildUiSpecialField?.GetValue(hud.m_buildUi) is BuildUiPieceButton special)
        {
            if (special != null)
            {
                HideOn(BadgeHost(special));
                HideOn(special.gameObject);
            }
        }

        if (hud.m_buildIcon != null)
        {
            HideOn(hud.m_buildIcon.gameObject);
        }
    }

    private static GameObject BadgeHost(BuildUiPieceButton button)
    {
        if (button == null)
        {
            return null;
        }

        if (PieceButtonIconField?.GetValue(button) is Image icon && icon != null)
        {
            return icon.gameObject;
        }

        return button.gameObject;
    }

    private static void HideOn(GameObject go)
    {
        Transform existing = FindBadge(go != null ? go.transform : null);
        if (existing != null)
        {
            existing.gameObject.SetActive(false);
        }
    }

    private static Transform FindBadge(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        Transform direct = root.Find(BadgeName);
        if (direct != null)
        {
            return direct;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindBadge(root.GetChild(i));
            if (found != null)
            {
                return found;
            }
        }

        return null;
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

    private static void CleanupStrayBadges(BuildUi ui)
    {
        if (ui == null)
        {
            return;
        }

        Transform[] transforms = ui.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform t = transforms[i];
            if (t == null || t.name != BadgeName)
            {
                continue;
            }

            Canvas canvas = t.GetComponent<Canvas>();
            if (canvas != null)
            {
                UnityEngine.Object.DestroyImmediate(t.gameObject);
                continue;
            }

            BuildUiPieceButton owner = t.GetComponentInParent<BuildUiPieceButton>();
            GameObject host = owner != null ? BadgeHost(owner) : null;
            if (host == null || t.parent != host.transform)
            {
                UnityEngine.Object.DestroyImmediate(t.gameObject);
            }
        }
    }

    private static void SetBadge(GameObject iconGo, TMP_FontAsset font, Material fontMaterial, bool visible, string label, Color color)
    {
        Transform existing = FindBadge(iconGo.transform);
        if (existing != null && (existing.GetComponent<Canvas>() != null || existing.parent != iconGo.transform))
        {
            UnityEngine.Object.DestroyImmediate(existing.gameObject);
            existing = null;
        }

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

        chip.maskable = true;
        chip.gameObject.SetActive(true);
        chip.transform.SetAsLastSibling();
        TextMeshProUGUI text = chip.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
        {
            HudUi.ApplyVanillaFont(text, Hud.instance);
            text.text = label;
            text.color = color;
            text.maskable = true;
        }
    }

    private static Image CreateBadge(GameObject iconGo, TMP_FontAsset font, Material fontMaterial)
    {
        Image chip = HudUi.CreatePanel(iconGo.transform, BadgeName, ChipBackground);
        RectTransform rt = chip.rectTransform;
        // Bottom-right of the icon; stay a normal child so ScrollRect clips and the icon cannot cover us.
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.sizeDelta = new Vector2(28f, 16f);
        rt.anchoredPosition = new Vector2(-2f, 2f);
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
