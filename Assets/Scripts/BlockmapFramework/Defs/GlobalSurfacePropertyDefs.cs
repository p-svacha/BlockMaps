using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.Defs
{
    /// <summary>
    /// The collection of all SurfaceDefs that are useful globally for all projects.
    /// </summary>
    public static class GlobalSurfacePropertyDefs
    {
        public static List<SurfacePropertyDef> Defs = new List<SurfacePropertyDef>()
        {
            new SurfacePropertyDef()
            {
                DefName = "Grass",
                Label = "grass",
                Description = "Surface property of grassy surfaces",
                MovementSpeedModifier = 0.5f,
            },

            new SurfacePropertyDef()
            {
                DefName = "Sand",
                Label = "sand",
                Description = "Surface property of sandy surfaces",
                MovementSpeedModifier = 0.35f,
            },

            new SurfacePropertyDef()
            {
                DefName = "Concrete",
                Label = "concrete",
                Description = "Surface property used by many concrete or asphalt-like surfaces",
                MovementSpeedModifier = 1f,
            },

            new SurfacePropertyDef()
            {
                DefName = "Dirt",
                Label = "dirt",
                Description = "Surface property used by soily surfaces",
                MovementSpeedModifier = 0.8f,
            },

            new SurfacePropertyDef()
            {
                DefName = "Water",
                Label = "water",
                Description = "Surface property used by water",
                MovementSpeedModifier = 0.2f,
                Paintable = false,
            },
        };
    }
}
