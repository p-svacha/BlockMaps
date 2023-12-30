using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldEditor
{
    public class EditorEntityLibrary : EntityLibrary
    {
        private const string ENTITY_PREFAB_PATH = "Editor/Entities/";

        public override Entity GetEntity(string id)
        {
            string fullPath = ENTITY_PREFAB_PATH + id;
            if (id == "character") return Resources.Load<EditorMovingEntity>(fullPath);

            return Resources.Load<StaticEntity>(fullPath);

            throw new System.Exception("Id " + id + " does not exist.");
        }
    }
}
