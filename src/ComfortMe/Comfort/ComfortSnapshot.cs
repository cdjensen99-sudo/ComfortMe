using System.Collections.Generic;
using UnityEngine;

namespace ComfortMe;

/// <summary>
/// Nearby comfort covering the player: max value per group, unique None prefabs, shelter.
/// Vanilla grouping: only the best piece in a group counts; None stacks once per unique piece.
/// Unlit fires / cold hot tubs stay in range with GetComfort() == 0; they are placed, not missing.
/// </summary>
internal sealed class ComfortSnapshot
{
    private const float CacheSeconds = 0.2f;
    private const float CacheMoveSqr = 0.25f * 0.25f;

    private static ComfortSnapshot cached;
    private static Vector3 cachedPos;
    private static float cachedTime = -999f;
    private static bool cachedShelter;

    internal static ComfortSnapshot Empty { get; } = new ComfortSnapshot();

    internal int Total { get; private set; }

    internal bool InShelter { get; private set; }

    internal Dictionary<Piece.ComfortGroup, int> GroupMax { get; } = new Dictionary<Piece.ComfortGroup, int>();

    internal Dictionary<Piece.ComfortGroup, string> GroupSource { get; } = new Dictionary<Piece.ComfortGroup, string>();

    internal List<UniqueComfort> Uniques { get; } = new List<UniqueComfort>();

    private readonly HashSet<string> placedKeys = new HashSet<string>();
    private readonly HashSet<string> activeKeys = new HashSet<string>();
    private readonly HashSet<string> inactiveKeys = new HashSet<string>();

    internal static ComfortSnapshot Capture(Player player)
    {
        if (player == null)
        {
            return Empty;
        }

        Vector3 pos = player.transform.position;
        bool shelter = player.InShelter();
        float now = Time.time;
        if (cached != null
            && now - cachedTime < CacheSeconds
            && (pos - cachedPos).sqrMagnitude < CacheMoveSqr
            && shelter == cachedShelter)
        {
            return cached;
        }

        ComfortSnapshot snap = new ComfortSnapshot
        {
            InShelter = shelter,
            Total = SE_Rested.CalculateComfortLevel(player),
        };

        List<Piece> nearby = new List<Piece>();
        Piece.GetAllComfortPiecesInRadius(pos, ModConstants.ComfortRadius, nearby);
        for (int i = 0; i < nearby.Count; i++)
        {
            Piece piece = nearby[i];
            if (piece == null || piece.m_comfort <= 0)
            {
                continue;
            }

            AddIdentity(snap.placedKeys, piece);
            int live = piece.GetComfort();
            if (live > 0)
            {
                AddIdentity(snap.activeKeys, piece);
                Piece.ComfortGroup group = piece.m_comfortGroup;
                string local = PieceDiscovery.LocalName(piece);
                if (group == Piece.ComfortGroup.None)
                {
                    if (!snap.Uniques.Exists(u => u.Name == local))
                    {
                        snap.Uniques.Add(new UniqueComfort(local, live));
                    }
                }
                else if (!snap.GroupMax.TryGetValue(group, out int current) || live > current)
                {
                    snap.GroupMax[group] = live;
                    snap.GroupSource[group] = local;
                }
            }
            else
            {
                AddIdentity(snap.inactiveKeys, piece);
            }
        }

        cached = snap;
        cachedPos = pos;
        cachedTime = now;
        cachedShelter = shelter;
        return snap;
    }

    internal int CurrentFor(Piece.ComfortGroup group)
    {
        if (group == Piece.ComfortGroup.None)
        {
            return 0;
        }

        return GroupMax.TryGetValue(group, out int value) ? value : 0;
    }

    internal bool HasPlaced(Piece piece)
    {
        return Matches(placedKeys, piece);
    }

    internal bool HasActive(Piece piece)
    {
        return Matches(activeKeys, piece);
    }

    internal bool IsInactivePresent(Piece piece)
    {
        return Matches(inactiveKeys, piece) && !Matches(activeKeys, piece);
    }

    private static void AddIdentity(HashSet<string> keys, Piece piece)
    {
        if (!string.IsNullOrEmpty(piece.m_name))
        {
            keys.Add(piece.m_name);
        }

        string prefab = PrefabName(piece);
        if (!string.IsNullOrEmpty(prefab))
        {
            keys.Add(prefab);
        }
    }

    private static bool Matches(HashSet<string> keys, Piece piece)
    {
        if (piece == null || keys.Count == 0)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(piece.m_name) && keys.Contains(piece.m_name))
        {
            return true;
        }

        string prefab = PrefabName(piece);
        return !string.IsNullOrEmpty(prefab) && keys.Contains(prefab);
    }

    private static string PrefabName(Piece piece)
    {
        return piece.gameObject != null ? Utils.GetPrefabName(piece.gameObject) : string.Empty;
    }
}

internal readonly struct UniqueComfort
{
    internal UniqueComfort(string name, int comfort)
    {
        Name = name;
        Comfort = comfort;
    }

    internal string Name { get; }

    internal int Comfort { get; }
}
