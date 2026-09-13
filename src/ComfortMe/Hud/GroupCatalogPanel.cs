using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ComfortMe;

/// <summary>
/// Slim room breakdown: vanilla total plus the pieces that currently count.
/// Lives on its own Screen Space Overlay canvas so the 1.0 hammer canvas cannot cover it.
/// </summary>
internal static class GroupCatalogPanel
{
    private const string PanelName = "ComfortMeCatalog";
    private const int OverlaySorting = 5000;

    private static readonly Color PanelBackground = new Color(0.04f, 0.05f, 0.05f, 0.92f);
    private static readonly Color HeaderColor = new Color(0.92f, 0.93f, 0.9f, 1f);

    private static Image panel;
    private static TextMeshProUGUI header;
    private static TextMeshProUGUI body;
    private static Canvas overlay;
    private static string lastText;
    private static bool loggedCreate;
    private static bool loggedPlace;

    internal static void Hide()
    {
        if (panel != null)
        {
            panel.gameObject.SetActive(false);
        }

        if (overlay != null && overlay)
        {
            overlay.gameObject.SetActive(false);
        }

        lastText = null;
    }

    internal static bool IsHovered()
    {
        if (panel == null || !panel.gameObject.activeInHierarchy)
        {
            return false;
        }

        Canvas canvas = panel.canvas;
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        return RectTransformUtility.RectangleContainsScreenPoint(panel.rectTransform, Input.mousePosition, cam);
    }

    internal static void Clear()
    {
        lastText = null;
        loggedCreate = false;
        loggedPlace = false;
        header = null;
        body = null;
        if (panel != null && panel)
        {
            UnityEngine.Object.Destroy(panel.gameObject);
        }

        panel = null;
        if (overlay != null && overlay)
        {
            UnityEngine.Object.Destroy(overlay.gameObject);
        }

        overlay = null;
    }

    internal static void Dump()
    {
        string msg;
        if (panel == null || !panel)
        {
            msg = "ComfortMe catalog: panel is null (hammer menu may be closed).";
        }
        else
        {
            RectTransform rt = panel.rectTransform;
            Transform parent = panel.transform.parent;
            string bodyPreview = body != null && !string.IsNullOrEmpty(body.text)
                ? body.text.Replace('\n', '|')
                : "";
            if (bodyPreview.Length > 160)
            {
                bodyPreview = bodyPreview.Substring(0, 160);
            }

            msg =
                "ComfortMe catalog: active=" + panel.gameObject.activeInHierarchy
                + " overlay=" + (overlay != null && overlay ? overlay.sortingOrder.ToString() : "null")
                + " parent=" + (parent != null ? parent.name : "null")
                + " anchors=" + rt.anchorMin + "->" + rt.anchorMax
                + " pos=" + rt.anchoredPosition
                + " size=" + rt.rect.size
                + " screen=" + Screen.width + "x" + Screen.height
                + " font=" + (header != null && header.font != null ? header.font.name : "null")
                + " header='" + (header != null ? header.text : "") + "'"
                + " body='" + bodyPreview + "'";
        }

        ComfortMePlugin.Log?.LogInfo(msg);
        if (Console.instance != null)
        {
            Console.instance.Print(msg);
        }

        ComfortSnapshot snap = ComfortSnapshot.Capture(Player.m_localPlayer);
        if (snap == null || snap.CountingPieces.Count == 0)
        {
            return;
        }

        List<string> names = new List<string>(snap.CountingPieces.Count);
        for (int i = 0; i < snap.CountingPieces.Count; i++)
        {
            Piece piece = snap.CountingPieces[i];
            if (piece == null)
            {
                continue;
            }

            names.Add(PieceDiscovery.LocalName(piece));
        }

        string counting = "ComfortMe counting: " + string.Join(", ", names.ToArray());
        ComfortMePlugin.Log?.LogInfo(counting);
        if (Console.instance != null)
        {
            Console.instance.Print(counting);
        }
    }

    internal static void RefreshFromHud(Hud hud, ComfortSnapshot snapshot)
    {
        Refresh(hud, snapshot);
    }

    internal static void Refresh(Hud hud, ComfortSnapshot snapshot)
    {
        if (hud == null || snapshot == null || !ModConfig.Enabled.Value || !ModConfig.ShowGroupCatalog.Value || !HudUi.HoldingPlaceTool())
        {
            Hide();
            return;
        }

        EnsurePanel(hud);
        if (panel == null)
        {
            return;
        }

        if (overlay != null && overlay)
        {
            overlay.gameObject.SetActive(true);
        }

        PlaceBesideBuildUi(hud);
        HudUi.ApplyVanillaFont(header, hud);
        HudUi.ApplyVanillaFont(body, hud);
        panel.gameObject.SetActive(true);

        StringBuilder builder = new StringBuilder(256);
        builder.Append("This room: ").Append(snapshot.Total);
        if (!snapshot.InShelter)
        {
            builder.Append("  (unsheltered)");
        }

        builder.Append('\n');
        builder.Append("Base  +1");
        if (snapshot.InShelter)
        {
            builder.Append("\nShelter  +1");
        }
        else
        {
            builder.Append("\nShelter  missing");
        }

        AppendCounting(builder, snapshot);

        string text = builder.ToString();
        if (text == lastText)
        {
            return;
        }

        lastText = text;
        int split = text.IndexOf('\n');
        if (header != null)
        {
            header.text = split >= 0 ? text.Substring(0, split) : text;
        }

        if (body != null)
        {
            body.text = split >= 0 ? text.Substring(split + 1) : string.Empty;
            body.gameObject.SetActive(true);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(panel.rectTransform);
    }

    private static void AppendCounting(StringBuilder builder, ComfortSnapshot snapshot)
    {
        List<KeyValuePair<Piece.ComfortGroup, int>> groups = new List<KeyValuePair<Piece.ComfortGroup, int>>(snapshot.GroupMax);
        groups.Sort((a, b) => b.Value.CompareTo(a.Value));
        for (int i = 0; i < groups.Count; i++)
        {
            Piece.ComfortGroup group = groups[i].Key;
            int value = groups[i].Value;
            snapshot.GroupSource.TryGetValue(group, out string source);
            builder.Append('\n');
            builder.Append(string.IsNullOrEmpty(source) ? PieceDiscovery.GroupLabel(group) : source);
            builder.Append("  +").Append(value);
            builder.Append("  (").Append(PieceDiscovery.GroupLabel(group)).Append(')');
        }

        for (int i = 0; i < snapshot.Uniques.Count; i++)
        {
            UniqueComfort unique = snapshot.Uniques[i];
            builder.Append('\n');
            builder.Append(unique.Name).Append("  +").Append(unique.Comfort);
        }
    }

    private static void EnsurePanel(Hud hud)
    {
        if (panel != null && !panel)
        {
            panel = null;
            header = null;
            body = null;
            lastText = null;
        }

        Transform parent = ResolveParent(hud);
        if (panel != null && parent != null && panel.transform.parent != parent)
        {
            UnityEngine.Object.Destroy(panel.gameObject);
            panel = null;
            header = null;
            body = null;
            lastText = null;
        }

        if (panel != null)
        {
            return;
        }

        if (parent == null)
        {
            return;
        }

        TMP_FontAsset font = HudUi.FontFrom(hud);
        panel = HudUi.CreatePanel(parent, PanelName, PanelBackground);
        panel.maskable = false;
        RectTransform rt = panel.rectTransform;
        rt.sizeDelta = new Vector2(320f, 0f);
        rt.SetAsLastSibling();

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 8, 8);
        layout.spacing = 3f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        header = HudUi.CreateText(panel.transform, "Header", font, null, 16f);
        header.alignment = TextAlignmentOptions.TopLeft;
        header.color = HeaderColor;
        header.maskable = false;
        header.textWrappingMode = TextWrappingModes.Normal;
        LayoutElement headerLayout = header.gameObject.AddComponent<LayoutElement>();
        headerLayout.minWidth = 280f;
        headerLayout.minHeight = 22f;
        HudUi.ApplyVanillaFont(header, hud);

        body = HudUi.CreateText(panel.transform, "Body", font, null, 14f);
        body.alignment = TextAlignmentOptions.TopLeft;
        body.color = HeaderColor;
        body.maskable = false;
        body.textWrappingMode = TextWrappingModes.Normal;
        LayoutElement bodyLayout = body.gameObject.AddComponent<LayoutElement>();
        bodyLayout.minWidth = 280f;
        bodyLayout.minHeight = 40f;
        HudUi.ApplyVanillaFont(body, hud);

        if (!loggedCreate)
        {
            loggedCreate = true;
            ComfortMePlugin.Log?.LogInfo("Catalog shown on overlay canvas (room breakdown).");
        }

        PlaceBesideBuildUi(hud);
    }

    private static void PlaceBesideBuildUi(Hud hud)
    {
        if (panel == null || hud == null)
        {
            return;
        }

        RectTransform rt = panel.rectTransform;
        RectTransform parentRt = rt.parent as RectTransform;
        if (parentRt == null)
        {
            return;
        }

        rt.sizeDelta = new Vector2(320f, 0f);
        rt.SetAsLastSibling();
        rt.pivot = new Vector2(0f, 1f);
        rt.anchorMin = new Vector2(0.84f, 0.78f);
        rt.anchorMax = new Vector2(0.84f, 0.78f);
        rt.anchoredPosition = Vector2.zero;
        LogPlaceOnce($"Catalog dock 0.84,0.78 parent={parentRt.name} size={parentRt.rect.size}");
    }

    private static void LogPlaceOnce(string message)
    {
        if (loggedPlace)
        {
            return;
        }

        loggedPlace = true;
        ComfortMePlugin.Log?.LogInfo(message);
    }

    private static Transform ResolveParent(Hud hud)
    {
        EnsureOverlay(hud);
        return overlay != null ? overlay.transform : null;
    }

    private static void EnsureOverlay(Hud hud)
    {
        if (overlay != null && !overlay)
        {
            overlay = null;
        }

        if (overlay != null)
        {
            overlay.gameObject.SetActive(true);
            return;
        }

        Canvas hudCanvas = null;
        if (hud.m_rootObject != null)
        {
            hudCanvas = hud.m_rootObject.GetComponent<Canvas>()
                ?? hud.m_rootObject.GetComponentInParent<Canvas>();
        }

        Transform host = hudCanvas != null && hudCanvas.transform.parent != null
            ? hudCanvas.transform.parent
            : hud.transform;

        GameObject go = new GameObject("ComfortMeOverlay");
        go.layer = hudCanvas != null ? hudCanvas.gameObject.layer : 5;
        go.transform.SetParent(host, false);

        overlay = go.AddComponent<Canvas>();
        overlay.renderMode = RenderMode.ScreenSpaceOverlay;
        overlay.overrideSorting = true;
        overlay.sortingOrder = OverlaySorting;
        overlay.pixelPerfect = false;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        CanvasScaler hudScaler = hudCanvas != null ? hudCanvas.GetComponent<CanvasScaler>() : null;
        if (hudScaler != null)
        {
            scaler.uiScaleMode = hudScaler.uiScaleMode;
            scaler.referenceResolution = hudScaler.referenceResolution;
            scaler.screenMatchMode = hudScaler.screenMatchMode;
            scaler.matchWidthOrHeight = hudScaler.matchWidthOrHeight;
            scaler.referencePixelsPerUnit = hudScaler.referencePixelsPerUnit;
            scaler.scaleFactor = hudScaler.scaleFactor;
        }
        else
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
        }

        ComfortMePlugin.Log?.LogInfo(
            $"Catalog overlay canvas sorting={OverlaySorting} parent={host.name} hudCanvas={(hudCanvas != null ? hudCanvas.name : "null")}");
    }
}
