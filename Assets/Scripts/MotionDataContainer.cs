using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tcc
{
    [CreateAssetMenu]
    public class MotionDataContainer : ScriptableObject
    {
        public MotionData[] motionDataList;
    }

    [System.Serializable]
    public class MotionData
    {
        public int motionID;
        public Sprite sprite;
    }
}