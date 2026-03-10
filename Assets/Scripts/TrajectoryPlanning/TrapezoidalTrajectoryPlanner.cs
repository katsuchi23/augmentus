using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrajectoryPlanning
{
    public static class TrapezoidalTrajectoryPlanner
    {
        public static TrajectoryPlan Generate(Vector3 start, Vector3 end, MotionProfileSettings settings)
        {
            settings.Validate();

            var displacement = end - start;
            var totalDistance = displacement.magnitude;

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
            var timeToMaxVelocity = settings.maxVelocity / settings.acceleration;
            var distanceToMaxVelocity = 0.5f * settings.acceleration * timeToMaxVelocity * timeToMaxVelocity;
            var timeToStopFromMaxVelocity = settings.maxVelocity / settings.deceleration;
            var distanceToStopFromMaxVelocity = 0.5f * settings.deceleration * timeToStopFromMaxVelocity * timeToStopFromMaxVelocity;

            bool isTriangular;
            float peakVelocity;
            float accelerationTime;
            float cruiseTime;
            float decelerationTime;
            float accelerationDistance;
            float cruiseDistance;

            if (distanceToMaxVelocity + distanceToStopFromMaxVelocity <= totalDistance)
            {
                isTriangular = false;
                peakVelocity = settings.maxVelocity;
                accelerationTime = timeToMaxVelocity;
                decelerationTime = timeToStopFromMaxVelocity;
                accelerationDistance = distanceToMaxVelocity;
                cruiseDistance = totalDistance - distanceToMaxVelocity - distanceToStopFromMaxVelocity;
                cruiseTime = cruiseDistance / peakVelocity;
            }
            else
            {
                isTriangular = true;
                peakVelocity = Mathf.Sqrt((2f * totalDistance * settings.acceleration * settings.deceleration) /
                    (settings.acceleration + settings.deceleration));
                accelerationTime = peakVelocity / settings.acceleration;
                decelerationTime = peakVelocity / settings.deceleration;
                accelerationDistance = (peakVelocity * peakVelocity) / (2f * settings.acceleration);
                cruiseDistance = 0f;
                cruiseTime = 0f;
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
                    accelerationTime,
                    cruiseTime,
                    accelerationDistance,
                    cruiseDistance,
                    peakVelocity,
                    settings.deceleration));
            }

            samples.Add(CreateSample(
                start,
                end,
                direction,
                totalTime,
                totalDistance,
                accelerationTime,
                cruiseTime,
                accelerationDistance,
                cruiseDistance,
                peakVelocity,
                settings.deceleration));

            return new TrajectoryPlan(start, end, totalDistance, totalTime, peakVelocity, isTriangular, samples);
        }

        private static TrajectorySample CreateSample(
            Vector3 start,
            Vector3 end,
            Vector3 direction,
            float time,
            float totalDistance,
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
                velocity = peakVelocity * (time / Mathf.Max(accelerationTime, Mathf.Epsilon));
                distance = 0.5f * velocity * time;
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

