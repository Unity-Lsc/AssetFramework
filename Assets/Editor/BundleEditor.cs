using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BundleEditor
{

    public static string ABCONFIG_PATH = "Assets/Editor/ABConfig.asset";

    //key是AB包名,value是路径  所有文件夹的AB包的Dict
    public static Dictionary<string, string> AllFileDirDict = new Dictionary<string, string>();

    [MenuItem("Tools/打包")]
    public static void Build() {
        //BuildPipeline.BuildAssetBundles(Application.streamingAssetsPath, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
        //AssetDatabase.Refresh();//刷新编辑器

        AllFileDirDict.Clear();
        ABConfig abCfg = AssetDatabase.LoadAssetAtPath<ABConfig>(ABCONFIG_PATH);

        //文件夹
        foreach (ABConfig.FileDirABName fileDir in abCfg.mAllFileDirAB) {
            if(AllFileDirDict.ContainsKey(fileDir.ABName)) {
                Debug.LogError("AB包配置名字重复:" + fileDir.ABName);
            }else {
                AllFileDirDict.Add(fileDir.ABName, fileDir.Path);
            }
        }

        //文件
        string[] allStrs = AssetDatabase.FindAssets("t:Prefab", abCfg.mAllPrefabPath.ToArray());//该路径集合下所有的Prefab文件的GUID
        for (int i = 0; i < allStrs.Length; i++) {
            string path = AssetDatabase.GUIDToAssetPath(allStrs[i]);
            EditorUtility.DisplayProgressBar("查找Prefab", "Prefabs:" + path, i * 1.0f / allStrs.Length);
        }

        EditorUtility.ClearProgressBar();

        


    }

}
