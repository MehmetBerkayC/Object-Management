using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SpawnZone : PersistableObject
{

    [System.Serializable]
    public struct SpawnConfiguration
    {
        public ShapeFactory[] factories;

        // Movement
        public enum MovementDirection
        {
            Forward,
            Upward,
            Outward,
            Random
        }
        
        public MovementDirection movementDirection;

        public FloatRange speed;
        public FloatRange angularSpeed;
        public FloatRange scale;

        // Color
        public ColorRangeHSV color;
        public bool uniformColor;

        // Oscillation
        public MovementDirection oscillationDirection;
        public FloatRange oscillationAmplitude;
        public FloatRange oscillationFrequency;

        // Satellite
        [System.Serializable]
        public struct SatelliteConfiguration
        {
            public IntRange amount;
         
            [FloatRangeSlider(0.1f, 1f)]
            public FloatRange relativeScale;

            public FloatRange orbitRadius;
            public FloatRange orbitFrequency;
        }

        public SatelliteConfiguration satellite;

        // Object Lifecycle
        [System.Serializable]
        public struct LifecycleConfiguration
        {
            [FloatRangeSlider(0f, 2f)]
            public FloatRange growingDuration;

            [FloatRangeSlider(0f, 2f)]
            public FloatRange dyingDuration;

            public Vector2 RandomDurations
            {
                get
                {
                    return new Vector2(
                        growingDuration.RandomValueInRange,
                        dyingDuration.RandomValueInRange
                    );
                }
            }
        }

        public LifecycleConfiguration lifecycle;
    }

    [SerializeField] SpawnConfiguration spawnConfig;

    public abstract Vector3 SpawnPoint { get; }

    public virtual void SpawnShapes()
    {
        int factoryIndex = Random.Range(0, spawnConfig.factories.Length);
        Shape shape = spawnConfig.factories[factoryIndex].GetRandom();

        Transform t = shape.transform;
        t.localPosition = SpawnPoint;
        t.localRotation = Random.rotation;
        t.localScale = Vector3.one * spawnConfig.scale.RandomValueInRange;

        // Coloring
        SetupColor(shape);

        // Rotation
        float angularSpeed = spawnConfig.angularSpeed.RandomValueInRange;
        if(angularSpeed != 0f)
        {
            var rotation = shape.AddBehavior<RotationShapeBehavior>();
            rotation.AngularVelocity = Random.onUnitSphere * angularSpeed;
        }


        // Movement
        float speed = spawnConfig.speed.RandomValueInRange;
        if (speed != 0f)
        {
            var movement = shape.AddBehavior<MovementShapeBehavior>();
            movement.Velocity = GetDirectionVector(spawnConfig.movementDirection, t) * speed;
        }

        SetupOscillation(shape);

        // For lifecycle growing - dying
        Vector2 lifecycleDurations = spawnConfig.lifecycle.RandomDurations;

        // Satellites
        int satelliteCount = spawnConfig.satellite.amount.RandomValueInRange;
        for(int i = 0; i < satelliteCount; i++)
        {
            CreateSatelliteFor(shape, lifecycleDurations);
        }

        // Create lifecycle for shape after its satellites done setups, 
        // otherwise satellites scale gets calculated wrong,
        // their scale depend on the focal shape!
        SetupLifecycle(shape, lifecycleDurations);
    }

    Vector3 GetDirectionVector(SpawnConfiguration.MovementDirection direction, Transform t)
    {
        switch (direction)
        {
            case SpawnConfiguration.MovementDirection.Upward:
                return transform.up;
            case SpawnConfiguration.MovementDirection.Outward:
                return (t.localPosition - transform.position).normalized;
            case SpawnConfiguration.MovementDirection.Random:
                return Random.onUnitSphere;
            default:
                return transform.forward;
        }
    }

    void SetupLifecycle(Shape shape, Vector2 growingDurations)
    {
        if (growingDurations.x > 0f)
        {
            shape.AddBehavior<GrowingShapeBehavior>().Initialize(
                shape, growingDurations.x
            );
        }else if(growingDurations.y > 0f)
        {
            shape.AddBehavior<DyingShapeBehavior>().Initialize(
                shape, growingDurations.y
            );
        }
    }

    void SetupOscillation(Shape shape)
    {
        float amplitude = spawnConfig.oscillationAmplitude.RandomValueInRange;
        float frequency = spawnConfig.oscillationFrequency.RandomValueInRange;
        if (amplitude == 0f || frequency == 0f)
        {
            return;
        }
        var oscillation = shape.AddBehavior<OscillationShapeBehavior>();
        oscillation.Offset = GetDirectionVector(spawnConfig.oscillationDirection, shape.transform) * amplitude;
        oscillation.Frequency = frequency;
    }

    void SetupColor(Shape shape)
    {
        if (spawnConfig.uniformColor)
        {
            shape.SetColor(spawnConfig.color.RandomInRange);
        }
        else
        {
            for (int i = 0; i < shape.ColorCount; i++)
            {
                shape.SetColor(spawnConfig.color.RandomInRange, i);
            }
        }
    }

    void CreateSatelliteFor(Shape focalShape, Vector2 lifecycleDurations)
    {
        int factoryIndex = Random.Range(0, spawnConfig.factories.Length);
        Shape shape = spawnConfig.factories[factoryIndex].GetRandom();
        Transform t = shape.transform;
        t.localRotation = Random.rotation;
        t.localScale = focalShape.transform.localScale * spawnConfig.satellite.relativeScale.RandomValueInRange;
        shape.AddBehavior<SatelliteShapeBehavior>().Initialize(
            shape, focalShape, 
            spawnConfig.satellite.orbitRadius.RandomValueInRange, 
            spawnConfig.satellite.orbitFrequency.RandomValueInRange
            );

        SetupLifecycle(shape, lifecycleDurations);
    }

}
