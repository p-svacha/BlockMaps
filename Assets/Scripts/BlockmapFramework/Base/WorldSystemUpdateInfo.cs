using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Struct holding information about a pending world system update.
    /// </summary>
    public class WorldSystemUpdateInfo
    {
        /// <summary>
        /// The area of the world that needs to be updated.
        /// </summary>
        public Parcel Area { get; private set; }

        /// <summary>
        /// The callback function that gets executed when the update is done.
        /// </summary>
        public System.Action Callback { get; private set; }

        public WorldSystemUpdateInfo(Parcel area, Action callback)
        {
            Area = area;
            Callback = callback;
        }
    }
}
