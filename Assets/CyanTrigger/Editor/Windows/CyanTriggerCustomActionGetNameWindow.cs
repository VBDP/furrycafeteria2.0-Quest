using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Cyan.CT.Editor
{
    public class CyanTriggerCustomActionGetNameWindow : EditorWindow
    {
        private const string DefaultActionNamespace = "UserCustom";
        private const string DefaultActionName = "MyAction";
        
        private CyanTriggerDataInstance _dataInstance;
        private string _path;
        private int _eventIndex;
        private List<int> _selectedActions;
        
        // Filled by user
        public string actionNamespace = DefaultActionNamespace;
        public string actionName = DefaultActionName;
        public string comment;
        
        public static void RequestCustomActionNamespace(
            CyanTriggerDataInstance dataInstance, 
            Object saveReference,
            int eventIndex,
            List<int> selectedActions)
        {
            string path = "";
            if (saveReference is CyanTrigger ct)
            {
                path = ct.gameObject.scene.path;
            }
            else if (saveReference is CyanTriggerProgramAsset cta)
            {
                path = AssetDatabase.GetAssetPath(cta);
            }
            
            if (string.IsNullOrEmpty(path))
            {
                // TODO better error
                Debug.LogError("Invalid save path!");
                return;
            }

            // ReSharper disable once PossibleNullReferenceException
            string fullPath = new FileInfo(path).Directory.FullName;
            // ReSharper disable once PossibleNullReferenceException
            string projectPath = new DirectoryInfo(Application.dataPath).Parent.FullName;
            
            var window = GetWindow<CyanTriggerCustomActionGetNameWindow>(utility: true, title: "Create Custom Action", focus: true);
            window._dataInstance = dataInstance;
            window._eventIndex = eventIndex;
            window._selectedActions = selectedActions;
            window._path = fullPath.Substring(projectPath.Length+1);
            window.comment = dataInstance.events[eventIndex].eventInstance.comment.comment;

            window.minSize = window.maxSize = new Vector2(320, 210);
            
            window.ShowModalUtility();
        }
        
        private void CreateCustomAction()
        {
            CyanTriggerProgramAssetExporter.CreateCustomActionFromData(
                _dataInstance, _path, actionNamespace, actionName, comment, _eventIndex, _selectedActions);
        }

        private void OnGUI()
        {
            float windowPadding = 6;
            EditorGUILayout.Space(windowPadding);
            EditorGUILayout.BeginHorizontal();
            // Padding
            EditorGUILayout.Space(windowPadding);
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.LabelField(new GUIContent("Please input a name for your new Custom Action:"), EditorStyles.boldLabel);

            actionNamespace = EditorGUILayout.TextField(new GUIContent("Action Namespace", "The namespace is used for grouping different actions when searching."), actionNamespace);
            actionName = EditorGUILayout.TextField(new GUIContent("Action Name", "The name of the action."), actionName);
            comment = EditorGUILayout.TextField(new GUIContent("Description", "Optional short description of what this Custom Action does."), comment);

            actionNamespace = CyanTriggerNameHelpers.SanitizeName(actionNamespace);
            actionName = CyanTriggerNameHelpers.SanitizeName(actionName);
            
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

            bool hasComment = !string.IsNullOrWhiteSpace(comment);
            Rect rect = EditorGUILayout.BeginVertical();
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight * 3);

            var boxStyle = new GUIStyle();
            boxStyle.normal.background = CyanTriggerImageResources.BackgroundColorBox;
            GUI.Box(new Rect(rect.x, rect.y, rect.width, rect.height), GUIContent.none, boxStyle);

            if (Event.current.rawType == EventType.Repaint)
            {
                float padding = EditorGUIUtility.singleLineHeight;
                Rect labelRect = new Rect(rect.x + padding, rect.y + EditorGUIUtility.singleLineHeight,
                    rect.width - padding * 2, EditorGUIUtility.singleLineHeight);

                if (hasComment)
                {
                    labelRect.y -= EditorGUIUtility.singleLineHeight / 2;
                    Rect commentRect = new Rect(labelRect);
                    labelRect.y += EditorGUIUtility.singleLineHeight;
                    string commentText = $"// {comment}".Colorize(CyanTriggerColorTheme.Comment, true);
                    CyanTriggerEditorGUIUtil.CommentStyle.Draw(commentRect, commentText, false, false, true, false);
                }
                
                bool withColor = CyanTriggerSettings.Instance.useColorThemes;
                string displayNameColor = $"{actionNamespace}*".Colorize(CyanTriggerColorTheme.CustomActionName, withColor);
                string period = ".".Colorize(CyanTriggerColorTheme.Punctuation, withColor);
                string actionNameColor = actionName.Colorize(CyanTriggerColorTheme.ActionName, withColor);
                string label = $"{displayNameColor}{period}{actionNameColor}";
                
                CyanTriggerEditorGUIUtil.TreeViewLabelStyle.Draw(labelRect, label, false, false, true, true);
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Create", GUILayout.Width(100)))
            {
                // Catch errors to prevent them from blocking closing of the window
                // Close after to ensure that file creation still happens before user can interact with unity again.
                try
                {
                    CreateCustomAction();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                Close();
            }
            
            if (GUILayout.Button("Cancel", GUILayout.Width(100)))
            {
                Close();
            }
            
            if (GUILayout.Button("Wiki", GUILayout.Width(100)))
            {
                Application.OpenURL(CyanTriggerDocumentationLinks.CustomAction);
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(windowPadding);
            EditorGUILayout.EndHorizontal();
        }
    }
}