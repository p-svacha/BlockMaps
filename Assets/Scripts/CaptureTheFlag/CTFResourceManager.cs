using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public class CTFResourceManager : MonoBehaviour
    {
        public static CTFResourceManager Singleton;
        private void Awake()
        {
            Singleton = GameObject.Find("ResourceManager").GetComponent<CTFResourceManager>();
        }

        [Header("Textures")]
        public Texture2D ReachableTileTexture;
    }
}
