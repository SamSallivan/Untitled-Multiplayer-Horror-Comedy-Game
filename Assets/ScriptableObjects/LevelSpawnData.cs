using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelSpawnData", menuName = "ScriptableObjects/LevelSpawnData")]
public class LevelSpawnData : ScriptableObject
{
    public enum MonsterType
    {
        NightCrawler,
    }
    [System.Serializable]
    public struct SpawnData
    {
        public MonsterType mType;
        public int spawnLocationIndex;
        public float spawnTime;

    }

    public List<GameObject> monsters;
    public List<SpawnData> spawnData;
}
