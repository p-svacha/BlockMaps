namespace BlockmapFramework
{
    /// <summary>
    /// Determines where and how an Entity is placed in the world.
    /// </summary>
    public enum EntityPlacementType
    {
        /// <summary>
        /// Entity is attached to a specific BlockmapNode, which is it's OriginNode.
        /// </summary>
        AttachedToNode = 0,

        /// <summary>
        /// Entity floats freely in any 3D grid cell.
        /// </summary>
        FreeFloating = 1,

        /// <summary>
        /// Entity is inside another entity's inventory.
        /// It does not occupy any node or cell, is not rendered and does not affect the navmesh or vision.
        /// </summary>
        InInventory = 2
    }
}