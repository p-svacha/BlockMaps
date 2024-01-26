using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public enum GenerationPhase
    {
        WaitingForFixedUpdate,
        Generating,
        Initializing,
        Done
    }
}
