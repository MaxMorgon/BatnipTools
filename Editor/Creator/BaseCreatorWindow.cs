using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Lib.BatnipCreator
{
    public abstract class BaseCreatorWindow : EditorWindow
    {
        private static readonly Color SelectionHighlightColor = new (0.24f, 0.48f, 0.90f);
        private static readonly Vector2 WindowSize = new(800, 300);
        private const string SEARCH_FIELD_CONTROL = "SearchField";
        
        private List<string> items;
        private readonly List<string> filteredItems = new();
        private string searchQuery = "";
        
        private Vector2 scrollPos;
        private int selectedIndex = 0;
        
        public void Initialize(string windowHeader)
        {
            minSize = maxSize = WindowSize;
            titleContent = new GUIContent(windowHeader);
            items = FetchItems();
            FilterItems();
        }

        protected abstract List<string> FetchItems();
        protected abstract void ItemRunRequested(string item);

        private void OnGUI()
        {
            GUI.SetNextControlName(SEARCH_FIELD_CONTROL);
            EditorGUI.BeginChangeCheck();
            {
                searchQuery = EditorGUILayout.TextField(searchQuery);
            }
            if (EditorGUI.EndChangeCheck())
                FilterItems();

            HandleKeyboardNavigation();
            
            EditorGUI.FocusTextInControl(SEARCH_FIELD_CONTROL);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            for (var i = 0; i < filteredItems.Count; i++)
            {
                var isSelected = (i == selectedIndex);
                var style = new GUIStyle(EditorStyles.label);
                if (isSelected)
                    style.normal.textColor = Color.white;
                var rect = GUILayoutUtility.GetRect(new GUIContent(filteredItems[i]), style);
                if (isSelected)
                    EditorGUI.DrawRect(rect, SelectionHighlightColor);
                GUI.Label(rect, filteredItems[i], style);

                if (Event.current.type != EventType.MouseDown || !rect.Contains(Event.current.mousePosition))
                    continue;
                
                selectedIndex = i;
                if (Event.current.clickCount == 2)
                    ItemRunRequested(filteredItems[selectedIndex]);
                
                Event.current.Use();
            }
            EditorGUILayout.EndScrollView();
        }

        private void HandleKeyboardNavigation()
        {
            if (Event.current.type == EventType.Used)
            {
                if (Event.current.keyCode == KeyCode.DownArrow)
                {
                    selectedIndex = Mathf.Min(selectedIndex + 1, filteredItems.Count - 1);
                    Event.current.Use();
                    Repaint();
                }
                else if (Event.current.keyCode == KeyCode.UpArrow)
                {
                    selectedIndex = Mathf.Max(selectedIndex - 1, 0);
                    Event.current.Use();
                    Repaint();
                }
                else if (Event.current.keyCode == KeyCode.Return && filteredItems.Count > 0)
                {
                    ItemRunRequested(filteredItems[selectedIndex]);
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.Escape)
                {
                    Close();
                }
            }
        }

        private void FilterItems()
        {
            filteredItems.Clear();
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                filteredItems.AddRange(items);
            }
            else
            {
                foreach (var item in items)
                    if (FuzzyMatch(searchQuery, item))
                        filteredItems.Add(item);
            }
            selectedIndex = 0;
        }
        
        private static bool FuzzyMatch(string text, string query)
        {
            text = text.ToLower();
            query = query.ToLower();
            
            var words = Regex.Split(text, @"\s+");

            foreach (var word in words)
            {
                if(string.IsNullOrWhiteSpace(word))
                    continue;
                
                if (!query.Contains(word))
                    return false;
            }
            
            return true;
        }

        protected static void PopulateMenuItems(IEnumerable<Type> types, string menuRoot, List<string> populateInto)
        {
            var attributes = types
                .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .SelectMany(method => method.GetCustomAttributes(typeof(MenuItem), false));

            foreach (var att in attributes)
            {
                if (att is MenuItem menu && menu.menuItem.StartsWith(menuRoot))
                    populateInto.Add(menu.menuItem[menuRoot.Length..]);
            }
        }
    }
}