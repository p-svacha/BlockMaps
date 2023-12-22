using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public class CTFGame : MonoBehaviour
    {
        private void Start()
        {
            CTFWorldGenerator.GenerateWorld("CTFWorld", 10, 10);
        }
    }
}
