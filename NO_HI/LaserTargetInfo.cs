using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace NO_HI;

internal static class LaserTargetInfo
{
    private const string AryxTargetingPodType = "AryxWeaponryExpansion.AryxExternalTargetingPodAddon";
    
    // Preview plane doesn't always seem to apply IHardpointAddon properties, so need to actually look into/inspect
    // weapon pack targeting pod properties on hardpoints to check their values, and update it on hardpoint changes
    internal static Snapshot GetSnapshot(Aircraft? aircraft, IReadOnlyList<WeaponSelector> selectors,
        Dictionary<WeaponMount, PodContribution> podMountCache)
    {
        if (aircraft == null)
            return Snapshot.Empty;
        
        var prefab = aircraft.definition?.unitPrefab;
        var designators = prefab != null
            ? prefab.GetComponentsInChildren<LaserDesignator>(true)
            : aircraft.transform.root.GetComponentsInChildren<LaserDesignator>(true);
        var byRange = new Dictionary<int, int>();
        var total = 0;
        foreach (var designator in designators)
        {
            if (designator == null)
                continue;
            
            var count = Mathf.Max(0, designator.GetMaxTargets());
            if (count == 0)
                continue;
            
            total += count;
            var meters = Mathf.Max(0, Mathf.RoundToInt(designator.range));
            byRange.TryGetValue(meters, out var previous);
            byRange[meters] = previous + count;
        }
        
        var seenSets = new HashSet<HardpointSet>();
        var podTargets = 0;
        foreach (var selector in selectors)
        {
            if (selector == null || selector.hardpointSet == null || !seenSets.Add(selector.hardpointSet))
                continue;
            
            var hardpointCount = selector.hardpointSet.hardpoints?.Count ?? 0;
            if (hardpointCount == 0)
                continue;
            
            WeaponMount? mount;
            try
            {
                mount = selector.GetValue();
            }
            catch (ArgumentOutOfRangeException)
            {
                continue;
            }
            catch (IndexOutOfRangeException)
            {
                continue;
            }
            
            if (mount == null)
                continue;
            
            var pod = GetPodContribution(mount, podMountCache);
            podTargets += pod.TargetsPerHardpoint * hardpointCount;
        }
        
        // Check a targeting pod's range stat so it can be showed independently on its own hardpoint
        // (fallback value is 10km if it can't be determined/plane doesn't have a base targeting pod to inherit)
        var podRange = GetPodRange(aircraft, byRange);
        if (podTargets > 0)
        {
            byRange.TryGetValue(podRange, out var previous);
            byRange[podRange] = previous + podTargets;
            total += podTargets;
        }
        
        if (total == 0)
            return new Snapshot(0, [], podRange);
        
        var ranges = new List<RangeBracket>(byRange.Count);
        foreach (var bracket in byRange)
            ranges.Add(new RangeBracket(bracket.Key, bracket.Value));
        ranges.Sort((a, b) => b.RangeMeters.CompareTo(a.RangeMeters));
        return new Snapshot(total, ranges, podRange);
    }
    
    private static int GetPodRange(Aircraft aircraft, Dictionary<int, int> nativeRanges)
    {
        var activeDesignator = aircraft.GetLaserDesignator();
        if (activeDesignator != null && activeDesignator.range > 0f)
            return Mathf.RoundToInt(activeDesignator.range);
        
        var fallback = 0;
        foreach (var meters in nativeRanges.Keys)
            fallback = Mathf.Max(fallback, meters);
        return fallback > 0 ? fallback : 10000;
    }
    
    internal static PodContribution GetPodContribution(WeaponMount mount,
        Dictionary<WeaponMount, PodContribution> cache)
    {
        if (cache.TryGetValue(mount, out var cached))
            return cached;
        
        var count = 0;
        var isPod = false;
        if (mount.prefab != null)
            foreach (var weapon in mount.prefab.GetComponentsInChildren<Weapon>(true))
            {
                if (weapon == null || weapon.GetType().FullName != AryxTargetingPodType)
                    continue;
                
                isPod = true;
                var field = weapon.GetType().GetField("addedLaserTargets",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field?.GetValue(weapon) is int added)
                    count += Mathf.Max(0, added);
            }
        
        var result = new PodContribution(isPod, count);
        cache[mount] = result;
        return result;
    }
    
    internal static bool IsRelevantMount(WeaponMount mount)
    {
        if (mount == null || mount.prefab == null)
            return false;
        
        if (HasLaserSeeker(mount.info?.weaponPrefab) || HasLaserSeeker(mount.prefab))
            return true;
        
        foreach (var weapon in mount.prefab.GetComponentsInChildren<Weapon>(true))
        {
            if (weapon == null)
                continue;
            
            if (weapon.GetType().FullName == AryxTargetingPodType || HasLaserSeeker(weapon.info?.weaponPrefab))
                return true;
        }
        
        return false;
    }
    
    private static bool HasLaserSeeker(GameObject? prefab) =>
        prefab != null && prefab.GetComponentInChildren<LaserSeeker>(true) != null;
    
    internal readonly struct PodContribution
    {
        internal readonly bool IsPod;
        internal readonly int TargetsPerHardpoint;
        
        internal PodContribution(bool isPod, int targetsPerHardpoint)
        {
            IsPod = isPod;
            TargetsPerHardpoint = targetsPerHardpoint;
        }
    }
    
    internal readonly struct RangeBracket
    {
        internal readonly int RangeMeters;
        internal readonly int Targets;
        
        internal RangeBracket(int rangeMeters, int targets)
        {
            RangeMeters = rangeMeters;
            Targets = targets;
        }
        
        internal string Format() => $"{UnitConverter.DistanceReading(RangeMeters)} ({Targets}x)";
    }
    
    internal sealed class Snapshot
    {
        internal static readonly Snapshot Empty = new(0, [], 10000);
        internal readonly int PodRangeMeters;
        internal readonly IReadOnlyList<RangeBracket> Ranges;
        internal readonly int TotalTargets;
        
        internal Snapshot(int totalTargets, IReadOnlyList<RangeBracket> ranges, int podRangeMeters)
        {
            TotalTargets = totalTargets;
            Ranges = ranges;
            PodRangeMeters = podRangeMeters;
        }
    }
}