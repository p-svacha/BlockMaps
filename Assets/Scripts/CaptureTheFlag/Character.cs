using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public class Character : MonoBehaviour
    {
        public MovingEntity Entity { get; private set; }

        private void Awake()
        {
            Entity = GetComponent<MovingEntity>(); 
        }
    }
}
