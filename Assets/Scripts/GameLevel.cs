using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameLevel : PersistableObject
{
	[SerializeField]
	SpawnZone spawnZone;

    [SerializeField] int populationLimit;

    public int PopolationLimit
    {
        get
        {
            return populationLimit;
        }
    }

    // Before namechange levelobjects were persistentobjects
    [UnityEngine.Serialization.FormerlySerializedAs("persistentObjects")]
	[SerializeField] GameLevelObject[] levelObjects;
	public static GameLevel Current { get; private set; }


    private void OnEnable()
    {
		Current = this;    
		if(levelObjects == null)
        {
			levelObjects = new GameLevelObject[0];
        }
    }

    public void GameUpdate()
    {
        for(int i = 0; i < levelObjects.Length; i++)
        {
            levelObjects[i].GameUpdate();
        }
    }


	public void SpawnShapes()
    {
        spawnZone.SpawnShapes();
    }


    public override void Save(GameDataWriter writer)
    {
		writer.Write(levelObjects.Length);
		for(int i = 0; i < levelObjects.Length; i++)
        {
			levelObjects[i].Save(writer);
        }
    }

    public override void Load(GameDataReader reader)
    {
		int savedCount = reader.ReadInt();
		for(int i = 0; i < savedCount; i++)
        {
			levelObjects[i].Load(reader);
        }
    }
}
