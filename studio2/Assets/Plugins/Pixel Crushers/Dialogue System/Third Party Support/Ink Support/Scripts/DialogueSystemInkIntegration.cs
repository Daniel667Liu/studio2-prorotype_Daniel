using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;
using Ink.Runtime;
using System.Collections;

namespace PixelCrushers.DialogueSystem.InkSupport
{

    /// <summary>
    /// Integrates Ink with the Dialogue System. In this integration, Ink does the
    /// processing, and the Dialogue System does the UI and handles triggers. It
    /// also handles saving/loading and exposes functions to manage quests and
    /// show alerts.
    /// </summary>
    [AddComponentMenu("Pixel Crushers/Dialogue System/Third Party Support/Ink/Dialogue System Ink Integration")]
    public class DialogueSystemInkIntegration : MonoBehaviour
    {

        #region Variables

        [Tooltip("All Ink stories.")]
        public List<TextAsset> inkJSONAssets = new List<TextAsset>();

        [Tooltip("Reset story state when conversation starts.")]
        public bool resetStateOnConversationStart = false;

        [Tooltip("In your Ink files, actor names precede text, as in 'Monsieur Fogg: A wager.' Integration should extract actor names from text.")]
        public bool actorNamesPrecedeLines = false;

        [Tooltip("Trim whitespace from the beginning and end of lines.")]
        public bool trimText = true;

        [Tooltip("Append a line feed at the end of player response subtitles.")]
        public bool appendNewlineToPlayerResponses = false;

        [Tooltip("When player chooses response from menu, pull next line from story to show as response's subtitle.")]
        public bool playerDialogueTextFollowsResponseText = false;

        [Tooltip("Set Dialogue Manager's Input Settings > Always Force Response Menu to false.")]
        public bool disableAlwaysForceResponseMenu = true;

        [Tooltip("Force response menu for for single choices (but not player subtitle lines).")]
        public bool forceResponseMenuForSingleChoices = false;

        [Tooltip("When a line in Ink has a {Sequence()} function, tie it to the timing of the subtitle.")]
        public bool tieSequencesToDialogueEntries = true;

        [Tooltip("Log detailed activity.")]
        public bool debug = false;

        // All loaded stories:
        public List<Story> stories { get; set; }

        public DialogueDatabase database { get; protected set; }

        // Template used to create new Dialogue System assets on the fly:
        protected Template template;

        protected const int PlayerActorID = 1;
        protected const int StoryActorID = 2; // e.g. NPC

        protected Conversation lastInkConversation = null;
        protected int nextStoryConversationID = 10000;
        [HideInInspector] public bool isResuming = false; // Temp variable to know if we're resuming from a saved game.
        [HideInInspector] public bool isPlayerSpeaking = false; // Temp variable to remember if next line is the expanded player choice text.
        protected string jumpToKnot = string.Empty; // If set, jump to this knot.
        protected string sequenceToPlayWithSubtitle = string.Empty; // May be set by {Sequence} function.
        protected bool originalAlwaysForceResponseMenu;

        protected static DialogueSystemInkIntegration m_instance = null;
        protected static bool m_registeredLuaFunctions = false;

        public static DialogueSystemInkIntegration instance { get { return m_instance; } }

        /// <summary>
        /// Knot that conversation started at, or blank if started at the beginning of the story.
        /// </summary>
        public static string lastStartingPoint { get; set; }

        protected Dictionary<int, Story> storyDict = new Dictionary<int, Story>(); // <conversationID, story>
        protected Dictionary<int, int> storyPlayerIDDict = new Dictionary<int, int>(); // <conversationID, playerActorID>
        protected Dictionary<int, int> storyActorIDDict = new Dictionary<int, int>(); // <conversationID, storyActorID>

        /// <summary>
        /// Invoked after the integration has loaded and initialized all stories.
        /// </summary>
        public event System.Action loadedStories = delegate { };

        /// <summary>
        /// Last choice that player made.
        /// </summary>
        protected string lastPlayerChoice;

        #endregion

        #region Initialization

#if UNITY_2019_2_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            m_instance = null;
            m_registeredLuaFunctions = false;
        }
#endif

        protected virtual void Awake()
        {
            if (m_instance != null)
            {
                Destroy(this);
                return;
            }
            m_instance = this;
            database = null;
            RegisterLuaFunctions();
        }

        protected virtual void Start()
        {
            CreateDatabase();

            // Observe Ink variables:
            foreach (var story in stories)
            {
                ObserveStoryVariables(story);
            }

            loadedStories();
        }

        protected virtual void CreateDatabase()
        {
            // Create database that will hold the stories:
            database = ScriptableObject.CreateInstance<DialogueDatabase>();
            database.name = "Ink";
            template = Template.FromDefault();
            database.ResetEmphasisSettings();

            // Copy info from Dialogue Manager's Initial Database:
            if (DialogueManager.instance.initialDatabase != null)
            {
                database.emphasisSettings = DialogueManager.instance.initialDatabase.emphasisSettings;
            }

            // Default actors (in case initial database doesn't define these):            
            var playerActor = CreateActor(template, PlayerActorID, "Player", true);
            database.actors.Add(playerActor);
            var npcActor = CreateActor(template, StoryActorID, "NPC", false);
            database.actors.Add(npcActor);

            // Load each story from JSON and add to database as a stub conversation:
            stories = new List<Story>();
            storyDict = new Dictionary<int, Story>();
            for (int i = 0; i < inkJSONAssets.Count; i++)
            {
                AddStoryToDatabase(inkJSONAssets[i], nextStoryConversationID);
                nextStoryConversationID++;
            }

            // Add database:
            DialogueManager.AddDatabase(database);
        }

        protected virtual Actor CreateActor(Template template, int actorID, string actorName, bool isPlayer)
        {
            Actor actor;
            var existingPlayerActor = DialogueManager.masterDatabase.GetActor(PlayerActorID);
            if (existingPlayerActor != null)
            {
                actor = new Actor(existingPlayerActor);
                actor.fields = Field.CopyFields(existingPlayerActor.fields);
            }
            else
            {
                actor = template.CreateActor(actorID, actorName, isPlayer);
            }
            return actor;
        }

        protected virtual void AddStoryToDatabase(TextAsset asset, int conversationID)
        {
            if (debug || DialogueDebug.LogInfo) Debug.Log("Dialogue System: Loading Ink story " + asset.name);
            var story = new Story(asset.text);
            AddStoryToDatabase(asset.name, story, conversationID);
        }

        /// <summary>
        /// Adds a story at runtime.
        /// </summary>
        /// <param name="storyTitle">The story's title.</param>
        /// <param name="storyText">The story in JSON format.</param>
        public virtual void AddStory(string storyTitle, string storyJSON)
        {
            if (string.IsNullOrEmpty(storyJSON)) return;
            if (debug || DialogueDebug.LogInfo) Debug.Log("Dialogue System: Loading Ink story " + storyTitle);
            var story = new Story(storyJSON);
            AddStoryToDatabase(storyTitle, story, nextStoryConversationID);
            nextStoryConversationID++;
        }

        protected virtual void AddStoryToDatabase(string storyTitle, Story story, int conversationID)
        {
            stories.Add(story);
            storyDict.Add(conversationID, story);

            if (debug) Debug.Log($"Create conversation [{conversationID}] {storyTitle}");

            // Fake conversation for Ink:
            var inkConversation = template.CreateConversation(conversationID, storyTitle);
            inkConversation.ActorID = PlayerActorID;
            inkConversation.ConversantID = StoryActorID;

            // Note: We used to set conversation overrides to disable Always Force Response Menu.
            // We now temporarily set DialogueManager.displaySettings.inputSettings.alwaysForceResponseMenu
            // so we don't clobber other Dialogue Manager > Input Settings.

            // Start entry:
            var startEntry = template.CreateDialogueEntry(0, conversationID, "START");
            startEntry.ActorID = PlayerActorID;
            startEntry.ConversantID = StoryActorID;
            startEntry.Sequence = "None()";
            inkConversation.dialogueEntries.Add(startEntry);

            // Fake entry:
            var inkStoryEntry = template.CreateDialogueEntry(1, conversationID, storyTitle);
            inkStoryEntry.ActorID = StoryActorID;
            inkStoryEntry.ConversantID = PlayerActorID;
            inkConversation.dialogueEntries.Add(inkStoryEntry);
            // Start --> fake entry:
            startEntry.outgoingLinks.Add(new Link(conversationID, 0, conversationID, 1));

            // Add fake conversation to database:
            database.conversations.Add(inkConversation);

            // Add story variables to database:
            var variables = story.variablesState;
            int variableID = 1;
            foreach (var variableName in variables)
            {
                if (database.GetVariable(variableName) != null) continue;
                var variable = template.CreateVariable(variableID++, variableName, string.Empty);
                SetVariableValue(variable, variables[variableName]);
                database.variables.Add(variable);
            }

            // Register external functions:
            BindExternalFunctions(story);
        }

        public virtual void ResetStories()
        {
            stories.ForEach(story => story.ResetState());
        }

        #endregion

        #region Variables

        protected virtual void SetVariableValue(Variable variable, object value)
        {
            var initialValue = Field.Lookup(variable.fields, "Initial Value");
            if (initialValue == null) return;
            initialValue.value = (value != null) ? value.ToString() : string.Empty;
            initialValue.type = GetFieldType(value);
        }

        protected FieldType GetFieldType(object value)
        {
            if (value == null) return FieldType.Text;
            var type = value.GetType();
            if (type == typeof(bool)) return FieldType.Boolean;
            if (type == typeof(int) || type == typeof(float) || type == typeof(double)) return FieldType.Number;
            return FieldType.Text;
        }

        protected virtual void ObserveStoryVariables(Story story)
        {
            foreach (var variableName in story.variablesState)
            {
                story.ObserveVariable(variableName, OnVariableChange);
            }
        }

        protected virtual void OnVariableChange(string variableName, object newValue)
        {
            if (debug || DialogueDebug.LogInfo) Debug.Log("Dialogue System: Ink variable '" + variableName + "' changed to " + newValue);
            DialogueLua.SetVariable(variableName, newValue);
        }

        #endregion

        #region Handle active conversation

        public static void SetConversationStartingPoint(string knot)
        {
            if (m_instance != null)
            {
                m_instance.jumpToKnot = knot;
                lastStartingPoint = knot;
            }
        }

        public static Story LookupStory(string storyName)
        {
            if (instance == null) return null;
            for (int i = 0; i < instance.inkJSONAssets.Count; i++)
            {
                if (string.Equals(instance.inkJSONAssets[i].name, storyName))
                {
                    return instance.stories[i];
                }
            }
            return null;
        }

        protected virtual void OnConversationStart(Transform actorTransform)
        {
            for (int i = 0; i < inkJSONAssets.Count; i++)
            {
                if (string.Equals(inkJSONAssets[i].name, DialogueManager.lastConversationStarted))
                {
                    var activeStory = stories[i];
                    if (resetStateOnConversationStart) activeStory.ResetState();

                    // Record story's current player ID and NPC ID:
                    var actor = DialogueManager.masterDatabase.GetActor(DialogueActor.GetActorName(actorTransform));
                    var currentStoryPlayerID = (actor != null) ? actor.id : PlayerActorID;
                    var conversant = DialogueManager.masterDatabase.GetActor(DialogueActor.GetActorName(DialogueManager.currentConversant));
                    var currentStoryActorID = (conversant != null) ? conversant.id : StoryActorID;
                    storyPlayerIDDict[DialogueManager.lastConversationID] = currentStoryPlayerID;
                    storyActorIDDict[DialogueManager.lastConversationID] = currentStoryActorID;

                    if (disableAlwaysForceResponseMenu)
                    {
                        originalAlwaysForceResponseMenu = DialogueManager.displaySettings.inputSettings.alwaysForceResponseMenu;
                        DialogueManager.displaySettings.inputSettings.alwaysForceResponseMenu = false;
                    }

                    if (debug) Debug.Log($"[{Time.frameCount}] Start [{DialogueManager.lastConversationID}] {DialogueManager.lastConversationStarted} with actorID={currentStoryPlayerID}/conversantID={currentStoryActorID}");

                    return;
                }
            }
        }

        protected virtual Story GetCurrentStory(int conversationID)
        {
            Story story;
            return storyDict.TryGetValue(conversationID, out story) ? story : null;
        }

        protected virtual int GetCurrentPlayerID(int conversationID)
        {
            int actorID;
            return storyPlayerIDDict.TryGetValue(conversationID, out actorID) ? actorID : PlayerActorID;
        }

        protected virtual int GetCurrentActorID(int conversationID)
        {
            int actorID;
            return storyActorIDDict.TryGetValue(conversationID, out actorID) ? actorID : StoryActorID;
        }

        protected virtual void OnConversationEnd(Transform actor)
        {
            if (disableAlwaysForceResponseMenu)
            {
                DialogueManager.displaySettings.inputSettings.alwaysForceResponseMenu = originalAlwaysForceResponseMenu;
            }

            // Reset entry 1:
            var inkConversation = DialogueManager.masterDatabase.GetConversation(DialogueManager.lastConversationStarted);
            var entry = inkConversation.dialogueEntries.Find(x => x.id == 1);
            if (entry != null)
            {
                entry.Title = string.Empty;
                entry.ActorID = StoryActorID;
                entry.outgoingLinks.Clear();
            }
            inkConversation.dialogueEntries.RemoveAll(x => x.id >= 2);
        }

        protected virtual void OnPrepareConversationLine(DialogueEntry entry)
        {
            if (entry.id == 0 || !storyDict.ContainsKey(entry.conversationID))
            {
                // START entry or not a fake Ink conversation: Do nothing special.
                return;
            }
            var activeStory = storyDict[entry.conversationID];
            var inkConversation = (lastInkConversation != null && lastInkConversation.id == entry.conversationID) ? lastInkConversation
                : DialogueManager.masterDatabase.GetConversation(entry.conversationID);
            lastInkConversation = inkConversation;

            if (debug) Debug.Log($"[{Time.frameCount}] PrepareLine [{entry.conversationID}:{entry.id}]");

            //-----------------------------------------------------------------------------------------------------------------------------
            // If entry id 1, continue Ink story and show next Ink content as subtitle (or player menu if always force player menu):
            if (entry.id == 1)
            {
                // If jump is specified, jump there:
                if (!string.IsNullOrEmpty(jumpToKnot))
                {
                    activeStory.ChoosePathString(jumpToKnot);
                    jumpToKnot = string.Empty;
                }

                entry.outgoingLinks.Clear();
                inkConversation.dialogueEntries.RemoveAll(x => x.id >= 2);

                // If we can't continue, show choices or stop conversation: (early exit)
                if (!activeStory.canContinue)
                {
                    if (activeStory.currentChoices.Count > 0)
                    {
                        entry.DialogueText = activeStory.currentText;
                        AddResponses(activeStory, inkConversation, entry);
                    }
                    else
                    {
                        entry.DialogueText = string.Empty;
                        entry.Sequence = "Continue()";
                        return;
                    }
                }

                // Get next story text:
                var text = isResuming ? activeStory.currentText : activeStory.Continue();

                if (isPlayerSpeaking)
                {
                    if (!string.IsNullOrEmpty(lastPlayerChoice) && !text.StartsWith(lastPlayerChoice))
                    {
                        // Player line was silent, so this is the next line:
                        isPlayerSpeaking = false;
                    }
                }
                lastPlayerChoice = string.Empty;
                if (trimText) text = text.Trim();
                var currentStoryPlayerID = GetCurrentPlayerID(entry.conversationID);
                var currentStoryActorID = GetCurrentActorID(entry.conversationID);
                entry.ActorID = isPlayerSpeaking ? currentStoryPlayerID : currentStoryActorID;
                entry.ConversantID = isPlayerSpeaking ? currentStoryActorID : currentStoryPlayerID;
                if (isPlayerSpeaking && appendNewlineToPlayerResponses) text += "\n";
                isResuming = false;
                if (actorNamesPrecedeLines) TryExtractPrependedActor(ref text, entry);
                ProcessTags(activeStory, entry);
                entry.DialogueText = text;
                entry.Sequence = string.Empty;
                var isPlayerLine = entry.ActorID == PlayerActorID;
                var hasChoices = activeStory.currentChoices.Count > 0;
                var forceSingleChoiceMenu = !isPlayerSpeaking && !hasChoices && isPlayerLine && DialogueManager.displaySettings.inputSettings.alwaysForceResponseMenu;
                isPlayerSpeaking = false;

                // Prepare outgoing links:
                if (forceSingleChoiceMenu)
                {
                    AddForcedResponse(activeStory, inkConversation, entry);
                }
                else if (hasChoices)
                {
                    AddResponses(activeStory, inkConversation, entry);
                }
                else if (activeStory.canContinue)
                {
                    // Add loopback entry:
                    var loopEntry = template.CreateDialogueEntry(2, inkConversation.id, "Forced Choice");
                    loopEntry.ActorID = currentStoryActorID;
                    loopEntry.Sequence = "Continue()";
                    loopEntry.outgoingLinks.Add(new Link(inkConversation.id, 2, inkConversation.id, 1));
                    inkConversation.dialogueEntries.Add(loopEntry);
                    entry.outgoingLinks.Add(new Link(inkConversation.id, 1, inkConversation.id, 2));
                }
            }

            //-----------------------------------------------------------------------------------------------------------------------------
            // If looping back from forced choice, reset entry 1:
            else if (entry.Title == "Forced Choice")
            {
                if (debug) Debug.Log($"[{Time.frameCount}] Loopback from forced choice [{entry.conversationID}]");

                var entry1 = inkConversation.dialogueEntries.Find(x => x.id == 1);
                entry1.ActorID = StoryActorID; // Prevent menu.
            }

            //-----------------------------------------------------------------------------------------------------------------------------
            // Choice entry: Choose choice.
            else
            {
                var choiceIndex = Field.LookupInt(entry.fields, "Choice Index");

                if (debug) Debug.Log($"[{Time.frameCount}] ChooseChoice [{entry.conversationID}:{entry.id}] {choiceIndex}");

                if (!(0 <= choiceIndex && choiceIndex < activeStory.currentChoices.Count))
                {
                    Debug.LogWarning($"Dialogue System: Internal Ink integration error. Choice index is {choiceIndex} but story only has {activeStory.currentChoices.Count} choices.");
                }
                else
                {
                    activeStory.ChooseChoiceIndex(choiceIndex);
                }

                var entry1 = inkConversation.dialogueEntries.Find(x => x.id == 1);
                entry1.ActorID = StoryActorID; // Prevent menu.
                isPlayerSpeaking = true;
                lastPlayerChoice = entry.subtitleText;
            }
        }

        protected virtual void ProcessTags(Story activeStory, DialogueEntry entry)
        {
            foreach (var tag in activeStory.currentTags)
            {
                ProcessTag(tag, entry);
            }
        }

        protected virtual void ProcessTag(string tag, DialogueEntry entry)
        {
            if (tag.StartsWith("Actor=")) entry.ActorID = GetActorID(tag.Substring("Actor=".Length), entry.ActorID);
            else if (tag.StartsWith("Conversant=")) entry.ConversantID = GetActorID(tag.Substring("Conversant=".Length), entry.ConversantID);
        }

        protected virtual void AddForcedResponse(Story activeStory, Conversation inkConversation, DialogueEntry entry)
        {
            // Change this entry to blank and set up link to this text as menu choice:
            var menuEntry = template.CreateDialogueEntry(2, inkConversation.id, "Forced Choice");
            menuEntry.ActorID = entry.ActorID;
            menuEntry.ConversantID = entry.ConversantID;
            menuEntry.MenuText = entry.DialogueText;
            menuEntry.Sequence = entry.Sequence;
            if (DialogueManager.DisplaySettings.subtitleSettings.skipPCSubtitleAfterResponseMenu)
            {
                menuEntry.Sequence = "Continue(); " + menuEntry.Sequence;
            }
            menuEntry.outgoingLinks.Add(new Link(inkConversation.id, 2, inkConversation.id, 1));
            inkConversation.dialogueEntries.Add(menuEntry);

            // Clear this entry and link to menu entry:
            entry.DialogueText = string.Empty;
            entry.Sequence = "Continue()";
            entry.outgoingLinks.Add(new Link(inkConversation.id, 1, inkConversation.id, 2));
        }

        protected virtual void AddResponses(Story activeStory, Conversation inkConversation, DialogueEntry entry)
        {
            entry.outgoingLinks.Clear();
            inkConversation.dialogueEntries.RemoveAll(x => x.id >= 2);
            for (int i = 0; i < activeStory.currentChoices.Count; i++)
            {
                Choice choice = activeStory.currentChoices[i];
                var choiceText = choice.text;
                if (trimText) choiceText = choiceText.Trim();
                if (activeStory.currentChoices.Count == 1 && forceResponseMenuForSingleChoices)
                {
                    choiceText = "[f]" + choiceText;
                }
                var responseEntry = template.CreateDialogueEntry(2 + i, inkConversation.id, "Choice " + i);
                var currentStoryPlayerID = GetCurrentPlayerID(entry.conversationID);
                var currentStoryActorID = GetCurrentActorID(entry.conversationID);
                responseEntry.ActorID = currentStoryPlayerID;
                responseEntry.ConversantID = currentStoryActorID;
                if (actorNamesPrecedeLines) TryExtractPrependedActor(ref choiceText, responseEntry);
                responseEntry.MenuText = choiceText;
                responseEntry.Sequence = "Continue()"; // Will be shown in loopback to entry 1 (unless silent).
                responseEntry.DialogueText = string.Empty;
                Field.SetValue(responseEntry.fields, "Choice Index", i);
                responseEntry.outgoingLinks.Add(new Link(inkConversation.id, responseEntry.id, inkConversation.id, entry.id));
                inkConversation.dialogueEntries.Add(responseEntry);
                entry.outgoingLinks.Add(new Link(inkConversation.id, entry.id, inkConversation.id, responseEntry.id));
            }
        }

        // Extract actor ID from 'Actor: Text'.
        protected virtual void TryExtractPrependedActor(ref string text, DialogueEntry entry)
        {
            if (text != null && text.Contains(":"))
            {
                var colonPos = text.IndexOf(':');
                if (colonPos < text.Length - 1)
                {
                    var actorName = text.Substring(0, colonPos);
                    text = text.Substring(colonPos + 1).TrimStart();
                    var actor = DialogueManager.MasterDatabase.GetActor(actorName);
                    if (actor != null)
                    {
                        if (entry.ConversantID == actor.id)
                        { // If conversant points to new actor (speaker), point it to the other participant.
                            entry.ConversantID = entry.ActorID;
                        }

                        entry.ActorID = actor.id;
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to extract an actor name from the beginning of a string.
        /// </summary>
        /// <param name="text">Text possibly containing an actor name and colon at the beginning.</param>
        /// <param name="actorName">Actor name (if extracted).</param>
        /// <returns>True if an actor name was extracted; false otherwise.</returns>
        public static bool TryExtractPrependedActor(ref string text, out string actorName)
        {
            if (text != null && text.Contains(":"))
            {
                var colonPos = text.IndexOf(':');
                if (colonPos < text.Length - 1)
                {
                    actorName = text.Substring(0, colonPos);
                    text = text.Substring(colonPos + 1).TrimStart();
                    return true;
                }
            }
            actorName = string.Empty;
            return false;
        }

        protected virtual int GetActorID(string actorName, int defaultID)
        {
            var actor = DialogueManager.MasterDatabase.GetActor(actorName);
            return (actor != null) ? actor.id : defaultID;
        }

        protected virtual void OnConversationLine(Subtitle subtitle)
        {
            if (subtitle == null || !storyDict.ContainsKey(subtitle.dialogueEntry.conversationID))
            {
                // Not the fake Ink conversation: Do nothing special.
            }
            else
            {
                if (string.IsNullOrEmpty(subtitle.formattedText.text) ||
                    (string.IsNullOrEmpty(sequenceToPlayWithSubtitle) && (subtitle.sequence == "None()" || subtitle.sequence == "Continue()")))
                {
                    return;
                }
                subtitle.sequence = sequenceToPlayWithSubtitle;
                sequenceToPlayWithSubtitle = string.Empty;
            }
        }

        #endregion

        #region External Functions

        protected virtual void BindExternalFunctions(Story story)
        {
            if (story == null) return;
            story.BindExternalFunction("ShowAlert", (string message) => { DialogueManager.ShowAlert(message); });
            story.BindExternalFunction("Sequence", (string sequence) => { PlaySequenceFromInk(sequence); });
            story.BindExternalFunction("CurrentQuestState", (string questName) => { return QuestLog.CurrentQuestState(questName); });
            story.BindExternalFunction("CurrentQuestEntryState", (string questName, int entryNumber) => { return QuestLog.CurrentQuestEntryState(questName, entryNumber); });
            story.BindExternalFunction("SetQuestState", (string questName, string state) => { QuestLog.SetQuestState(questName, state); });
            story.BindExternalFunction("SetQuestEntryState", (string questName, int entryNumber, string state) => { QuestLog.SetQuestEntryState(questName, entryNumber, state); });
            story.BindExternalFunction("GetBoolVariable", (string variableName) => { return DialogueLua.GetVariable(variableName).asBool; });
            story.BindExternalFunction("GetIntVariable", (string variableName) => { return DialogueLua.GetVariable(variableName).asInt; });
            story.BindExternalFunction("GetStringVariable", (string variableName) => { return DialogueLua.GetVariable(variableName).asString; });
            story.BindExternalFunction("SetBoolVariable", (string variableName, bool value) => { DialogueLua.SetVariable(variableName, value); });
            story.BindExternalFunction("SetIntVariable", (string variableName, int value) => { DialogueLua.SetVariable(variableName, value); });
            story.BindExternalFunction("SetStringVariable", (string variableName, string value) => { DialogueLua.SetVariable(variableName, value); });
        }

        protected virtual void PlaySequenceFromInk(string sequence)
        {
            sequence = sequence.Replace(@"[[", "{{").Replace(@"]]", @"}}"); // Ink doesn't parse {{ }} in external funcs, so allow [[ ]].
            if (DialogueManager.isConversationActive && tieSequencesToDialogueEntries)
            {
                if (!string.IsNullOrEmpty(sequenceToPlayWithSubtitle)) sequenceToPlayWithSubtitle += "; ";
                sequenceToPlayWithSubtitle += sequence;
            }
            else
            {
                StartCoroutine(PlaySequenceAtEndOfFrame(sequence));
            }

        }

        IEnumerator PlaySequenceAtEndOfFrame(string sequence)
        {
            // Must wait for end of frame for conversation state to be fully updated.
            yield return new WaitForEndOfFrame();
            Transform barker = null;
            Transform listener = null;
            if (DialogueManager.currentConversationState != null)
            {
                barker = DialogueManager.currentConversationState.subtitle.speakerInfo.transform;
                listener = DialogueManager.currentConversationState.subtitle.listenerInfo.transform;
            }
            DialogueManager.PlaySequence(sequence, barker, listener);
        }

        protected virtual void UnbindExternalFunctions(Story story)
        {
            if (story == null) return;
            story.UnbindExternalFunction("ShowAlert");
            story.UnbindExternalFunction("Sequence");
            story.UnbindExternalFunction("CurrentQuestState");
            story.UnbindExternalFunction("CurrentQuestEntryState");
            story.UnbindExternalFunction("SetQuestState");
            story.UnbindExternalFunction("SetQuestEntryState");
            story.UnbindExternalFunction("GetBoolVariable");
            story.UnbindExternalFunction("GetIntVariable");
            story.UnbindExternalFunction("GetStringVariable");
            story.UnbindExternalFunction("SetBoolVariable");
            story.UnbindExternalFunction("SetIntVariable");
            story.UnbindExternalFunction("SetStringVariable");
        }

        #endregion

        #region Lua Functions

        protected virtual void RegisterLuaFunctions()
        {
            if (m_registeredLuaFunctions) return;
            m_registeredLuaFunctions = true;
            Lua.RegisterFunction("SetInkBool", null, SymbolExtensions.GetMethodInfo(() => SetInkBool(string.Empty, false)));
            Lua.RegisterFunction("SetInkNumber", null, SymbolExtensions.GetMethodInfo(() => SetInkNumber(string.Empty, (double)0)));
            Lua.RegisterFunction("SetInkString", null, SymbolExtensions.GetMethodInfo(() => SetInkString(string.Empty, string.Empty)));
            Lua.RegisterFunction("GetInkBool", null, SymbolExtensions.GetMethodInfo(() => GetInkBool(string.Empty)));
            Lua.RegisterFunction("GetInkNumber", null, SymbolExtensions.GetMethodInfo(() => GetInkNumber(string.Empty)));
            Lua.RegisterFunction("GetInkString", null, SymbolExtensions.GetMethodInfo(() => GetInkString(string.Empty)));
        }

        public static void SetInkBool(string variableName, bool value)
        {
            if (m_instance != null) m_instance.SetInkVariableValue(variableName, value);
        }

        public static void SetInkNumber(string variableName, double value)
        {
            if (m_instance != null) m_instance.SetInkVariableValue(variableName, (float)value);
        }

        public static void SetInkString(string variableName, string value)
        {
            if (m_instance != null) m_instance.SetInkVariableValue(variableName, value);
        }

        public static bool GetInkBool(string variableName)
        {
            if (m_instance == null) return false;
            var value = m_instance.GetInkVariableValue(variableName);
            if (value == null)
            {
                return false;
            }
            else if (value.GetType() == typeof(bool))
            {
                return (bool)value;
            }
            else
            {
                return Tools.StringToBool(value.ToString());
            }
        }

        public static double GetInkNumber(string variableName)
        {
            if (m_instance == null) return 0;
            var value = m_instance.GetInkVariableValue(variableName);
            if (value == null) return 0;
            else if (value.GetType() == typeof(float)) return (float)value;
            else if (value.GetType() == typeof(int)) return (int)value;
            else if (value.GetType() == typeof(double)) return (double)value;
            else return Tools.StringToFloat(value.ToString());
        }

        public static string GetInkString(string variableName)
        {
            if (m_instance == null) return string.Empty;
            var value = m_instance.GetInkVariableValue(variableName);
            return (value != null) ? value.ToString() : string.Empty;
        }

        protected virtual void SetInkVariableValue(string variableName, object value)
        {
            foreach (var story in stories)
            {
                var storyContainsVariable = false;
                foreach (var variable in story.variablesState)
                {
                    if (string.Equals(variable, variableName))
                    {
                        storyContainsVariable = true;
                        break;
                    }
                }
                if (storyContainsVariable) story.variablesState[variableName] = value;
            }
        }

        protected virtual object GetInkVariableValue(string variableName)
        {
            foreach (var story in stories)
            {
                var storyContainsVariable = false;
                foreach (var variable in story.variablesState)
                {
                    if (string.Equals(variable, variableName))
                    {
                        storyContainsVariable = true;
                        break;
                    }
                }
                if (storyContainsVariable) return story.variablesState[variableName];
            }
            return null;
        }

        #endregion

        #region Get Actors In Story

        public virtual Story GetStory(string storyName)
        {
            for (int i = 0; i < inkJSONAssets.Count; i++)
            {
                if (string.Equals(inkJSONAssets[i].name, storyName))
                {
                    return new Story(inkJSONAssets[i].text);
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the names of all actors in the story or knot.
        /// </summary>
        /// <param name="storyName">Story name.</param>
        /// <param name="knotName">Knot name, or blank for entire story.</param>
        public virtual string[] GetActorsInStory(string storyName, string knotName)
        {
            var actors = new List<string>();

            var story = GetStory(storyName);
            if (story == null) return actors.ToArray();
            story.ResetState();

            if (!string.IsNullOrEmpty(knotName))
            {
                story.ChoosePathString(knotName);
            }

            var visitedChoices = new HashSet<Choice>();
            var knotPath = string.IsNullOrEmpty(knotName) ? string.Empty : knotName + ".";
            RecordActorsInStoryRecursive(story, knotPath, visitedChoices, actors, 0);

            return actors.ToArray();
        }

        protected void RecordActorsInStoryRecursive(Story story, string knotPath, HashSet<Choice> visitedChoices, List<string> actors, int recursionDepth)
        {
            if (recursionDepth > 1024) return; // Safeguard to prevent infinite recursion.

            int safeguard = 0;
            bool hasMoreText;

            // Process all lines until we get to a choice:
            do
            {
                // Check actor tags:
                foreach (var tag in story.currentTags)
                {
                    if (tag.StartsWith("Actor=")) RecordActor(actors, tag.Substring("Actor=".Length));
                    else if (tag.StartsWith("Conversant=")) RecordActor(actors, tag.Substring("Conversant=".Length));
                }

                // Check prepended actor names:
                if (actorNamesPrecedeLines)
                {
                    var text = story.currentText;
                    if (text != null && text.Contains(":"))
                    {
                        var colonPos = text.IndexOf(':');
                        if (colonPos < text.Length - 1)
                        {
                            RecordActor(actors, text.Substring(0, colonPos));
                        }
                    }
                }

                hasMoreText = story.canContinue;
                if (story.canContinue)
                {
                    story.Continue();
                }
            } while (hasMoreText && ++safeguard < 16348);

            // Process all choices recursively:
            var numChoices = story.currentChoices.Count;
            if (numChoices > 0)
            {
                var savedState = story.state.ToJson();
                var knotOnly = !string.IsNullOrEmpty(knotPath);
                for (int i = 0; i < numChoices; i++)
                {
                    var choice = story.currentChoices[i];
                    if (visitedChoices.Contains(choice)) continue;
                    if (knotOnly && !choice.sourcePath.StartsWith(knotPath)) continue;
                    visitedChoices.Add(choice);
                    story.ChooseChoiceIndex(i);
                    RecordActorsInStoryRecursive(story, knotPath, visitedChoices, actors, recursionDepth + 1);
                    story.state.LoadJson(savedState);
                }
            }
        }

        protected void RecordActor(List<string> actors, string actorName)
        {
            if (string.IsNullOrEmpty(actorName) || actors.Contains(actorName)) return;
            actors.Add(actorName);
        }

        #endregion

    }
}
