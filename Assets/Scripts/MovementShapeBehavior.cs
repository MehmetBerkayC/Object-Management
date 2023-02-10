using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Unlike shape prefabs, each shape behavior has its own type, thus all code is strongly-typed.
 * It is not possible for a behavior to be added to the wrong pool.
 * However, that is only true when each behavior only extends ShapeBehavior. 
 * Technically, it is possible to extend another behavior, 
 * for example some weird movement type that extends MovementShapeBehavior. 
 * Then it would be possible to add an instance of that behavior to the 
 * ShapeBehaviorPool<MovementShapeBehavior> pool, instead of its own type's pool.
 * To prevent that, we can make it impossible to extend MovementShapeBehavior, by adding the sealed keyword to it.
 */
public sealed class MovementShapeBehavior : ShapeBehavior
{
    public Vector3 Velocity { get; set; }

    public override ShapeBehaviorType BehaviorType
    {
        get
        {
            return ShapeBehaviorType.Movement;
        }
    }

    public override void Recycle()
    {
        ShapeBehaviorPool<MovementShapeBehavior>.Reclaim(this);
    }

    public override void GameUpdate(Shape shape)
    {
        shape.transform.localPosition += Velocity * Time.deltaTime;
    }

    public override void Save(GameDataWriter writer)
    {
        writer.Write(Velocity);
    }
    public override void Load(GameDataReader reader)
    {
        Velocity = reader.ReadVector3();
    }
}
