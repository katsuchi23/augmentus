using UnityEngine;

namespace TrajectoryPlanning
{
    public static class TrajectoryDemoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateDemoControllerIfMissing()
        {
            if (Object.FindFirstObjectByType<TrajectoryDemoController>() != null)
            {
                return;
            }

            var host = new GameObject("Trajectory Demo Controller");
            host.AddComponent<TrajectoryDemoController>();
        }
    }
}
