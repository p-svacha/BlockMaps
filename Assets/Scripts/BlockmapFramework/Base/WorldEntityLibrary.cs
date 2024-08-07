using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        public Entity GetEntityInstance(World world, EntityData data)
        {
            string[] attributes = data.TypeId.Split('_');
            string baseId = attributes[0];

            // Procedural entity
            if (ProceduralEntityManager.Instance.GetAllProceduralEntityIds().Any(x => x.ToString() == baseId))
            {
                ProceduralEntityId id = ProceduralEntityManager.Instance.GetAllProceduralEntityIds().First(x => x.ToString() == baseId);
                int height = int.Parse(attributes[1]);
                return ProceduralEntityManager.Instance.GetProceduralEntityInstance(id, height);
            }

            // Ladder entity
            if (baseId == Ladder.LADDER_ENTITY_NAME)
            {
                return Ladder.GetInstance(world, data);
            }

            // Door entity
            if (baseId == Door.DOOR_ENTITY_NAME)
            {
                return Door.GetInstance(world, data);
            }

            return GetCustomEntityInstance(world, data.TypeId);
        }

        protected abstract Entity GetCustomEntityInstance(World world, string id);
    }
}
