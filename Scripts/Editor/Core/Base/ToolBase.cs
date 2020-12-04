﻿#if UNITY_EDITOR
using Pumkin.AvatarTools.Interfaces;
using Pumkin.AvatarTools.Tools;
using Pumkin.AvatarTools.UI;
using Pumkin.Core;
using Pumkin.Core.Extensions;
using Pumkin.Core.Helpers;
using Pumkin.Core.UI;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace Pumkin.AvatarTools.Base
{
    /// <summary>
    /// Base tool class. Should be inherited to create a tool
    /// </summary>
    public abstract class ToolBase : ITool, IDisposable
    {
        public string GameConfigurationString { get; set; }
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
        public virtual GUIContent Content
        {
            get
            {
                if(_content == null)
                    _content = CreateGUIContent();
                return _content;
            }
        }

        protected virtual GUIContent CreateGUIContent()
        {
            return new GUIContent(UIDefs.Name, UIDefs.Description);
        }

        public virtual ISettingsContainer Settings => null;
        public virtual UIDefinition UIDefs { get; set; }

        bool _allowUpdate;
        GUIContent _content;

        EditorApplication.CallbackFunction updateCallback;

        public SerializedObject serializedObject;

        public ToolBase()
        {
            if(UIDefs == null)
                UIDefs = new UIDefinition(GetType().Name);
            SetupSettings();
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

        protected virtual void SetupSettings() { }

        public virtual void DrawUI(params GUILayoutOption[] options)
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
                    Settings?.Editor?.OnInspectorGUINoScriptField();
                });
            }
        }

        public virtual bool TryExecute(GameObject target)
        {
            try
            {
                if(Prepare(target) && DoAction(target))
                {
                    serializedObject.ApplyModifiedProperties();
                    Finish(target, true);
                    return true;
                }
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
            Finish(target, false);
            return false;
        }

        protected virtual bool Prepare(GameObject target)
        {
            if(!target)
            {
                PumkinTools.LogError("No avatar selected");
                return false;
            }

            serializedObject = new SerializedObject(target);
            return true;
        }

        protected abstract bool DoAction(GameObject target);

        protected virtual void Finish(GameObject target, bool success)
        {
            if(success)
                PumkinTools.Log($"<b>{UIDefs.Name}</b> completed successfully");
            else
                PumkinTools.LogWarning($"<b>{UIDefs.Name}</b> failed");
        }

        public virtual void Update()
        {
            if(!CanUpdate)
                return;
        }

        public virtual void Dispose()
        {
            EditorApplication.update -= Update;
        }
    }
}
#endif