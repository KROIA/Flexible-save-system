using System.Collections.Generic;
namespace SaveLoadSystem.Editor
{
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(SaveableEntity))]
    public class SaveableEntityEditor : SaveIDManagerEditor<SaveableEntity>
    {
        // Implementation for finding all sceene objects of type SaveableEntity
        override protected SaveableEntity[] GetObjects()
        {

#if UNITY_2021_3_OR_NEWER
            return FindObjectsByType<SaveableEntity>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            return FindObjectsOfType<SaveableEntity>();
#endif
        }
    }
}