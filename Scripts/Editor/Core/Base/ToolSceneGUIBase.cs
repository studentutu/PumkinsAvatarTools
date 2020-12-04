﻿using NUnit.Framework;
using Pumkin.AvatarTools.Interfaces;
using Pumkin.AvatarTools.Tools;
using Pumkin.AvatarTools.UI;
using Pumkin.Core;
using Pumkin.Core.Helpers;
using Pumkin.Core.UI;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Experimental.UIElements;
using UnityEngine;

namespace Pumkin.AvatarTools.Base
{
    public abstract class ToolSceneGUIBase : ITool
    {
        public bool CanDrawSceneGUI
        {
            get
            {
                return _allowSceneGUI;
            }

            set
            {
                if(_allowSceneGUI == value)
                    return;

                if(_allowSceneGUI = value)    //Intentional assign + check
                    SetupOnSceneGUIDelegate(true);
                else
                    SetupOnSceneGUIDelegate(false);
            }
        }

        protected virtual Vector2 WindowSize { get; set; } = new Vector2(200, 50);

        float Padding { get; set; } = 10;

        public string GameConfigurationString { get; set; }

        public virtual ISettingsContainer Settings { get => null; }

        public bool CanUpdate
        {
            get
            {
                return _allowUpdate;
            }

            set
            {
                if(_allowUpdate == value)
                    return;

                if(_allowUpdate = value)    //Intentional assign + check
                    SetupUpdateCallback(ref updateCallback, true);
                else
                    SetupUpdateCallback(ref updateCallback, false);
            }
        }

        protected ToolStatus Status = ToolStatus.Waiting;

        public virtual GUIContent Content
        {
            get
            {
                if(_content == null)
                    _content = CreateGUIContent();
                return _content;
            }
        }

        public virtual UIDefinition UIDefs { get; set; }

        bool _allowSceneGUI;
        bool _allowUpdate;
        GUIContent _content;
        Tool editorToolOld;

        public SerializedObject serializedObject;

        EditorApplication.CallbackFunction updateCallback;

        public ToolSceneGUIBase()
        {
            if(UIDefs == null)
                UIDefs = new UIDefinition(GetType().Name);
            SetupSettings();
        }

        protected virtual void SetupSettings() { }

        protected virtual GUIContent CreateGUIContent()
        {
            return new GUIContent(UIDefs.Name, UIDefs.Description);
        }

        void SetupUpdateCallback(ref EditorApplication.CallbackFunction callback, bool add)
        {
            if(callback == null)
            {
                PumkinTools.LogVerbose($"Setting up Update callback for <b>{UIDefs.Name}</b>");
                callback = new EditorApplication.CallbackFunction(Update);
            }

            if(!add)
                EditorApplication.update -= callback;
            else
                EditorApplication.update += callback;
        }

        void SetupOnSceneGUIDelegate(bool add)
        {
            if(add)
            {
                PumkinTools.LogVerbose($"Setting up Update callback for <b>{UIDefs.Name}</b>");
                SceneView.onSceneGUIDelegate += OnSceneGUI;
            }
            else
            {
                SceneView.onSceneGUIDelegate -= OnSceneGUI;
            }
        }

        public void OnSceneGUI(SceneView scene)
        {
            if(!CanDrawSceneGUI)
                return;

            Rect rect = scene.camera.pixelRect;
            rect = new Rect(Padding, rect.height - Padding - WindowSize.y, WindowSize.x, WindowSize.y);
            float minButtonWidth = rect.width / 2 - Styles.Box.padding.left - Styles.Box.padding.right;

            Handles.BeginGUI();
            {
                GUILayout.BeginArea(rect, Styles.Box);
                {
                    GUILayout.Label(UIDefs.Name);

                    if(CanDrawSceneGUI)
                    {
                        DrawInsideSceneWindowGUI();
                    }

                    GUILayout.BeginHorizontal();
                    {
                        if(GUILayout.Button("Cancel", GUILayout.MinWidth(minButtonWidth)))
                            PressedCancel(serializedObject?.targetObject as GameObject);

                        if(GUILayout.Button("Apply", GUILayout.MinWidth(minButtonWidth)))
                            PressedApply(serializedObject?.targetObject as GameObject);
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndArea();
            }
            Handles.EndGUI();

            try
            {
                if(CanDrawSceneGUI)
                {
                    if(!serializedObject?.targetObject)
                    {
                        Status = ToolStatus.Error;
                        Finish(null, false);
                    }
                    else
                        DrawHandles(serializedObject?.targetObject as GameObject);
                }
            }
            catch {}
        }

        protected virtual void DrawHandles(GameObject target) { }
        protected virtual void DrawInsideSceneWindowGUI() { }

        protected virtual bool Prepare(GameObject target)
        {
            Status = ToolStatus.InProgress;
            if(!target)
            {
                PumkinTools.LogError("No avatar selected");
                return false;
            }

            serializedObject = new SerializedObject(target);
            return true;
        }

        public bool TryExecute(GameObject target)
        {
            try
            {
                if(Prepare(target))
                {
                    editorToolOld = UnityEditor.Tools.current;
                    UnityEditor.Tools.current = Tool.None;
                    CanDrawSceneGUI = true;
                    UIDefs.EnabledInUI = false;
                }
                else
                {
                    Status = ToolStatus.Error;
                    Finish(target, false);
                }
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
            return false;
        }

        protected virtual void Finish(GameObject target, bool success)
        {
            CanDrawSceneGUI = false;
            UIDefs.EnabledInUI = true;

            UnityEditor.Tools.current = editorToolOld;

            if(success)
            {
                Status = ToolStatus.CompletedOK;
                PumkinTools.Log($"<b>{UIDefs.Name}</b> completed successfully");
            }
            else
            {
                if(Status == ToolStatus.Canceled)
                    PumkinTools.Log($"<b>{UIDefs.Name}</b> was cancelled");
                else
                    PumkinTools.LogWarning($"<b>{UIDefs.Name}</b> failed");
            }
        }

        protected virtual void PressedApply(GameObject target)
        {
            if(!DoAction(target))
            {
                Finish(target, false);
                return;
            }
            serializedObject.ApplyModifiedProperties();
            Finish(target, true);
        }

        protected abstract bool DoAction(GameObject target);

        protected virtual void PressedCancel(GameObject target)
        {
            Status = ToolStatus.Canceled;
            Finish(target, false);
        }

        public void Dispose()
        {
            EditorApplication.update -= Update;
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
        }

        public void Update()
        {
            if(!CanUpdate)
                return;
        }

        public void DrawUI(params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginHorizontal();
            {
                if(GUILayout.Button(Content, Styles.SubToolButton, options))
                    TryExecute(PumkinTools.SelectedAvatar);
                if(Settings != null)
                    UIDefs.ExpandSettings = GUILayout.Toggle(UIDefs.ExpandSettings, Icons.Options, Styles.MediumIconButton);
            }
            EditorGUILayout.EndHorizontal();

            //Draw settings here
            if(Settings != null && UIDefs.ExpandSettings)
            {
                UIHelpers.VerticalBox(() =>
                {
                    EditorGUILayout.Space();
                    Settings?.Editor?.OnInspectorGUI();
                });
            }
        }
    }
}