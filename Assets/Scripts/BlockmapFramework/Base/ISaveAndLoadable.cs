using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Interface that needs to be implemented for all classes whose instances need to be easily saveable and loadable.
    /// Classes implementing this interface need an empty constructor.
    /// </summary>
    public interface ISaveAndLoadable
    {
        /// <summary>
        /// Defines all data that needs to be saved and loaded with "SaveLoadManager.SaveOrLoadX".
        /// <br/>You can detect if you are in a save or load call by checking SaveLoadManager.iÎsLoading / IsSaving.
        /// </summary>
        public void ExposeDataForSaveAndLoad();
    }
}
