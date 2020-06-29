using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class BundleEditor
{
    /// <summary>
    /// 配置文件路径
    /// </summary>
    public static string ABCONFIG_PATH = "Assets/Editor/ABConfig.asset";
    /// <summary>
    /// AssetBundle文件生成路径
    /// </summary>
    public static string BundleTargetPath = Application.streamingAssetsPath;

    /// <summary>
    /// key是AB包名,value是路径  所有文件夹的AB包的Dict
    /// </summary>
    public static Dictionary<string, string> AllFileDirDict = new Dictionary<string, string>();

    /// <summary>
    /// 过滤的List 添加的是路径
    /// </summary>
    public static List<string> AllFileDirFilterList = new List<string>();

    /// <summary>
    /// 单个prefab的ab包  key是预制体的名字 value是所有依赖项的路径集合
    /// </summary>
    public static Dictionary<string, List<string>> AllPrefabDirDict = new Dictionary<string, List<string>>();

    [MenuItem("Tools/打包")]
    public static void Build() {

        AllFileDirFilterList.Clear();
        AllFileDirDict.Clear();
        ABConfig abCfg = AssetDatabase.LoadAssetAtPath<ABConfig>(ABCONFIG_PATH);

        //文件夹
        foreach (ABConfig.FileDirABName fileDir in abCfg.mAllFileDirAB) {
            if(AllFileDirDict.ContainsKey(fileDir.ABName)) {
                Debug.LogError("AB包配置名字重复:" + fileDir.ABName);
            }else {
                AllFileDirDict.Add(fileDir.ABName, fileDir.Path);
                AllFileDirFilterList.Add(fileDir.Path);
            }
        }

        //文件
        string[] allStrs = AssetDatabase.FindAssets("t:Prefab", abCfg.mAllPrefabPath.ToArray());//该路径集合下所有的Prefab文件的GUID
        for (int i = 0; i < allStrs.Length; i++) {
            string path = AssetDatabase.GUIDToAssetPath(allStrs[i]);
            EditorUtility.DisplayProgressBar("查找Prefab", "Prefabs:" + path, i * 1.0f / allStrs.Length);

            if(!containAllFileAB(path)) {
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                string[] allDepends = AssetDatabase.GetDependencies(path);//所有依赖项的路径
                List<string> allDependPathList = new List<string>();//存储所有的依赖项路径
                for (int j = 0; j < allDepends.Length; j++) {
                    //Debug.Log(allDepends[j]);
                    if(!containAllFileAB(allDepends[j]) && !allDepends[j].EndsWith(".cs")) {
                        AllFileDirFilterList.Add(allDepends[j]);
                        allDependPathList.Add(allDepends[j]);
                    }
                }
                if(AllPrefabDirDict.ContainsKey(obj.name)) {
                    Debug.LogErrorFormat("存在相同名字:{0} 的Prefab!", obj.name);
                }else {
                    AllPrefabDirDict.Add(obj.name, allDependPathList);
                }
            }

        }

        //1.设置AB包名
        //1.1文件夹设置AB包名
        foreach (string name in AllFileDirDict.Keys) {
            setABName(name, AllFileDirDict[name]);
        }
        //1.2文件和其所有依赖项设置AB包名
        foreach (string name in AllPrefabDirDict.Keys) {
            setABName(name, AllPrefabDirDict[name]);
        }

        //2.打包操作
        buildAssetBundle();

        //3.清理AB包名
        string[] allABNames = AssetDatabase.GetAllAssetBundleNames();//获取所有的AB包名
        for (int i = 0; i < allABNames.Length; i++) {//清理掉所有的AB包名(以便于.meta文件不会因为设置包名而产生更改)
            AssetDatabase.RemoveAssetBundleName(allABNames[i], true);
            EditorUtility.DisplayProgressBar("清除AssetBundle包名", "名字:" + allABNames[i], i * 1.0f / allABNames.Length);
        }

        AssetDatabase.Refresh();//刷新编辑器
        EditorUtility.ClearProgressBar();
    }

    /// <summary>
    /// 设置AB包名
    /// </summary>
    /// <param name="name">名字</param>
    /// <param name="path">路径</param>
    static void setABName(string name,string path) {
        AssetImporter assetImporter = AssetImporter.GetAtPath(path);
        if(assetImporter == null) {
            Debug.LogError("不存在此路径下的文件:" + path);
        }else {
            assetImporter.assetBundleName = name;
        }
    }
    static void setABName(string name,List<string> pathList) {
        for (int i = 0; i < pathList.Count; i++) {
            setABName(name, pathList[i]);
        }
    }

    /// <summary>
    /// 打AB包操作
    /// </summary>
    static void buildAssetBundle() {
        string[] allABNames = AssetDatabase.GetAllAssetBundleNames();//获取所有的AB包名
        Dictionary<string, string> resPathDict = new Dictionary<string, string>();//key为全路径 value为包名
        for (int i = 0; i < allABNames.Length; i++) {
            string[] allBundlePath = AssetDatabase.GetAssetPathsFromAssetBundle(allABNames[i]);
            for (int j = 0; j < allBundlePath.Length; j++) {
                if (allBundlePath[j].EndsWith(".cs"))
                    continue;
                Debug.Log("此AB包:" + allABNames[i] + "  下包含的资源文件路径:" + allBundlePath[j]);
                resPathDict.Add(allBundlePath[j], allABNames[i]);
            }
        }

        deleteAssetBundle();

        //生成自己的配置表
        writeData(resPathDict);

        BuildPipeline.BuildAssetBundles(BundleTargetPath, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
    }

    static void writeData(Dictionary<string,string> resPathDict) {
        AssetBundleConfig config = new AssetBundleConfig();
        config.ABBaseList = new List<ABBase>();
        foreach (string path in resPathDict.Keys) {
            ABBase abBase = new ABBase();
            abBase.Path = path;
            abBase.Crc = CRC32.GetCRC32(path);
            abBase.ABName = resPathDict[path];
            abBase.AssetName = path.Remove(0, path.LastIndexOf("/") + 1);
            abBase.ABDependceList = new List<string>();
            string[] resDependce = AssetDatabase.GetDependencies(path);
            for (int i = 0; i < resDependce.Length; i++) {
                string tempPath = resDependce[i];
                if (tempPath == path || tempPath.EndsWith(".cs"))
                    continue;
            }
        }

        //写入xml


        //写入二进制

    }

    /// <summary>
    /// 删除无用的AssetBundle文件(过滤删除)
    /// </summary>
    static void deleteAssetBundle() {
        string[] allBundleNames = AssetDatabase.GetAllAssetBundleNames();
        DirectoryInfo directoryInfo = new DirectoryInfo(BundleTargetPath);
        FileInfo[] fileInfos = directoryInfo.GetFiles("*", SearchOption.AllDirectories);
        for (int i = 0; i < fileInfos.Length; i++) {
            if(containABName(fileInfos[i].Name,allBundleNames) || fileInfos[i].Name.EndsWith(".meta")) {
                continue;
            }else {
                Debug.Log("此AB包已经被删除或者改名了:" + fileInfos[i].Name);
                if(File.Exists(fileInfos[i].FullName)) {
                    File.Delete(fileInfos[i].FullName);
                }
            }
        }
    }

    /// <summary>
    /// 遍历文件夹里的文件名 与设置的AB包进行检查判断
    /// </summary>
    static bool containABName(string name,string[] strs) {
        for (int i = 0; i < strs.Length; i++) {
            if (name == strs[i])
                return true;
        }
        return false;
    }

    /// <summary>
    /// 是否包含在已经有的AB包里  用来做AB包的冗余剔除
    /// </summary>
    static bool containAllFileAB(string path) {
        for (int i = 0; i < AllFileDirFilterList.Count; i++) {
            if(path == AllFileDirFilterList[i] || path.Contains(AllFileDirFilterList[i])) {
                return true;
            }
        }
        return false;
    }

}
