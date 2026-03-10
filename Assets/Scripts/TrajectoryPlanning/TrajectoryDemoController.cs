using UnityEngine;

namespace TrajectoryPlanning
{
    public sealed class TrajectoryDemoController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Transform startPoint;
        [SerializeField] private Transform endPoint;
        [SerializeField] private Transform movingObject;
        [SerializeField] private bool autoCreateDemoObjects = true;

        [Header("Profile Settings")]
        [SerializeField] private MotionProfileSettings profileSettings = default;
        [SerializeField] private bool loop = true;
        [SerializeField] private bool playOnStart = true;

        [Header("Fallback Positions")]
        [SerializeField] private Vector3 fallbackStartPosition = new Vector3(-4f, 0f, 0f);
        [SerializeField] private Vector3 fallbackEndPosition = new Vector3(4f, 0f, 0f);

        private TrajectoryPlan _plan;
        private float _elapsedTime;
        private bool _isPlaying;

        private void Reset()
        {
            profileSettings = MotionProfileSettings.Default;
        }

        private void Awake()
        {
            EnsureValidProfileSettings();

            EnsureSceneReferences();
            RebuildPlan();
            _isPlaying = playOnStart;
        }

        private void OnValidate()
        {
            EnsureValidProfileSettings();

            if (Application.isPlaying)
            {
                RebuildPlan();
            }
        }

        private void Update()
        {
            if (!_isPlaying || _plan == null || movingObject == null)
            {
                return;
            }

            _elapsedTime += Time.deltaTime;

            if (_plan.TotalTime <= Mathf.Epsilon)
            {
                movingObject.position = _plan.End;
                return;
            }

            if (_elapsedTime > _plan.TotalTime)
            {
                if (loop)
                {
                    _elapsedTime %= _plan.TotalTime;
                }
                else
                {
                    _elapsedTime = _plan.TotalTime;
                    _isPlaying = false;
                }
            }

            movingObject.position = _plan.EvaluatePosition(_elapsedTime);
        }

        private void OnDrawGizmos()
        {
            var startPosition = startPoint != null ? startPoint.position : fallbackStartPosition;
            var endPosition = endPoint != null ? endPoint.position : fallbackEndPosition;
            var settings = HasInvalidProfileSettings() ? MotionProfileSettings.Default : profileSettings;

            TrajectoryPlan previewPlan;
            try
            {
                previewPlan = TrapezoidalTrajectoryPlanner.Generate(startPosition, endPosition, settings);
            }
            catch
            {
                return;
            }

            Gizmos.color = Color.cyan;
            for (var i = 1; i < previewPlan.Samples.Count; i++)
            {
                Gizmos.DrawLine(previewPlan.Samples[i - 1].Position, previewPlan.Samples[i].Position);
            }

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(startPosition, 0.15f);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(endPosition, 0.15f);
        }

        public void RebuildPlan()
        {
            EnsureValidProfileSettings();
            var startPosition = startPoint != null ? startPoint.position : fallbackStartPosition;
            var endPosition = endPoint != null ? endPoint.position : fallbackEndPosition;
            _plan = TrapezoidalTrajectoryPlanner.Generate(startPosition, endPosition, profileSettings);
            _elapsedTime = 0f;

            if (movingObject != null)
            {
                movingObject.position = _plan.Start;
            }
        }

        public void Play()
        {
            RebuildPlan();
            _isPlaying = true;
        }

        private void EnsureSceneReferences()
        {
            if (!autoCreateDemoObjects)
            {
                return;
            }

            if (startPoint == null)
            {
                startPoint = CreatePrimitive("Start Point", PrimitiveType.Sphere, fallbackStartPosition, new Vector3(0.35f, 0.35f, 0.35f), Color.green);
            }

            if (endPoint == null)
            {
                endPoint = CreatePrimitive("End Point", PrimitiveType.Sphere, fallbackEndPosition, new Vector3(0.35f, 0.35f, 0.35f), Color.red);
            }

            if (movingObject == null)
            {
                movingObject = CreatePrimitive("Trajectory Cube", PrimitiveType.Cube, startPoint.position, new Vector3(0.5f, 0.5f, 0.5f), Color.yellow);
            }
        }

        private static Transform CreatePrimitive(string objectName, PrimitiveType primitiveType, Vector3 position, Vector3 scale, Color color)
        {
            var instance = GameObject.CreatePrimitive(primitiveType);
            instance.name = objectName;
            instance.transform.position = position;
            instance.transform.localScale = scale;

            var renderer = instance.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }

            return instance.transform;
        }

        private bool HasInvalidProfileSettings()
        {
            return profileSettings.sampleInterval <= 0f ||
                profileSettings.maxVelocity <= 0f ||
                profileSettings.acceleration <= 0f ||
                profileSettings.deceleration <= 0f;
        }

        private void EnsureValidProfileSettings()
        {
            if (HasInvalidProfileSettings())
            {
                profileSettings = MotionProfileSettings.Default;
            }
        }
    }
}
