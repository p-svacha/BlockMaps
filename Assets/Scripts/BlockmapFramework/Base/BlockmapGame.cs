using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// The heart object of any project using the Blockmap Framework. It is the only MonoBehaviour with logic and responsible to connect the Unity update loop with the blockmap loop.
    /// </summary>
    public class BlockmapGame : MonoBehaviour
    {
        public World World { get; private set; }

        /// <summary>
        /// Sets a world object as the new world of this game.
        /// <br/>Also starts the initialization of that world (drawing, navmesh, vision etc.).
        /// </summary>
        public virtual void SetAndInitializeWorld(World world, System.Action callback)
        {
            // Destory GameObject of previous world
            if(World != null) Destroy(World.WorldObject);

            // Set new world
            World = world;

            // Start world initialization
            World.Initialize(callback);
        }

        public void DestroyWorld()
        {
            Destroy(World.WorldObject);
            World = null;
        }

        protected virtual void Update()
        {
            World?.Tick();
        }
    }
}
