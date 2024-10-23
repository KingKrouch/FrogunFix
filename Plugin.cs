using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Cinemachine;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace FrogunFix;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log;

    public struct ConfigVariables
    {
        public static ConfigEntry<bool> _bForceCustomResolution;
        public static ConfigEntry<int>  _iHorizontalResolution;
        public static ConfigEntry<int>  _iVerticalResolution;
        public static ConfigEntry<int> _iCameraHeightOffset;
        public static ConfigEntry<int> _iCameraDistanceOffset;
    }

    public void LoadConfig()
    {
        // Resolution Config
        ConfigVariables._bForceCustomResolution = Config.Bind("Resolution", "Force Custom Resolution", true, "Self Explanatory. A temporary toggle for custom resolutions until I can figure out how to go about removing the resolution count restrictions.");
        ConfigVariables._iHorizontalResolution  = Config.Bind("Resolution", "Horizontal Resolution",   Screen.currentResolution.width);
        ConfigVariables._iVerticalResolution    = Config.Bind("Resolution", "Vertical Resolution",     Screen.currentResolution.height);
        // Camera Config
        ConfigVariables._iCameraHeightOffset    = Config.Bind("Camera", "Camera Height Offset", 0);
        ConfigVariables._iCameraDistanceOffset  = Config.Bind("Camera", "Camera Distance Offset", 0);
    }

    public override void Load()
    {
        // Plugin startup logic
        Log = base.Log;
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        LoadConfig();
        Harmony.CreateAndPatchAll(typeof(UIPatches));
        Harmony.CreateAndPatchAll(typeof(ResolutionPatches));
        Time.fixedDeltaTime = 1.0f / Screen.currentResolution.refreshRate; // Set FixedUpdate rate to refresh rate to fix stutter.

        QualitySettings.shadows          = ShadowQuality.HardOnly; // Default: HardOnly
        QualitySettings.shadowResolution = ShadowResolution.VeryHigh; // Default: High
        QualitySettings.antiAliasing     = 0; // Raising this may cause weird stitching artifacts on meshes.
        QualitySettings.vSyncCount       = 1; // Sample for disabling VSync.
    }

    [HarmonyPatch]
    public class UIPatches
    {
        private static void AdjustAspectRatioFitter(AspectRatioFitter arf, AspectRatioFitter.AspectMode aspectMode)
        {
            arf.aspectMode = aspectMode;
            arf.enabled    = true;
            // Check if the display aspect ratio is less than 16:9, and if so, disable the AspectRatioFitter and use the old transforms.
            if (Screen.currentResolution.m_Width / Screen.currentResolution.m_Height >= 1920.0f / 1080.0f) {
                arf.aspectRatio = 1920.0f / 1080.0f;
            }
            else {
                arf.aspectRatio = Screen.currentResolution.m_Width / (float)Screen.currentResolution.m_Height;
            }
        }
        
        [HarmonyPatch(typeof(CanvasScaler), "OnEnable")]
        [HarmonyPostfix]
        public static void CanvasScalerFixes(CanvasScaler __instance)
        {
            __instance.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        }

        [HarmonyPatch(typeof(MenuMain), nameof(MenuMain.Start))]
        [HarmonyPostfix]
        public static void MainMenuAspectRatioFitter(MenuMain __instance)
        {
            var mainMenuARF = __instance.gameObject.GetComponent<AspectRatioFitter>();
            if (mainMenuARF != null) return;
            mainMenuARF = __instance.gameObject.AddComponent<AspectRatioFitter>();
            Debug.Log("Adding Aspect Ratio Fitter to " + __instance.gameObject.name);
            AdjustAspectRatioFitter(mainMenuARF, AspectRatioFitter.AspectMode.HeightControlsWidth);
        }
        
        [HarmonyPatch(typeof(ControllerRefocus), nameof(ControllerRefocus.Update))]
        [HarmonyPostfix]
        public static void LevelSelectUICanvasAspectRatioFitter(ControllerRefocus __instance)
        {
            if (__instance.gameObject.name != "LevelSelectUICanvas") return;
            var levelSelectUIARF = __instance.gameObject.GetComponent<AspectRatioFitter>();
            if (levelSelectUIARF != null) return;
            levelSelectUIARF = __instance.gameObject.AddComponent<AspectRatioFitter>();
            Debug.Log("Adding Aspect Ratio Fitter to " + __instance.gameObject.name);
            AdjustAspectRatioFitter(levelSelectUIARF, AspectRatioFitter.AspectMode.HeightControlsWidth);
        }
        
        [HarmonyPatch(typeof(ControlUI), nameof(ControlUI.Start))]
        [HarmonyPostfix]
        public static void ControlUIAspectRatioFitter(ControlUI __instance)
        {
            var controlUIARF = __instance.gameObject.GetComponent<AspectRatioFitter>();
            if (controlUIARF != null) return;
            controlUIARF = __instance.gameObject.AddComponent<AspectRatioFitter>();
            Debug.Log("Adding Aspect Ratio Fitter to " + __instance.gameObject.name);
            AdjustAspectRatioFitter(controlUIARF, AspectRatioFitter.AspectMode.HeightControlsWidth);
        }

        [HarmonyPatch(typeof(SceneFader), nameof(SceneFader.Start))]
        [HarmonyPostfix]
        public static void SceneFaderAspectRatioFitter(SceneFader __instance)
        {
            var sceneFaderARF = __instance.gameObject.GetComponent<AspectRatioFitter>();
            if (sceneFaderARF != null) return;
            sceneFaderARF = __instance.gameObject.AddComponent<AspectRatioFitter>();
            Debug.Log("Adding Aspect Ratio Fitter to " + __instance.gameObject.name);
            AdjustAspectRatioFitter(sceneFaderARF, AspectRatioFitter.AspectMode.HeightControlsWidth);
        }
    }

    [HarmonyPatch]
    public class CameraPatches
    {
        // TODO: Fix this.
        [HarmonyPatch(typeof(CinemachineBrain), nameof(CinemachineBrain.Start))]
        [HarmonyPostfix]
        public static void AdjustCameraScaling(CinemachineBrain __instance)
        {
            Debug.Log("Found object with name " + __instance.gameObject.name);
            if (__instance.gameObject.name != "Main Camera") return;
            Debug.Log("Found Game Object containing 'Main Camera' name");
            var camera = __instance.gameObject.GetComponent<Camera>();
            if (camera == null) return;
            var fov = camera.fieldOfView;
            Debug.Log("Adding Physical Camera Properties to " + __instance.gameObject.name);
            camera.usePhysicalProperties = true;
            camera.sensorSize            = new Vector2(16, 9);
            camera.gateFit               = Camera.GateFitMode.Overscan;
            camera.fieldOfView           = fov;
            Debug.Log("Camera properties set successfully");
        }
        
        [HarmonyPatch(typeof(CinemachineFreeLook), nameof(CinemachineFreeLook.Start))]
        [HarmonyPostfix]
        public static void AdjustCameraDistance(CinemachineFreeLook __instance)
        {
            for (var i = 0; i < __instance.m_Orbits.Length; i++) {
                // Copy the orbit struct to a local variable
                var orbit = __instance.m_Orbits[i];
        
                // Modify the local copy
                orbit.m_Radius += ConfigVariables._iCameraDistanceOffset.Value;
                orbit.m_Height += ConfigVariables._iCameraHeightOffset.Value;
        
                // Assign the modified copy back to the array
                __instance.m_Orbits[i] = orbit;
            }
        }
    }

    [HarmonyPatch]
    public class ResolutionPatches
    {
        [HarmonyPatch(typeof(MenuOptions), nameof(MenuOptions.updateResolution))]
        [HarmonyPostfix]
        public static void ForceCustomResolution(MenuOptions __instance)
        {
            Screen.SetResolution(ConfigVariables._iHorizontalResolution.Value, ConfigVariables._iVerticalResolution.Value, FullScreenMode.FullScreenWindow);
            Debug.Log("Forcing custom resolution.");
        }
    }
}
