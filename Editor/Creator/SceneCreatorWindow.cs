using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BatnipTools.Creator
{
    public class SceneCreatorWindow : BaseCreatorWindow
    {
        private const string MENU_ITEMS_FOLDER = "GameObject/";
        private const string WINDOW_HEADER = "Create in Scene";

        private static List<string> menuItemsCache;
        private static Dictionary<string, Type> componentsTypeByKey;
        private static Dictionary<string, GameObject> prefabsByItem;
        private static List<string> allItems;
        
        [MenuItem("Tools/Batnip/Scene Creator")]
        private static void OpenSceneCreator()
        {
            var window = GetWindow<SceneCreatorWindow>();
            window.Initialize(WINDOW_HEADER);
            window.Show();
        }
        
        protected override List<string> FetchItems()
        {
            allItems ??= new List<string>();
            allItems.Clear();
            
            // Initialize compile time related keys
            if (menuItemsCache == null)
            {
                menuItemsCache = new List<string>();
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                var types = assemblies
                    .SelectMany(asm => asm.GetTypes())
                    .ToArray();
            
                PopulateMenuItems(types, MENU_ITEMS_FOLDER, menuItemsCache);
                PopulateComponents(types);
            }
            
            // Add cached to items
            allItems.AddRange(menuItemsCache);
            allItems.AddRange(componentsTypeByKey.Keys);
            
            // Populate prefabs
            PopulatePrefabs(allItems);
            return allItems;
        }

        private void PopulateComponents(Type[] types)
        {
            var componentTypes = types.Where(IsInstantiatableComponent);
            componentsTypeByKey = new Dictionary<string, Type>();
            foreach (var componentType in componentTypes)
            {
                if (!componentsTypeByKey.TryAdd(componentType.FullName, componentType))
                {
                    var other = componentsTypeByKey[componentType.FullName!];
                    Debug.Log($"Found duplicate component of type {componentType.Name} fullname1: {componentType.FullName} fullname2: {other.FullName}");
                }
            }
        }
        
        private bool IsInstantiatableComponent(Type type){
            return type.IsSubclassOf(typeof(Component))
                && !type.IsAbstract;
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
            if (componentsTypeByKey.TryGetValue(item, out var componentType))
            {
                var selectedGameObject = Selection.activeGameObject;
                if (selectedGameObject == null)
                {
                    Debug.LogError($"Can't add component {componentType.Name} as there is no game object selected.");
                    return;
                }
                
                selectedGameObject.AddComponent(componentType);
                return;
            }
            
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