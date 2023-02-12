using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Game : PersistableObject
{
    const int saveVersion = 6;

    public static Game Instance { get; private set; }

    // Randomness
    Random.State mainRandomState;
    [SerializeField] bool reseedOnLoad;

    [SerializeField] ShapeFactory[] shapeFactories;
    
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

    [SerializeField] Slider creationSpeedSlider;
    [SerializeField] Slider destructionSpeedSlider;

    // Levels
    [SerializeField] int levelCount;
    private int loadedLevelBuildIndex;

    private void OnEnable()
    {
        Instance = this;

        if(shapeFactories[0].FactoryId != 0)
        {
            for(int i = 0; i < shapeFactories.Length; i++)
            {
                shapeFactories[i].FactoryId = i;
            }
        }
        
    }

    private void Start()
    {
        mainRandomState = Random.state;

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

        BeginNewGame();
        StartCoroutine(LoadLevel(1));
    }

    private void Update()
    {

        if (Input.GetKeyDown(createKey))
        {
            GameLevel.Current.SpawnShapes();
        }   

        if (Input.GetKeyDown(newGameKey))
        {
            BeginNewGame();
            StartCoroutine(LoadLevel(loadedLevelBuildIndex));
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

  
    }

    private void FixedUpdate()
    {
        // Update every shape in 1 fixedUpdate cycle -> Should be better performance
        // also doing this before spawning new shapes keeps the behavior consistent with older versions
        for(int i = 0; i < shapes.Count; i++)
        {
            shapes[i].GameUpdate();
        }

        creationProgress += CreationSpeed * Time.deltaTime;
        while (creationProgress >= 1f)
        {
            creationProgress -= 1f;
            GameLevel.Current.SpawnShapes();
        }

        destructionProgress += DestructionSpeed * Time.deltaTime;
        while (destructionProgress >= 1f)
        {
            destructionProgress -= 1f;
            DestroyShape();
        }
    }

    public void AddShape(Shape shape)
    {
        shapes.Add(shape);
    }

    private void DestroyShape()
    {
        if(shapes.Count > 0)
        {
            int index = Random.Range(0, shapes.Count);
            shapes[index].Recycle();

            // Removing the object frome list by putting it last then removing - no gaps in list -
            int lastIndex = shapes.Count - 1;
            shapes[index] = shapes[lastIndex];
            shapes.RemoveAt(lastIndex);
        }
    }

    private void BeginNewGame()
    {
        // Randomising games
        Random.state = mainRandomState;
        int seed = Random.Range(0, int.MaxValue) ^ (int)Time.unscaledTime;
        mainRandomState = Random.state;
        Random.InitState(seed);

        creationSpeedSlider.value = CreationSpeed = 0;
        destructionSpeedSlider.value = DestructionSpeed = 0;

        for(int i = 0; i < shapes.Count; i++)
        {
            shapes[i].Recycle();
        }
        shapes.Clear();
    }

    public override void Save(GameDataWriter writer)
    {
        writer.Write(shapes.Count);
        writer.Write(Random.state);
        writer.Write(CreationSpeed);
        writer.Write(creationProgress);
        writer.Write(DestructionSpeed);
        writer.Write(destructionProgress);
        writer.Write(loadedLevelBuildIndex);
        GameLevel.Current.Save(writer);
        for(int i = 0; i < shapes.Count; i++)
        {
            writer.Write(shapes[i].OriginFactory.FactoryId);
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

        StartCoroutine(LoadGame(reader));
    }
    
    IEnumerator LoadGame (GameDataReader reader)
    {
        int version = reader.Version;

        // if it is an old savefile, version will be negative (reading old count value)
        // if this is the case "version" is actually object count of the old save, use that
        int count = version <= 0 ? -version : reader.ReadInt();

        if(version >= 3)
        {
            Random.State state = reader.ReadRandomState();
            if (!reseedOnLoad)
            {
                Random.state = state;
            }
            creationSpeedSlider.value = CreationSpeed = reader.ReadFloat();
            creationProgress = reader.ReadFloat();
            destructionSpeedSlider.value = DestructionSpeed = reader.ReadFloat();
            destructionProgress = reader.ReadFloat();
        }

        yield return LoadLevel(version < 2 ? 1 : reader.ReadInt());
        if(version >= 3)
        {
            GameLevel.Current.Load(reader);
        }

        for(int i = 0; i < count; i++)
        {
            int factoryId = version >= 5 ? reader.ReadInt() : 0;
            // if old save file get cubes(0) else get shapeId
            int shapeId = version > 0 ? reader.ReadInt() : 0;
            int materialId = version > 0 ? reader.ReadInt() : 0;
            Shape instance = shapeFactories[factoryId].Get(shapeId, materialId);
            instance.Load(reader);
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
