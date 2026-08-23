using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ComfortMe;

internal static class HudUi
{
    private static Sprite whiteSprite;

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
        return hud != null && hud.m_buildSelection != null ? hud.m_buildSelection.font : null;
    }

    internal static Material FontMaterialFrom(Hud hud)
    {
        return hud != null && hud.m_buildSelection != null ? hud.m_buildSelection.fontSharedMaterial : null;
    }

    internal static TextMeshProUGUI CreateText(Transform parent, string name, TMP_FontAsset font, Material fontMaterial, float size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        if (fontMaterial != null)
        {
            tmp.fontSharedMaterial = fontMaterial;
        }

        tmp.fontSize = size;
        tmp.raycastTarget = false;
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
        return image;
    }

    internal static string Hex(Color color)
    {
        return ColorUtility.ToHtmlStringRGB(color);
    }
}
