using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WorldEditor
{
    public class EditorEntityLibrary : WorldEntityLibrary
    {
        private const string ENTITY_PREFAB_PATH = "Editor/Entities/";
        private BlockEditor Editor;

        public void Init(BlockEditor editor)
        {
            Editor = editor;
        }

        public override Entity GetEntityInstance(World world, string id)
        {
            string[] attributes = id.Split('_');
            id = attributes[0];

            string fullPath = ENTITY_PREFAB_PATH + id;

            // Editor character
            if (id == "character")
            {
                EditorMovingEntity prefab = Resources.Load<EditorMovingEntity>(fullPath);
                EditorMovingEntity instance = GameObject.Instantiate(prefab, world.transform);
                float movementSpeed = float.Parse(attributes[1]);
                float vision = float.Parse(attributes[2]);
                int height = int.Parse(attributes[3]);
                bool canSwim = bool.Parse(attributes[4]);
                instance.PreInit(movementSpeed, vision, height, canSwim);

                return instance;
            }

            // Default
            StaticEntity staticEntity = Resources.Load<StaticEntity>(fullPath);
            return GameObject.Instantiate(staticEntity, world.transform);

            throw new System.Exception("Id " + id + " does not exist.");
        }
    }
}
