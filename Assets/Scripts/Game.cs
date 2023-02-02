using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Game : PersistableObject
{
    const int saveVersion = 2;

    [SerializeField] ShapeFactory shapeFactory;
    
    // Spawn 
    public SpawnZone SpawnZoneOfLevel { get; set; }
    public static Game Instance { get; private set; }

    // Keys
    [SerializeField] KeyCode createKey = KeyCode.C;
    [SerializeField] KeyCode newGameKey = KeyCode.N;
    [SerializeField] KeyCode saveKey = KeyCode.S;
    [SerializeField] KeyCode loadKey = KeyCode.L;
    [SerializeField] KeyCode destroyKey = KeyCode.X;

    // Memory
    List<Shape> shapes;
    [SerializeField] PersistantStorage storage;

    // GUI
    public float CreationSpeed { get; set; }
    float creationProgress;
    public float DestructionSpeed { get; set; }
    float destructionProgress;

    // Levels
    [SerializeField] int levelCount;
    private int loadedLevelBuildIndex;

    private void OnEnable()
    {
        Instance = this;
    }

    private void Start()
    {

        shapes = new List<Shape>();

        // in builds this won't be necessary cause there won't be a level already loaded
        if (Application.isEditor)
        {
            // While working on the game, you already have the level open in editor,
            // we don't want it load itself in playmode while there is a level already active in hierarchy
            for(int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene loadedScene = SceneManager.GetSceneAt(i);
                if(loadedScene.name.Contains("Level "))
                {
                    SceneManager.SetActiveScene(loadedScene);
                    loadedLevelBuildIndex = loadedScene.buildIndex;
                    return;
                }
            }
        }

        StartCoroutine(LoadLevel(1));
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
        
        for(int i = 1; i <= levelCount; i++)
        {
            if(Input.GetKeyDown(KeyCode.Alpha0 + i))
            {
                BeginNewGame();
                StartCoroutine(LoadLevel(i));
                return;
            }
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
        t.localPosition = SpawnZoneOfLevel.SpawnPoint;
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
        writer.Write(loadedLevelBuildIndex);
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

        StartCoroutine(LoadLevel(version < 2 ? 1 : reader.ReadInt()));

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

    // While doing all this loading you would show a loading screen normally
    private IEnumerator LoadLevel(int levelBuildIndex)
    {
        // loading asyncronously means game won't freeze while loading scene but,
        // Update() still is active, acquiring player inputs,
        // to prevent this game needs to disable itself before beginning the loading and enable after loadingprocess 
        enabled = false;

        // if a level is already loaded while trying for a new one, unload it
        if(loadedLevelBuildIndex > 0)
        {
            yield return SceneManager.UnloadSceneAsync(loadedLevelBuildIndex);
        }

        // wait for the scene to load (regular yield skips 1 frame exactly) so we can find and set the scene active
        yield return SceneManager.LoadSceneAsync(levelBuildIndex, LoadSceneMode.Additive);
        SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(levelBuildIndex));

        loadedLevelBuildIndex = levelBuildIndex;

        enabled = true;
    }
}
