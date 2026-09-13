using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ComfortMe;

/// <summary>
/// Forge-style sparkle beams: from counting comfort pieces while hovering the This room
/// panel or Rested status icon, and from the placement ghost to the player while placing
/// a comfort piece. Never from crafting stations, station upgrades, Base, or Shelter.
/// </summary>
internal static class ComfortLinks
{
    private static readonly FieldInfo StatusEffectsField = AccessTools.Field(typeof(Hud), "m_statusEffects");
    private static readonly FieldInfo PlacementGhostField = AccessTools.Field(typeof(Player), "m_placementGhost");
    private static readonly List<GameObject> beams = new List<GameObject>();
    private static readonly List<RectTransform> restedIcons = new List<RectTransform>();
    private static GameObject prefab;
    private static Transform beamRoot;
    private static bool loggedPrefab;
    private static bool loggedMissing;
    private static int suppressFrame = -1;
    private static bool suppressCached;

    internal static void Clear()
    {
        Hide();
        restedIcons.Clear();
        prefab = null;
        loggedPrefab = false;
        loggedMissing = false;
        suppressFrame = -1;
        if (beamRoot != null && beamRoot)
        {
            Object.Destroy(beamRoot.gameObject);
        }

        beamRoot = null;
    }

    internal static void BindRestedIcons(Hud hud, List<StatusEffect> effects)
    {
        restedIcons.Clear();
        if (hud == null || effects == null)
        {
            return;
        }

        List<RectTransform> icons = StatusEffectsField?.GetValue(hud) as List<RectTransform>;
        if (icons == null)
        {
            return;
        }

        int count = Mathf.Min(icons.Count, effects.Count);
        for (int i = 0; i < count; i++)
        {
            if (effects[i] is not SE_Rested)
            {
                continue;
            }

            RectTransform icon = icons[i];
            if (icon != null && icon.gameObject.activeInHierarchy)
            {
                restedIcons.Add(icon);
            }
        }
    }

    /// <summary>
    /// Hide vanilla workbench/forge upgrade connection lines while ComfortMe is showing
    /// comfort sparkles. Keep them when the ghost itself is a station upgrade.
    /// </summary>
    internal static bool SuppressVanillaStationLines()
    {
        int frame = Time.frameCount;
        if (frame == suppressFrame)
        {
            return suppressCached;
        }

        suppressFrame = frame;
        suppressCached = ComputeSuppressVanillaStationLines();
        return suppressCached;
    }

    internal static void Tick(Hud hud)
    {
        if (!ModConfig.Enabled.Value || !ModConfig.ShowComfortLinks.Value)
        {
            Hide();
            return;
        }

        Player player = Player.m_localPlayer;
        if (player == null)
        {
            Hide();
            return;
        }

        Piece ghost = GetPlacementGhostPiece(player);
        bool ghostLine = ShouldDrawGhostLine(player, ghost);
        bool hover = GroupCatalogPanel.IsHovered() || IsRestedIconHovered();
        if (!hover && !ghostLine)
        {
            Hide();
            return;
        }

        ComfortSnapshot snapshot = hover ? ComfortSnapshot.Capture(player) : null;
        Show(player, snapshot, ghostLine ? ghost : null);
    }

    internal static void Hide()
    {
        for (int i = 0; i < beams.Count; i++)
        {
            GameObject beam = beams[i];
            if (beam != null)
            {
                beam.SetActive(false);
            }
        }
    }

    private static bool ComputeSuppressVanillaStationLines()
    {
        if (!ModConfig.Enabled.Value || !ModConfig.ShowComfortLinks.Value)
        {
            return false;
        }

        Player player = Player.m_localPlayer;
        if (player == null)
        {
            return false;
        }

        Piece ghost = GetPlacementGhostPiece(player);
        if (IsStationExtension(ghost))
        {
            return false;
        }

        if (ShouldDrawGhostLine(player, ghost))
        {
            return true;
        }

        return GroupCatalogPanel.IsHovered() || IsRestedIconHovered();
    }

    private static bool IsRestedIconHovered()
    {
        Vector2 mouse = Input.mousePosition;
        for (int i = 0; i < restedIcons.Count; i++)
        {
            RectTransform icon = restedIcons[i];
            if (icon == null || !icon.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (ContainsScreenPoint(icon, mouse))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsScreenPoint(RectTransform rt, Vector2 screen)
    {
        Canvas canvas = rt.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        return RectTransformUtility.RectangleContainsScreenPoint(rt, screen, cam);
    }

    private static void Show(Player player, ComfortSnapshot snapshot, Piece ghost)
    {
        if (!ResolvePrefab())
        {
            return;
        }

        Vector3 playerPos = player.transform.position + Vector3.up * 1.1f;
        int used = 0;
        if (snapshot != null)
        {
            List<Piece> pieces = snapshot.CountingPieces;
            for (int i = 0; i < pieces.Count; i++)
            {
                Piece piece = pieces[i];
                if (!IsComfortLinkSource(piece))
                {
                    continue;
                }

                used = PlaceBeam(used, piece.transform.position + Vector3.up * 0.4f, playerPos);
            }
        }

        if (IsComfortLinkSource(ghost))
        {
            used = PlaceBeam(used, ghost.transform.position + Vector3.up * 0.4f, playerPos);
        }

        for (int i = used; i < beams.Count; i++)
        {
            if (beams[i] != null)
            {
                beams[i].SetActive(false);
            }
        }
    }

    private static bool IsComfortLinkSource(Piece piece)
    {
        if (piece == null || piece.gameObject == null || !piece.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (piece.m_comfort <= 0)
        {
            return false;
        }

        return !IsStationPiece(piece);
    }

    private static bool IsStationPiece(Piece piece)
    {
        if (piece == null)
        {
            return false;
        }

        return piece.GetComponent<CraftingStation>() != null
            || piece.GetComponentInParent<CraftingStation>() != null
            || IsStationExtension(piece);
    }

    private static bool IsStationExtension(Piece piece)
    {
        if (piece == null)
        {
            return false;
        }

        return piece.GetComponent<StationExtension>() != null
            || piece.GetComponentInChildren<StationExtension>() != null;
    }

    private static int PlaceBeam(int used, Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        float length = delta.magnitude;
        if (length < 0.05f)
        {
            return used;
        }

        GameObject beam = GetBeam(used);
        Transform t = beam.transform;
        t.position = from;
        t.rotation = Quaternion.LookRotation(delta / length);
        t.localScale = new Vector3(1f, 1f, length);
        beam.SetActive(true);
        return used + 1;
    }

    private static Piece GetPlacementGhostPiece(Player player)
    {
        if (player == null || PlacementGhostField == null)
        {
            return null;
        }

        object raw = PlacementGhostField.GetValue(player);
        if (raw is Piece piece)
        {
            return piece.gameObject != null && piece.gameObject.activeInHierarchy ? piece : null;
        }

        GameObject ghost = raw as GameObject;
        if (ghost == null || !ghost.activeInHierarchy)
        {
            return null;
        }

        return ghost.GetComponent<Piece>() ?? ghost.GetComponentInChildren<Piece>();
    }

    private static bool ShouldDrawGhostLine(Player player, Piece ghost)
    {
        if (player == null || !IsComfortLinkSource(ghost))
        {
            return false;
        }

        return Vector3.Distance(ghost.transform.position, player.transform.position) <= ModConstants.ComfortRadius;
    }

    private static GameObject GetBeam(int index)
    {
        while (index >= beams.Count)
        {
            beams.Add(null);
        }

        GameObject beam = beams[index];
        if (beam == null)
        {
            EnsureBeamRoot();
            beam = Object.Instantiate(prefab, beamRoot, false);
            beam.name = "ComfortMeLink";
            StripStationScripts(beam);
            beams[index] = beam;
        }

        return beam;
    }

    private static void EnsureBeamRoot()
    {
        if (beamRoot != null && beamRoot)
        {
            return;
        }

        GameObject root = new GameObject("ComfortMeLinks");
        Object.DontDestroyOnLoad(root);
        beamRoot = root.transform;
    }

    private static void StripStationScripts(GameObject beam)
    {
        StationExtension[] extensions = beam.GetComponentsInChildren<StationExtension>(true);
        for (int i = 0; i < extensions.Length; i++)
        {
            Object.Destroy(extensions[i]);
        }

        CraftingStation[] stations = beam.GetComponentsInChildren<CraftingStation>(true);
        for (int i = 0; i < stations.Length; i++)
        {
            Object.Destroy(stations[i]);
        }
    }

    private static bool ResolvePrefab()
    {
        if (prefab != null)
        {
            return true;
        }

        GameObject sceneFallback = null;
        StationExtension[] extensions = Resources.FindObjectsOfTypeAll<StationExtension>();
        for (int i = 0; i < extensions.Length; i++)
        {
            StationExtension extension = extensions[i];
            if (extension == null || extension.m_connectionPrefab == null)
            {
                continue;
            }

            GameObject candidate = extension.m_connectionPrefab;
            if (!candidate.scene.IsValid())
            {
                prefab = candidate;
                break;
            }

            if (sceneFallback == null)
            {
                sceneFallback = candidate;
            }
        }

        if (prefab == null)
        {
            prefab = sceneFallback;
        }

        if (prefab != null)
        {
            if (!loggedPrefab)
            {
                loggedPrefab = true;
                ComfortMePlugin.Log?.LogInfo($"Comfort links using '{prefab.name}'.");
            }

            return true;
        }

        if (!loggedMissing)
        {
            loggedMissing = true;
            ComfortMePlugin.Log?.LogWarning("Comfort links: no station connection prefab found yet.");
        }

        return false;
    }
}
