using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Singleton class to easily access and create procedural entity instances.
    /// </summary>
    public class ProceduralEntityManager
    {
        private static ProceduralEntityManager _Instance;
        private List<ProceduralEntityId> ProceduralEntities;

        private ProceduralEntityManager()
        {
            ProceduralEntities = new List<ProceduralEntityId>()
            { 
                ProceduralEntityId.PE001,
            };
        }

        public static ProceduralEntityManager Instance
        {
            get
            {
                if (_Instance == null) _Instance = new ProceduralEntityManager();
                return _Instance;
            }
        }

        public List<ProceduralEntityId> GetAllProceduralEntityIds() => ProceduralEntities;

        /// <summary>
        /// Returns an unregistered instance of a procedural entity.
        /// </summary>
        public ProceduralEntity GetProceduralEntityInstance(ProceduralEntityId id, int height)
        {
            GameObject peObject = new GameObject(id.ToString());
            ProceduralEntity pe = null;
            switch(id)
            {
                case ProceduralEntityId.PE001:
                    pe = peObject.AddComponent<PE001_Hedge>();
                    break;
            }

            if (pe == null) throw new System.Exception("ProceduralEntityId instancing not yet handled in ProceduralEntityManager for id " + id.ToString());
            pe.InitProceduralEntity(height);

            return pe;
        }
    }
}
