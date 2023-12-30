using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public class CTF_EntityLibrary : EntityLibrary
    {
        private const string ENTITY_PREFAB_PATH = "CTF/Entities/";

        public override Entity GetEntity(string id)
        {
            return Resources.Load<StaticEntity>(ENTITY_PREFAB_PATH + id + ".prefab");

            throw new System.Exception("Id " + id + " does not exist.");
        }
    }
}