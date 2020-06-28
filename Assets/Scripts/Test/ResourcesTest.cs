using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

public class ResourcesTest : MonoBehaviour
{

    public GameObject AttackObj;

    // Start is called before the first frame update
    void Start()
    {
        //4中加载资源的方式

        #region 1.拖到组件上
        //Instantiate(AttackObj);
        #endregion

        #region 2.Resources (最多存储2G的大小.一般用来加载配置文件)
        //Resources.Load
        #endregion

        #region 3.AssetBundle
        //AssetBundle assetBundle = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/attack");
        //GameObject obj = Instantiate(assetBundle.LoadAsset<GameObject>("attack"));
        #endregion

        #region 4.AssetDataBase.LoadAtPath
        //GameObject go = Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameData/Prefabs/Attack.prefab"));
        #endregion

        //XML
        //SerializeTest();
        //DeSerializeTest();

        //二进制
        //BinarySerTest();
        //BinaryDeSeriaTest();
        
        //readAssets();
    }

    #region XML
    void SerializeTest() {
        TestSerializa testSeria = new TestSerializa();
        testSeria.Id = 1;
        testSeria.Name = "测试XML";
        testSeria.List = new List<int>();
        testSeria.List.Add(1);
        testSeria.List.Add(2);
        testSeria.List.Add(3);

        xmlSerialize(testSeria);
    }

    void DeSerializeTest() {
        TestSerializa test = xmlDeSerialize();
        Debug.Log("Id:" + test.Id);
        Debug.Log("Name:" + test.Name);
        foreach (var item in test.List) {
            Debug.Log("List:" + item);
        }
    }

    /// <summary>
    /// xml的序列化
    /// </summary>
    void xmlSerialize(TestSerializa seria) {
        FileStream fs = new FileStream(Application.dataPath + "/test.xml", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
        XmlSerializer xml = new XmlSerializer(seria.GetType());
        xml.Serialize(sw, seria);
        sw.Close();
        fs.Close();
    }

    /// <summary>
    /// xml的反向序列化
    /// </summary>
    TestSerializa xmlDeSerialize() {
        FileStream fs = new FileStream(Application.dataPath + "/test.xml", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        XmlSerializer xml = new XmlSerializer(typeof(TestSerializa));
        TestSerializa test = (TestSerializa)xml.Deserialize(fs);
        fs.Close();
        return test;
    }
    #endregion

    #region 二进制

    void BinarySerTest() {
        TestSerializa testSeria = new TestSerializa();
        testSeria.Id = 2;
        testSeria.Name = "测试Binary";
        testSeria.List = new List<int>();
        testSeria.List.Add(10);
        testSeria.List.Add(20);
        testSeria.List.Add(30);
        binarySerialize(testSeria);
    }

    void BinaryDeSeriaTest() {
        //TestSerializa test = binaryDeserialize();
        //Debug.Log("Id:" + test.Id);
        //Debug.Log("Name:" + test.Name);
        //foreach (var item in test.List) {
        //    Debug.Log("List:" + item);
        //}
    }

    /// <summary>
    /// 二进制的序列化
    /// </summary>
    void binarySerialize(TestSerializa test) {
        FileStream fs = new FileStream(Application.dataPath + "/test.bytes", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(fs, test);
        fs.Close();
    }

    /// <summary>
    /// 二进制的反序列化
    /// </summary>
    /// <returns></returns>
    //TestSerializa binaryDeserialize() {
    //    TextAsset ta = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/test.bytes");
    //    MemoryStream ms = new MemoryStream(ta.bytes);
    //    BinaryFormatter bf = new BinaryFormatter();
    //    TestSerializa test = (TestSerializa)bf.Deserialize(ms);
    //    ms.Close();
    //    return test;
    //}

    #endregion


    void readAssets() {
        //AssetSerialize asset = UnityEditor.AssetDatabase.LoadAssetAtPath<AssetSerialize>("Assets/TestAssets.asset");

        //Debug.Log("Id:" + asset.Id);
        //Debug.Log("Name:" + asset.Name);
        //foreach (var item in asset.TestList) {
        //    Debug.Log("List:" + item);
        //}
    }


}
