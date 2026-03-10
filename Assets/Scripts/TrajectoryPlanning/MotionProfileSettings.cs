using System;
using UnityEngine;

namespace TrajectoryPlanning
{
    [Serializable]
    public struct MotionProfileSettings
    {
        [Min(0.001f)]
        public float sampleInterval;

        [Min(0.001f)]
        public float maxVelocity;

        [Min(0.001f)]
        public float acceleration;

        [Min(0.001f)]
        public float deceleration;

        public static MotionProfileSettings Default => new MotionProfileSettings
        {
            sampleInterval = 0.05f,
            maxVelocity = 2f,
            acceleration = 2f,
            deceleration = 2f
        };

        public void Validate()
        {
            if (sampleInterval <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleInterval), "Sample interval must be greater than zero.");
            }

            if (maxVelocity <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maxVelocity), "Max velocity must be greater than zero.");
            }

            if (acceleration <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(acceleration), "Acceleration must be greater than zero.");
            }

            if (deceleration <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deceleration), "Deceleration must be greater than zero.");
            }
        }
    }
}

