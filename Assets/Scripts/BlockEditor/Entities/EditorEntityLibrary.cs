using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WorldEditor
{
    public class EditorEntityLibrary : WorldEntityLibrary
    {
        private const string ENTITY_PREFAB_PATH = "Entities/Prefabs/";
        private BlockEditor Editor;

        public void Init(BlockEditor editor)
        {
            Editor = editor;
        }

        protected override Entity GetCustomEntityInstance(World world, string id)
        {
            string[] attributes = id.Split('_');
            string idPrefix = attributes[0];

            string fullPath = ENTITY_PREFAB_PATH + id;

            // Editor character
            if (idPrefix == "character")
            {
                fullPath = ENTITY_PREFAB_PATH + idPrefix;
                EditorMovingEntity prefab = Resources.Load<EditorMovingEntity>(fullPath);
                if (prefab == null) throw new System.Exception("Resource " + fullPath + " could not be loaded.");
                EditorMovingEntity instance = GameObject.Instantiate(prefab, world.transform);
                float movementSpeed = float.Parse(attributes[1]);
                float vision = float.Parse(attributes[2]);
                int height = int.Parse(attributes[3]);
                bool canSwim = bool.Parse(attributes[4]);
                ClimbingCategory climbSkill = (ClimbingCategory)(int.Parse(attributes[5]));
                instance.PreInit(movementSpeed, vision, height, canSwim, climbSkill);

                return instance;
            }

            // Default
            Entity entity = Resources.Load<Entity>(fullPath);
            if (entity == null) throw new System.Exception("Resource " + fullPath + " could not be loaded.");
            return GameObject.Instantiate(entity, world.transform);

            throw new System.Exception("Id " + id + " does not exist.");
        }
    }
}
