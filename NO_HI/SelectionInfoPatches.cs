using HarmonyLib;

// ReSharper disable InconsistentNaming

namespace NO_HI;

[HarmonyPatch]
internal static class SelectionInfoPatches
{
    [HarmonyPatch(typeof(AircraftSelectionMenu), nameof(AircraftSelectionMenu.Initialize))]
    [HarmonyPostfix]
    private static void InitializePostfix(AircraftSelectionMenu __instance)
    {
        var ui = __instance.GetComponent<SelectionInfoBehaviour>() ??
                 __instance.gameObject.AddComponent<SelectionInfoBehaviour>();
        ui.Initialise(__instance);
    }
    
    [HarmonyPatch(typeof(AircraftSelectionMenu), nameof(AircraftSelectionMenu.SpawnPreview))]
    [HarmonyPostfix]
    private static void SpawnPreviewPostfix(AircraftSelectionMenu __instance)
    {
        __instance.GetComponent<SelectionInfoBehaviour>()?.Refresh();
    }
    
    [HarmonyPatch(typeof(AircraftSelectionMenu), nameof(AircraftSelectionMenu.UpdateReadouts))]
    [HarmonyPostfix]
    private static void UpdateReadoutsPostfix(AircraftSelectionMenu __instance)
    {
        __instance.GetComponent<SelectionInfoBehaviour>()?.Refresh();
    }
    
    [HarmonyPatch(typeof(WeaponSelector), nameof(WeaponSelector.SplitSymmetry))]
    [HarmonyPostfix]
    private static void SplitSymmetryPostfix(WeaponSelector __instance)
    {
        __instance.GetComponentInParent<SelectionInfoBehaviour>()?.Refresh();
    }
    
    [HarmonyPatch(typeof(WeaponSelector), nameof(WeaponSelector.UpdateSymmetry))]
    [HarmonyPostfix]
    private static void UpdateSymmetryPostfix(WeaponSelector __instance)
    {
        __instance.GetComponentInParent<SelectionInfoBehaviour>()?.Refresh();
    }
}