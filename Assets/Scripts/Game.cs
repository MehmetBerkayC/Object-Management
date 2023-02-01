using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Game : PersistableObject
{
    const int saveVersion = 1;

    public ShapeFactory shapeFactory;

    // Keys
    public KeyCode createKey = KeyCode.C;
    public KeyCode newGameKey = KeyCode.N;
    public KeyCode saveKey = KeyCode.S;
    public KeyCode loadKey = KeyCode.L;
    public KeyCode destroyKey = KeyCode.X;

    // Memory
    List<Shape> shapes;
    public PersistantStorage storage;

    // GUI
    public float CreationSpeed { get; set; }
    float creationProgress;
    public float DestructionSpeed { get; set; }
    float destructionProgress;

    private void Awake()
    {
        shapes = new List<Shape>();
        StartCoroutine(LoadLevel());
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
            storage.Save(this, saveVersion);
        }
        
        if (Input.GetKeyDown(loadKey))
        {
            BeginNewGame();
            storage.Load(this);
        }
        
        if (Input.GetKeyDown(destroyKey))
        {
            DestroyShape();
        }

        creationProgress += CreationSpeed * Time.deltaTime;
        while(creationProgress >= 1f)
        {
            creationProgress -= 1f;
            CreateShape();
        }

        destructionProgress += DestructionSpeed * Time.deltaTime;
        while (destructionProgress >= 1f)
        {
            destructionProgress -= 1f;
            DestroyShape();
        }
    }

    private void CreateShape()
    {
        Shape instance = shapeFactory.GetRandom();
        Transform t = instance.transform;
        t.localPosition = Random.insideUnitSphere * 5f;
        t.localRotation = Random.rotation;
        t.localScale = Vector3.one * Random.Range(0.1f, 1f);

        instance.SetColor(Random.ColorHSV(
            hueMin: 0f, hueMax: 1f,
            saturationMin: 0.5f, saturationMax: 1f,
            valueMin: 0.25f, valueMax: 1f,
            alphaMin: 1f, alphaMax: 1f
            ));

        shapes.Add(instance);
    }

    private void DestroyShape()
    {
        if(shapes.Count > 0)
        {
            int index = Random.Range(0, shapes.Count);
            shapeFactory.Reclaim(shapes[index]);

            // Removing the object frome list by putting it last then removing - no gaps in list -
            int lastIndex = shapes.Count - 1;
            shapes[index] = shapes[lastIndex];
            shapes.RemoveAt(lastIndex);
        }
    }

    private void BeginNewGame()
    {
        for(int i = 0; i < shapes.Count; i++)
        {
            shapeFactory.Reclaim(shapes[i]);
        }
        shapes.Clear();
    }

    public override void Save(GameDataWriter writer)
    {
        writer.Write(shapes.Count);
        for(int i = 0; i < shapes.Count; i++)
        {
            writer.Write(shapes[i].ShapeId);
            writer.Write(shapes[i].MaterialId);
            shapes[i].Save(writer);
        }
    }

    public override void Load(GameDataReader reader)
    {
        int version = reader.Version;
        
        if (version > saveVersion)
        {
            Debug.LogError("Unsupported future save version " + version);
            return;
        }

        // if it is an old savefile, version will be negative (reading old count value)
        // if this is the case "version" is actually object count of the old save, use that
        int count = version <= 0 ? -version : reader.ReadInt();

        for(int i = 0; i < count; i++)
        {
            // if old save file get cubes(0) else get shapeId
            int shapeId = version > 0 ? reader.ReadInt() : 0;
            int materialId = version > 0 ? reader.ReadInt() : 0;
            Shape instance = shapeFactory.Get(shapeId, materialId);
            instance.Load(reader);
            shapes.Add(instance);
        }
    }

    private IEnumerator LoadLevel()
    {
        SceneManager.LoadScene("Level 1", LoadSceneMode.Additive);
        // wait for the next frame which the scene gets loaded (1frame)
        yield return null;
        SceneManager.SetActiveScene(SceneManager.GetSceneByName("Level 1"));
    }
}
