using NUnit.Framework;
using UnityEngine;

namespace TrajectoryPlanning.Tests
{
    public sealed class TrapezoidalTrajectoryPlannerTests
    {
        [Test]
        public void Generate_ReturnsSingleZeroVelocitySample_WhenDistanceIsZero()
        {
            var start = new Vector3(1f, 2f, 3f);
            var plan = TrapezoidalTrajectoryPlanner.Generate(start, start, CreateSettings());

            Assert.That(plan.TotalDistance, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(plan.TotalTime, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(plan.Samples.Count, Is.EqualTo(1));
            Assert.That(plan.Samples[0].Position, Is.EqualTo(start));
            Assert.That(plan.Samples[0].Velocity, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void Generate_CreatesTrapezoidalProfile_WhenCruisePhaseExists()
        {
            var settings = CreateSettings(sampleInterval: 0.1f, maxVelocity: 2f, acceleration: 1f, deceleration: 1f);
            var plan = TrapezoidalTrajectoryPlanner.Generate(Vector3.zero, new Vector3(10f, 0f, 0f), settings);

            Assert.That(plan.IsTriangular, Is.False);
            Assert.That(plan.PeakVelocity, Is.EqualTo(settings.maxVelocity).Within(0.0001f));
            Assert.That(plan.Samples[0].Position, Is.EqualTo(Vector3.zero));
            Assert.That(plan.Samples[plan.Samples.Count - 1].Position, Is.EqualTo(new Vector3(10f, 0f, 0f)));
            Assert.That(plan.Samples[plan.Samples.Count - 1].Time, Is.EqualTo(plan.TotalTime).Within(0.0001f));

            var plateauSamples = 0;
            foreach (var sample in plan.Samples)
            {
                if (Mathf.Abs(sample.Velocity - settings.maxVelocity) <= 0.0001f)
                {
                    plateauSamples++;
                }
            }

            Assert.That(plateauSamples, Is.GreaterThan(1));
        }

        [Test]
        public void Generate_FallsBackToTriangularProfile_WhenDistanceIsShort()
        {
            var settings = CreateSettings(sampleInterval: 0.05f, maxVelocity: 5f, acceleration: 2f, deceleration: 2f);
            var plan = TrapezoidalTrajectoryPlanner.Generate(Vector3.zero, Vector3.right, settings);

            Assert.That(plan.IsTriangular, Is.True);
            Assert.That(plan.PeakVelocity, Is.LessThan(settings.maxVelocity));
            Assert.That(plan.TotalDistance, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(plan.Samples[plan.Samples.Count - 1].Position, Is.EqualTo(Vector3.right));
        }

        [Test]
        public void EvaluatePosition_InterpolatesBetweenGeneratedSamples()
        {
            var settings = CreateSettings(sampleInterval: 0.5f, maxVelocity: 2f, acceleration: 1f, deceleration: 1f);
            var plan = TrapezoidalTrajectoryPlanner.Generate(Vector3.zero, new Vector3(6f, 0f, 0f), settings);
            var evaluatedPosition = plan.EvaluatePosition(plan.TotalTime * 0.5f);

            Assert.That(evaluatedPosition.x, Is.GreaterThan(0f));
            Assert.That(evaluatedPosition.x, Is.LessThan(6f));
            Assert.That(evaluatedPosition.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(evaluatedPosition.z, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void Generate_Throws_WhenSampleIntervalIsInvalid()
        {
            var settings = CreateSettings(sampleInterval: 0f);

            Assert.That(
                () => TrapezoidalTrajectoryPlanner.Generate(Vector3.zero, Vector3.one, settings),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void Generate_PreservesInitialVelocity_WhenReplanningInFlight()
        {
            var settings = CreateSettings(sampleInterval: 0.05f, maxVelocity: 3f, acceleration: 2f, deceleration: 2f);
            var plan = TrapezoidalTrajectoryPlanner.Generate(Vector3.zero, new Vector3(8f, 0f, 0f), settings, 1.5f);

            Assert.That(plan.Samples[0].Velocity, Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(plan.Samples[0].Position, Is.EqualTo(Vector3.zero));
            Assert.That(plan.Samples[plan.Samples.Count - 1].Position, Is.EqualTo(new Vector3(8f, 0f, 0f)));
        }

        [Test]
        public void Generate_AlwaysIncludesExactEndPointSample_EvenWithLargeSampleInterval()
        {
            var settings = CreateSettings(sampleInterval: 10f, maxVelocity: 2f, acceleration: 1f, deceleration: 1f);
            var end = new Vector3(3f, 0f, 0f);
            var plan = TrapezoidalTrajectoryPlanner.Generate(Vector3.zero, end, settings);

            Assert.That(plan.Samples.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(plan.Samples[plan.Samples.Count - 1].Position, Is.EqualTo(end));
            Assert.That(plan.Samples[plan.Samples.Count - 1].Distance, Is.EqualTo(plan.TotalDistance).Within(0.0001f));
        }

        [Test]
        public void Generate_ProducesMonotonicTimeDistanceAndBoundedVelocity()
        {
            var settings = CreateSettings(sampleInterval: 0.1f, maxVelocity: 2.5f, acceleration: 1.5f, deceleration: 1.5f);
            var plan = TrapezoidalTrajectoryPlanner.Generate(Vector3.zero, new Vector3(7f, 0f, 0f), settings);

            var previousTime = -1f;
            var previousDistance = -1f;

            foreach (var sample in plan.Samples)
            {
                Assert.That(sample.Time, Is.GreaterThan(previousTime));
                Assert.That(sample.Distance, Is.GreaterThanOrEqualTo(previousDistance));
                Assert.That(sample.Velocity, Is.GreaterThanOrEqualTo(0f));
                Assert.That(sample.Velocity, Is.LessThanOrEqualTo(settings.maxVelocity + 0.0001f));

                previousTime = sample.Time;
                previousDistance = sample.Distance;
            }
        }

        [Test]
        public void EvaluateVelocity_InterpolatesBetweenSamples_AndMatchesBoundaryValues()
        {
            var settings = CreateSettings(sampleInterval: 0.5f, maxVelocity: 2f, acceleration: 1f, deceleration: 1f);
            var plan = TrapezoidalTrajectoryPlanner.Generate(Vector3.zero, new Vector3(6f, 0f, 0f), settings);

            Assert.That(plan.EvaluateVelocity(0f), Is.EqualTo(plan.Samples[0].Velocity).Within(0.0001f));
            Assert.That(plan.EvaluateVelocity(plan.TotalTime), Is.EqualTo(plan.Samples[plan.Samples.Count - 1].Velocity).Within(0.0001f));

            var midpointVelocity = plan.EvaluateVelocity(plan.TotalTime * 0.5f);
            Assert.That(midpointVelocity, Is.GreaterThanOrEqualTo(0f));
            Assert.That(midpointVelocity, Is.LessThanOrEqualTo(settings.maxVelocity + 0.0001f));
        }

        [Test]
        public void Generate_ClampsInitialVelocity_ToConfiguredMaxVelocity()
        {
            var settings = CreateSettings(sampleInterval: 0.05f, maxVelocity: 2f, acceleration: 2f, deceleration: 2f);
            var plan = TrapezoidalTrajectoryPlanner.Generate(Vector3.zero, new Vector3(5f, 0f, 0f), settings, 99f);

            Assert.That(plan.Samples[0].Velocity, Is.EqualTo(settings.maxVelocity).Within(0.0001f));
        }

        private static MotionProfileSettings CreateSettings(
            float sampleInterval = 0.05f,
            float maxVelocity = 2f,
            float acceleration = 2f,
            float deceleration = 2f)
        {
            return new MotionProfileSettings
            {
                sampleInterval = sampleInterval,
                maxVelocity = maxVelocity,
                acceleration = acceleration,
                deceleration = deceleration
            };
        }
    }
}
