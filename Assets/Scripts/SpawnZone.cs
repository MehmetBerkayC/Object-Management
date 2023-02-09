using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SpawnZone : PersistableObject
{

    [System.Serializable]
    public struct SpawnConfiguration
    {
        public enum MovementDirection
        {
            Forward,
            Upward,
            Outward,
            Random
        }

        public ShapeFactory[] factories;

        public MovementDirection movementDirection;

        public FloatRange speed;
        public FloatRange angularSpeed;
        public FloatRange scale;

        public ColorRangeHSV color;

        public bool uniformColor;
    }

    [SerializeField] SpawnConfiguration spawnConfig;

    public abstract Vector3 SpawnPoint { get; }

    public virtual Shape SpawnShape()
    {
        int factoryIndex = Random.Range(0, spawnConfig.factories.Length);
        Shape shape = spawnConfig.factories[factoryIndex].GetRandom();

        Transform t = shape.transform;
        t.localPosition = SpawnPoint;
        t.localRotation = Random.rotation;
        t.localScale = Vector3.one * spawnConfig.scale.RandomValueInRange;

        if (spawnConfig.uniformColor)
        {
            shape.SetColor(spawnConfig.color.RandomInRange);

        }
        else
        {
            for(int i = 0; i < shape.ColorCount; i++)
            {
                shape.SetColor(spawnConfig.color.RandomInRange, i);
            }
        }

        shape.AngularVelocity = Random.onUnitSphere * spawnConfig.angularSpeed.RandomValueInRange;

        Vector3 direction;
        switch (spawnConfig.movementDirection)
        {
            case SpawnConfiguration.MovementDirection.Forward:
                direction = transform.forward;
                break;
            case SpawnConfiguration.MovementDirection.Upward:
                direction = transform.up;
                break;
            case SpawnConfiguration.MovementDirection.Outward:
                direction = (t.localPosition - transform.position).normalized;
                break;
            default:
                direction = Random.onUnitSphere;
                break;
        }

        shape.Velocity = direction * spawnConfig.speed.RandomValueInRange;

        return shape;
    }


}
