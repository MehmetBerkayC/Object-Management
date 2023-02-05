using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLevel : PersistableObject
{
	[SerializeField]
	SpawnZone spawnZone;

	public static GameLevel Current { get; private set; }

    private void OnEnable()
    {
		Current = this;    
    }

	public Vector3 SpawnPoint
	{
		get
		{
			return spawnZone.SpawnPoint;
		}
	}


    public override void Save(GameDataWriter writer)
    {
		//
    }

    public override void Load(GameDataReader reader)
    {
		//
    }
}
