using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Game : PersistableObject
{
    const int saveVersion = 7;

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
    List<ShapeInstance> killList, markAsDyingList;
    int dyingShapeCount;

    [SerializeField] PersistantStorage storage;

    [SerializeField] float destroyDuration;

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

    bool inGameUpdateLoop;

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
        killList = new List<ShapeInstance>();
        markAsDyingList = new List<ShapeInstance>();

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
        // This bool is needed because the Kill method can interfere with this loop
        inGameUpdateLoop = true;
        // Update every shape in 1 fixedUpdate cycle -> Should be better performance
        // also doing this before spawning new shapes keeps the behavior consistent with older versions
        for(int i = 0; i < shapes.Count; i++)
        {
            shapes[i].GameUpdate();
        }

        inGameUpdateLoop = false;

        creationProgress += CreationSpeed * Time.deltaTime;
        while (creationProgress >= 1f)
        {
            creationProgress -= 1f;
            GameLevel.Current.GameUpdate();
        }

        destructionProgress += DestructionSpeed * Time.deltaTime;
        while (destructionProgress >= 1f)
        {
            destructionProgress -= 1f;
            DestroyShape();
        }

        // Limiting Shape Amount
        int limit = GameLevel.Current.PopolationLimit;
        if(limit > 0)
        {
            while(shapes.Count - dyingShapeCount > limit)
            {
                DestroyShape();
            }
        }

        if(killList.Count > 0)
        {
            for(int i = 0; i < killList.Count; i++)
            {
                if (killList[i].IsValid)
                {
                    KillImmediately(killList[i].Shape);
                }
            }
            killList.Clear();
        }

        if(markAsDyingList.Count > 0)
        {
            for(int i = 0; i < markAsDyingList.Count; i++)
            {
                if (markAsDyingList[i].IsValid)
                {
                    MarkAsDyingImmediately(markAsDyingList[i].Shape);
                }
            }
            markAsDyingList.Clear();
        }
    }

    public void AddShape(Shape shape)
    {
        shape.SaveIndex = shapes.Count;
        shapes.Add(shape);
    }

    public Shape GetShape(int index)
    {
        return shapes[index];
    }

    private void DestroyShape()
    {
        if(shapes.Count - dyingShapeCount > 0)
        {
            Shape shape = shapes[Random.Range(dyingShapeCount, shapes.Count)];
           if(destroyDuration > 0)
            {
                KillImmediately(shape);
            }
            else
            {
                shape.AddBehavior<DyingShapeBehavior>().Initialize(shape, destroyDuration);
            }
        }
    }

    public bool IsMarkedAsDying(Shape shape)
    {
        return shape.SaveIndex < dyingShapeCount;
    }

    public void MarkAsDying(Shape shape)
    {
        if (inGameUpdateLoop)
        {
            markAsDyingList.Add(shape);
        }
        else
        {
            MarkAsDyingImmediately(shape);
        }
    }

    private void MarkAsDyingImmediately(Shape shape)
    {
        int index = shape.SaveIndex;
        if(index < dyingShapeCount) // this shape already marked dying
        {
            return;
        }
        shapes[dyingShapeCount].SaveIndex = index;
        shapes[index] = shapes[dyingShapeCount];
        shape.SaveIndex = dyingShapeCount;
        shapes[dyingShapeCount++] = shape;
    }

    public void Kill(Shape shape)
    {
        if (inGameUpdateLoop)
        {
            killList.Add(shape);
        }
        else
        {
            KillImmediately(shape);
        } 
    }

    private void KillImmediately(Shape shape)
    {
        // Removing the object frome list by putting it last then removing - no gaps in list -
        int index = shape.SaveIndex;
        shape.Recycle();

        if(index < dyingShapeCount && index < --dyingShapeCount)
        {
            shapes[dyingShapeCount].SaveIndex = index;
            shapes[index] = shapes[dyingShapeCount];
            index = dyingShapeCount;
        }

        int lastIndex = shapes.Count - 1;

        // if there is at least a single non-dying shape move it to the end
        // if not, the shape is already at the end of the list -no moving needed-
        if (index < lastIndex) 
        {
            shapes[lastIndex].SaveIndex = index;
            shapes[index] = shapes[lastIndex];
        }
        shapes.RemoveAt(lastIndex);
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

        dyingShapeCount = 0;
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

        for(int i = 0; i < shapes.Count; i++)
        {
            shapes[i].ResolveShapeInstances();
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
