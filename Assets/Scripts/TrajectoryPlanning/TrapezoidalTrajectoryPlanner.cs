using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrajectoryPlanning
{
    public static class TrapezoidalTrajectoryPlanner
    {
        public static TrajectoryPlan Generate(Vector3 start, Vector3 end, MotionProfileSettings settings)
        {
            return Generate(start, end, settings, 0f);
        }

        public static TrajectoryPlan Generate(Vector3 start, Vector3 end, MotionProfileSettings settings, float initialVelocity)
        {
            settings.Validate();

            var displacement = end - start;
            var totalDistance = displacement.magnitude;
            var clampedInitialVelocity = Mathf.Clamp(initialVelocity, 0f, settings.maxVelocity);

            if (totalDistance <= Mathf.Epsilon)
            {
                return new TrajectoryPlan(
                    start,
                    end,
                    0f,
                    0f,
                    0f,
                    true,
                    new List<TrajectorySample>
                    {
                        new TrajectorySample(0f, 0f, 0f, start)
                    });
            }

            var direction = displacement / totalDistance;
            var distanceToStopFromInitialVelocity = (clampedInitialVelocity * clampedInitialVelocity) / (2f * settings.deceleration);

            bool isTriangular;
            float peakVelocity;
            float accelerationTime;
            float cruiseTime;
            float decelerationTime;
            float accelerationDistance;
            float cruiseDistance;
            float effectiveDeceleration = settings.deceleration;

            if (totalDistance <= distanceToStopFromInitialVelocity && clampedInitialVelocity > Mathf.Epsilon)
            {
                isTriangular = true;
                peakVelocity = clampedInitialVelocity;
                accelerationTime = 0f;
                accelerationDistance = 0f;
                cruiseDistance = 0f;
                cruiseTime = 0f;
                effectiveDeceleration = (clampedInitialVelocity * clampedInitialVelocity) / (2f * totalDistance);
                decelerationTime = clampedInitialVelocity / effectiveDeceleration;
            }
            else
            {
                var timeToMaxVelocity = (settings.maxVelocity - clampedInitialVelocity) / settings.acceleration;
                var distanceToMaxVelocity = ((settings.maxVelocity * settings.maxVelocity) - (clampedInitialVelocity * clampedInitialVelocity)) /
                    (2f * settings.acceleration);
                var timeToStopFromMaxVelocity = settings.maxVelocity / settings.deceleration;
                var distanceToStopFromMaxVelocity = (settings.maxVelocity * settings.maxVelocity) / (2f * settings.deceleration);

                if (distanceToMaxVelocity + distanceToStopFromMaxVelocity <= totalDistance)
                {
                    isTriangular = false;
                    peakVelocity = settings.maxVelocity;
                    accelerationTime = Mathf.Max(0f, timeToMaxVelocity);
                    decelerationTime = timeToStopFromMaxVelocity;
                    accelerationDistance = Mathf.Max(0f, distanceToMaxVelocity);
                    cruiseDistance = totalDistance - accelerationDistance - distanceToStopFromMaxVelocity;
                    cruiseTime = cruiseDistance / peakVelocity;
                }
                else
                {
                    isTriangular = true;
                    peakVelocity = Mathf.Sqrt(((2f * totalDistance * settings.acceleration * settings.deceleration) +
                        (settings.deceleration * clampedInitialVelocity * clampedInitialVelocity)) /
                        (settings.acceleration + settings.deceleration));
                    accelerationTime = Mathf.Max(0f, (peakVelocity - clampedInitialVelocity) / settings.acceleration);
                    decelerationTime = peakVelocity / settings.deceleration;
                    accelerationDistance = Mathf.Max(0f, ((peakVelocity * peakVelocity) - (clampedInitialVelocity * clampedInitialVelocity)) /
                        (2f * settings.acceleration));
                    cruiseDistance = 0f;
                    cruiseTime = 0f;
                }
            }

            var totalTime = accelerationTime + cruiseTime + decelerationTime;
            var samples = new List<TrajectorySample>();

            for (var sampleTime = 0f; sampleTime < totalTime; sampleTime += settings.sampleInterval)
            {
                samples.Add(CreateSample(
                    start,
                    end,
                    direction,
                    sampleTime,
                    totalDistance,
                    clampedInitialVelocity,
                    settings.acceleration,
                    accelerationTime,
                    cruiseTime,
                    accelerationDistance,
                    cruiseDistance,
                    peakVelocity,
                    effectiveDeceleration));
            }

            samples.Add(CreateSample(
                start,
                end,
                direction,
                totalTime,
                totalDistance,
                clampedInitialVelocity,
                settings.acceleration,
                accelerationTime,
                cruiseTime,
                accelerationDistance,
                cruiseDistance,
                peakVelocity,
                effectiveDeceleration));

            return new TrajectoryPlan(start, end, totalDistance, totalTime, peakVelocity, isTriangular, samples);
        }

        private static TrajectorySample CreateSample(
            Vector3 start,
            Vector3 end,
            Vector3 direction,
            float time,
            float totalDistance,
            float initialVelocity,
            float acceleration,
            float accelerationTime,
            float cruiseTime,
            float accelerationDistance,
            float cruiseDistance,
            float peakVelocity,
            float deceleration)
        {
            float distance;
            float velocity;

            if (time <= accelerationTime)
            {
                velocity = initialVelocity + (acceleration * time);
                distance = (initialVelocity * time) + (0.5f * acceleration * time * time);
            }
            else if (time <= accelerationTime + cruiseTime)
            {
                var cruiseElapsed = time - accelerationTime;
                velocity = peakVelocity;
                distance = accelerationDistance + (peakVelocity * cruiseElapsed);
            }
            else
            {
                var decelerationElapsed = time - accelerationTime - cruiseTime;
                velocity = Mathf.Max(0f, peakVelocity - (deceleration * decelerationElapsed));
                distance = accelerationDistance + cruiseDistance + (peakVelocity * decelerationElapsed) -
                    (0.5f * deceleration * decelerationElapsed * decelerationElapsed);
            }

            distance = Mathf.Clamp(distance, 0f, totalDistance);
            var position = time >= Mathf.Epsilon && Mathf.Abs(totalDistance - distance) <= 0.0001f
                ? end
                : start + (direction * distance);

            return new TrajectorySample(time, distance, velocity, position);
        }
    }
}
