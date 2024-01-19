using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public enum ClimbPhase
    {
        None,
        PreClimb,
        InClimb, // only used for SingleClimb
        ClimbUp, // Only used for DoubleClimb
        ClimbTransfer, // Only used for DoubleClimb
        ClimbDown, // Only used for DoubleClimb
        PostClimb,
    }
}
