using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace TrajectoryPlanning
{
    public sealed class TrajectoryDemoController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Transform startPoint;
        [SerializeField] private Transform endPoint;
        [SerializeField] private Transform movingObject;
        [SerializeField] private LineRenderer pathRenderer;
        [SerializeField] private bool autoCreateDemoObjects = true;

        [Header("Profile Settings")]
        [SerializeField] private MotionProfileSettings profileSettings = default;
        [SerializeField] private bool loop = true;
        [SerializeField] private bool playOnStart = true;

        [Header("Visualization")]
        [SerializeField] private bool showPathInGame = true;
        [SerializeField] private Color pathColor = Color.cyan;
        [SerializeField] private float pathWidth = 0.08f;
        [SerializeField] private bool enableRuntimeDragControls = true;
        [SerializeField] private bool autoCreateCameraRig = true;
        [SerializeField] private bool showRuntimeGui = true;

        [Header("Fallback Positions")]
        [SerializeField] private Vector3 fallbackStartPosition = new Vector3(-4f, 0f, 0f);
        [SerializeField] private Vector3 fallbackEndPosition = new Vector3(4f, 0f, 0f);

        private TrajectoryPlan _plan;
        private float _elapsedTime;
        private bool _isPlaying;
        private string _sampleIntervalInput;
        private string _maxVelocityInput;
        private string _accelerationInput;
        private string _decelerationInput;
        private string _startXInput;
        private string _startYInput;
        private string _startZInput;
        private string _endXInput;
        private string _endYInput;
        private string _endZInput;
        private string _runtimeGuiStatus;
        private Vector3 _plannedEndPosition;
        private bool _hasPlannedEndPosition;
        private float _goalUpdateTimer;
        private bool _pendingReplan;

        private void Reset()
        {
            profileSettings = MotionProfileSettings.Default;
            SyncRuntimeInputsFromScene();
        }

        private void Awake()
        {
            EnsureValidProfileSettings();

            EnsureSceneReferences();
            EnsurePresentationObjects();
            EnsurePathRenderer();
            SyncRuntimeInputsFromScene();
            SnapPlannedGoalToDesired();
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
            UpdatePlannedGoalTracking();

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

        private void OnGUI()
        {
            if (!showRuntimeGui || !Application.isPlaying)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(12f, 12f, 350f, 610f), GUI.skin.window);
            GUILayout.Label("Trajectory Controls");

            _sampleIntervalInput = DrawTextField("Sample Interval", _sampleIntervalInput);
            _maxVelocityInput = DrawTextField("Max Velocity", _maxVelocityInput);
            _accelerationInput = DrawTextField("Acceleration", _accelerationInput);
            _decelerationInput = DrawTextField("Deceleration", _decelerationInput);

            GUILayout.Space(8f);
            GUILayout.Label("Start Position");
            DrawVector3Fields(ref _startXInput, ref _startYInput, ref _startZInput);

            GUILayout.Space(8f);
            GUILayout.Label("End Position");
            DrawVector3Fields(ref _endXInput, ref _endYInput, ref _endZInput);

            GUILayout.Space(10f);
            if (GUILayout.Button("Apply Changes"))
            {
                ApplyRuntimeGuiInputs();
            }

            if (GUILayout.Button("Load Current Positions"))
            {
                SyncRuntimeInputsFromScene();
                _runtimeGuiStatus = "Loaded current scene values.";
            }

            if (!string.IsNullOrEmpty(_runtimeGuiStatus))
            {
                GUILayout.Space(8f);
                GUILayout.Label(_runtimeGuiStatus);
            }

            GUILayout.Space(10f);
            DrawRuntimeTelemetry();

            GUILayout.EndArea();
        }

        private void OnDrawGizmos()
        {
            var startPosition = startPoint != null ? startPoint.position : fallbackStartPosition;
            var previewEndPosition = Application.isPlaying && _hasPlannedEndPosition ? _plannedEndPosition : GetDesiredEndPosition();
            var desiredEndPosition = GetDesiredEndPosition();
            var settings = HasInvalidProfileSettings() ? MotionProfileSettings.Default : profileSettings;

            TrajectoryPlan previewPlan;
            try
            {
                previewPlan = TrapezoidalTrajectoryPlanner.Generate(startPosition, previewEndPosition, settings);
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
            Gizmos.DrawSphere(desiredEndPosition, 0.15f);
        }

        public void RebuildPlan()
        {
            RebuildPlan(false);
        }

        public void RebuildPlan(bool preserveCurrentMotion)
        {
            EnsureValidProfileSettings();
            if (!preserveCurrentMotion)
            {
                SnapPlannedGoalToDesired();
            }

            var startPosition = preserveCurrentMotion && movingObject != null
                ? movingObject.position
                : startPoint != null ? startPoint.position : fallbackStartPosition;
            var endPosition = GetPlanningEndPosition();
            var initialVelocity = preserveCurrentMotion ? GetCurrentVelocity() : 0f;
            _plan = TrapezoidalTrajectoryPlanner.Generate(startPosition, endPosition, profileSettings, initialVelocity);
            _elapsedTime = 0f;

            if (movingObject != null && !preserveCurrentMotion)
            {
                movingObject.position = _plan.Start;
            }

            UpdatePathRenderer();
            SyncRuntimeInputsFromScene();
            _pendingReplan = false;

            if (!preserveCurrentMotion)
            {
                _goalUpdateTimer = 0f;
            }
        }

        public void Play()
        {
            RebuildPlan(false);
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

            EnsureControlPoint(startPoint, TrajectoryControlPoint.ControlPointRole.Start);
            EnsureControlPoint(endPoint, TrajectoryControlPoint.ControlPointRole.End);
        }

        private void EnsurePresentationObjects()
        {
            EnsureCameraRig();
            EnsureLight();
        }

        private void EnsurePathRenderer()
        {
            if (pathRenderer != null)
            {
                ConfigurePathRenderer(pathRenderer);
                return;
            }

            var pathRendererObject = new GameObject("Trajectory Path");
            pathRendererObject.transform.SetParent(transform, false);
            pathRenderer = pathRendererObject.AddComponent<LineRenderer>();
            ConfigurePathRenderer(pathRenderer);
        }

        private void ConfigurePathRenderer(LineRenderer renderer)
        {
            renderer.useWorldSpace = true;
            renderer.loop = false;
            renderer.alignment = LineAlignment.View;
            renderer.textureMode = LineTextureMode.Stretch;
            renderer.numCapVertices = 4;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            if (renderer.sharedMaterial == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    renderer.sharedMaterial = new Material(shader);
                }
            }
        }

        private void EnsureCameraRig()
        {
            if (!autoCreateCameraRig)
            {
                return;
            }

            var existingSwitcher = FindFirstObjectByType<TrajectoryCameraSwitcher>();
            if (existingSwitcher != null)
            {
                EnsureCameraControllers(existingSwitcher);
                return;
            }

            var cameraRig = new GameObject("Trajectory Cameras");
            cameraRig.transform.SetParent(transform, false);
            var cameras = new List<Camera>();

            var existingCameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (existingCameras.Length > 0)
            {
                cameras.Add(existingCameras[0]);
                EnsureCameraController(existingCameras[0]);
                existingCameras[0].name = "Camera 1 - Overview";
            }

            var defaultCameraDefinitions = new List<(string name, Vector3 position, Quaternion rotation)>
            {
                ("Camera 1 - Overview", new Vector3(0f, 6f, -10f), Quaternion.Euler(25f, 0f, 0f)),
                ("Camera 2 - Side", new Vector3(-10f, 4f, 0f), Quaternion.Euler(15f, 90f, 0f)),
                ("Camera 3 - Top Angle", new Vector3(0f, 10f, -2f), Quaternion.Euler(65f, 0f, 0f))
            };

            for (var i = cameras.Count; i < 3; i++)
            {
                var cameraDefinition = defaultCameraDefinitions[i];
                cameras.Add(CreateCamera(cameraRig.transform, cameraDefinition.name, cameraDefinition.position, cameraDefinition.rotation));
            }

            var switcher = cameraRig.AddComponent<TrajectoryCameraSwitcher>();
            switcher.SetCameras(cameras);
        }

        private Camera CreateCamera(Transform parent, string cameraName, Vector3 position, Quaternion rotation)
        {
            var cameraObject = new GameObject(cameraName);
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.position = position;
            cameraObject.transform.rotation = rotation;
            cameraObject.tag = "Untagged";

            var cameraComponent = cameraObject.AddComponent<Camera>();
            cameraComponent.clearFlags = CameraClearFlags.SolidColor;
            cameraComponent.backgroundColor = new Color(0.09f, 0.11f, 0.14f);

            EnsureCameraController(cameraComponent);

            return cameraComponent;
        }

        private void EnsureLight()
        {
            if (FindFirstObjectByType<Light>() != null)
            {
                return;
            }

            var lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var lightComponent = lightObject.AddComponent<Light>();
            lightComponent.type = LightType.Directional;
            lightComponent.intensity = 1.1f;
        }

        private void UpdatePathRenderer()
        {
            if (pathRenderer == null)
            {
                return;
            }

            pathRenderer.enabled = showPathInGame;
            pathRenderer.startColor = pathColor;
            pathRenderer.endColor = pathColor;
            pathRenderer.startWidth = pathWidth;
            pathRenderer.endWidth = pathWidth;

            if (!showPathInGame || _plan == null)
            {
                pathRenderer.positionCount = 0;
                return;
            }

            pathRenderer.positionCount = _plan.Samples.Count;
            for (var i = 0; i < _plan.Samples.Count; i++)
            {
                pathRenderer.SetPosition(i, _plan.Samples[i].Position);
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

        public void HandleControlPointChanged(TrajectoryControlPoint.ControlPointRole role)
        {
            if (role == TrajectoryControlPoint.ControlPointRole.End && _isPlaying)
            {
                _pendingReplan = true;
                _runtimeGuiStatus = $"Goal updated. Path will replan in at most {FormatFloat(profileSettings.sampleInterval)}s.";
                return;
            }

            Play();
        }

        private void EnsureControlPoint(Transform point, TrajectoryControlPoint.ControlPointRole role)
        {
            if (point == null || !enableRuntimeDragControls)
            {
                return;
            }

            var controlPoint = point.GetComponent<TrajectoryControlPoint>();
            if (controlPoint == null)
            {
                controlPoint = point.gameObject.AddComponent<TrajectoryControlPoint>();
            }

            controlPoint.Initialize(this, role);
        }

        private void EnsureCameraControllers(TrajectoryCameraSwitcher switcher)
        {
            var sceneCameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var sceneCamera in sceneCameras)
            {
                EnsureCameraController(sceneCamera);
            }
        }

        private void EnsureCameraController(Camera cameraComponent)
        {
            if (cameraComponent == null)
            {
                return;
            }

            if (cameraComponent.GetComponent<TrajectoryCameraController>() == null)
            {
                cameraComponent.gameObject.AddComponent<TrajectoryCameraController>();
            }
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

        private void SyncRuntimeInputsFromScene()
        {
            var startPosition = startPoint != null ? startPoint.position : fallbackStartPosition;
            var endPosition = endPoint != null ? endPoint.position : fallbackEndPosition;

            _sampleIntervalInput = FormatFloat(profileSettings.sampleInterval);
            _maxVelocityInput = FormatFloat(profileSettings.maxVelocity);
            _accelerationInput = FormatFloat(profileSettings.acceleration);
            _decelerationInput = FormatFloat(profileSettings.deceleration);

            _startXInput = FormatFloat(startPosition.x);
            _startYInput = FormatFloat(startPosition.y);
            _startZInput = FormatFloat(startPosition.z);

            _endXInput = FormatFloat(endPosition.x);
            _endYInput = FormatFloat(endPosition.y);
            _endZInput = FormatFloat(endPosition.z);
        }

        private void ApplyRuntimeGuiInputs()
        {
            var currentStartPosition = startPoint != null ? startPoint.position : fallbackStartPosition;
            var currentEndPosition = endPoint != null ? endPoint.position : fallbackEndPosition;
            var sampleIntervalChanged = !Mathf.Approximately(profileSettings.sampleInterval, ParseOrCurrent(_sampleIntervalInput, profileSettings.sampleInterval));
            var maxVelocityChanged = !Mathf.Approximately(profileSettings.maxVelocity, ParseOrCurrent(_maxVelocityInput, profileSettings.maxVelocity));
            var accelerationChanged = !Mathf.Approximately(profileSettings.acceleration, ParseOrCurrent(_accelerationInput, profileSettings.acceleration));
            var decelerationChanged = !Mathf.Approximately(profileSettings.deceleration, ParseOrCurrent(_decelerationInput, profileSettings.deceleration));

            if (!TryParseFloat(_sampleIntervalInput, out var sampleInterval) || sampleInterval <= 0f)
            {
                _runtimeGuiStatus = "Sample interval must be a number greater than 0.";
                return;
            }

            if (!TryParseFloat(_maxVelocityInput, out var maxVelocity) || maxVelocity <= 0f)
            {
                _runtimeGuiStatus = "Max velocity must be a number greater than 0.";
                return;
            }

            if (!TryParseFloat(_accelerationInput, out var acceleration) || acceleration <= 0f)
            {
                _runtimeGuiStatus = "Acceleration must be a number greater than 0.";
                return;
            }

            if (!TryParseFloat(_decelerationInput, out var deceleration) || deceleration <= 0f)
            {
                _runtimeGuiStatus = "Deceleration must be a number greater than 0.";
                return;
            }

            if (!TryParseVector3(_startXInput, _startYInput, _startZInput, out var startPosition))
            {
                _runtimeGuiStatus = "Start position fields must be valid numbers.";
                return;
            }

            if (!TryParseVector3(_endXInput, _endYInput, _endZInput, out var endPosition))
            {
                _runtimeGuiStatus = "End position fields must be valid numbers.";
                return;
            }

            profileSettings.sampleInterval = sampleInterval;
            profileSettings.maxVelocity = maxVelocity;
            profileSettings.acceleration = acceleration;
            profileSettings.deceleration = deceleration;

            var startChanged = Vector3.Distance(currentStartPosition, startPosition) > 0.0001f;
            var endChanged = Vector3.Distance(currentEndPosition, endPosition) > 0.0001f;

            if (startPoint != null)
            {
                startPoint.position = startPosition;
            }
            else
            {
                fallbackStartPosition = startPosition;
            }

            if (endPoint != null)
            {
                endPoint.position = endPosition;
            }
            else
            {
                fallbackEndPosition = endPosition;
            }

            if (_isPlaying && !startChanged && (endChanged || sampleIntervalChanged || maxVelocityChanged || accelerationChanged || decelerationChanged))
            {
                _pendingReplan = true;
                _goalUpdateTimer = 0f;
                _runtimeGuiStatus = $"Update queued. Path will replan in at most {FormatFloat(profileSettings.sampleInterval)}s.";
                return;
            }

            Play();
            _runtimeGuiStatus = "Trajectory restarted from the start point.";
        }

        private static string DrawTextField(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(120f));
            var updatedValue = GUILayout.TextField(value ?? string.Empty, GUILayout.Width(180f));
            GUILayout.EndHorizontal();
            return updatedValue;
        }

        private static void DrawVector3Fields(ref string xValue, ref string yValue, ref string zValue)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("X", GUILayout.Width(14f));
            xValue = GUILayout.TextField(xValue ?? string.Empty, GUILayout.Width(80f));
            GUILayout.Label("Y", GUILayout.Width(14f));
            yValue = GUILayout.TextField(yValue ?? string.Empty, GUILayout.Width(80f));
            GUILayout.Label("Z", GUILayout.Width(14f));
            zValue = GUILayout.TextField(zValue ?? string.Empty, GUILayout.Width(80f));
            GUILayout.EndHorizontal();
        }

        private static bool TryParseVector3(string xInput, string yInput, string zInput, out Vector3 parsedVector)
        {
            parsedVector = default;
            if (!TryParseFloat(xInput, out var x) || !TryParseFloat(yInput, out var y) || !TryParseFloat(zInput, out var z))
            {
                return false;
            }

            parsedVector = new Vector3(x, y, z);
            return true;
        }

        private static bool TryParseFloat(string rawValue, out float parsedValue)
        {
            return float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue) ||
                float.TryParse(rawValue, NumberStyles.Float, CultureInfo.CurrentCulture, out parsedValue);
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static float ParseOrCurrent(string rawValue, float fallbackValue)
        {
            return TryParseFloat(rawValue, out var parsedValue) ? parsedValue : fallbackValue;
        }

        private void DrawRuntimeTelemetry()
        {
            GUILayout.Label("Plan Telemetry");

            if (_plan == null)
            {
                GUILayout.Label("No active plan.");
                return;
            }

            GUILayout.Label($"Current Velocity: {FormatFloat(GetCurrentVelocity())} units/s");
            GUILayout.Label($"Planned Duration: {FormatFloat(_plan.TotalTime)} s");
            GUILayout.Label($"Peak Velocity: {FormatFloat(_plan.PeakVelocity)} units/s");
            GUILayout.Label($"Configured Max Velocity: {FormatFloat(profileSettings.maxVelocity)} units/s");
            GUILayout.Label($"Profile Type: {(_plan.IsTriangular ? "Triangular" : "Trapezoidal")}");
            GUILayout.Label($"Sample Count: {_plan.Samples.Count}");
            GUILayout.Label($"Applied Acceleration: {FormatFloat(profileSettings.acceleration)} units/s^2");
            GUILayout.Label($"Applied Deceleration: {FormatFloat(profileSettings.deceleration)} units/s^2");
            GUILayout.Label($"Replan Interval: {FormatFloat(profileSettings.sampleInterval)} s");

            if (_isPlaying)
            {
                var timeToNextReplan = Mathf.Max(0f, profileSettings.sampleInterval - _goalUpdateTimer);
                GUILayout.Label($"Next Replan In: {FormatFloat(timeToNextReplan)} s");
            }
        }

        private float GetCurrentVelocity()
        {
            if (_plan == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, _plan.EvaluateVelocity(_elapsedTime));
        }

        private void UpdatePlannedGoalTracking()
        {
            if (!Application.isPlaying || !_isPlaying)
            {
                return;
            }

            _goalUpdateTimer += Time.deltaTime;
            var updateStep = Mathf.Max(profileSettings.sampleInterval, 0.01f);
            if (_goalUpdateTimer < updateStep)
            {
                return;
            }

            while (_goalUpdateTimer >= updateStep)
            {
                _goalUpdateTimer -= updateStep;
                if (SyncPlannedGoalToDesired() || _pendingReplan)
                {
                    RebuildPlan(true);
                    break;
                }
            }
        }

        private Vector3 GetDesiredEndPosition()
        {
            return endPoint != null ? endPoint.position : fallbackEndPosition;
        }

        private Vector3 GetPlanningEndPosition()
        {
            if (!_hasPlannedEndPosition)
            {
                _plannedEndPosition = GetDesiredEndPosition();
                _hasPlannedEndPosition = true;
            }

            return _plannedEndPosition;
        }

        private void SnapPlannedGoalToDesired()
        {
            _plannedEndPosition = GetDesiredEndPosition();
            _hasPlannedEndPosition = true;
        }

        private bool SyncPlannedGoalToDesired()
        {
            var desiredEndPosition = GetDesiredEndPosition();
            if (!_hasPlannedEndPosition)
            {
                _plannedEndPosition = desiredEndPosition;
                _hasPlannedEndPosition = true;
                return true;
            }

            var previousPlannedEnd = _plannedEndPosition;
            _plannedEndPosition = desiredEndPosition;
            return Vector3.Distance(previousPlannedEnd, _plannedEndPosition) > 0.0001f;
        }
    }
}
