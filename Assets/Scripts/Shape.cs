using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shape : PersistableObject
{
    [SerializeField]
    MeshRenderer[] meshRenderers;

    int shapeId = int.MinValue;

    // Using a property to set value once then make this field readonly
    public int ShapeId
    {
        get
        {
            return shapeId;
        }

        set
        {
            if (shapeId == int.MinValue && value != int.MinValue)
            {
                shapeId = value;
            }
            else
            {
                Debug.Log("Not allowed to change shapeId.");
            }
        }
    }

    // Which Factory made this shape
    ShapeFactory originFactory;
    
    public ShapeFactory OriginFactory
    {
        get
        {
            return originFactory;
        }
        set
        {
            if (originFactory == null)
            {
                originFactory = value;
            }
            else
            {
                Debug.LogError("Not allowed to change origin factory.");
            }
        }
    }
    
    public void Recycle()
    {
        // When recycling behaviours get added again, to remove duplicates
        for(int i = 0; i< behaviorList.Count; i++)
        {
            Destroy(behaviorList[i]);
        }
        behaviorList.Clear();

        OriginFactory.Reclaim(this);
    }

    // Material
    public int MaterialId { get; private set; }

    // Color
    Color[] colors;
    static int colorPropertyId = Shader.PropertyToID("_Color");
    static MaterialPropertyBlock sharedPropertyBlock;

    // Movement
    public Vector3 AngularVelocity { get; set; }
    public Vector3 Velocity { get; set; }

    // Behavior List
    List<ShapeBehavior> behaviorList = new List<ShapeBehavior>();

    private void Awake()
    {
        colors = new Color[meshRenderers.Length];
    }

    public void GameUpdate()
    {
        for(int i = 0; i < behaviorList.Count; i++)
        {
            behaviorList[i].GameUpdate(this);
        }
    }

    // Check SpawnZone for usage
    public T AddBehavior<T> () where T : ShapeBehavior
    {
        T behavior = gameObject.AddComponent<T>();
        behaviorList.Add(behavior);
        return behavior;
    }

    public void SetMaterial(Material material, int materialId) {
        
        for(int i = 0; i < meshRenderers.Length; i++)
        {
            meshRenderers[i].material = material;
        }
        MaterialId = materialId;
    }

    public void SetColor (Color color)
    {

        if (sharedPropertyBlock == null)
        {
            sharedPropertyBlock = new MaterialPropertyBlock();
        }

        sharedPropertyBlock.SetColor(colorPropertyId, color);

        for (int i = 0; i < meshRenderers.Length; i++)
        {
            colors[i] = color;
            meshRenderers[i].SetPropertyBlock(sharedPropertyBlock);
        }
    }

    // Property Block
    public int ColorCount
    {
        get
        {
            return colors.Length;
        }
    }
    public void SetColor(Color color, int index)
    {
        if(sharedPropertyBlock == null)
        {
            sharedPropertyBlock = new MaterialPropertyBlock();
        }

        sharedPropertyBlock.SetColor(colorPropertyId, color);
        colors[index] = color;
        meshRenderers[index].SetPropertyBlock(sharedPropertyBlock);
    }

    public override void Save(GameDataWriter writer)
    {
        base.Save(writer);

        writer.Write(colors.Length);

        for(int i = 0; i < colors.Length; i++)
        {
            writer.Write(colors[i]);
        }
        
        writer.Write(AngularVelocity);
        writer.Write(Velocity);
    }

    public override void Load(GameDataReader reader)
    {
        base.Load(reader);
        
        if(reader.Version >= 5)
        {
            LoadColors(reader);
        }
        else
        {
            SetColor(reader.Version > 0 ? reader.ReadColor() : Color.white);
        }

        AngularVelocity = reader.Version >= 4 ? reader.ReadVector3() : Vector3.zero;
        Velocity = reader.Version >= 4 ? reader.ReadVector3() : Vector3.zero;
    }

    private void LoadColors(GameDataReader reader)
    {
        int count = reader.ReadInt();
        
        int max = count <= colors.Length ? count : colors.Length;
        int i = 0;
        
        // No problems here
        for (; i < max; i++)
        {
            SetColor(reader.ReadColor(), i);
        }

        // If stored more colors than we currently need, we must read the rest of them
        if (count > colors.Length)
        {
            for(; i < count; i++)
            {
                reader.ReadColor();
            }
        }
        // If stored less colors than we need, need colors, must give a color
        else if (count < colors.Length)
        {
            for(; i < colors.Length; i++)
            {
                SetColor(Color.white, i);
            }
        }
    }
}
