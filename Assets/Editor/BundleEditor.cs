using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

public class BundleEditor
{
    /// <summary>
    /// 配置文件路径
    /// </summary>
    private static string ABCONFIG_PATH = "Assets/Editor/ABConfig.asset";
    /// <summary>
    /// 二进制文件路径
    /// </summary>
    private static string mAssetBundleBytesPath = "Assets/GameData/Data/AssetBundleConfig.bytes";

    /// <summary>
    /// AssetBundle文件生成路径
    /// </summary>
    private static string mBundleTargetPath = Application.streamingAssetsPath;

    /// <summary>
    /// key是AB包名,value是路径  所有文件夹的AB包的Dict
    /// </summary>
    private static Dictionary<string, string> mAllFileDirDict = new Dictionary<string, string>();

    /// <summary>
    /// 过滤的List 添加的是路径
    /// </summary>
    private static List<string> mAllFileDirFilterList = new List<string>();

    /// <summary>
    /// 单个prefab的ab包  key是预制体的名字 value是所有依赖项的路径集合
    /// </summary>
    private static Dictionary<string, List<string>> mAllPrefabDirDict = new Dictionary<string, List<string>>();

    /// <summary>
    /// 储存所有有效路径
    /// </summary>
    private static List<string> mConfigFileList = new List<string>();

    [MenuItem("Tools/打包")]
    public static void Build() {

        mAllFileDirFilterList.Clear();
        mAllFileDirDict.Clear();
        mAllPrefabDirDict.Clear();
        mConfigFileList.Clear();
        ABConfig abCfg = AssetDatabase.LoadAssetAtPath<ABConfig>(ABCONFIG_PATH);

        //文件夹
        foreach (ABConfig.FileDirABName fileDir in abCfg.mAllFileDirAB) {
            if(mAllFileDirDict.ContainsKey(fileDir.ABName)) {
                Debug.LogError("AB包配置名字重复:" + fileDir.ABName);
            }else {
                mAllFileDirDict.Add(fileDir.ABName, fileDir.Path);
                mAllFileDirFilterList.Add(fileDir.Path);
                mConfigFileList.Add(fileDir.Path);
            }
        }

        //文件
        string[] allStrs = AssetDatabase.FindAssets("t:Prefab", abCfg.mAllPrefabPath.ToArray());//该路径集合下所有的Prefab文件的GUID
        for (int i = 0; i < allStrs.Length; i++) {
            string path = AssetDatabase.GUIDToAssetPath(allStrs[i]);
            EditorUtility.DisplayProgressBar("查找Prefab", "Prefabs:" + path, i * 1.0f / allStrs.Length);
            mConfigFileList.Add(path);
            if(!containAllFileAB(path)) {
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                string[] allDepends = AssetDatabase.GetDependencies(path);//所有依赖项的路径
                List<string> allDependPathList = new List<string>();//存储所有的依赖项路径
                for (int j = 0; j < allDepends.Length; j++) {
                    //Debug.Log(allDepends[j]);
                    if(!containAllFileAB(allDepends[j]) && !allDepends[j].EndsWith(".cs")) {
                        mAllFileDirFilterList.Add(allDepends[j]);
                        allDependPathList.Add(allDepends[j]);
                    }
                }
                if(mAllPrefabDirDict.ContainsKey(obj.name)) {
                    Debug.LogErrorFormat("存在相同名字:{0} 的Prefab!", obj.name);
                }else {
                    mAllPrefabDirDict.Add(obj.name, allDependPathList);
                }
            }

        }

        //1.设置AB包名
        //1.1文件夹设置AB包名
        foreach (string name in mAllFileDirDict.Keys) {
            setABName(name, mAllFileDirDict[name]);
        }
        //1.2文件和其所有依赖项设置AB包名
        foreach (string name in mAllPrefabDirDict.Keys) {
            setABName(name, mAllPrefabDirDict[name]);
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
                if (allBundlePath[j].EndsWith(".cs") || !isValidPath(allBundlePath[j]))
                    continue;
                //Debug.Log("此AB包:" + allABNames[i] + "  下包含的资源文件路径:" + allBundlePath[j]);
                resPathDict.Add(allBundlePath[j], allABNames[i]);
            }
        }

        deleteAssetBundle();

        //生成自己的配置表
        writeData(resPathDict);

        BuildPipeline.BuildAssetBundles(mBundleTargetPath, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
    }
    /// <summary>
    /// 写入数据(xml文件 二进制文件)
    /// </summary>
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
                if (tempPath == path || path.EndsWith(".cs"))
                    continue;
                string abName;
                if(resPathDict.TryGetValue(tempPath,out abName)) {
                    if (abName == resPathDict[path])
                        continue;
                    if(!abBase.ABDependceList.Contains(abName)) {
                        abBase.ABDependceList.Add(abName);
                    }
                }
            }
            config.ABBaseList.Add(abBase);
        }

        //写入xml
        string xmlPath = Application.dataPath + "/AssetBundleConfig.xml";
        if (File.Exists(xmlPath)) File.Delete(xmlPath);
        FileStream fs = new FileStream(xmlPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
        XmlSerializer xmlSerializer = new XmlSerializer(config.GetType());
        xmlSerializer.Serialize(sw, config);
        sw.Close();
        fs.Close();

        //写入二进制
        foreach (ABBase abBase in config.ABBaseList) {
            //xml中的Path,只是为了方便查看路径.实际的加载是通过CRC进行.在二进制文件中清空path,可以减少二进制文件的大小
            abBase.Path = string.Empty;
        }
        string bytePath = mAssetBundleBytesPath;
        FileStream fileStream = new FileStream(bytePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(fileStream, config);
        fileStream.Close();
    }

    /// <summary>
    /// 删除无用的AssetBundle文件(过滤删除)
    /// </summary>
    static void deleteAssetBundle() {
        string[] allBundleNames = AssetDatabase.GetAllAssetBundleNames();
        DirectoryInfo directoryInfo = new DirectoryInfo(mBundleTargetPath);
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
        for (int i = 0; i < mAllFileDirFilterList.Count; i++) {
            if(path == mAllFileDirFilterList[i] || (path.Contains(mAllFileDirFilterList[i]) && path.Replace(mAllFileDirFilterList[i],"")[0] == '/')) {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 是否是有效路径
    /// </summary>
    static bool isValidPath(string path) {
        for (int i = 0; i < mConfigFileList.Count; i++) {
            if(path.Contains(mConfigFileList[i])) {
                return true;
            }
        }
        return false;
    }

}
