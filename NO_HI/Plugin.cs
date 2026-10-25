using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace NO_HI;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const string GeneralSection = "1. General";
    private const string HardpointSection = "2. Hardpoint Info Entries";
    internal static ConfigEntry<bool> ShowHardpointInfo = null!;
    internal static ConfigEntry<bool> HideZeroValues = null!;
    internal static ConfigEntry<bool> ShowEmptyCost = null!;
    internal static ConfigEntry<bool> ShowEmptyMass = null!;
    internal static ConfigEntry<bool> ShowMass = null!;
    internal static ConfigEntry<bool> ShowDrag = null!;
    internal static ConfigEntry<bool> ShowEmptyDrag = null!;
    internal static ConfigEntry<bool> ShowRcs = null!;
    internal static ConfigEntry<bool> ShowEmptyRcs = null!;
    internal static ConfigEntry<bool> ShowAircraftEmptyRcs = null!;
    internal static ConfigEntry<bool> ShowLaseTargets = null!;
    internal static ConfigEntry<bool> ShowLaseRanges = null!;
    private Harmony? _harmony;
    
    
    private new static ManualLogSource Logger { get; set; } = null!;
    
    private void Awake()
    {
        Logger = base.Logger;
        
        ShowHardpointInfo = Config.Bind(GeneralSection, "1. Show Hardpoint Info", true,
            "Show panels above hardpoint selections with extra hardpoint info");
        HideZeroValues = Config.Bind(GeneralSection, "2. Hide Zero Values", true,
            "Hide stats that are zero, if every value is zero the extra panel hides. Exception is made " +
            "for regular + empty pairs, so that if RCS is non-zero and emptyRCS is zero, the emptyRCS still shows " +
            "so it's easier to compare them");
        ShowAircraftEmptyRcs = Config.Bind(GeneralSection, "3. Show Aircraft Empty RCS", true,
            "Show total RCS value of plane when all hardpoints are expended, in info panel below RCS value");
        
        ShowRcs = Config.Bind(HardpointSection, "1a. RCS", true,
            "Show mount's RCS (when not empty)");
        ShowEmptyRcs = Config.Bind(HardpointSection, "1b. Empty RCS", true,
            "Show mount's empty RCS");
        ShowMass = Config.Bind(HardpointSection, "2a. Mass", true,
            "Show mount's mass");
        ShowEmptyMass = Config.Bind(HardpointSection, "2b. Empty Mass", true,
            "Show empty mount's mass");
        ShowDrag = Config.Bind(HardpointSection, "3a. Drag", true,
            "Show mount's drag");
        ShowEmptyDrag = Config.Bind(HardpointSection, "3b. Empty Drag", true,
            "Show mount's empty drag");
        ShowEmptyCost = Config.Bind(HardpointSection, "4. Empty Cost", false,
            "Show mount's empty cost");
        ShowLaseTargets = Config.Bind(HardpointSection, "5a. Lase Targets", true,
            "Show the plane's total laser designation targets (shows in panels above laser guided " +
            "weapons and Aryx Weaponry Pack targeting pods).");
        ShowLaseRanges = Config.Bind(HardpointSection, "5b. Lase Ranges", true,
            "Show laser designator max ranges.");
        
        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll();
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }
    
    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
    }
}