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

    public int MaterialId { get; private set; }
    public void SetMaterial(Material material, int materialId) {
        GetComponent<MeshRenderer>().material = material;
        MaterialId = materialId;
    }
}
