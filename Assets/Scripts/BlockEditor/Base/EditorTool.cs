using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldEditor
{
    public abstract class EditorTool : MonoBehaviour
    {
        protected BlockEditor Editor;
        protected World World => Editor.World;
        public abstract EditorToolId Id { get; }
        public abstract string Name { get; }
        public abstract Sprite Icon { get; }

        protected const string IconBasePath = "BlockEditor/ToolIcons/";

        /// <summary>
        /// Gets called once when the editor is stared up
        /// </summary>
        public virtual void Init(BlockEditor editor)
        {
            Editor = editor;
        }

        /// <summary>
        /// Gets executed when a new world has been set in the editor.
        /// </summary>
        public virtual void OnNewWorld() { }
        public virtual void UpdateTool() { }
        public virtual void HandleLeftClick() { }
        public virtual void HandleLeftDrag() { }
        public virtual void HandleRightClick() { }
        public virtual void HandleRightDrag() { }
        public virtual void HandleMiddleClick() { }
        public virtual void HandleKeyboardInputs() { }

        public virtual void OnDeselect() { }
        public virtual void OnSelect() { }

        public virtual void OnHoveredGroundNodeChanged(GroundNode oldNode, GroundNode newNode) { }
        public virtual void OnHoveredNodeChanged(BlockmapNode oldNode, BlockmapNode newNode) { }
        public virtual void OnHoveredChunkChanged(Chunk oldChunk, Chunk newChunk) { }
        public virtual void OnHoveredEntityChanged(Entity oldEntity, Entity newEntity) { }

    }
}
