using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SpawnZone : PersistableObject
{
    public enum SpawnMovementDirection
    {
        Forward,
        Upward,
        Outward,
        Random
    }
    public abstract Vector3 SpawnPoint { get; }

    [SerializeField] SpawnMovementDirection spawnMovementDirection;

    [SerializeField] FloatRange spawnSpeed;

    public virtual void ConfigureSpawn(Shape shape)
    {
        Transform t = shape.transform;
        t.localPosition = SpawnPoint;
        t.localRotation = Random.rotation;
        t.localScale = Vector3.one * Random.Range(0.1f, 1f);

        shape.SetColor(Random.ColorHSV(
            hueMin: 0f, hueMax: 1f,
            saturationMin: 0.5f, saturationMax: 1f,
            valueMin: 0.25f, valueMax: 1f,
            alphaMin: 1f, alphaMax: 1f
            ));

        shape.AngularVelocity = Random.onUnitSphere * Random.Range(0f, 90f);

        Vector3 direction;
        if(spawnMovementDirection == SpawnMovementDirection.Upward)
        {
            direction = transform.up;
        }
        else if(spawnMovementDirection == SpawnMovementDirection.Forward)
        {
            direction = transform.forward;
        }
        else if(spawnMovementDirection == SpawnMovementDirection.Outward)
        {
            direction = (t.localPosition - transform.position).normalized;
        }
        else
        {
            direction = Random.onUnitSphere;
        }

        shape.Velocity = direction * spawnSpeed.RandomValueInRange;
    }


}
