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
        /// Takes a World object as an input and creates all Unity GameObjects for that world.
        /// <br/>This is now the world of this game.
        /// </summary>
        public virtual void SetWorld(World world)
        {
            // Destory GameObject of previous world
            if(World != null) Destroy(World.WorldObject);

            // Set new world
            World = world;

            // Start world initialization
            World.Initialize();
        }

        protected virtual void Update()
        {
            World?.Tick();
        }

        protected virtual void FixedUpdate()
        {
            World?.FixedUpdate();
        }
    }
}
