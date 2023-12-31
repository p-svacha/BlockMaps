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

        public virtual void Init(BlockEditor editor)
        {
            Editor = editor;
        }

        public virtual void UpdateTool() { }
        public virtual void HandleLeftClick() { }
        public virtual void HandleLeftDrag() { }
        public virtual void HandleRightClick() { }
        public virtual void HandleRightDrag() { }

        public virtual void OnDeselect() { }
        public virtual void OnSelect() { }

        public virtual void OnHoveredSurfaceNodeChanged(SurfaceNode oldNode, SurfaceNode newNode) { }
        public virtual void OnHoveredNodeChanged(BlockmapNode oldNode, BlockmapNode newNode) { }
        public virtual void OnHoveredChunkChanged(Chunk oldChunk, Chunk newChunk) { }
        public virtual void OnHoveredEntityChanged(Entity oldEntity, Entity newEntity) { }

    }
}
