using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SpawnZone : PersistableObject
{
    [SerializeField, Range(0f, 50f)]
    float spawnSpeed;

    float spawnProgress;

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

            public bool uniformLifecycles;
        }

        public SatelliteConfiguration satellite;

        // Object Lifecycle
        [System.Serializable]
        public struct LifecycleConfiguration
        {
            [FloatRangeSlider(0f, 2f)]
            public FloatRange growingDuration;

            [FloatRangeSlider(0f, 100f)]
            public FloatRange adultDuration;
            
            [FloatRangeSlider(0f, 2f)]
            public FloatRange dyingDuration;

            public Vector3 RandomDurations
            {
                get
                {
                    return new Vector3(
                        growingDuration.RandomValueInRange,
                        adultDuration.RandomValueInRange,
                        dyingDuration.RandomValueInRange
                    );
                }
            }
        }

        public LifecycleConfiguration lifecycle;
    }

    [SerializeField] SpawnConfiguration spawnConfig;

    public abstract Vector3 SpawnPoint { get; }

    // Important:
    // Every spawn zone that has a positive spawn speed,
    // must be included in the persistent object list of its level,
    // otherwise it won't be saved and loaded.
    private void FixedUpdate()
    {
        spawnProgress += Time.deltaTime * spawnSpeed;
        while(spawnProgress >= 1f)
        {
            spawnProgress -= 1f;
            SpawnShapes();
        }
    }

    public override void Save(GameDataWriter writer)
    {
        writer.Write(spawnProgress);
    }

    public override void Load(GameDataReader reader)
    {
        spawnProgress = reader.ReadFloat();
    }

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

        // For lifecycle growing - adult - dying
        Vector3 lifecycleDurations = spawnConfig.lifecycle.RandomDurations;

        // Satellites
        int satelliteCount = spawnConfig.satellite.amount.RandomValueInRange;
        for(int i = 0; i < satelliteCount; i++)
        {
            CreateSatelliteFor(
                shape, 
                spawnConfig.satellite.uniformLifecycles ? 
                lifecycleDurations : spawnConfig.lifecycle.RandomDurations);
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

    void SetupLifecycle(Shape shape, Vector3 durations)
    {
        if (durations.x > 0f)
        {
            if(durations.y > 0f || durations.z > 0f)
            {
                shape.AddBehavior<LifecycleShapeBehavior>().Initialize(
                    shape, durations.x, durations.y, durations.z
                    );
            }
            else
            {
                shape.AddBehavior<GrowingShapeBehavior>().Initialize(
                    shape, durations.x
                );
            }
        }else if(durations.y > 0f)
        {
            shape.AddBehavior<LifecycleShapeBehavior>().Initialize(
                shape, durations.x, durations.y, durations.z
                );
        }
        else if(durations.z > 0f)
        {
            shape.AddBehavior<DyingShapeBehavior>().Initialize(
                shape, durations.z
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

    void CreateSatelliteFor(Shape focalShape, Vector3 lifecycleDurations)
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
