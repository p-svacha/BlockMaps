using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public abstract class CtfItem : Entity
    {
        // Context
        protected CtfMatch Match;
        new public CtfCharacter Holder => (CtfCharacter)base.Holder;

        // Texture
        protected const string ITEM_TEXTURES_PATH = "CaptureTheFlag/Items/";
        protected abstract Texture2D ItemTexture { get; }

        // Animation
        private const float ROTATION_SPEED = 20f;
        private float RotationOffset;
        private const float BOB_SPEED = 2f;
        private const float BOB_HEIGHT = 0.1f;
        private float BobOffset;

        #region Init

        protected override void OnInitialized()
        {
            // Create UI Sprite
            _UiSprite = HelperFunctions.TextureToSprite(ItemTexture);

            // Set item texture on model
            MeshRenderer.materials[1].mainTexture = ItemTexture;
        }

        public void OnMatchReady(CtfMatch match)
        {
            Match = match;
        }

        #endregion

        #region Actions

        /// <summary>
        /// The effect that gets triggered when this item is consumed.
        /// </summary>
        public abstract void TriggerConsumeEffect();

        #endregion

        #region Render

        public override void Render(float alpha)
        {
            UpdateMeshObjectTransform();
        }

        protected override void SetMeshObjectTransform(Vector3 position, Quaternion rotation)
        {
            // Rotate the object
            RotationOffset += ROTATION_SPEED * Time.deltaTime;
            if (RotationOffset > 360) RotationOffset -= 360;
            MeshObject.transform.rotation = Quaternion.Euler(rotation.eulerAngles + new Vector3(0, RotationOffset, 0));

            // Bob up and down
            BobOffset += BOB_SPEED * Time.deltaTime;
            float bobHeight = Mathf.Sin(BobOffset) * BOB_HEIGHT;
            MeshObject.transform.position = new Vector3(position.x, position.y + bobHeight, position.z);
        }

        #endregion

        #region Getters

        private Sprite _UiSprite;
        public override Sprite UiSprite => _UiSprite;

        #endregion
    }
}
