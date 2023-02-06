using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shape : PersistableObject
{
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

    Color color;

    public int MaterialId { get; private set; }

    MeshRenderer meshRenderer;

    static int colorPropertyId = Shader.PropertyToID("_Color");
    static MaterialPropertyBlock sharedPropertyBlock;

    public Vector3 AngularVelocity { get; set; }
    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void GameUpdate()
    {
        transform.Rotate(Vector3.forward * 50f * Time.deltaTime);
    }


    public void SetMaterial(Material material, int materialId) {
        meshRenderer.material = material;
        MaterialId = materialId;
    }

    public void SetColor (Color color)
    {
        this.color = color;

        if (sharedPropertyBlock == null)
        {
            sharedPropertyBlock = new MaterialPropertyBlock();
        }
        sharedPropertyBlock.SetColor(colorPropertyId, color);
        meshRenderer.SetPropertyBlock(sharedPropertyBlock);
    }

    public override void Save(GameDataWriter writer)
    {
        base.Save(writer);
        writer.Write(color);
        writer.Write(AngularVelocity);
    }

    public override void Load(GameDataReader reader)
    {
        base.Load(reader);
        SetColor(reader.Version > 0 ? reader.ReadColor() : Color.white);
        AngularVelocity = reader.Version >= 4 ? reader.ReadVector3() : Vector3.zero;
    }
}
