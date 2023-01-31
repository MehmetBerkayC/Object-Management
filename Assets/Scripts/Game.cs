using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Game : PersistableObject
{
    const int saveVersion = 1;

    public ShapeFactory shapeFactory;

    // Keys
    public KeyCode createKey = KeyCode.C;
    public KeyCode newGameKey = KeyCode.N;
    public KeyCode saveKey = KeyCode.S;
    public KeyCode loadKey = KeyCode.L;

    // Memory
    List<Shape> shapes;
    public PersistantStorage storage;

    private void Awake()
    {
        shapes = new List<Shape>();
    }

    private void Update()
    {

        if (Input.GetKeyDown(createKey))
        {
            CreateShape();
        }

        if (Input.GetKeyDown(newGameKey))
        {
            BeginNewGame();
        }

        if (Input.GetKeyDown(saveKey))
        {
            storage.Save(this);
        }
        
        if (Input.GetKeyDown(loadKey))
        {
            BeginNewGame();
            storage.Load(this);
        }
    }

    private void CreateShape()
    {
        Shape instance = shapeFactory.GetRandom();
        Transform t = instance.transform;
        t.localPosition = Random.insideUnitSphere * 5f;
        t.localRotation = Random.rotation;
        t.localScale = Vector3.one * Random.Range(0.1f, 1f);

        shapes.Add(instance);
    }

    private void BeginNewGame()
    {
        for(int i = 0; i < shapes.Count; i++)
        {
            Destroy(shapes[i].gameObject);
        }
        shapes.Clear();
    }

    public override void Save(GameDataWriter writer)
    {
        writer.Write(-saveVersion);
        writer.Write(shapes.Count);
        for(int i = 0; i < shapes.Count; i++)
        {
            shapes[i].Save(writer);
        }
    }

    public override void Load(GameDataReader reader)
    {
        int version = -reader.ReadInt();
        // if it is an old savefile, version will be negative (reading old count value)
        // if this is the case "version" is actually object count of the old save, use that
        int count = version <= 0 ? -version : reader.ReadInt();
        for(int i = 0; i < count; i++)
        {
            Shape instance = shapeFactory.Get(0);
            instance.Load(reader);
            shapes.Add(instance);
        }
    }
}
