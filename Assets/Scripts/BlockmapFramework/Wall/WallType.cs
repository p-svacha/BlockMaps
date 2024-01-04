using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    [CreateAssetMenu(fileName = "WallType", menuName = "Wall Type")]
    public class WallType : ScriptableObject
    {
        public string Id;
        public string Name;
        public bool BlocksVision;
        public Sprite PreviewSprite;
    }
}
