﻿#if UNITY_EDITOR
using Pumkin.AvatarTools.Interfaces;
using Pumkin.AvatarTools.Modules;
using Pumkin.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Pumkin.AvatarTools.UI
{
    /// <summary>
    /// Main UI class, responsible for drawing everything
    /// </summary>
    class MainUI
    {
        public List<IUIModule> UIModules = new List<IUIModule>();
        bool drawSettings = false;
        int configurationIndex = 0;

        Vector2 scroll = Vector2.zero;

        public IUIModule OrphanHolder
        {
            get => _orphanHolder ?? (_orphanHolder = new OrphanHolderModule());
            private set => _orphanHolder = value;
        }

        IUIModule _orphanHolder;

        public MainUI()
        {
            var index = Array.IndexOf(PumkinTools.Configurations, PumkinTools.ConfigurationString);
            configurationIndex = index > 0 ? index : 0;
        }

        public MainUI(List<IUIModule> modules)
        {
            UIModules = modules;
        }

        public void Draw()
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Pumkin's Avatar Tools", Styles.TitleLabel);
                if(GUILayout.Button(Icons.Settings, Styles.MediumIconButton))
                    drawSettings = !drawSettings;
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(20f);

            if(drawSettings)
            {
                EditorGUI.BeginChangeCheck();
                {
                    configurationIndex = EditorGUILayout.Popup("Configuration", configurationIndex, PumkinTools.Configurations);
                }
                if(EditorGUI.EndChangeCheck())
                {
                    PumkinTools.ConfigurationString = PumkinTools.Configurations[configurationIndex];
                }
            }
            else
            {
                PumkinTools.SelectedAvatar = EditorGUILayout.ObjectField("Avatar", PumkinTools.SelectedAvatar, typeof(GameObject), true) as GameObject;

                if(GUILayout.Button("Select from Scene"))
                    PumkinTools.SelectedAvatar = Selection.activeGameObject ?? PumkinTools.SelectedAvatar;

                UIHelpers.DrawGUILine();

                scroll = EditorGUILayout.BeginScrollView(scroll);

                //Draw modules
                foreach(var mod in UIModules)
                {
                    if(mod != null)
                    {
                        try
                        {
                            if(!mod.IsHidden)
                                mod.DrawUI();
                        }
                        catch(Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                    else
                    {
                        PumkinTools.Log($"'{mod}' is null");
                    }
                }

                //Draw Orphan Holder module
                try
                {
                    if(OrphanHolder != null)
                        if(!OrphanHolder.IsHidden)
                            OrphanHolder.DrawUI();
                }
                catch(Exception e)
                {
                    Debug.LogException(e);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        public IUIModule FindModule(string name)
        {
            return UIModules.FirstOrDefault(s => string.Equals(name, s.Name, StringComparison.InvariantCultureIgnoreCase));
        }

        public bool HasModule(IUIModule module)
        {
            return UIModules.Exists(m => m == module);
        }

        public void AddModule(IUIModule module)
        {
            if(module != null)
                UIModules.Add(module);
        }

        public bool RemoveModule(IUIModule module)
        {
            return UIModules.Remove(module);
        }

        public int RemoveModule(string name)
        {
            return UIModules.RemoveAll(m => string.Equals(m.Name, name, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Orders modules by their OrderInUI value
        /// </summary>
        public void OrderModules()
        {
            UIModules = UIModules.OrderBy(m => m.OrderInUI).ToList();
            UIModules.ForEach(x => x.OrderChildren());
            OrphanHolder.OrderChildren();
        }

        public static implicit operator bool(MainUI ui)
        {
            return !ReferenceEquals(ui, null);
        }
    }
}
#endif