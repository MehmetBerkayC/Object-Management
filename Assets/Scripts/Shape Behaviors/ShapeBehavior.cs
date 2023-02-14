using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ShapeBehavior
#if UNITY_EDITOR
    : ScriptableObject
#endif
{
#if UNITY_EDITOR
    private void OnEnable()
    {
        if (IsReclaimed)
        {
            Recycle();
        }
    }
#endif
    public bool IsReclaimed { get; set; }
    public abstract ShapeBehaviorType BehaviorType { get; }
    public abstract bool GameUpdate(Shape shape);
    public abstract void Save(GameDataWriter writer);
    public abstract void Load(GameDataReader reader);
    public abstract void Recycle();

    public virtual void ResolveShapeInstances() { }
}
