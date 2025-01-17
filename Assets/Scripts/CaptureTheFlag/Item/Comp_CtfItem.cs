using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public abstract class Comp_CtfItem : EntityComp
    {
        private CtfItem Entity => (CtfItem)entity;
        private CompProperties_CtfItem Props => (CompProperties_CtfItem)props;


        #region Getters

        public Texture ItemTexture => Props.ItemTexture;

        #endregion

    }
}
