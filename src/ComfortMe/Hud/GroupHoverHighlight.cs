using System.Collections;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ComfortMe;

/// <summary>
/// On the Comfort hammer row, hovering a grouped piece lightens the cell plate of every
/// unlocked sibling in that vanilla ComfortGroup. Never tints the icon or the +N chip.
/// </summary>
internal static class GroupHoverHighlight
{
    private const string OverlayName = "ComfortMeGroupPlate";
    private static readonly Color Lift = new Color(1f, 1f, 1f, 0.28f);

    private static readonly FieldInfo HoveredField = AccessTools.Field(typeof(BuildUi), "m_currentHoveredPieceButton");
    private static readonly FieldInfo ButtonsField = AccessTools.Field(typeof(BuildUi), "m_pieceButtons");
    private static readonly FieldInfo ButtonField = AccessTools.Field(typeof(BuildUiPieceButton), "m_button");
    private static readonly FieldInfo IconField = AccessTools.Field(typeof(BuildUiPieceButton), "m_icon");
    private static readonly FieldInfo UpgradeArrowField = AccessTools.Field(typeof(BuildUiPieceButton), "m_upgradeArrow");
    private static readonly FieldInfo FavoriteStarField = AccessTools.Field(typeof(BuildUiPieceButton), "m_favoriteStar");

    private static bool loggedPlate;

    internal static void Clear()
    {
        loggedPlate = false;
        HideAll();
    }

    internal static void Tick()
    {
        if (!ModConfig.Enabled.Value || !ModConfig.ShowGroupHoverHighlight.Value || !ComfortUsageTag.IsComfortTabSelected())
        {
            HideAll();
            return;
        }

        Hud hud = Hud.instance;
        BuildUi ui = hud != null ? hud.m_buildUi : null;
        if (ui == null)
        {
            HideAll();
            return;
        }

        BuildUiPieceButton hovered = HoveredField?.GetValue(ui) as BuildUiPieceButton;
        Piece hoverPiece = hovered != null ? hovered.Piece : null;
        Piece.ComfortGroup group = hoverPiece != null ? hoverPiece.m_comfortGroup : Piece.ComfortGroup.None;
        bool highlight = hoverPiece != null && hoverPiece.m_comfort > 0 && group != Piece.ComfortGroup.None;

        if (ButtonsField?.GetValue(ui) is not IList buttons)
        {
            return;
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] is not BuildUiPieceButton button || button == null)
            {
                continue;
            }

            Piece piece = button.Piece;
            bool match = highlight
                && piece != null
                && piece.m_comfort > 0
                && piece.m_comfortGroup == group;
            SetLift(button, match);
        }
    }

    private static void HideAll()
    {
        Hud hud = Hud.instance;
        BuildUi ui = hud != null ? hud.m_buildUi : null;
        if (ui == null || ButtonsField?.GetValue(ui) is not IList buttons)
        {
            return;
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] is BuildUiPieceButton button && button != null)
            {
                SetLift(button, false);
            }
        }
    }

    private static void SetLift(BuildUiPieceButton button, bool lifted)
    {
        Image plate = ResolvePlate(button);
        if (plate == null)
        {
            return;
        }

        Transform existing = plate.transform.Find(OverlayName);
        if (!lifted)
        {
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
            }

            return;
        }

        Image overlay = existing != null ? existing.GetComponent<Image>() : null;
        if (overlay == null)
        {
            overlay = HudUi.CreatePanel(plate.transform, OverlayName, Lift);
            RectTransform rt = overlay.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            overlay.raycastTarget = false;
            overlay.maskable = true;
            if (!loggedPlate)
            {
                loggedPlate = true;
                ComfortMePlugin.Log?.LogInfo($"Group hover plate '{plate.name}' on '{button.name}'.");
            }
        }

        overlay.sprite = plate.sprite != null ? plate.sprite : HudUi.WhiteSprite;
        overlay.type = plate.type;
        overlay.color = Lift;
        overlay.gameObject.SetActive(true);
        overlay.transform.SetAsFirstSibling();
    }

    private static Image ResolvePlate(BuildUiPieceButton button)
    {
        Image icon = IconField?.GetValue(button) as Image;
        if (ButtonField?.GetValue(button) is Button uiButton
            && uiButton.targetGraphic is Image target
            && !IsForbidden(target, button, icon))
        {
            return target;
        }

        Image root = button.GetComponent<Image>();
        if (root != null && !IsForbidden(root, button, icon))
        {
            return root;
        }

        Image[] images = button.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (!IsForbidden(image, button, icon))
            {
                return image;
            }
        }

        return null;
    }

    private static bool IsForbidden(Image image, BuildUiPieceButton button, Image icon)
    {
        if (image == null)
        {
            return true;
        }

        if (image == icon
            || image.name == OverlayName
            || image.name == "ComfortMeBadge")
        {
            return true;
        }

        if (UpgradeArrowField?.GetValue(button) is Image arrow && image == arrow)
        {
            return true;
        }

        if (FavoriteStarField?.GetValue(button) is Image star && image == star)
        {
            return true;
        }

        return false;
    }
}
