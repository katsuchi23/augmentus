using System.Collections.Generic;
using UnityEngine;

namespace TrajectoryPlanning
{
    public sealed class TrajectoryCameraSwitcher : MonoBehaviour
    {
        [SerializeField] private List<Camera> cameras = new List<Camera>();
        [SerializeField] private int activeCameraIndex;

        private void Start()
        {
            ActivateCamera(activeCameraIndex);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                ActivateCamera(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                ActivateCamera(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                ActivateCamera(2);
            }
        }

        public void SetCameras(IEnumerable<Camera> configuredCameras)
        {
            cameras.Clear();
            cameras.AddRange(configuredCameras);
            ActivateCamera(activeCameraIndex);
        }

        private void ActivateCamera(int index)
        {
            if (cameras.Count == 0)
            {
                return;
            }

            activeCameraIndex = Mathf.Clamp(index, 0, cameras.Count - 1);
            for (var i = 0; i < cameras.Count; i++)
            {
                if (cameras[i] == null)
                {
                    continue;
                }

                var isActive = i == activeCameraIndex;
                cameras[i].enabled = isActive;
                cameras[i].tag = isActive ? "MainCamera" : "Untagged";
            }
        }
    }
}
