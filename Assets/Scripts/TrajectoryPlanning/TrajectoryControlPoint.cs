using UnityEngine;

namespace TrajectoryPlanning
{
    public sealed class TrajectoryControlPoint : MonoBehaviour
    {
        public enum ControlPointRole
        {
            Start,
            End
        }

        public enum DragPlane
        {
            XY,
            XZ
        }

        [SerializeField] private DragPlane dragPlane = DragPlane.XZ;

        private Camera _dragCamera;
        private Plane _movementPlane;
        private float _dragOffset;
        private TrajectoryDemoController _controller;
        private ControlPointRole _role;

        public void Initialize(TrajectoryDemoController controller, ControlPointRole role)
        {
            _controller = controller;
            _role = role;
        }

        private void OnMouseDown()
        {
            _dragCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (_dragCamera == null)
            {
                return;
            }

            _movementPlane = dragPlane == DragPlane.XY
                ? new Plane(Vector3.forward, transform.position)
                : new Plane(Vector3.up, transform.position);

            var ray = _dragCamera.ScreenPointToRay(Input.mousePosition);
            if (_movementPlane.Raycast(ray, out var enter))
            {
                var hitPoint = ray.GetPoint(enter);
                _dragOffset = dragPlane == DragPlane.XY
                    ? transform.position.z - hitPoint.z
                    : transform.position.y - hitPoint.y;
            }
        }

        private void OnMouseDrag()
        {
            if (_dragCamera == null)
            {
                return;
            }

            var ray = _dragCamera.ScreenPointToRay(Input.mousePosition);
            if (!_movementPlane.Raycast(ray, out var enter))
            {
                return;
            }

            var hitPoint = ray.GetPoint(enter);
            transform.position = dragPlane == DragPlane.XY
                ? new Vector3(hitPoint.x, hitPoint.y, hitPoint.z + _dragOffset)
                : new Vector3(hitPoint.x, hitPoint.y + _dragOffset, hitPoint.z);

            if (_controller != null)
            {
                _controller.HandleControlPointChanged(_role);
            }
        }
    }
}
