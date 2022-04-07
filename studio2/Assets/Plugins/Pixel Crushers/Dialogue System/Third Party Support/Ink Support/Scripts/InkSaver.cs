using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.InkSupport
{

    /// <summary>
    /// Saver script for the Dialogue System's Ink integration. Replaces the
    /// DialogueSystemInkIntegration.includeInSaveData checkbox.
    /// 
    /// Note: If conversation(s) are active, currently saves only the most
    /// recently-started active conversation, to preserve the format of
    /// old saved games.
    /// </summary>
    [AddComponentMenu("Pixel Crushers/Dialogue System/Third Party Support/Ink/Ink Saver")]
    public class InkSaver : Saver
    {
        // This two variables let the ApplyData method know the reason for applying data so it can decide how to handle it.
        private bool isLoadingGame = false;
        private bool isChangingScene = false;

        public override void OnEnable()
        {
            base.OnEnable();
            SaveSystem.loadStarted += OnLoadStarted;
        }

        public override void OnDisable()
        {
            base.OnDisable();
            SaveSystem.loadStarted -= OnLoadStarted;
        }

        protected void OnLoadStarted()
        {
            isLoadingGame = true;
        }

        public override void OnBeforeSceneChange()
        {
            base.OnBeforeSceneChange();
            if (!isLoadingGame) isChangingScene = true;
        }

        public override string RecordData()
        {
            var data = new InkSaverData();

            data.current = string.Empty;
            data.jsonDict = new Dictionary<string, string>();
            if (DialogueSystemInkIntegration.instance != null)
            {
                data.wasPlayerSpeaking = DialogueSystemInkIntegration.instance.isPlayerSpeaking;
                for (int i = 0; i < DialogueSystemInkIntegration.instance.inkJSONAssets.Count; i++)
                {
                    if (DialogueSystemInkIntegration.instance.inkJSONAssets[i] == null) continue;
                    var storyName = DialogueSystemInkIntegration.instance.inkJSONAssets[i].name;
                    var json = DialogueSystemInkIntegration.instance.stories[i].state.ToJson();
                    data.jsonDict.Add(storyName, json);
                    if (DialogueManager.isConversationActive && string.Equals(storyName, DialogueManager.lastConversationStarted))
                    {
                        data.current = storyName;
                    }
                }
            }
            if (DialogueDebug.logInfo) Debug.Log("Dialogue System: Recording that active story is '" + data.current + "' (isPlayerSpeaking=" + data.wasPlayerSpeaking+ ").");
            return SaveSystem.Serialize(data);
        }

        public override void ApplyData(string s)
        {
            isLoadingGame = false;
            if (isChangingScene)
            {
                isChangingScene = false; // No need to stop and restart current conversation if changing scene; just let it keep running.
                return;
            }
            if (string.IsNullOrEmpty(s)) return;
            var data = SaveSystem.Deserialize<InkSaverData>(s);
            if (data == null) return;

            DialogueSystemInkIntegration.instance.ResetStories();
            for (int i = 0; i < DialogueSystemInkIntegration.instance.inkJSONAssets.Count; i++)
            {
                if (DialogueSystemInkIntegration.instance.inkJSONAssets[i] == null) continue;
                var storyName = DialogueSystemInkIntegration.instance.inkJSONAssets[i].name;
                string json;
                if (data.jsonDict.TryGetValue(storyName, out json))
                { 
                    if (DialogueDebug.logInfo) Debug.Log("Dialogue System: Restoring story '" + storyName + "' state: " + json);
                    DialogueSystemInkIntegration.instance.stories[i].state.LoadJson(json);
                }
            }
            DialogueManager.StopConversation();
            if (!string.IsNullOrEmpty(data.current))
            {
                DialogueSystemInkIntegration.instance.isResuming = true;
                DialogueSystemInkIntegration.instance.isPlayerSpeaking = data.wasPlayerSpeaking;
                if (DialogueDebug.logInfo) Debug.Log("Dialogue System: Resuming story '" + data.current + "' (wasPlayerSpeaking=" + data.wasPlayerSpeaking + ").");
                DialogueManager.StartConversation(data.current);
            }
        }

        public override void OnRestartGame()
        {
            DialogueSystemInkIntegration.instance.ResetStories();
        }
    }

    [Serializable]
    public class InkSaverData : ISerializationCallbackReceiver
    {
        public string current;
        public bool wasPlayerSpeaking;
        public Dictionary<string, string> jsonDict;

        [SerializeField] private List<string> storyNames;
        [SerializeField] private List<string> storyJsons;

        public void OnAfterDeserialize()
        {
            jsonDict = new Dictionary<string, string>();
            if (storyNames != null && storyJsons != null)
            {
                for (int i = 0; i < Mathf.Min(storyNames.Count, storyJsons.Count); i++)
                {
                    jsonDict.Add(storyNames[i], storyJsons[i]);
                }
            }
        }

        public void OnBeforeSerialize()
        {
            storyNames = new List<string>();
            storyJsons = new List<string>();
            if (jsonDict != null)
            {
                foreach (var kvp in jsonDict)
                {
                    storyNames.Add(kvp.Key);
                    storyJsons.Add(kvp.Value);
                }
            }
        }
    }

}
