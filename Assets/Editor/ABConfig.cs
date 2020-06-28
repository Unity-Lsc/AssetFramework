using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ABConfig", menuName = "CreateABConfig", order = 0)]
public class ABConfig : ScriptableObject
{
    //单个文件所在文件夹路径 会遍历这个文件夹下面所有的prefab.所有的prefab的名字不能重复,必须保证名字的唯一性
    public List<string> mAllPrefabPath = new List<string>();

    public List<FileDirABName> mAllFileDirAB = new List<FileDirABName>();

    [System.Serializable]
    public struct FileDirABName {
        /// <summary>
        /// AB包名
        /// </summary>
        public string ABName;
        /// <summary>
        /// 路径
        /// </summary>
        public string Path;
    }
}
