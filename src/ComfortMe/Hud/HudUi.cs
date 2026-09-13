using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ComfortMe;

internal static class HudUi
{
    private static Sprite whiteSprite;
    private static TMP_FontAsset cachedFont;

    internal static Sprite WhiteSprite
    {
        get
        {
            if (whiteSprite == null)
            {
                Texture2D tex = Texture2D.whiteTexture;
                whiteSprite = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }

            return whiteSprite;
        }
    }

    internal static TMP_FontAsset FontFrom(Hud hud)
    {
        Resolve(hud);
        return cachedFont;
    }

    internal static Material FontMaterialFrom(Hud hud)
    {
        return null;
    }

    /// <summary>
    /// True only while a place tool is equipped and the 1.0 (or legacy) piece menu is actually showing.
    /// Hotkey-unequip leaves BuildUi in memory; do not treat that as an open menu.
    /// </summary>
    internal static bool BuildMenuOpen(Hud hud)
    {
        Player player = Player.m_localPlayer;
        if (player == null || !player.InPlaceMode())
        {
            return false;
        }

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

    /// <summary>
    /// True while a place tool is equipped, including ghost placement with the piece grid closed.
    /// </summary>
    internal static bool HoldingPlaceTool()
    {
        Player player = Player.m_localPlayer;
        return player != null && player.InPlaceMode();
    }

    internal static void ApplyVanillaFont(TMP_Text dest, Hud hud)
    {
        if (dest == null)
        {
            return;
        }

        Resolve(hud);
        if (cachedFont != null)
        {
            dest.font = cachedFont;
        }
    }

    internal static TextMeshProUGUI CreateText(Transform parent, string name, TMP_FontAsset font, Material fontMaterial, float size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset resolved = font != null ? font : cachedFont;
        if (resolved != null)
        {
            tmp.font = resolved;
        }

        tmp.fontSize = size;
        tmp.raycastTarget = false;
        tmp.maskable = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.richText = true;
        return tmp;
    }

    internal static Image CreatePanel(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = WhiteSprite;
        image.color = color;
        image.raycastTarget = false;
        image.maskable = true;
        return image;
    }

    internal static string Hex(Color color)
    {
        return ColorUtility.ToHtmlStringRGB(color);
    }

    private static void Resolve(Hud hud)
    {
        if (cachedFont != null)
        {
            return;
        }

        TMP_Text source = FindSource(hud);
        if (source != null && source.font != null)
        {
            cachedFont = source.font;
            ComfortMePlugin.Log?.LogInfo($"Using HUD font '{cachedFont.name}'.");
            return;
        }

        TMP_FontAsset[] all = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        for (int i = 0; i < all.Length; i++)
        {
            TMP_FontAsset asset = all[i];
            if (asset == null)
            {
                continue;
            }

            string name = asset.name ?? string.Empty;
            if (name.IndexOf("Liberation", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                continue;
            }

            cachedFont = asset;
            ComfortMePlugin.Log?.LogInfo($"Using fallback font '{cachedFont.name}'.");
            return;
        }
    }

    private static TMP_Text FindSource(Hud hud)
    {
        if (hud == null)
        {
            return null;
        }

        if (IsUsable(hud.m_buildSelection))
        {
            return hud.m_buildSelection;
        }

        if (IsUsable(hud.m_pieceDescription))
        {
            return hud.m_pieceDescription;
        }

        TMP_Text[] texts = hud.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (!IsUsable(text) || IsOurs(text))
            {
                continue;
            }

            return text;
        }

        return null;
    }

    private static bool IsUsable(TMP_Text text)
    {
        return text != null && text.font != null;
    }

    private static bool IsOurs(TMP_Text text)
    {
        string name = text.name;
        return name == "Header" || name == "Body" || name == "Text";
    }
}
