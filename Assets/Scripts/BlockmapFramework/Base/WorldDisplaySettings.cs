using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Class for holding data about how things in a world are rendered.
    /// <br/>Each world contains exactly one instance of this class.
    /// </summary>
    public class WorldDisplaySettings
    {
        public bool IsShowingGrid { get; private set; }
        public bool IsShowingNavmesh { get; private set; }
        public bool IsShowingTextures { get; private set; }
        public bool IsShowingTileBlending { get; private set; }

        public VisionCutoffMode VisionCutoffMode { get; private set; }

        /// <summary>
        /// Everything higher than this altitude will be hidden when vision cutoff is enabled.
        /// </summary>
        public int VisionCutoffAltitude { get; private set; }

        /// <summary>
        /// Walls behind the PerspectiveVisionCutoffTarget will still be rendered this much higher than VisionCutoffAltitude.
        /// </summary>
        public int VisionCutoffPerpectiveHeight { get; private set; }

        /// <summary>
        /// When VisionCutoffMode is set to PerspectiveCutoff, this entity will be used as a reference point.
        /// </summary>
        public Entity PerspectiveVisionCutoffTarget { get; private set; }

        /// <summary>
        /// True if VisionCutoffMode is not set to Off.
        /// </summary>
        public bool IsVisionCutoffEnabled => VisionCutoffMode != VisionCutoffMode.Off;
        

        public void ShowGrid(bool value) => IsShowingGrid = value;
        public void ToggleGrid() => IsShowingGrid = !IsShowingGrid;

        public void ShowNavmesh(bool value) => IsShowingNavmesh = value;
        public void ToggleNavmesh() => IsShowingNavmesh = !IsShowingNavmesh;

        public void ShowTextures(bool value) => IsShowingTextures = value;
        public void ToggleTextures() => IsShowingTextures = !IsShowingTextures;

        public void ShowTileBlending(bool value) => IsShowingTileBlending = value;
        public void ToggleTileBlending() => IsShowingTileBlending = !IsShowingTileBlending;

        public void SetVisionCutoffMode(VisionCutoffMode mode) => VisionCutoffMode = mode;
        public void SetVisionCutoffAltitude(int alt) => VisionCutoffAltitude = alt;
        public void SetVisionCutoffPerspectiveHeight(int h) => VisionCutoffPerpectiveHeight = h;
        public void SetVisionCutoffPerspectiveTarget(Entity e) => PerspectiveVisionCutoffTarget = e;
    }

    public enum VisionCutoffMode
    {
        /// <summary>
        /// Vision cutoff does not impact how things are rendered.
        /// </summary>
        Off,

        /// <summary>
        /// Everything at and above VisionCutoffPerpectiveMaxAltitude will not be rendered.
        /// <br/>Additionally walls in front of the character are hidden starting from VisionCutoffAltitude.
        /// </summary>
        PerspectiveCutoff,

        /// <summary>
        /// Everything at and above VisionCutoffAltitude will not be rendered.
        /// </summary>
        AbsoluteCutoff
    }
}
