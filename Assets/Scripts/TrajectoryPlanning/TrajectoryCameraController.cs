using UnityEngine;

namespace TrajectoryPlanning
{
    [RequireComponent(typeof(Camera))]
    public sealed class TrajectoryCameraController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float lookSpeed = 120f;
        [SerializeField] private float zoomSpeed = 10f;

        private void Update()
        {
            var cameraComponent = GetComponent<Camera>();
            if (cameraComponent == null || !cameraComponent.enabled)
            {
                return;
            }

            UpdateRotation();
            UpdateTranslation();
        }

        private void UpdateRotation()
        {
            if (!Input.GetMouseButton(1))
            {
                return;
            }

            var yaw = Input.GetAxis("Mouse X") * lookSpeed * Time.deltaTime;
            var pitch = -Input.GetAxis("Mouse Y") * lookSpeed * Time.deltaTime;
            transform.Rotate(Vector3.up, yaw, Space.World);
            transform.Rotate(Vector3.right, pitch, Space.Self);
        }

        private void UpdateTranslation()
        {
            var forward = Input.GetAxisRaw("Vertical");
            var strafe = Input.GetAxisRaw("Horizontal");
            var lift = 0f;

            if (Input.GetKey(KeyCode.E))
            {
                lift += 1f;
            }

            if (Input.GetKey(KeyCode.Q))
            {
                lift -= 1f;
            }

            var direction = (transform.forward * forward) + (transform.right * strafe) + (Vector3.up * lift);
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            transform.position += direction * moveSpeed * Time.deltaTime;

            var scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > Mathf.Epsilon)
            {
                transform.position += transform.forward * scroll * zoomSpeed * Time.deltaTime * 10f;
            }
        }
    }
}

