using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrajectoryPlanning
{
    public sealed class TrajectoryPlan
    {
        private readonly IReadOnlyList<TrajectorySample> _samples;

        public TrajectoryPlan(
            Vector3 start,
            Vector3 end,
            float totalDistance,
            float totalTime,
            float peakVelocity,
            bool isTriangular,
            IReadOnlyList<TrajectorySample> samples)
        {
            Start = start;
            End = end;
            TotalDistance = totalDistance;
            TotalTime = totalTime;
            PeakVelocity = peakVelocity;
            IsTriangular = isTriangular;
            _samples = samples ?? throw new ArgumentNullException(nameof(samples));
        }

        public Vector3 Start { get; }

        public Vector3 End { get; }

        public float TotalDistance { get; }

        public float TotalTime { get; }

        public float PeakVelocity { get; }

        public bool IsTriangular { get; }

        public IReadOnlyList<TrajectorySample> Samples => _samples;

        public Vector3 EvaluatePosition(float time)
        {
            if (_samples.Count == 0)
            {
                return Start;
            }

            if (time <= 0f || _samples.Count == 1)
            {
                return _samples[0].Position;
            }

            if (time >= TotalTime)
            {
                return _samples[_samples.Count - 1].Position;
            }

            for (var i = 1; i < _samples.Count; i++)
            {
                var next = _samples[i];
                if (time > next.Time)
                {
                    continue;
                }

                var previous = _samples[i - 1];
                var duration = next.Time - previous.Time;
                if (duration <= Mathf.Epsilon)
                {
                    return next.Position;
                }

                var normalized = Mathf.InverseLerp(previous.Time, next.Time, time);
                return Vector3.Lerp(previous.Position, next.Position, normalized);
            }

            return _samples[_samples.Count - 1].Position;
        }

        public float EvaluateVelocity(float time)
        {
            if (_samples.Count == 0)
            {
                return 0f;
            }

            if (time <= 0f || _samples.Count == 1)
            {
                return _samples[0].Velocity;
            }

            if (time >= TotalTime)
            {
                return _samples[_samples.Count - 1].Velocity;
            }

            for (var i = 1; i < _samples.Count; i++)
            {
                var next = _samples[i];
                if (time > next.Time)
                {
                    continue;
                }

                var previous = _samples[i - 1];
                var duration = next.Time - previous.Time;
                if (duration <= Mathf.Epsilon)
                {
                    return next.Velocity;
                }

                var normalized = Mathf.InverseLerp(previous.Time, next.Time, time);
                return Mathf.Lerp(previous.Velocity, next.Velocity, normalized);
            }

            return _samples[_samples.Count - 1].Velocity;
        }
    }
}
