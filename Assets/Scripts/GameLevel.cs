using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLevel : PersistableObject
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

	[SerializeField] PersistableObject[] persistentObjects;
	public static GameLevel Current { get; private set; }

    private void OnEnable()
    {
		Current = this;    
		if(persistentObjects == null)
        {
			persistentObjects = new PersistableObject[0];
        }
    }


	public void SpawnShapes()
    {
        spawnZone.SpawnShapes();
    }


    public override void Save(GameDataWriter writer)
    {
		writer.Write(persistentObjects.Length);
		for(int i = 0; i < persistentObjects.Length; i++)
        {
			persistentObjects[i].Save(writer);
        }
    }

    public override void Load(GameDataReader reader)
    {
		int savedCount = reader.ReadInt();
		for(int i = 0; i < savedCount; i++)
        {
			persistentObjects[i].Load(reader);
        }
    }
}
