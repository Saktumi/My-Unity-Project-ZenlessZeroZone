// Cristian Pop - https://boxophobic.com/

using UnityEngine;
using UnityEditor;
using Boxophobic.StyledGUI;
using Boxophobic.Utils;
using System.IO;

public class HeightFogHub : EditorWindow
{
#if UNITY_2019_3_OR_NEWER
    const int GUI_HEIGHT = 18;
#else
    const int GUI_HEIGHT = 14;
#endif

    string assetFolder = "Assets/BOXOPHOBIC/Polyverse Wind";
    string userFolder = "Assets/BOXOPHOBIC/User";

    string[] pipelinePaths;
    string[] pipelineOptions;
    string pipelinesPath;
    int pipelineIndex;

    GUIStyle stylePopup;

    Color bannerColor;
    string bannerText;
    string helpURL;
    static HeightFogHub window;
    //Vector2 scrollPosition = Vector2.zero;

    [MenuItem("Window/BOXOPHOBIC/Atmospheric Height Fog/Hub", false, 1031)]
    public static void ShowWindow()
    {
        window = GetWindow<HeightFogHub>(false, "Atmospheric Height Fog", true);
        window.minSize = new Vector2(389, 220);
    }

    void OnEnable()
    {
        //Safer search, there might be many user folders
        string[] searchFolders;

        searchFolders = AssetDatabase.FindAssets("Atmospheric Height Fog");

        for (int i = 0; i < searchFolders.Length; i++)
        {
            if (AssetDatabase.GUIDToAssetPath(searchFolders[i]).EndsWith("Atmospheric Height Fog.pdf"))
            {
                assetFolder = AssetDatabase.GUIDToAssetPath(searchFolders[i]);
                assetFolder = assetFolder.Replace("/Atmospheric Height Fog.pdf", "");
            }
        }

        searchFolders = AssetDatabase.FindAssets("User");

        for (int i = 0; i < searchFolders.Length; i++)
        {
            if (AssetDatabase.GUIDToAssetPath(searchFolders[i]).EndsWith("User.pdf"))
            {
                userFolder = AssetDatabase.GUIDToAssetPath(searchFolders[i]);
                userFolder = userFolder.Replace("/User.pdf", "");
                userFolder += "/Atmospheric Height Fog/";
            }
        }

        pipelinesPath = assetFolder + "/Core/Pipelines";

        GetPackages();

        for (int i = 0; i < pipelineOptions.Length; i++)
        {
            if (pipelineOptions[i] == SettingsUtils.LoadSettingsData(userFolder + "Pipeline.asset", ""))
            {
                pipelineIndex = i;
            }
        }

        var assetVersionString = SettingsUtils.LoadSettingsData(assetFolder + "/Core/Editor/Version.asset", "100");
        var bannerVersion = assetVersionString.ToString();
        bannerVersion = bannerVersion.Insert(1, ".");
        bannerVersion = bannerVersion.Insert(3, ".");

        bannerColor = new Color(0.55f, 0.7f, 1f);
        bannerText = "Atmospheric Height Fog " + bannerVersion;
        helpURL = "https://docs.google.com/document/d/1pIzIHIZ-cSh2ykODSZCbAPtScJ4Jpuu7lS3rNEHCLbc/edit#heading=h.hbq3w8ae720xlwXKdkI/edit#heading=h.hbq3w8ae720x";
    }

    void OnGUI()
    {
        SetGUIStyles();

        StyledGUI.DrawWindowBanner(bannerColor, bannerText, helpURL);

        GUILayout.BeginHorizontal();
        GUILayout.Space(20);

        GUILayout.BeginVertical();

        if (File.Exists(assetFolder + "/Core/Editor/HeightFogHubAutorun.cs"))
        {
            EditorGUILayout.HelpBox("Welcome to Atmospheric Height Fog! Press Install to go to the Render Pipeline selection tab!", MessageType.Info, true);

            GUILayout.Space(15);

            if (GUILayout.Button("Install", GUILayout.Height(24)))
            {
                InstallAsset();
                SetDefineSymbols();
            }
        }
        else
        {
            if (EditorApplication.isCompiling)
            {
                GUI.enabled = false;
            }

            DrawInterface();

            GUI.enabled = true;
        }

        GUILayout.EndVertical();

        GUILayout.Space(13);
        GUILayout.EndHorizontal();
    }

    void SetGUIStyles()
    {
        stylePopup = new GUIStyle(EditorStyles.popup)
        {
            alignment = TextAnchor.MiddleCenter
        };
    }

    void DrawInterface()
    {
        EditorGUILayout.HelpBox("Click the Render Pipeline Install button to switch to another render pipeline!", MessageType.Info, true);

        if (pipelineOptions[pipelineIndex].Contains("Universal"))
        {
            EditorGUILayout.HelpBox("With Universal Render Pipeline, the Depth Texture needs to be enabled on the URP Asset or on the Main Camera!", MessageType.Info, true);
        }

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();

        GUILayout.Label("Render Pipeline Support", GUILayout.Width(220));
        pipelineIndex = EditorGUILayout.Popup(pipelineIndex, pipelineOptions, stylePopup);

        if (GUILayout.Button("Import", GUILayout.Width(80), GUILayout.Height(GUI_HEIGHT)))
        {
            SettingsUtils.SaveSettingsData(userFolder + "Pipeline.asset", pipelineOptions[pipelineIndex]);

            ImportPackage();

            GUIUtility.ExitGUI();
        }

        GUILayout.EndHorizontal();

    }

    void GetPackages()
    {
        pipelinePaths = Directory.GetFiles(pipelinesPath, "*.unitypackage", SearchOption.TopDirectoryOnly);

        pipelineOptions = new string[pipelinePaths.Length];

        for (int i = 0; i < pipelineOptions.Length; i++)
        {
            pipelineOptions[i] = Path.GetFileNameWithoutExtension(pipelinePaths[i].Replace("Built-in Pipeline", "Standard"));
        }
    }

    void InstallAsset()
    {
        FileUtil.DeleteFileOrDirectory(assetFolder + "/Core/Editor/HeightFogHubAutorun.cs");

        if (File.Exists(assetFolder + "/Core/Editor/HeightFogHubAutorun.cs.meta"))
        {
            FileUtil.DeleteFileOrDirectory(assetFolder + "/Core/Editor/HeightFogHubAutorun.cs.meta");
        }

        SettingsUtils.SaveSettingsData(userFolder + "Pipeline.asset", "Standard");

        AssetDatabase.Refresh();

        GUIUtility.ExitGUI();
    }

    void ImportPackage()
    {
        SettingsUtils.SaveSettingsData(userFolder + "Pipeline.asset", pipelineOptions[pipelineIndex]);

        AssetDatabase.ImportPackage(pipelinePaths[pipelineIndex], false);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Atmospheric Height Fog] " + pipelineOptions[pipelineIndex] + " package imported in your project!");
    }

    void SetDefineSymbols()
    {
        var defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

        if (!defineSymbols.Contains("ATMOSPHERIC_HEIGHT_FOG"))
        {
            defineSymbols += ";ATMOSPHERIC_HEIGHT_FOG;";
        }

        PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, defineSymbols);
    }
}


