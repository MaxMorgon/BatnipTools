using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Lib.BatnipCreator
{
    public class SceneCreatorWindow : BaseCreatorWindow
    {
        private const string MENU_ITEMS_FOLDER = "GameObject/";
        private const string WINDOW_HEADER = "Create in Scene";

        private static List<string> itemsCache;
        
        [MenuItem("Tools/Batnip/Scene Creator")]
        private static void OpenSceneCreator()
        {
            var window = GetWindow<SceneCreatorWindow>();
            window.Initialize(WINDOW_HEADER);
            window.Show();
        }
        
        protected override List<string> FetchItems()
        {
            if(itemsCache != null)
                return itemsCache;
            
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var types = assemblies
                .SelectMany(asm => asm.GetTypes());
            
            var result = new List<string>();
            
            PopulateMenuItems(types, MENU_ITEMS_FOLDER, result);

            result.Sort();
            itemsCache = result;
            return result;
        }

        protected override void ItemRunRequested(string item)
        {
            EditorApplication.ExecuteMenuItem(MENU_ITEMS_FOLDER + item);
            Focus();
        }
    }
}