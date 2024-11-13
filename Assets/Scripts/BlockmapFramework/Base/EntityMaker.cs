using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
	/// <summary>
	/// The class that is responsible for creating new physical things in the world.
	/// </summary>
	public static class EntityMaker
	{
		public static Entity MakeEntity(EntityDef def)
		{
			Entity obj = (Entity)Activator.CreateInstance(def.EntityClass);
			return obj;
		}
	}
}
