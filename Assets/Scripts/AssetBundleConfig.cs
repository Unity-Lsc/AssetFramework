using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Serialization;

[System.Serializable]
public class AssetBundleConfig
{
    [XmlElement("ABList")]
    public List<ABBase> ABBaseList { get; set; }
}

[System.Serializable]
public class ABBase {

    /// <summary>
    /// 全路径
    /// </summary>
    [XmlAttribute("Path")]
    public string Path { get; set; }

    /// <summary>
    /// 唯一标识
    /// </summary>
    [XmlAttribute("Crc")]
    public uint Crc { get; set; }

    /// <summary>
    /// AB包名
    /// </summary>
    [XmlAttribute("ABName")]
    public string ABName { get; set; }

    /// <summary>
    /// 资源名
    /// </summary>
    [XmlAttribute("AssetName")]
    public string AssetName { get; set; }

    /// <summary>
    /// 依赖的资源
    /// </summary>
    [XmlElement("ABDependceList")]
    public List<string> ABDependceList { get; set; }

}
