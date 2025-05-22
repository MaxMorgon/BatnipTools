using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityToolbarExtender;

namespace BatnipTools.CompilationTracker
{
    [InitializeOnLoad]
    public static class CompilationTracker
    {
        public static bool HasPendingChanges { get; private set; }

        private static readonly FileSystemWatcher ScriptsWatcher;

        static CompilationTracker()
        {
            ScriptsWatcher = new FileSystemWatcher(Application.dataPath, "*.cs")
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
            };
            
            AddListeners();
            InitializeGUI();
            InitializeOriginalHashes();
        }

        private static void AddListeners()
        {
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            
            ScriptsWatcher.Changed += OnScriptFileChanged;
            ScriptsWatcher.Created += OnScriptFileChanged;
            ScriptsWatcher.Renamed += OnScriptFileChanged;
            ScriptsWatcher.Deleted += OnScriptFileChanged;
        }

        #region Change Tracking

        private static void OnScriptFileChanged(object sender, FileSystemEventArgs e)
        {
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Deleted:
                    PostChangesState.Remove(e.FullPath);
                    break;
                case WatcherChangeTypes.Renamed:
                    PostChangesState.Remove(e.FullPath);
                    PostChangesState[e.FullPath] = CalculateFileHash(e.FullPath);
                    break;
                case WatcherChangeTypes.Created:
                case WatcherChangeTypes.Changed:
                    PostChangesState[e.FullPath] = CalculateFileHash(e.FullPath);
                    break;
            }

            var hasFileCountChange = PostChangesState.Count != PreChangesState.Count;
            if (hasFileCountChange)
            {
                HasPendingChanges = true;
                return;
            }

            HasPendingChanges = PreChangesState.Any(CheckFileChangePredicate);
        }

        private static bool CheckFileChangePredicate(KeyValuePair<string, string> originalPair)
        {
            return PostChangesState.TryGetValue(originalPair.Key, out var modifiedValue) && modifiedValue != originalPair.Value;
        }

        private static void OnCompilationFinished(object value)
        {
            InitializeOriginalHashes();
            HasPendingChanges = false;
        }

        private static readonly Dictionary<string, string> PreChangesState = new();
        private static readonly Dictionary<string, string> PostChangesState = new();

        public static void InitializeOriginalHashes()
        {
            PreChangesState.Clear();
            PostChangesState.Clear();
            
            var osDataPath = Application.dataPath.Replace("/", Path.DirectorySeparatorChar.ToString());
            var files = Directory.GetFiles(osDataPath, "*.cs", SearchOption.AllDirectories);
            
            foreach (var file in files)
                PreChangesState[file] = PostChangesState[file] = CalculateFileHash(file);
        }
        
        private static string CalculateFileHash(string filePath)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            
            var hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        #endregion

        #region GUI

        private const float INDICATOR_SIZE = 18;  
        
        private enum IndicatorState
        {
            None,
            NoChangesPending,
            ChangesPending,
            Compiling
        }

        private static IndicatorState indicatorState;

        private static Texture2D indicatorTexture;

        private static readonly Dictionary<IndicatorState, Color> IndicatorStateColors = new()
        {
            { IndicatorState.NoChangesPending, Color.green },
            { IndicatorState.ChangesPending, Color.red },
            { IndicatorState.Compiling, Color.yellow }
        };

        private static void InitializeGUI()
        {
            ToolbarExtender.LeftToolbarGUI.Add(OnIndicatorGUI);
            
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            // Texture becomes null when changing scenes in the editor
            if (indicatorTexture == null)
            {
                indicatorTexture = new Texture2D(1, 1);
                indicatorState = IndicatorState.None;
            }
            
            var newIndicatorState = GetIndicatorState();
            if (newIndicatorState != indicatorState)
            {                
                indicatorState = newIndicatorState;

                var indicatorColor = IndicatorStateColors[indicatorState];
                indicatorTexture.SetPixel(0, 0, indicatorColor);
                indicatorTexture.Apply();
                
                ToolbarExtender.Repaint();
            }
        }

        private static void OnIndicatorGUI()
        {
            if (indicatorTexture != null)
            {
                GUILayout.FlexibleSpace();
                var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(INDICATOR_SIZE), GUILayout.Height(INDICATOR_SIZE));
                GUI.DrawTexture(rect, indicatorTexture);
            }
        }

        private static IndicatorState GetIndicatorState()
        {
            if (EditorApplication.isCompiling)
                return IndicatorState.Compiling;

            if (HasPendingChanges)
                return IndicatorState.ChangesPending;

            return IndicatorState.NoChangesPending;
        }

        #endregion
    }
}