#if UNITY_EDITOR
using System;
using NervBox.Editor;
using NervBox.GameMode;
using UnityEditor;
using UnityEngine;

namespace NervWareSDK.Editor
{
    [CustomEditor(typeof(GamemodeInfo))]
    public class GamemodeInfoEditor : NervWareStyledEditor
    {
        private GamemodeInfo Info => target as GamemodeInfo;
        private SerializedProperty _spawnList;

        protected override void InitializeProperties()
        {
            _spawnList = serializedObject.FindProperty("spawnPoints");
        }

        protected override string GetInspectorName()
        {
            return "GAMEMODE INFO";
        }

        protected override void DrawInspector()
        {
            if (_spawnList.arraySize == 0)
            {
                _spawnList.InsertArrayElementAtIndex(0);
            }

            SerializedProperty first = _spawnList.GetArrayElementAtIndex(0);

            SerializedProperty innerList = first.FindPropertyRelative("spawns");
            
            DrawSection("SPAWN POINTS", () =>
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(innerList, new GUIContent("Spawn Points"), true);
                }
                if (GUILayout.Button("Add Spawn Point"))
                {
                    GameObject newSpawn = new GameObject("Spawn Point");
                    int size = innerList.arraySize;
                    innerList.InsertArrayElementAtIndex(size);
                    innerList.GetArrayElementAtIndex(size).objectReferenceValue = newSpawn.transform;
                }

                if (GUILayout.Button("Clear Spawn Points"))
                {
                    innerList.ClearArray();
                }
            });
        }

        private void OnSceneGUI()
        {
            if (Info.spawnPoints.Count == 0)
            {
                Info.spawnPoints.Add(new GamemodeInfo.SpawnList());
            }

            foreach (var transform in Info.spawnPoints[0].spawns)
            {
                //draw handles for each
                if (transform == null)
                {
                    continue;
                }
             
                var position = transform.position;
                var rotation = transform.rotation;
                EditorGUI.BeginChangeCheck();
                if (Tools.current == Tool.Move)
                {
                    position = Handles.PositionHandle(position, rotation);
                }
                else if (Tools.current == Tool.Rotate)
                {
                    rotation = Handles.RotationHandle(rotation, position);
                }
                

                var euler = rotation.eulerAngles;
                euler.x = 0.0f;
                euler.z = 0.0f;
                rotation = Quaternion.Euler(euler);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(transform, "Move Spawn Point");
                    transform.SetPositionAndRotation(position, rotation);
                }
            }
        }
    }
}
#endif