using HarmonyLib;

// ReSharper disable InconsistentNaming

namespace NO_HI;

[HarmonyPatch]
internal static class SelectionInfoPatches
{
    private static void RefreshMenu(AircraftSelectionMenu menu)
    {
        if (menu == null)
            return;
        
        var ui = menu.GetComponent<SelectionInfoBehaviour>();
        if (ui != null)
            ui.Refresh();
    }
    
    private static void RefreshSelector(WeaponSelector selector)
    {
        if (selector == null)
            return;
        
        var ui = selector.GetComponentInParent<SelectionInfoBehaviour>();
        if (ui != null)
            ui.Refresh();
    }
    
    [HarmonyPatch(typeof(AircraftSelectionMenu), nameof(AircraftSelectionMenu.Initialize))]
    [HarmonyPostfix]
    private static void InitializePostfix(AircraftSelectionMenu __instance)
    {
        if (__instance == null)
            return;
        
        var ui = __instance.GetComponent<SelectionInfoBehaviour>();
        if (ui == null)
            ui = __instance.gameObject.AddComponent<SelectionInfoBehaviour>();
        
        ui.Initialise(__instance);
    }
    
    [HarmonyPatch(typeof(AircraftSelectionMenu), nameof(AircraftSelectionMenu.SpawnPreview))]
    [HarmonyPostfix]
    private static void SpawnPreviewPostfix(AircraftSelectionMenu __instance)
    {
        RefreshMenu(__instance);
    }
    
    [HarmonyPatch(typeof(AircraftSelectionMenu), nameof(AircraftSelectionMenu.UpdateReadouts))]
    [HarmonyPostfix]
    private static void UpdateReadoutsPostfix(AircraftSelectionMenu __instance)
    {
        RefreshMenu(__instance);
    }
    
    [HarmonyPatch(typeof(WeaponSelector), nameof(WeaponSelector.SplitSymmetry))]
    [HarmonyPostfix]
    private static void SplitSymmetryPostfix(WeaponSelector __instance)
    {
        RefreshSelector(__instance);
    }
    
    [HarmonyPatch(typeof(WeaponSelector), nameof(WeaponSelector.UpdateSymmetry))]
    [HarmonyPostfix]
    private static void UpdateSymmetryPostfix(WeaponSelector __instance)
    {
        RefreshSelector(__instance);
    }
}