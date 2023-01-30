using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Game : PersistableObject
{
    public PersistableObject prefab;

    // Keys
    public KeyCode createKey = KeyCode.C;
    public KeyCode newGameKey = KeyCode.N;
    public KeyCode saveKey = KeyCode.S;
    public KeyCode loadKey = KeyCode.L;

    // Memory
    List<PersistableObject> objects;
    public PersistantStorage storage;

    private void Awake()
    {
        objects = new List<PersistableObject>();
    }

    private void Update()
    {

        if (Input.GetKeyDown(createKey))
        {
            CreateObject();
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

    private void CreateObject()
    {
        PersistableObject o = Instantiate(prefab);
        Transform t = o.transform;
        t.localPosition = Random.insideUnitSphere * 5f;
        t.localRotation = Random.rotation;
        t.localScale = Vector3.one * Random.Range(0.1f, 1f);

        objects.Add(o);
    }

    private void BeginNewGame()
    {
        for(int i = 0; i < objects.Count; i++)
        {
            Destroy(objects[i].gameObject);
        }
        objects.Clear();
    }
}
