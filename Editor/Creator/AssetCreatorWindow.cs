using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace BatnipTools.Creator
{
    public class AssetCreatorWindow : BaseCreatorWindow
    {
        private const string MENU_ITEMS_FOLDER = "Assets/Create/";
        private const string WINDOW_HEADER = "Create Asset";

        private static List<string> itemsCache;
        private static Dictionary<string, Type> itemToScriptableObjectType;
        
        [MenuItem("Tools/Batnip/Asset Creator")]
        private static void OpenSceneCreator()
        {
            var window = GetWindow<AssetCreatorWindow>();
            window.Initialize(WINDOW_HEADER);
            window.Show();
        }
        
        protected override List<string> FetchItems()
        {
            if(itemsCache != null)
                return itemsCache;
            
            var result = new List<string>();
            
            var types = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(asm => asm.GetTypes())
                .ToArray();

            PopulateScriptableObjects(types, result);
            PopulateMenuItems(types, MENU_ITEMS_FOLDER, result);

            result.Sort();
            itemsCache = result;
            return result;
        }

        protected override void ItemRunRequested(string item)
        {
            if (itemToScriptableObjectType.TryGetValue(item, out var type))
                CreateScriptableObjectFromType(type);
            else
            {
                EditorApplication.ExecuteMenuItem(MENU_ITEMS_FOLDER + item);
                Focus();
            }
        }

        private void CreateScriptableObjectFromType(Type type)
        {
            var instance = CreateInstance(type);
            var path = GetAssetCreationPath(type.Name);
            
            AssetDatabase.CreateAsset(instance, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = instance;
        }

        private string GetAssetCreationPath(string assetName)
        {
            var path = GetProjectWindowFilePath();
            if (path != null)
                if (!AssetDatabase.IsValidFolder(path))
                    path = Path.GetDirectoryName(path);

            var myBool = false;
            if (myBool)
                throw new Exception();
            
            path ??= "Assets";
            path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(path, assetName + ".asset"));
            
            return path;
        }

        [CanBeNull]
        private string GetProjectWindowFilePath()
        {
            var projectWindowUtilType = typeof(ProjectWindowUtil);
            var getActiveFolderPath = projectWindowUtilType.GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);
            var obj = getActiveFolderPath!.Invoke(null, Array.Empty<object>());
            var pathToCurrentFolder = obj.ToString();
            return pathToCurrentFolder;
        }

        private void PopulateScriptableObjects(Type[] types, List<string> populateIn)
        {
            itemToScriptableObjectType = new Dictionary<string, Type>();
            
            var scriptableTypes = types
                .Where(IsCreatableScriptableObjectType);

            foreach (var type in scriptableTypes)
            {
                populateIn.Add(type.FullName);
                itemToScriptableObjectType.Add(type.FullName!, type);
            }
        }

        private bool IsCreatableScriptableObjectType(Type type)
        {
            if (!type.IsSubclassOf(typeof(ScriptableObject)))
                return false;

            var isCreatable = type.GetCustomAttributes(typeof(CreateAssetMenuAttribute), false);
            
            return isCreatable.Length > 0
                   && !type.IsAbstract
                   && !type.IsGenericTypeDefinition
                   && type.GetConstructor(Type.EmptyTypes) != null; // Has parameterless constructor
        }
    }
}