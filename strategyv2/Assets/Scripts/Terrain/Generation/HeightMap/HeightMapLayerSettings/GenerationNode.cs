using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HeightMapGeneration
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "NewGenerationNode", menuName = "settings/HeightMap/BaseNode", order = 0)]
    public class GenerationNode : ScriptableObject
    {
        [SerializeField]
        public Algorithm Type;
        [SerializeField]
        public bool ShowInternalData;

        [SerializeField]
        public string GUID = "";

        public bool Enabled = true;

        public ScriptableObject GetSettings()
        {
            Type settingsType = NodeUtils.AlgorithmTypeMap[this.Type];
            return AssetDatabase.LoadAssetAtPath($"Assets/Saves/GenerationTempFiles/{GUID.ToString()}.asset", settingsType) as ScriptableObject;
        }
    }
}
