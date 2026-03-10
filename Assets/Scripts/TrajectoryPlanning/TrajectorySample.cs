using UnityEngine;

namespace TrajectoryPlanning
{
    public readonly struct TrajectorySample
    {
        public TrajectorySample(float time, float distance, float velocity, Vector3 position)
        {
            Time = time;
            Distance = distance;
            Velocity = velocity;
            Position = position;
        }

        public float Time { get; }

        public float Distance { get; }

        public float Velocity { get; }

        public Vector3 Position { get; }
    }
}

