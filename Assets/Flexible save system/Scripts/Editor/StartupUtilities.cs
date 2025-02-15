using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace SaveLoadSystem.Editor
{
    [InitializeOnLoad]
    public class StartupUtilities : EditorWindow
    {
        public static int timer { get; private set; }
        public static bool autoUpdateID = true;

        public static System.Action updateIDs { get; set; }
        static StartupUtilities()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += PlayModeChange;
        }
        static void Update()
        {

        }
        static void PlayModeChange(PlayModeStateChange c)
        {
            switch (c)
            {
                case PlayModeStateChange.ExitingEditMode:
                    {
                        GenerateEmptyIDs();
                        CheckForIDConflicts();
                        break;
                    }
                case PlayModeStateChange.EnteredEditMode:
                    {
                        GenerateEmptyIDs();
                        CheckForIDConflicts();
                        break;
                    }
                case PlayModeStateChange.EnteredPlayMode:
                    {
                        break;
                    }
                case PlayModeStateChange.ExitingPlayMode:
                    {
                        break;
                    }
            }
        }

        static void CheckForIDConflicts()
        {
            CheckForIDConflicts_prefabs();
            CheckForIDConflicts_scriptableObjects();
            CheckForIDConflicts_GameObject();
        }
        static void CheckForIDConflicts_prefabs()
        {
            SaveablePrefabsEditor.GenerateListWithAllPrefabs();
            List<SaveablePrefabsEditor.ConflictData> conflicts = SaveablePrefabsEditor.GetIDConflicts();
            if (conflicts.Count > 0)
            {
                string text = "";
                for (int i = 0; i < conflicts.Count; ++i)
                {
                    text = conflicts[i].message + "\n";
                }
                if (IDConflictPopup("Prefab ID Conflict", text))
                {
                    SaveablePrefabsEditor.ResolveIDConflicts();
                }
            }
        }
        static void CheckForIDConflicts_scriptableObjects()
        {
            Dictionary<string, ScriptableObject> allIdList = new Dictionary<string, ScriptableObject>();
            Dictionary<string, List<ScriptableObject>> conflicts = new Dictionary<string, List<ScriptableObject>>();

            ScriptableObject[] objs = Resources.LoadAll<ScriptableObject>("");
            for (int i = 0; i < objs.Length; ++i)
            {
                if (objs[i] == null) continue;
                {
                    if (objs[i] is ISaveID)
                    {
                        ISaveID saveID_interface = objs[i] as ISaveID;

                        
                        if (allIdList.ContainsKey(saveID_interface.GetID()))
                        {
                            if (!conflicts.ContainsKey(saveID_interface.GetID()))
                                conflicts.Add(saveID_interface.GetID(), new List<ScriptableObject>());
                            List<ScriptableObject> list;
                            conflicts.TryGetValue(saveID_interface.GetID(), out list);
                            if (list != null)
                                list.Add(objs[i]);
                            goto nextObj;
                        }
                        allIdList.Add(saveID_interface.GetID(), objs[i]);
                    }
                }
            nextObj:;
            }

            if (conflicts.Count > 0)
            {
                string text = "";
                int counter = 0;
                int index = 0;
                foreach (var conflict in conflicts)
                {
                    foreach (var el in conflict.Value)
                    {
                        text = el.name + " ID: " + conflict.Key + " \n";
                        ++counter;
                        if (counter > 20)
                        {
                            text += "some more...";
                            break;
                        }
                    }
                    Debug.LogWarning("Conflict [" + index + "]: " + allIdList[conflict.Key].name + " ID=" + conflict.Key, allIdList[conflict.Key]);
                    ++index;
                }
                if (IDConflictPopup("Scriptable Objects ID Conflict", text))
                {
                    // Resolve
                    foreach (var conflict in conflicts)
                    {
                        foreach (var elem in conflict.Value)
                        {
                            if (conflict.Value is ISaveID)
                            {
                                ISaveID saveID_interface = conflict.Value as ISaveID;
                                SerializedObject obj = new SerializedObject(elem);
                                SerializedProperty idProp = obj.FindProperty("m_ID");
                                if (idProp != null)
                                {
                                    idProp.stringValue = System.Guid.NewGuid().ToString();
                                    obj.ApplyModifiedProperties();
                                }
                            }
                        }
                    }
                }
            }
        }
        static void CheckForIDConflicts_GameObject()
        {
            Dictionary<string, GameObject> allIdList = new Dictionary<string, GameObject>();
            Dictionary<string, List<GameObject>> conflicts = new Dictionary<string, List<GameObject>>();

#if UNITY_2021_3_OR_NEWER
            GameObject[] objs = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            GameObject[] objs = FindObjectsOfType<GameObject>();
#endif
            for (int i = 0; i < objs.Length; ++i)
            {
                if (objs[i] == null) continue;
                Component[] saveIDs = objs[i].GetComponents(typeof(ISaveID));
                for (int j = 0; j < saveIDs.Length; ++j)
                {
                    if (saveIDs[j] is ISaveID)
                    {
                        ISaveID saveID_interface = saveIDs[j] as ISaveID;

                        GameObject inAllList;
                        allIdList.TryGetValue(saveID_interface.GetID(), out inAllList);
                        bool isComponent = false;
                        if (inAllList != null)
                        {
                            Component[] ids = inAllList.GetComponents(typeof(ISaveID));
                            foreach (var id in ids)
                                if (id == saveIDs[j])
                                    isComponent = true;
                        }

                        if (allIdList.ContainsKey(saveID_interface.GetID()))
                        {
                            if (!isComponent)
                            {
                                if (!conflicts.ContainsKey(saveID_interface.GetID()))
                                {
                                    conflicts.Add(saveID_interface.GetID(), new List<GameObject>());
                                }

                                List<GameObject> list;
                                conflicts.TryGetValue(saveID_interface.GetID(), out list);
                                if (list != null)
                                    list.Add(objs[i]);
                            }
                            goto nextObj;
                        }
                        allIdList.Add(saveID_interface.GetID(), objs[i]);
                    }
                }
            nextObj:;
            }
            if (conflicts.Count > 0)
            {
                string text = "";
                int counter = 0;
                int index = 0;
                foreach (var conflict in conflicts)
                {
                    foreach (var el in conflict.Value)
                    {
                        text = el.name + " ID: " + conflict.Key + " \n";
                        ++counter;
                        if (counter > 20)
                        {
                            text += "some more...";
                            break;
                        }
                    }

                    Debug.LogWarning("Conflict [" + index + "]: " + allIdList[conflict.Key].name + " ID=" + conflict.Key, allIdList[conflict.Key]);
                    ++index;
                }
                if (IDConflictPopup("GameObjects ID Conflict", text))
                {
                    // Resolve
                    foreach (var conflict in conflicts)
                    {
                        foreach (var elem in conflict.Value)
                        {
                            Component[] saveIDs = elem.GetComponents(typeof(ISaveID));
                            for (int j = 0; j < saveIDs.Length; ++j)
                            {
                                if (saveIDs[j] is ISaveID)
                                {
                                    ISaveID saveID_interface = saveIDs[j] as ISaveID;
                                    SerializedObject obj = new SerializedObject(saveIDs[j]);
                                    SerializedProperty idProp = obj.FindProperty("m_ID");
                                    if (idProp != null)
                                    {
                                        idProp.stringValue = System.Guid.NewGuid().ToString();
                                        obj.ApplyModifiedProperties();
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        static bool IDConflictPopup(string title, string message)
        {
            return EditorUtility.DisplayDialog("Save Load System: " + title,
                                             message, "Try to resolve", "Ignore");
        }

        static void GenerateEmptyIDs()
        {
            if (updateIDs != null)
                updateIDs();
        }
    }



}