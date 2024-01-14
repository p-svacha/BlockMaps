using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Just a reference to a ladder that is attachable to a GameObject.
    /// </summary>
    public class LadderReference : MonoBehaviour
    {
        public Ladder Ladder { get; private set; }

        public void Init(Ladder ladder)
        {
            Ladder = ladder;
        }
    }
}
