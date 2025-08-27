using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NervBox.GameMode
{
    public class GamemodeInfo : MonoBehaviour
    {
        public int numTeams => spawnPoints.Count;

        [Serializable]
        public struct TeamSpawn
        {
            public int team;
            public Transform spawn;
        }

        [Serializable]
        public struct SpawnList
        {
            public int count => spawns.Count;
            public List<Transform> spawns;
        }

        [SerializeField] public Transform ammoStoreSpawnPoint;
        [SerializeField] public List<SpawnList> spawnPoints = new();
        [SerializeField] public List<TeamSpawn> keyObjectSpawns = new();
        public List<Transform> enemySpawnPoints = new();

        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (spawnPoints != null)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
                var gameObject = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Packages/com.quantumlionlabs.nervwaresdk/Editor/Resources/NervManLowPoly.prefab");
                var meshes = gameObject.GetComponentsInChildren<MeshFilter>();
                for (int i = 0; i < spawnPoints.Count; i++)
                {
                    var curr = spawnPoints[i];
                    for (int j = 0; j < curr.spawns.Count; j++)
                    {
                        if (curr.spawns[j] == null) continue;
                        Gizmos.matrix = curr.spawns[j].localToWorldMatrix * Matrix4x4.Rotate(Quaternion.Euler(90.0f, 0.0f, 0.0f));
                        foreach (var meshFilter in meshes)
                        {
                            Gizmos.DrawMesh(meshFilter.sharedMesh);
                        }
                    }
                }
            }

            if (keyObjectSpawns != null)
            {
                Gizmos.color = new Color(0f, 0f, 1f, 0.5f);
                for (int i = 0; i < keyObjectSpawns.Count; i++)
                {
                    var curr = keyObjectSpawns[i];
                    if (curr.spawn == null) continue;
                    Gizmos.DrawSphere(curr.spawn.position, 0.2f);
                }
            }

            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            foreach (var enemy in enemySpawnPoints)
            {
                if (enemy == null) continue;
                Gizmos.DrawSphere(enemy.position, 0.2f);
            }
        }
        #endif
    }
}
