﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[CreateAssetMenu(fileName = "TestAssets",menuName = "CreateAssets",order = 0)]
public class AssetSerialize : ScriptableObject
{
    public int Id;
    public string Name;
    public List<int> TestList;
}
