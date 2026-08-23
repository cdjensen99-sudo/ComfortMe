using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ComfortMe;

/// <summary>
/// Selected-piece group ladder vs this room (red lower, grey equal, green upgrade).
/// Includes off-tab pieces with a tab label. Must not cover Hygge's Group/Value/Nearest panel.
/// </summary>
internal static class GroupCatalogPanel
{
    private const string PanelName = "ComfortMeCatalog";

    private static readonly Color PanelBackground = new Color(0.04f, 0.05f, 0.05f, 0.86f);
    private static readonly Color HeaderColor = new Color(0.92f, 0.93f, 0.9f, 1f);
    private static readonly Color UpgradeGreen = new Color(0.35f, 0.85f, 0.55f, 1f);
    private static readonly Color EqualGrey = new Color(0.72f, 0.72f, 0.72f, 1f);
    private static readonly Color LowerRed = new Color(0.85f, 0.36f, 0.36f, 1f);
    private static readonly Color UnlitAmber = new Color(0.95f, 0.72f, 0.28f, 1f);

    private static Image panel;
    private static TextMeshProUGUI header;
    private static TextMeshProUGUI body;
    private static string lastText;

    internal static void Hide()
    {
        if (panel != null)
        {
            panel.gameObject.SetActive(false);
        }

        lastText = null;
    }

    internal static void Refresh(Hud hud, Piece selected, ComfortSnapshot snapshot)
    {
        if (hud == null || snapshot == null || !ModConfig.Enabled.Value || !Hud.IsPieceSelectionVisible())
        {
            Hide();
            return;
        }

        EnsurePanel(hud);
        if (panel == null)
        {
            return;
        }

        panel.gameObject.SetActive(true);
        StringBuilder builder = new StringBuilder(256);
        builder.Append("This room: ").Append(snapshot.Total);
        if (!snapshot.InShelter)
        {
            builder.Append("  (unsheltered)");
        }

        bool showGroup = ModConfig.ShowGroupCatalog.Value && selected != null && selected.m_comfort > 0;
        if (showGroup)
        {
            ComfortClassification classification = UpgradeClassifier.Classify(selected, snapshot);
            builder.Append('\n');
            builder.Append(PieceDiscovery.GroupLabel(selected.m_comfortGroup));
            if (selected.m_comfortGroup == Piece.ComfortGroup.None)
            {
                if (snapshot.HasActive(selected))
                {
                    builder.Append(" here: already placed");
                }
                else if (snapshot.HasPlaced(selected))
                {
                    builder.Append(" here: unlit");
                }
                else
                {
                    builder.Append(" here: missing");
                }
            }
            else
            {
                builder.Append(" here: +").Append(classification.Current);
            }

            AppendGroupRows(builder, selected, snapshot);
        }

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
            body.gameObject.SetActive(body.text.Length > 0);
        }
    }

    private static void AppendGroupRows(StringBuilder builder, Piece selected, ComfortSnapshot snapshot)
    {
        Player player = Player.m_localPlayer;
        PieceTable table = PieceDiscovery.GetTable(player);
        List<DiscoveredPiece> discovered = PieceDiscovery.AllComfort(player);
        List<CatalogRow> rows = new List<CatalogRow>();
        HashSet<string> seen = new HashSet<string>();

        for (int i = 0; i < discovered.Count; i++)
        {
            Piece piece = discovered[i].Piece;
            if (piece.m_comfortGroup != selected.m_comfortGroup)
            {
                continue;
            }

            if (!seen.Add(piece.m_name))
            {
                continue;
            }

            ComfortClassification classification = UpgradeClassifier.Classify(piece, snapshot);
            rows.Add(new CatalogRow(
                PieceDiscovery.LocalName(piece),
                piece.m_comfort,
                classification.Verdict,
                discovered[i].Category != selected.m_category
                    ? PieceDiscovery.CategoryLabel(table, discovered[i].Category)
                    : null));
        }

        rows.Sort(CompareRows);
        for (int i = 0; i < rows.Count; i++)
        {
            CatalogRow row = rows[i];
            Color color = ColorFor(row.Verdict);
            builder.Append("\n<color=#").Append(HudUi.Hex(color)).Append('>');
            builder.Append(row.Name).Append("  +").Append(row.Comfort);
            if (row.Verdict == ComfortVerdict.Unlit)
            {
                builder.Append("  (unlit)");
            }

            if (!string.IsNullOrEmpty(row.Tab))
            {
                builder.Append("  (").Append(row.Tab).Append(')');
            }

            builder.Append("</color>");
        }
    }

    private static int CompareRows(CatalogRow a, CatalogRow b)
    {
        int comfort = a.Comfort.CompareTo(b.Comfort);
        return comfort != 0 ? comfort : string.CompareOrdinal(a.Name, b.Name);
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

        TMP_FontAsset font = HudUi.FontFrom(hud);
        Material fontMaterial = HudUi.FontMaterialFrom(hud);
        if (parent == null || font == null)
        {
            return;
        }

        panel = HudUi.CreatePanel(parent, PanelName, PanelBackground);
        Canvas canvas = panel.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 31000;
        RectTransform rt = panel.rectTransform;
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-8f, -6f);
        rt.sizeDelta = new Vector2(280f, 0f);
        rt.SetAsLastSibling();

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 8, 8);
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        header = HudUi.CreateText(panel.transform, "Header", font, fontMaterial, 16f);
        header.alignment = TextAlignmentOptions.TopLeft;
        header.color = HeaderColor;
        header.textWrappingMode = TextWrappingModes.Normal;
        LayoutElement headerLayout = header.gameObject.AddComponent<LayoutElement>();
        headerLayout.minWidth = 220f;

        body = HudUi.CreateText(panel.transform, "Body", font, fontMaterial, 14f);
        body.alignment = TextAlignmentOptions.TopLeft;
        body.color = HeaderColor;
        body.textWrappingMode = TextWrappingModes.Normal;
        LayoutElement bodyLayout = body.gameObject.AddComponent<LayoutElement>();
        bodyLayout.minWidth = 220f;
    }

    private static Transform ResolveParent(Hud hud)
    {
        if (hud.m_pieceSelectionWindow != null)
        {
            return hud.m_pieceSelectionWindow.transform;
        }

        return hud.m_buildHud != null ? hud.m_buildHud.transform : null;
    }

    private readonly struct CatalogRow
    {
        internal CatalogRow(string name, int comfort, ComfortVerdict verdict, string tab)
        {
            Name = name;
            Comfort = comfort;
            Verdict = verdict;
            Tab = tab;
        }

        internal string Name { get; }

        internal int Comfort { get; }

        internal ComfortVerdict Verdict { get; }

        internal string Tab { get; }
    }
}
