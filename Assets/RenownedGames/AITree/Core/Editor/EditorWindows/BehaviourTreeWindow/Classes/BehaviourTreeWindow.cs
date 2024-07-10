/* ================================================================
   ----------------------------------------------------------------
   Project   :   AI Tree
   Publisher :   Renowned Games
   Developer :   Tamerlan Shakirov, Zinnur Davleev
   ----------------------------------------------------------------
   Copyright 2022-2023 Renowned Games All rights reserved.
   ================================================================ */

using RenownedGames.AITree;
using RenownedGames.ExLibEditor.Windows;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using EditorResources = RenownedGames.ExLibEditor.EditorResources;

namespace RenownedGames.AITreeEditor
{
    public sealed class BehaviourTreeWindow : EditorWindow, IHasCustomMenu
    {
        private static List<BehaviourTreeWindow> Instances;

        public sealed class Styles
        {
            private GUIStyle lockButtonStyle;

            /// <summary>
            /// Use it only in GUI calls.
            /// </summary>
            /// <returns></returns>
            public GUIStyle GetLockButtonStyle()
            {
                if (lockButtonStyle == null)
                {
                    lockButtonStyle = new GUIStyle("IN LockButton");
                }
                return lockButtonStyle;
            }
        }

        // Stored required properties.
        private bool isLocked;
        private bool hasChanges;
        private int frameCount;
        private int lastRunnerID;
        private BehaviourTree lastSharedTree;
        private BehaviourTree selectedTree;
        private AITreeSettings settings;

        private Styles styles;
        private BehaviourTreeGraph graph;
        private Label treeName;
        private Label nodeDescription;
        private ToolbarMenu toolbarAssets;
        private ToolbarButton saveButton;
        private ToolbarToggle autoSaveToggle;
        private VisualElement simulatingBorder;

        /// <summary>
        /// Static constructor of behaviour tree window.
        /// </summary>
        static BehaviourTreeWindow()
        {
            Instances = new List<BehaviourTreeWindow>();
        }

        /// <summary>
        /// This function is called when the window is loaded.
        /// </summary>
        private void OnEnable()
        {
            Instances.Add(this);
            Instances.TrimExcess();

            Texture2D icon = EditorResources.Load<Texture2D>("Images/Icons/Window/BehaviourTreeIcon.png");
            titleContent = new GUIContent("Behaviour Tree", icon);

            styles = new Styles();

            LoadVisualElements();

            if (selectedTree != null)
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    TrackEditor(selectedTree);
                }
            }
            else
            {
                OnSelectionChange();
            }

            EditorApplication.playModeStateChanged -= OnPlayMoveStateChanged;
            EditorApplication.playModeStateChanged += OnPlayMoveStateChanged;
            AITreeSettings.Saved -= OnSettingsSaved;
            AITreeSettings.Saved += OnSettingsSaved;

            settings = AITreeSettings.instance;

#if UNITY_2022_3_OR_NEWER
            ShowHotKeyNotification();
#endif
        }

        /// <summary>
        /// Called when the window gets keyboard focus.
        /// </summary>
        private void OnFocus()
        {
            if (settings == null)
            {
                settings = AITreeSettings.instance;
            }

            if (HasUnloadedVisualElements())
            {
                LoadVisualElements();
                graph.PopulateView(selectedTree);
            }

            UpdateTitleLabel();
        }

        /// <summary>
        /// Called for rendering and handling GUI events.
        /// </summary>
        private void OnGUI()
        {
#if UNITY_2022_3_OR_NEWER
            if (focusedWindow == this)
            {
                HotKeyUtility.SetEvent(Event.current);
            }
#endif
        }

        /// <summary>
        /// Called at 10 frames per second to give the inspector a chance to update.
        /// </summary>
        private void OnInspectorUpdate()
        {
            graph?.OnInspectorUpdate();
        }

        /// <summary>
        /// This function is called when the window is closed.
        /// </summary>
        private void OnDisable()
        {
            Instances.Remove(this);
            Instances.TrimExcess();

            SaveChanges();
            EditorApplication.playModeStateChanged -= OnPlayMoveStateChanged;
            AITreeSettings.Saved -= OnSettingsSaved;
            graph.OnClose();

#if UNITY_2022_3_OR_NEWER
            HotKeyUtility.SetEvent(null);
#endif
        }

        /// <summary>
        /// Called every time the project changes.
        /// </summary>
        private void OnProjectChange()
        {
            if (selectedTree == null || graph.GetBehaviourTree() == null)
            {
                TrackEditor(null);
            }
            else
            {
                if (EditorUtility.IsDirty(selectedTree))
                {
                    hasChanges = true;
                    saveButton.SetEnabled(true);
                    if (autoSaveToggle.value)
                    {
                        SaveChanges();
                    }
                }
            }
        }

        /// <summary>
        /// Called every time the selection changes.
        /// </summary>
        private void OnSelectionChange()
        {
            if(Selection.activeObject is BehaviourTree behaviourTree)
            {
                lastRunnerID = -1;
                lastSharedTree = behaviourTree;
                TrackEditor(behaviourTree);
            }
            else if(Selection.activeGameObject != null && Selection.activeGameObject.TryGetComponent(out BehaviourRunner runner))
            {
                lastRunnerID = runner.GetInstanceID();
                lastSharedTree = runner.GetSharedBehaviourTree();
                behaviourTree = EditorApplication.isPlaying ? runner.GetBehaviourTree() : runner.GetSharedBehaviourTree();
                TrackEditor(behaviourTree);
            }
            else
            {
                graph.ClearSelection();
            }
        }

        /// <summary>
        /// Save behaviour tree changes.
        /// </summary>
        public override void SaveChanges()
        {
            base.SaveChanges();

            if (selectedTree != null && !selectedTree.IsRunning())
            {
                AssetDatabase.SaveAssetIfDirty(selectedTree);
                AssetDatabase.Refresh();
                MarkAsSaved();
            }
        }

        /// <summary>
        /// Discards behaviour tree changes.
        /// </summary>
        public override void DiscardChanges()
        {
            base.DiscardChanges();
        }

        /// <summary>
        /// Marks behaviour tree window as changed and need to save.
        /// </summary>
        public void MarkAsChanged()
        {
            MarkAsChanged(true);
        }

        /// <summary>
        /// Marks behaviour tree window as saved.
        /// </summary>
        public void MarkAsSaved()
        {
            MarkAsSaved(true);
        }

        /// <summary>
        /// Marks behaviour tree window as changed and need to save.
        /// </summary>
        internal void MarkAsChanged(bool notifyAll)
        {
            EditorUtility.SetDirty(selectedTree);
            hasChanges = true;
            saveButton.SetEnabled(true);

            if (autoSaveToggle.value)
            {
                SaveChanges();
            }

            if (notifyAll)
            {
                foreach (BehaviourTreeWindow window in Instances)
                {
                    if (window.GetInstanceID() != GetInstanceID() && window.GetSelectedTree() == selectedTree)
                    {
                        window.MarkAsChanged(false);
                    }
                }
            }
        }

        /// <summary>
        /// Marks behaviour tree window as saved.
        /// </summary>
        internal void MarkAsSaved(bool notifyAll)
        {
            hasChanges = false;
            saveButton.SetEnabled(false);

            if (notifyAll)
            {
                foreach (BehaviourTreeWindow window in Instances)
                {
                    if (window.GetInstanceID() != GetInstanceID() && window.GetSelectedTree() == selectedTree)
                    {
                        window.MarkAsSaved(false);
                    }
                }
            }
        }

        /// <summary>
        /// Start tracking specified behaviour tree.
        /// </summary>
        /// <param name="target">Behaviour tree reference.</param>
        public void TrackEditor(BehaviourTree target)
        {
            if (HasUnloadedVisualElements())
            {
                LoadVisualElements();
            }

            if (!isLocked || (selectedTree == null && target != null))
            {
                if (target != null)
                {
                    if (!Application.isPlaying && !AssetDatabase.IsNativeAsset(target))
                    {
                        target.BecameNativeAsset -= OnBecameNativeAsset;
                        target.BecameNativeAsset += OnBecameNativeAsset;
                        return;
                    }

                    selectedTree = target;

                    Simulating(selectedTree.IsRunning());
                    graph.ClearSelection();
                    graph.PopulateView(selectedTree);
                    treeName.text = ObjectNames.NicifyVariableName(selectedTree.name);

                    if (NodeInspectorWindow.HasOpenInstances())
                    {
                        NodeInspectorWindow.NotifyTrackEditor(selectedTree);
                    }

                    EditorApplication.update += FrameAll;
                }
                else
                {
                    selectedTree = null;
                    treeName.text = "BEHAVIOUR TREE";
                    Simulating(false);
                    graph.ClearSelection();
                    graph.ClearGraph();
                }
            }
        }

        /// <summary>
        /// Start tracking specified behaviour tree for all opened AI Tree editor trackers.
        /// </summary>
        /// <param name="target"></param>
        private void TrackEditorInternal(BehaviourTree target)
        {
            TrackEditor(target);
            lastSharedTree = target;
            lastRunnerID = -1;

            if (BlackboardWindow.HasOpenInstances())
            {
                BlackboardWindow.NotifyTrackEditor(target.GetBlackboard());
            }

            if (BlackboardDetailsWindow.HasOpenInstances())
            {
                BlackboardDetailsWindow.NotifyTrackEditor(target.GetBlackboard());
            }

            if (NodeInspectorWindow.HasOpenInstances())
            {
                NodeInspectorWindow.NotifyTrackEditor(target);
            }

            if (BlackboardViewerWindow.HasOpenInstances())
            {
                BlackboardViewerWindow.NotifyTrackEditor(target);
            }
        }

        /// <summary>
        /// Called when the selected behavior tree is available to work with them.
        /// </summary>
        private void OnBecameNativeAsset(BehaviourTree behaviourTree)
        {
            behaviourTree.BecameNativeAsset -= OnBecameNativeAsset;
            TrackEditor(behaviourTree);
        }

        /// <summary>
        /// Focus view all elements in the graph.
        /// </summary>
        private void FrameAll()
        {
            graph.FrameAll();
            Repaint();

            // A two-frame plug is necessary in order to make sure that the alignment has been performed.
            // GraphView.FrameAll() can skip framing.
            if (frameCount > 1)
            {
                EditorApplication.update -= FrameAll;
                frameCount = 0;
            }
            frameCount++;
        }

        /// <summary>
        /// Simulating mode is enabled.
        /// </summary>
        /// <returns></returns>
        private bool Simulating()
        {
            return simulatingBorder.style.display == DisplayStyle.Flex;
        }

        /// <summary>
        /// Set simulating mode.
        /// </summary>
        private void Simulating(bool value)
        {
            simulatingBorder.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// Magic method which Unity detects automatically.
        /// </summary>
        /// <param name="position">Position of button.</param>
        private void ShowButton(Rect position)
        {
            isLocked = GUI.Toggle(position, isLocked, GUIContent.none, styles.GetLockButtonStyle());
        }

        /// <summary>
        /// Load all required Visual Elements.
        /// </summary>
        private void LoadVisualElements()
        {
            AITreeSettings settings = AITreeSettings.instance;

            rootVisualElement.Clear();

            VisualTreeAsset visualTree = settings.GetBehaviourTreeUXML();
            visualTree.CloneTree(rootVisualElement);
            rootVisualElement.styleSheets.Add(settings.GetBehaviourTreeUSS());

            graph = rootVisualElement.Q<BehaviourTreeGraph>();
            treeName = rootVisualElement.Q<Label>("tree-name");
            nodeDescription = rootVisualElement.Q<Label>("node-description");
            toolbarAssets = rootVisualElement.Q<ToolbarMenu>("toolbar-assets");
            saveButton = rootVisualElement.Q<ToolbarButton>("save-button");
            autoSaveToggle = rootVisualElement.Q<ToolbarToggle>("auto-save-toggle");
            simulatingBorder = rootVisualElement.Q<VisualElement>("simulating-border");

            graph.FrameAll();
            graph.SetWindow(this);

            graph.UpdateSelection += OnNodeSelectionChanged;
            graph.AssetChanged += MarkAsChanged;

            toolbarAssets.RegisterCallback<MouseDownEvent>(OnFillAssetToolbar, TrickleDown.TrickleDown);
            autoSaveToggle.RegisterValueChangedCallback(OnAutoSave);

            saveButton.SetEnabled(false);
            saveButton.clicked += SaveChanges;

            nodeDescription.visible = false;
            nodeDescription.text = string.Empty;
        }

        /// <summary>
        /// Check if window has new or unloaded visual elements.
        /// </summary>
        /// <returns>True if has new or unloaded visual elements, otherwise false.</returns>
        private bool HasUnloadedVisualElements()
        {
            return graph == null
                || treeName == null
                || toolbarAssets == null
                || saveButton == null
                || autoSaveToggle == null
                || simulatingBorder == null;
        }

        /// <summary>
        /// Update behaviour tree title label.
        /// </summary>
        private void UpdateTitleLabel()
        {
            if (treeName != null)
            {
                switch (settings.GetTreeNameMode())
                {
                    case AITreeSettings.TreeNameMode.Normal:
                        treeName.style.fontSize = 50;
                        treeName.visible = true;
                        break;
                    case AITreeSettings.TreeNameMode.Small:
                        treeName.style.fontSize = 25;
                        treeName.visible = true;
                        break;
                    case AITreeSettings.TreeNameMode.Disable:
                        treeName.style.fontSize = 50;
                        treeName.visible = false;
                        break;
                }
            }
        }

#if UNITY_2022_3_OR_NEWER
        /// <summary>
        /// Show a message if need to change the API.
        /// </summary>
        private static void ShowHotKeyNotification()
        {
            const string KEY = "RenownedGames.AITreeEditor.BehaviourTreeWindow.HotKeyNotification";
            if (!EditorPrefs.GetBool(KEY, false))
            {
                AITreeSettings settings = AITreeSettings.instance;
                if (!settings.HotKeyListener())
                {
                    bool change = EditorUtility.DisplayDialog("AI Tree", $"You are using the new version of Unity {Application.unityVersion}, we recommend switching to the new hotkey API, as some users have encountered hotkey problems in graph using the classic Unity API.\n\nYou can do this later in the settings.\n\nEdit/Preferences/AI Tree/Hot Key Listener", "Change", "Skip");
                    if (change)
                    {
                        settings.HotKeyListener(true);
                        settings.Save();

                        if (settings.HotKeyListener())
                        {
                            EditorUtility.DisplayDialog("AI Tree", "Settings successfully changed!", "Continue");
                        }
                    }
                    EditorPrefs.SetBool(KEY, true);
                }
            }
        }
#endif

        #region [IHasCustomMenu Implementation]
        /// <summary>
        /// Adds your custom menu items to an Editor Window.
        /// </summary>
        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Locked"), isLocked, () => isLocked = !isLocked);
        }
        #endregion

        #region [Callbacks]
        /// <summary>
        /// Called when the play mode changes.
        /// </summary>
        private void OnPlayMoveStateChanged(PlayModeStateChange state)
        {
            BehaviourRunner runner = EditorUtility.InstanceIDToObject(lastRunnerID) as BehaviourRunner;
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    if (runner != null)
                    {
                        TrackEditor(runner.GetBehaviourTree());
                    }
                    else
                    {
                        TrackEditor(lastSharedTree);
                    }
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    if (runner != null)
                    {
                        TrackEditor(runner.GetSharedBehaviourTree());
                    }
                    else
                    {
                        TrackEditor(lastSharedTree);
                    }
                    break;

            }
        }

        /// <summary>
        /// Called when a node is selected.
        /// </summary>
        private void OnNodeSelectionChanged(Node node)
        {
            nodeDescription.text = string.Empty;
            nodeDescription.visible = false;
            if (node != null)
            {
                if (node is RootNode)
                {
                    NodeInspectorWindow.NotifyTrackEditor(selectedTree);
                }
                else
                {
                    NodeInspectorWindow.NotifyTrackEditor(node);

                    if((AITreeSettings.instance.GetNodeTooltipMode() & AITreeSettings.NodeTooltipMode.GraphOverlay) != 0)
                    {
                        if (NodeTypeCache.TryGetNodeInfo(node.GetType(), out NodeTypeCache.NodeInfo nodeInfo))
                        {
                            if (nodeInfo.tooltipAttribute != null)
                            {
                                nodeDescription.visible = true;
                                nodeDescription.text = nodeInfo.tooltipAttribute.text;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called after clicking on assets button to fill in the assets toolbar dropdown menu.
        /// </summary>
        private void OnFillAssetToolbar(MouseDownEvent evt)
        {
            // Clear toolbar
            toolbarAssets.menu.MenuItems().Clear();

            // Create new behaviour tree
            toolbarAssets.menu.AppendAction("Create new Behaviour Tree", a =>
            {
                BehaviourTree behaviourTree = BehaviourTree.Create("Behaviour Tree");

                Selection.activeObject = behaviourTree;
                EditorGUIUtility.PingObject(behaviourTree);
            });

            // Link to created behaviour trees
            HashSet<string> names = new HashSet<string>();
            string[] guids = AssetDatabase.FindAssets("t:BehaviourTree");
            for (int i = 0; i < guids.Length; i++)
            {
                string guid = guids[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                BehaviourTree behaviourTree = AssetDatabase.LoadAssetAtPath<BehaviourTree>(path);
                if (behaviourTree != null)
                {
                    string name = System.IO.Path.GetFileNameWithoutExtension(path);
                    name = ObjectNames.NicifyVariableName(name);
                    if (!names.Add(name))
                    {
                        name += $" ({guid.Substring(0, Mathf.Min(8, guid.Length))}...)";
                    }

                    toolbarAssets.menu.AppendAction(name, a => TrackEditorInternal(behaviourTree), selectedTree != behaviourTree ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                }
            }
        }

        /// <summary>
        /// Called when auto save toggle changed.
        /// </summary>
        /// <param name="evt"></param>
        private void OnAutoSave(ChangeEvent<bool> evt)
        {
            saveButton.visible = !evt.newValue;
            if (evt.newValue && hasChanges)
            {
                SaveChanges();
            }
        }

        /// <summary>
        /// Called when AITreeSettings.Save() method performed.
        /// <br>It does not guarantee that the settings have been changed.</br>
        /// </summary>
        private void OnSettingsSaved(AITreeSettings settings)
        {
            UpdateTitleLabel();
        }
        #endregion

        #region [Static Methods]
        [MenuItem("Tools/AI Tree/Windows/Behaviour Tree", false, 20)]
        public static void Open()
        {
            if (HasOpenInstances())
            {
                BehaviourTreeWindow[] windows = GetInstances();
                for (int i = 0; i < windows.Length; i++)
                {
                    BehaviourTreeWindow window = windows[i];
                    if (!window.isLocked)
                    {
                        window.Focus();
                    }
                }
            }
            else
            {
                CreateWindow();
            }
        }

        /// <summary>
        /// Check if has open instances of Behaviour Tree window.
        /// </summary>
        /// <returns></returns>
        public static bool HasOpenInstances()
        {
            return Instances.Count > 0;
        }

        /// <summary>
        /// Get all open Behaviour Tree window instances.
        /// </summary>
        /// <returns>Array of open instances.</returns>
        public static BehaviourTreeWindow[] GetInstances()
        {
            return Instances.ToArray();
        }

        /// <summary>
        /// Notify all instance of behaviour tree windows, to track specified behaviour tree editor graph.
        /// </summary>
        /// <param name="target">Behaviour tree reference.</param>
        public static void NotifyTrackEditor(BehaviourTree target)
        {
            if (Instances.Count == 0)
            {
                BehaviourTreeWindow window = CreateWindow();
                window.TrackEditor(target);
                window.MoveToCenter();
                window.Show();
            }
            else
            {
                foreach (BehaviourTreeWindow window in Instances)
                {
                    window.TrackEditor(target);
                }
            }
        }

        /// <summary>
        /// Create new instance of Node Behaviour Tree.
        /// </summary>
        /// <returns>Instance of BehaviourTreeWindow.</returns>
        internal static BehaviourTreeWindow CreateWindow()
        {
            BehaviourTreeWindow window = CreateInstance<BehaviourTreeWindow>();
            window.saveChangesMessage = "Behaviour tree has unsaved changes. Would you like to save?";
            window.MoveToCenter();
            window.Show();
            return window;
        }

        /// <summary>
        /// Allows you to open the behavior the editor by opening the behavior tree asset.
        /// </summary>
        [OnOpenAsset]
        private static bool OnOpenAsset(int instanceId, int line)
        {
            Object asset = EditorUtility.InstanceIDToObject(instanceId);
            if (asset is BehaviourTree)
            {
                if (HasOpenInstances<BehaviourTreeWindow>())
                {
                    FocusWindowIfItsOpen<BehaviourTreeWindow>();
                }
                else
                {
                    Open();
                }
                return true;
            }
            return false;
        }
        #endregion

        #region [Getter / Setter]
        public int GetLastRunnerID()
        {
            return lastRunnerID;
        }

        public BehaviourTreeGraph GetGraph()
        {
            return graph;
        }

        public Label GetTreeName()
        {
            return treeName;
        }

        public BehaviourTree GetSelectedTree()
        {
            return selectedTree;
        }
        #endregion
    }
}