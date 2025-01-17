using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public class CompProperties_CtfItem : CompProperties
    {
        public Texture ItemTexture { get; init; } = null;

        public override CompProperties Clone()
        {
            return new CompProperties_CtfItem()
            {
                ItemTexture = this.ItemTexture
            };
        }
    }
}
