using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public class CTF_EntityLibrary : WorldEntityLibrary
    {
        private const string ENTITY_PREFAB_PATH = "CTF/Entities/";

        public override Entity GetEntityInstance(World world, string id)
        {
            return Resources.Load<StaticEntity>(ENTITY_PREFAB_PATH + id + ".prefab");

            throw new System.Exception("Id " + id + " does not exist.");
        }
    }
}