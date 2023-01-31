using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shape : PersistableObject
{
    // Using a property to set value once then make this field readonly
    public int ShapeId {
        get { 
            return shapeId; 
        }
        
        set {
            if(shapeId == int.MinValue && value != int.MinValue)
            {
                shapeId = value;
            }
            else
            {
                Debug.Log("Not allowed to change shapeId.");
            }
        }
    }

    int shapeId = int.MinValue;
}
