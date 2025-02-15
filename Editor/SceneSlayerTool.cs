using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ScenesListTool.Editor
{
    public class SceneSlayerTool : EditorWindow
    {
        private Vector2 scrollPosition;
        private Dictionary<string, bool> foldouts = new Dictionary<string, bool>();

        [MenuItem("Tools/SceneSlayer")]
        static void Init()
        {
            var window = (SceneSlayerTool)EditorWindow.GetWindow(typeof(SceneSlayerTool));
            
            // string[] res = Directory.GetFiles(Application.dataPath, "SceneSlayerTool.cs", SearchOption.AllDirectories);
            // if (res.Length == 0)
            // {
            //     Debug.LogError("error message ....");
            // }
            // string path = res[0].Replace("SceneSlayerTool.cs", "").Replace("\\", "/");
            //var icon = (Texture2D)AssetDatabase.LoadAssetAtPath($"{path}SceneSlayerIcon.png", typeof(Texture2D));
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Packages/com.dreadzitodev.sceneslayertoolpackage/Editor/SceneSlayerIcon.png"
            );
            window.titleContent = new GUIContent("SceneSlayer", icon);
            window.Show();
        }

        void OnGUI()
        {
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 16,
                padding = new RectOffset(0, 0, 10, 10)
            };

            GUILayout.Label("Scene Slayer", titleStyle);
            Rect lineRect = EditorGUILayout.GetControlRect(false, 2);
            DrawLine(lineRect);

            GUILayout.Space(10);

            var scenes = EditorBuildSettings.scenes;
            var sceneCategories = BuildSceneCategories(scenes);

            // Scroll view
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Draw categories
            foreach (var category in sceneCategories)
            {
                if (!foldouts.ContainsKey(category.Name))
                    foldouts[category.Name] = true;

                foldouts[category.Name] = EditorGUILayout.Foldout(foldouts[category.Name], category.Name, true);

                if (foldouts[category.Name])
                {
                    foreach (var subFolder in category.RootFolder.SubFolders)
                    {
                        DrawHierarchy(subFolder);
                    }

                    foreach (var scene in category.RootFolder.Scenes)
                    {
                        DrawScene(scene, 1);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private static void SelectAsset(string scenesPath)
        {
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(scenesPath, typeof(UnityEngine.Object));

            Selection.activeObject = obj;

            EditorGUIUtility.PingObject(obj);
        }

        /// <summary>
        /// Builds the categories based on the main subfolders inside "Scenes".
        /// </summary>
        private List<SceneCategory> BuildSceneCategories(EditorBuildSettingsScene[] scenes)
        {
            var categories = new Dictionary<string, SceneCategory>();

            foreach (var scene in scenes)
            {
                // Split the path into folders starting from "Scenes"
                var pathParts = GetRelativePathParts(scene.path);
                if (pathParts == null || pathParts.Length == 0)
                    continue;

                // Get the category (first level after "Scenes")
                var categoryName = pathParts[0];
                if (pathParts.Length == 1) // If it's directly under "Scenes"
                    categoryName = "Root";

                // Search for or create the category
                if (!categories.ContainsKey(categoryName))
                {
                    categories[categoryName] = new SceneCategory(categoryName);
                }

                // Build the folder hierarchy inside the category
                var currentCategory = categories[categoryName];
                currentCategory.AddScene(scene, pathParts.Skip(1).ToArray());
            }

            return categories.Values.ToList();
        }

        /// <summary>
        /// Recursively draws the folder and scene hierarchy.
        /// </summary>
        private void DrawHierarchy(FolderNode folder, int indent = 0)
        {
            // Draw the Foldout for this folder
            GUILayout.BeginHorizontal();
            GUILayout.Space((indent + 1) * 15); // Indentation for the hierarchy

            if (!foldouts.ContainsKey(folder.FullPath))
                foldouts[folder.FullPath] = true;

            foldouts[folder.FullPath] = EditorGUILayout.Foldout(foldouts[folder.FullPath], folder.Name, true);
            GUILayout.EndHorizontal();

            // If the Foldout is expanded
            if (foldouts[folder.FullPath])
            {
                // Draw subfolders
                foreach (var subFolder in folder.SubFolders)
                {
                    DrawHierarchy(subFolder, indent + 1); // Recursive call with greater indentation
                }

                // Draw scenes
                foreach (var scene in folder.Scenes)
                {
                    DrawScene(scene, indent + 2);
                }
            }
        }

        /// <summary>
        /// Draws a button for a scene with indentation.
        /// </summary>
        private void DrawScene(SceneInfo scene, int indent)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space((indent) * (20)); // Indentation for the scene

            var projectIcon = EditorGUIUtility.IconContent("Project");

            // Button to select the asset
            if (GUILayout.Button(projectIcon, GUILayout.Width(30)))
            {
                SelectAsset(scene.Path);
            }

            var guiButtonSkin = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                fixedWidth = 25
            };
            if (GUILayout.Button("+", guiButtonSkin))
            {
                EditorSceneManager.OpenScene(scene.Path, OpenSceneMode.Additive);
            }

            // Button to load the scene
            if (GUILayout.Button(scene.Name))
            {
                // First check if we need to save any modified scenes
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(scene.Path);
                }
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Gets the relative path parts starting from the "Scenes" folder.
        /// </summary>
        private string[] GetRelativePathParts(string fullPath)
        {
            var parts = fullPath.Split('/');
            var scenesIndex = Array.IndexOf(parts, "Scenes");
            if (scenesIndex == -1 || scenesIndex + 1 >= parts.Length)
                parts = new[] { parts.Last() };
            
            var relativePathParts = parts.Skip(scenesIndex + 1).ToArray();
            
            if (relativePathParts.Length <= 1)
                return new []{"Root", relativePathParts[0]};
            
            return relativePathParts; // Relative parts from "Scenes"
        }

        private void DrawLine(Rect rect, float marginPercent = .1f)
        {
            Handles.BeginGUI();
            Handles.color = Color.gray;
            var margin = (marginPercent * rect.xMax);
            Handles.DrawLine(new Vector3(rect.xMin + margin, rect.yMin), new Vector3(rect.xMax - margin, rect.yMin));
            Handles.EndGUI();
        }

        /// <summary>
        /// Node that represents a scene category.
        /// </summary>
        class SceneCategory
        {
            public string Name;
            public FolderNode RootFolder;

            public SceneCategory(string name)
            {
                Name = name;
                RootFolder = new FolderNode(name);
            }

            public void AddScene(EditorBuildSettingsScene scene, string[] relativePathParts)
            {
                var currentNode = RootFolder;

                // Build the nodes of the hierarchy for the folders
                for (int i = 0; i < relativePathParts.Length; i++)
                {
                    var part = relativePathParts[i];

                    if (i == relativePathParts.Length - 1) // If it's the last part (scene)
                    {
                        currentNode.Scenes.Add(new SceneInfo
                        {
                            Path = scene.path,
                            Name = System.IO.Path.GetFileNameWithoutExtension(scene.path)
                        });
                    }
                    else // If it's a folder
                    {
                        // Search for or create a node for this folder
                        var childNode = currentNode.SubFolders.FirstOrDefault(n => n.Name == part);
                        if (childNode == null)
                        {
                            childNode = new FolderNode(part, currentNode.FullPath);
                            currentNode.SubFolders.Add(childNode);
                        }
                        currentNode = childNode;
                    }
                }
            }
        }

        /// <summary>
        /// Node that represents a folder in the hierarchy.
        /// </summary>
        class FolderNode
        {
            public string Name;
            public string FullPath;
            public List<FolderNode> SubFolders = new List<FolderNode>();
            public List<SceneInfo> Scenes = new List<SceneInfo>();

            public FolderNode(string name, string parentPath = "")
            {
                Name = name;
                FullPath = string.IsNullOrEmpty(parentPath) ? name : $"{parentPath}/{name}";
            }
        }

        class SceneInfo
        {
            public string Name;
            public string Path;
        }
    }
}
