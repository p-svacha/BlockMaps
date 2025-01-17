using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public class CtfItem : Entity
    {
        // Components
        public Comp_CtfItem ItemComp { get; private set; }

        // Animation
        private const float ROTATION_SPEED = 20f;
        private float RotationOffset;
        private const float BOB_SPEED = 2f;
        private const float BOB_HEIGHT = 0.1f;
        private float BobOffset;

        #region Init

        protected override void OnCompInitialized(EntityComp comp)
        {
            base.OnCompInitialized(comp);

            if (comp is Comp_CtfItem item) ItemComp = item;
        }

        protected override void OnInitialized()
        {
            // Set item texture on model
            MeshRenderer.materials[1].mainTexture = ItemComp.ItemTexture;
        }

        #endregion

        #region Render

        public override void Render(float alpha)
        {
            // Rotate the object
            RotationOffset += ROTATION_SPEED * Time.deltaTime;
            if (RotationOffset > 360) RotationOffset -= 360;
            MeshObject.transform.rotation = Quaternion.Euler(WorldRotation.eulerAngles + new Vector3(0, RotationOffset, 0));

            // Bob up and down
            BobOffset += BOB_SPEED * Time.deltaTime;
            float bobHeight = Mathf.Sin(BobOffset) * BOB_HEIGHT;
            MeshObject.transform.position = new Vector3(WorldPosition.x, WorldPosition.y + bobHeight, WorldPosition.z);
        }

        #endregion

        #region Getters

        public override Sprite UiSprite => HelperFunctions.TextureToSprite(ItemComp.ItemTexture);

        #endregion
    }
}
