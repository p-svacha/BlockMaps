using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class ResourceManager : MonoBehaviour
    {
        public Material SurfaceMaterial;
        public Material CliffMaterial;
        public Material PathCurbMaterial;

        public static ResourceManager Singleton { get { return GameObject.Find("ResourceManager").GetComponent<ResourceManager>(); } }
    }
}
