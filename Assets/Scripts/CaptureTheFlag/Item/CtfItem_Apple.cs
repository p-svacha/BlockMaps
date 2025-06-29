using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public class CtfItem_Apple : CtfItem
    {
        protected override Texture2D ItemTexture => ResourceManager.LoadTexture(ITEM_TEXTURES_PATH + "Apple");

        public override void TriggerConsumeEffect()
        {
            Holder.AddStamina(20);
        }
    }
}
