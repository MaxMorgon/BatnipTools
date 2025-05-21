using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor.Creator
{
    public class SceneCreatorWindow : BaseCreatorWindow
    {
        private const string MENU_ITEMS_FOLDER = "GameObject/";
        private const string WINDOW_HEADER = "Create in Scene";

        private static List<string> menuItemsCache;
        private static Dictionary<string, GameObject> componentsCache;
        private static Dictionary<string, GameObject> prefabsByItem;
        
        [MenuItem("Tools/Batnip/Scene Creator")]
        private static void OpenSceneCreator()
        {
            var window = GetWindow<SceneCreatorWindow>();
            window.Initialize(WINDOW_HEADER);
            window.Show();
        }
        
        protected override List<string> FetchItems()
        {
            var result = new List<string>();
            if (menuItemsCache == null)
            {
                menuItemsCache = new List<string>();
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                var types = assemblies
                    .SelectMany(asm => asm.GetTypes());
            
                PopulateMenuItems(types, MENU_ITEMS_FOLDER, menuItemsCache);
            }
            
            result.AddRange(menuItemsCache);
            PopulatePrefabs(result);
            return result;
        }

        private void PopulatePrefabs(List<string> populateInto)
        {
            prefabsByItem ??= new Dictionary<string, GameObject>();
            prefabsByItem.Clear();
            var prefabs = AssetDatabase.FindAssets("t:Prefab");
            foreach (var prefabGUID in prefabs)
            {
                var itemPath = AssetDatabase.GUIDToAssetPath(prefabGUID);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(itemPath);
                prefabsByItem.Add(itemPath, prefab);
                populateInto.Add(itemPath);
            }
        }

        protected override void ItemRunRequested(string item)
        {
            if (prefabsByItem.TryGetValue(item, out var prefab))
            {
                var selectedGameObjectTransform = Selection.activeGameObject?.transform;
                var selectedTransformParent = selectedGameObjectTransform?.parent;
                var createdPrefab = (GameObject)PrefabUtility.InstantiatePrefab(prefab, selectedTransformParent);
                
                Selection.activeGameObject = createdPrefab;
                return;
            }
            
            EditorApplication.ExecuteMenuItem(MENU_ITEMS_FOLDER + item);
            Focus();
        }
    }
}