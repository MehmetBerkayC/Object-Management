using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ShapeInstance
{
	public Shape Shape { get; private set; }

	int instanceIdOrSaveIndex;

	public bool IsValid
	{
		get
		{
			return Shape && instanceIdOrSaveIndex == Shape.InstanceId;
		}
	}
	
	public ShapeInstance(Shape shape)
	{
		Shape = shape;
		instanceIdOrSaveIndex = shape.InstanceId;
	}

	public ShapeInstance(int saveIndex)
	{
		Shape = null;
		instanceIdOrSaveIndex = saveIndex;
	}

	public void Resolve()
    {
		if(instanceIdOrSaveIndex >= 0)
        {
			Shape = Game.Instance.GetShape(instanceIdOrSaveIndex);
			instanceIdOrSaveIndex = Shape.InstanceId;
		}
    }

	public static implicit operator ShapeInstance(Shape shape)
	{
		return new ShapeInstance(shape);
	}

	
}


