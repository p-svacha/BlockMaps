using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A collection of all entities that can be used inside a world, including procedural ones.
    /// <br/> Maps entity id's to entity objects.
    /// </summary>
    public abstract class WorldEntityLibrary
    {
        /// <summary>
        /// Returns an uninitialized entity instance according to the given id.
        /// </summary>
        public Entity GetEntityInstance(World world, string id)
        {
            // Check if its a procedural entity
            if(ProceduralEntities.TryGetValue(id, out ProceduralEntity procEntity)) return procEntity.GetInstance();

            return GetCustomEntityInstance(world, id);
        }

        protected abstract Entity GetCustomEntityInstance(World world, string id);

        public Dictionary<string, ProceduralEntity> ProceduralEntities = new Dictionary<string, ProceduralEntity>()
        {
            { PE001_Hedge.TYPE_ID, new PE001_Hedge() }
        };
    }
}
