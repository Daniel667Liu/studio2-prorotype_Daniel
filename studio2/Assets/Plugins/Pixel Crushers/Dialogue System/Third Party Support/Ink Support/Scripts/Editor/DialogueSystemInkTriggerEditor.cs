using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem.InkSupport
{

    [CustomEditor(typeof(DialogueSystemInkTrigger), true)]
    public class DialogueSystemInkTriggerEditor : DialogueSystemTriggerEditor
    {
        private List<InkEntrypoint> entrypoints;
        private string[] entrypointStrings;
        private string[] entrypointFullPaths;

        public override void OnEnable()
        {
            base.OnEnable();
            List<string> fullPaths;
            entrypoints = InkEditorUtility.GetAllEntrypoints(out fullPaths);
            entrypointStrings = InkEditorUtility.EntrypointsToStrings(entrypoints);
            entrypointFullPaths = fullPaths.ToArray();
        }

        protected void SetEntrypoint(SerializedProperty conversationProperty, SerializedProperty startAtKnotProperty, int index)
        {
            if (!(0 <= index && index < entrypoints.Count)) return;
            var entrypoint = entrypoints[index];
            conversationProperty.stringValue = entrypoint.story;
            if (string.IsNullOrEmpty(entrypoint.knot))
            {
                startAtKnotProperty.stringValue = string.Empty;
            }
            else
            {
                if (string.IsNullOrEmpty(entrypoint.stitch))
                {
                    startAtKnotProperty.stringValue = entrypoint.knot;
                }
                else
                {
                    startAtKnotProperty.stringValue = (entrypoints[index].knot + "." + entrypoints[index].stitch);
                }
            }
        }

        protected override void DrawConversationAction()
        {
            base.DrawConversationAction();
            if (foldouts.conversationFoldout)
            {
                EditorGUILayout.LabelField("Ink-Specific", EditorStyles.boldLabel);
                var conversationProperty = serializedObject.FindProperty("conversation");
                var startConversationAtKnotProperty = serializedObject.FindProperty("startConversationAtKnot");
                EditorGUILayout.PropertyField(startConversationAtKnotProperty, new GUIContent("Start At Knot/Stitch"), true);
                var startConversationFullPathProperty = serializedObject.FindProperty("startConversationFullPath");
                EditorGUILayout.PropertyField(startConversationFullPathProperty, new GUIContent("Start At Full Path"), true);
                var index = InkEditorUtility.GetEntrypointIndex(conversationProperty.stringValue, startConversationAtKnotProperty.stringValue, entrypoints);
                EditorGUI.BeginChangeCheck();
#if INK_FULLPATHS
                index = EditorGUILayout.Popup("Entrypoint Picker", index, entrypointFullPaths);
#else
                index = EditorGUILayout.Popup("Entrypoint Picker", index, entrypointStrings);
#endif
                if (EditorGUI.EndChangeCheck())
                {
                    SetEntrypoint(conversationProperty, startConversationAtKnotProperty, index);
                }
            }
        }

        protected override void DrawBarkAction()
        {
            base.DrawBarkAction();
            if (foldouts.barkFoldout)
            {
                EditorGUILayout.LabelField("Ink-Specific", EditorStyles.boldLabel);
                var barkConversationProperty = serializedObject.FindProperty("barkConversation");
                var barkKnotProperty = serializedObject.FindProperty("barkKnot");
                EditorGUILayout.PropertyField(barkKnotProperty, true);
                var index = InkEditorUtility.GetEntrypointIndex(barkConversationProperty.stringValue, barkKnotProperty.stringValue, entrypoints);
                EditorGUI.BeginChangeCheck();
#if INK_FULLPATHS
                index = EditorGUILayout.Popup("Entrypoint Picker", index, entrypointFullPaths);
#else
                index = EditorGUILayout.Popup("Entrypoint Picker", index, entrypointStrings);
#endif
                if (EditorGUI.EndChangeCheck())
                {
                    SetEntrypoint(barkConversationProperty, barkKnotProperty, index);
                }
            }
        }


    }
}
