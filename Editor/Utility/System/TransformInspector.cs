﻿using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Events;

namespace HT.Framework
{
    [CustomEditor(typeof(Transform))]
    internal sealed class TransformInspector : HTFEditor<Transform>
    {
        private static Page _currentPage = Page.Property;
        private static bool _copyQuaternion = false;
        
        private bool _onlyShowLocal = false;
        private AnimBool _showProperty;
        private AnimBool _showHierarchy;
        private AnimBool _showCopy;
        private GUIContent _propertyGC;
        private GUIContent _hierarchyGC;
        private GUIContent _copyGC;
        private Transform _parent;
        private string _lockSource;
        private bool _isLock = false;
        private bool _isLockPosition = false;
        private bool _isLockRotation = false;
        private bool _isLockScale = false;

        protected override bool IsEnableRuntimeData => false;

        protected override void OnDefaultEnable()
        {
            base.OnDefaultEnable();

            _onlyShowLocal = EditorPrefs.GetBool(EditorPrefsTable.Transform_OnlyShowLocal, false);
            _showProperty = new AnimBool(false, new UnityAction(Repaint));
            _showHierarchy = new AnimBool(false, new UnityAction(Repaint));
            _showCopy = new AnimBool(false, new UnityAction(Repaint));
            _propertyGC = new GUIContent();
            _propertyGC.image = EditorGUIUtility.IconContent("ToolHandleLocal").image;
            _propertyGC.text = "Property";
            _hierarchyGC = new GUIContent();
            _hierarchyGC.image = EditorGUIUtility.IconContent("ToolHandlePivot").image;
            _hierarchyGC.text = "Hierarchy";
            _copyGC = new GUIContent();
            _copyGC.image = EditorGUIUtility.IconContent("ToolHandleCenter").image;
            _copyGC.text = "Copy";

            SetLockState();
        }
        protected override void OnDefaultDisable()
        {
            base.OnDefaultDisable();

            Tools.hidden = false;
        }
        protected override void OnInspectorDefaultGUI()
        {
            base.OnInspectorDefaultGUI();

            GUILayout.Space(5);

            #region Property
            GUILayout.BeginVertical(GetBoxStyle(Page.Property));

            GUILayout.BeginHorizontal();
            GUILayout.Space(12);
            bool oldValue = _currentPage == Page.Property;
            _showProperty.target = EditorGUILayout.Foldout(oldValue, _propertyGC, true);
            if (_showProperty.target != oldValue)
            {
                if (_showProperty.target) _currentPage = Page.Property;
                else _currentPage = Page.None;
            }
            GUILayout.EndHorizontal();

            if (EditorGUILayout.BeginFadeGroup(_showProperty.faded))
            {
                if (_isLock)
                {
                    EditorGUILayout.HelpBox(_lockSource, MessageType.None);
                }
                
                if (!_onlyShowLocal)
                {
                    GUI.enabled = !_isLockPosition;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("P", GUILayout.Width(20));
                    EditorGUI.BeginChangeCheck();
                    Vector3 pos = EditorGUILayout.Vector3Field("", Target.position);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(Target, "Move " + Target.name);
                        Target.position = pos;
                        HasChanged();
                    }
                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_editicon.sml"), "InvisibleButton", GUILayout.Width(20), GUILayout.Height(20)))
                    {
                        GenericMenu gm = new GenericMenu();
                        gm.AddItem(new GUIContent("Copy"), false, () =>
                        {
                            GUIUtility.systemCopyBuffer = Target.position.ToCopyString("F4");
                            Log.Info("已复制：" + GUIUtility.systemCopyBuffer);
                        });
                        gm.AddItem(new GUIContent("Paste"), false, () =>
                        {
                            if (!string.IsNullOrEmpty(GUIUtility.systemCopyBuffer))
                            {
                                Undo.RecordObject(Target, "Paste position value");
                                Target.position = GUIUtility.systemCopyBuffer.ToPasteVector3(Vector3.zero);
                                HasChanged();
                            }
                        });
                        gm.ShowAsContext();
                    }
                    GUILayout.EndHorizontal();

                    GUI.enabled = !_isLockRotation;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("R", GUILayout.Width(20));
                    EditorGUI.BeginChangeCheck();
                    Vector3 rot = EditorGUILayout.Vector3Field("", Target.rotation.eulerAngles);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(Target, "Rotate " + Target.name);
                        Target.rotation = Quaternion.Euler(rot);
                        HasChanged();
                    }
                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_editicon.sml"), "InvisibleButton", GUILayout.Width(20), GUILayout.Height(20)))
                    {
                        GenericMenu gm = new GenericMenu();
                        gm.AddItem(new GUIContent("Copy"), false, () =>
                        {
                            if (_copyQuaternion)
                            {
                                GUIUtility.systemCopyBuffer = Target.rotation.ToCopyString("F4");
                                Log.Info("已复制：" + GUIUtility.systemCopyBuffer);
                            }
                            else
                            {
                                string x = ClampAngle(Target.rotation.eulerAngles.x).ToString();
                                string y = ClampAngle(Target.rotation.eulerAngles.y).ToString();
                                string z = ClampAngle(Target.rotation.eulerAngles.z).ToString();

                                GUIUtility.systemCopyBuffer = x + "f," + y + "f," + z + "f";
                                Log.Info("已复制：" + GUIUtility.systemCopyBuffer);
                            }
                        });
                        gm.AddItem(new GUIContent("Paste"), false, () =>
                        {
                            if (!string.IsNullOrEmpty(GUIUtility.systemCopyBuffer))
                            {
                                if (_copyQuaternion)
                                {
                                    Undo.RecordObject(Target, "Paste rotation value");
                                    Target.rotation = GUIUtility.systemCopyBuffer.ToPasteQuaternion(Quaternion.identity);
                                    HasChanged();
                                }
                                else
                                {
                                    Undo.RecordObject(Target, "Paste rotation value");
                                    Target.rotation = GUIUtility.systemCopyBuffer.ToPasteVector3(Vector3.zero).ToQuaternion();
                                    HasChanged();
                                }
                            }
                        });
                        gm.ShowAsContext();
                    }
                    GUILayout.EndHorizontal();

                    GUI.enabled = !_isLockScale;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("S", GUILayout.Width(20));
                    GUI.enabled = false;
                    EditorGUILayout.Vector3Field("", Target.lossyScale);
                    GUILayout.Button(EditorGUIUtility.IconContent("d_editicon.sml"), "InvisibleButton", GUILayout.Width(20), GUILayout.Height(20));
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();

                    GUI.enabled = true;
                }

                GUI.enabled = !_isLockPosition;

                GUILayout.BeginHorizontal();
                GUILayout.Label("LP", GUILayout.Width(20));
                EditorGUI.BeginChangeCheck();
                Vector3 localpos = EditorGUILayout.Vector3Field("", Target.localPosition);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(Target, "Move " + Target.name);
                    Target.localPosition = localpos;
                    HasChanged();
                }
                if (GUILayout.Button(EditorGUIUtility.IconContent("d_editicon.sml"), "InvisibleButton", GUILayout.Width(20), GUILayout.Height(20)))
                {
                    GenericMenu gm = new GenericMenu();
                    gm.AddItem(new GUIContent("Copy"), false, () =>
                    {
                        GUIUtility.systemCopyBuffer = Target.localPosition.ToCopyString("F4");
                        Log.Info("已复制：" + GUIUtility.systemCopyBuffer);
                    });
                    gm.AddItem(new GUIContent("Paste"), false, () =>
                    {
                        if (!string.IsNullOrEmpty(GUIUtility.systemCopyBuffer))
                        {
                            Undo.RecordObject(Target, "Paste localPosition value");
                            Target.localPosition = GUIUtility.systemCopyBuffer.ToPasteVector3(Vector3.zero);
                            HasChanged();
                        }
                    });
                    gm.ShowAsContext();
                }
                GUILayout.EndHorizontal();

                GUI.enabled = !_isLockRotation;

                GUILayout.BeginHorizontal();
                GUILayout.Label("LR", GUILayout.Width(20));
                EditorGUI.BeginChangeCheck();
                Vector3 localrot = EditorGUILayout.Vector3Field("", Target.localRotation.eulerAngles);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(Target, "Rotate " + Target.name);
                    Target.localRotation = Quaternion.Euler(localrot);
                    HasChanged();
                }
                if (GUILayout.Button(EditorGUIUtility.IconContent("d_editicon.sml"), "InvisibleButton", GUILayout.Width(20), GUILayout.Height(20)))
                {
                    GenericMenu gm = new GenericMenu();
                    gm.AddItem(new GUIContent("Copy"), false, () =>
                    {
                        if (_copyQuaternion)
                        {
                            GUIUtility.systemCopyBuffer = Target.localRotation.ToCopyString("F4");
                            Log.Info("已复制：" + GUIUtility.systemCopyBuffer);
                        }
                        else
                        {
                            string x = ClampAngle(Target.localRotation.eulerAngles.x).ToString();
                            string y = ClampAngle(Target.localRotation.eulerAngles.y).ToString();
                            string z = ClampAngle(Target.localRotation.eulerAngles.z).ToString();

                            GUIUtility.systemCopyBuffer = x + "f," + y + "f," + z + "f";
                            Log.Info("已复制：" + GUIUtility.systemCopyBuffer);
                        }
                    });
                    gm.AddItem(new GUIContent("Paste"), false, () =>
                    {
                        if (!string.IsNullOrEmpty(GUIUtility.systemCopyBuffer))
                        {
                            if (_copyQuaternion)
                            {
                                Undo.RecordObject(Target, "Paste localRotation value");
                                Target.localRotation = GUIUtility.systemCopyBuffer.ToPasteQuaternion(Quaternion.identity);
                                HasChanged();
                            }
                            else
                            {
                                Undo.RecordObject(Target, "Paste localRotation value");
                                Target.localRotation = GUIUtility.systemCopyBuffer.ToPasteVector3(Vector3.zero).ToQuaternion();
                                HasChanged();
                            }
                        }
                    });
                    gm.ShowAsContext();
                }
                GUILayout.EndHorizontal();

                GUI.enabled = !_isLockScale;

                GUILayout.BeginHorizontal();
                GUILayout.Label("LS", GUILayout.Width(20));
                EditorGUI.BeginChangeCheck();
                Vector3 localsca = EditorGUILayout.Vector3Field("", Target.localScale);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(Target, "Scale " + Target.name);
                    Target.localScale = localsca;
                    HasChanged();
                }
                if (GUILayout.Button(EditorGUIUtility.IconContent("d_editicon.sml"), "InvisibleButton", GUILayout.Width(20), GUILayout.Height(20)))
                {
                    GenericMenu gm = new GenericMenu();
                    gm.AddItem(new GUIContent("Copy"), false, () =>
                    {
                        GUIUtility.systemCopyBuffer = Target.localScale.ToCopyString("F4");
                        Log.Info("已复制：" + GUIUtility.systemCopyBuffer);
                    });
                    gm.AddItem(new GUIContent("Paste"), false, () =>
                    {
                        if (!string.IsNullOrEmpty(GUIUtility.systemCopyBuffer))
                        {
                            Undo.RecordObject(Target, "Paste localScale value");
                            Target.localScale = GUIUtility.systemCopyBuffer.ToPasteVector3(Vector3.one);
                            HasChanged();
                        }
                    });
                    gm.ShowAsContext();
                }
                GUILayout.EndHorizontal();

                GUI.enabled = true;
            }
            EditorGUILayout.EndFadeGroup();

            GUILayout.EndVertical();
            #endregion

            #region Hierarchy
            GUILayout.BeginVertical(GetBoxStyle(Page.Hierarchy));

            GUILayout.BeginHorizontal();
            GUILayout.Space(12);
            oldValue = _currentPage == Page.Hierarchy;
            _showHierarchy.target = EditorGUILayout.Foldout(oldValue, _hierarchyGC, true);
            if (_showHierarchy.target != oldValue)
            {
                if (_showHierarchy.target) _currentPage = Page.Hierarchy;
                else _currentPage = Page.None;
            }
            GUILayout.EndHorizontal();

            if (EditorGUILayout.BeginFadeGroup(_showHierarchy.faded))
            {
                if (_isLock)
                {
                    EditorGUILayout.HelpBox(_lockSource, MessageType.None);
                }
                
                GUI.enabled = !_isLock;

                GUILayout.BeginHorizontal();
                GUILayout.Label("Root", GUILayout.Width(LabelWidth));
                EditorGUILayout.ObjectField(Target.root, typeof(Transform), true);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Parent", GUILayout.Width(LabelWidth));
                GUI.color = Target.parent ? Color.white : Color.gray;
                _parent = EditorGUILayout.ObjectField(Target.parent, typeof(Transform), true) as Transform;
                if (_parent != Target.parent)
                {
                    Undo.RecordObject(Target, "Change Parent " + Target.name);
                    Target.SetParent(_parent);
                    HasChanged();
                }
                GUI.color = Color.white;
                GUILayout.EndHorizontal();

                GUI.enabled = !_isLock && Target.childCount > 0;

                GUILayout.BeginHorizontal();
                GUILayout.Label("Child Count", GUILayout.Width(LabelWidth));
                GUILayout.Label(Target.childCount.ToString());
                GUILayout.FlexibleSpace();
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Detach", EditorStyles.miniButton))
                {
                    if (EditorUtility.DisplayDialog("Prompt", "Are you sure you want to detach all children?", "Yes", "No"))
                    {
                        Undo.RecordObject(Target, "Detach Children");
                        Target.DetachChildren();
                        HasChanged();
                    }
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();

                GUI.backgroundColor = Color.yellow;

                GUI.enabled = !_isLock;
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Create Empty Parent", EditorStyles.miniButton))
                {
                    CreateEmptyParent();
                }
                GUILayout.EndHorizontal();

                GUI.enabled = true;

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Expand All Children", EditorStyles.miniButtonLeft))
                {
                    ExpandAllChildren();
                }
                if (GUILayout.Button("Collapse All Children", EditorStyles.miniButtonRight))
                {
                    CollapseAllChildren();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Collapse All", EditorStyles.miniButton))
                {
                    CollapseAll();
                }
                GUILayout.EndHorizontal();

                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndFadeGroup();

            GUILayout.EndVertical();
            #endregion

            #region Copy
            GUILayout.BeginVertical(GetBoxStyle(Page.Copy));

            GUILayout.BeginHorizontal();
            GUILayout.Space(12);
            oldValue = _currentPage == Page.Copy;
            _showCopy.target = EditorGUILayout.Foldout(oldValue, _copyGC, true);
            if (_showCopy.target != oldValue)
            {
                if (_showCopy.target) _currentPage = Page.Copy;
                else _currentPage = Page.None;
            }
            GUILayout.EndHorizontal();

            if (EditorGUILayout.BeginFadeGroup(_showCopy.faded))
            {
                GUI.backgroundColor = Color.yellow;

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Copy Name", EditorStyles.miniButtonLeft))
                {
                    GUIUtility.systemCopyBuffer = Target.name;
                    Log.Info("已复制：" + GUIUtility.systemCopyBuffer);
                }
                if (GUILayout.Button("Copy FullName", EditorStyles.miniButtonRight))
                {
                    GUIUtility.systemCopyBuffer = Target.FullName();
                    Log.Info("已复制：" + GUIUtility.systemCopyBuffer);
                }
                GUILayout.EndHorizontal();

                GUI.backgroundColor = Color.green;

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Copy To C# Public Field", EditorStyles.miniButton))
                {
                    GUIUtility.systemCopyBuffer = ToCSPublicField();
                    Log.Info("已复制：" + GUIUtility.systemCopyBuffer);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Copy To C# Private Field", EditorStyles.miniButton))
                {
                    GUIUtility.systemCopyBuffer = ToCSPrivateField();
                    Log.Info("已复制：" + GUIUtility.systemCopyBuffer);
                }
                GUILayout.EndHorizontal();

                GUI.backgroundColor = Color.white;

                GUILayout.BeginHorizontal();
                _copyQuaternion = GUILayout.Toggle(_copyQuaternion, "Copy Quaternion");
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndFadeGroup();

            GUILayout.EndVertical();
            #endregion
        }
        
        private void SetLockState()
        {
            HTBehaviour[] behaviours = Target.GetComponents<HTBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                Type type = behaviours[i].GetType();
                LockTransformAttribute attribute = type.GetCustomAttribute<LockTransformAttribute>();
                if (attribute != null)
                {
                    _lockSource = "Some values locking by " + type.Name + ".";
                    _isLock = true;
                    _isLockPosition = attribute.IsLockPosition;
                    _isLockRotation = attribute.IsLockRotation;
                    _isLockScale = attribute.IsLockScale;
                    Tools.hidden = true;
                    return;
                }
            }
            _lockSource = null;
            _isLock = false;
            _isLockPosition = false;
            _isLockRotation = false;
            _isLockScale = false;
            Tools.hidden = false;
        }
        private GUIStyle GetBoxStyle(Page page)
        {
            if (_currentPage == page)
                return "SelectionRect";
            else
                return "Box";
        }
        private void CreateEmptyParent()
        {
            GameObject parent = new GameObject("EmptyParent");
            parent.transform.SetParent(Target.parent);
            parent.transform.localPosition = Target.localPosition;
            parent.transform.localRotation = Quaternion.identity;
            parent.transform.localScale = Vector3.one;
            parent.transform.SetSiblingIndex(Target.GetSiblingIndex());
            Target.SetParent(parent.transform);
            Selection.activeGameObject = parent;
            EditorGUIUtility.PingObject(parent);
        }
        private void ExpandAllChildren()
        {
            Type type = EditorReflectionToolkit.GetTypeInEditorAssemblies("UnityEditor.SceneHierarchyWindow");
            EditorWindow window = EditorWindow.GetWindow(type);
            MethodInfo method = window.GetType().GetMethod("SetExpandedRecursive", BindingFlags.Public | BindingFlags.Instance);
            int id = Target.gameObject.GetInstanceID();
            method.Invoke(window, new object[] { id, true });
        }
        private void CollapseAllChildren()
        {
            Type type = EditorReflectionToolkit.GetTypeInEditorAssemblies("UnityEditor.SceneHierarchyWindow");
            EditorWindow window = EditorWindow.GetWindow(type);
            MethodInfo method = window.GetType().GetMethod("SetExpandedRecursive", BindingFlags.Public | BindingFlags.Instance);
            int id = Target.gameObject.GetInstanceID();
            method.Invoke(window, new object[] { id, false });
        }
        private void CollapseAll()
        {
            Type type = EditorReflectionToolkit.GetTypeInEditorAssemblies("UnityEditor.SceneHierarchyWindow");
            EditorWindow window = EditorWindow.GetWindow(type);
            object hierarchy = window.GetType().GetProperty("sceneHierarchy", BindingFlags.Public | BindingFlags.Instance).GetValue(window);
            int[] expandedIDs = hierarchy.GetType().GetMethod("GetExpandedIDs", BindingFlags.Public | BindingFlags.Instance).Invoke(hierarchy, null) as int[];
            MethodInfo method = hierarchy.GetType().GetMethod("ExpandTreeViewItem", BindingFlags.NonPublic | BindingFlags.Instance);
            object[] args = new object[2];
            args[1] = false;
            for (int i = 0; i < expandedIDs.Length; i++)
            {
                args[0] = expandedIDs[i];
                method.Invoke(hierarchy, args);
            }
        }
        private int ClampAngle(float angle)
        {
            if (angle > 180) angle -= 360;
            else if (angle < -180) angle += 360;

            return (int)angle;
        }
        private string ToCSPublicField()
        {
            string fieldName = Target.name.Trim().Replace(" ", "");
            string field = string.Format("[Label(\"{0}\")] public GameObject {1};", Target.name, fieldName);
            return field;
        }
        private string ToCSPrivateField()
        {
            string fieldName = Target.name.Trim().Replace(" ", "");
            char[] fieldNames = fieldName.ToCharArray();
            fieldNames[0] = char.ToLower(fieldNames[0]);
            string field = string.Format("[ObjectPath(\"{0}\")] private GameObject _{1};", Target.FullName(), new string(fieldNames));
            return field;
        }

        /// <summary>
        /// 分页
        /// </summary>
        private enum Page
        {
            None,
            Property,
            Hierarchy,
            Copy,
        }
    }
}