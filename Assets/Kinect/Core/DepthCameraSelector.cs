using UnityEngine;

namespace KinectVfx
{
    public enum DepthCameraSource
    {
        ZED,
        Kinect
    }

    /// <summary>
    /// Picks which depth camera feeds the VFX graphs. Enables the matching
    /// bridge component (ZEDPointCloudVFX or KinectPointCloud) on each VFX
    /// object and turns the ZED rig on only when the ZED is selected.
    /// The custom inspector applies the choice immediately, so it is saved with the scene.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class DepthCameraSelector : MonoBehaviour
    {
        public DepthCameraSource source = DepthCameraSource.ZED;

        [Tooltip("Objects carrying both ZEDPointCloudVFX and KinectPointCloud.")]
        public GameObject[] vfxObjects;

        [Tooltip("ZED rig (ZEDManager). Disabled when the Kinect is selected.")]
        public GameObject zedRig;

        void Awake()
        {
            Apply();
        }

        public void Apply()
        {
            bool useZed = source == DepthCameraSource.ZED;

            if (zedRig != null && zedRig.activeSelf != useZed)
            {
                Record(zedRig);
                zedRig.SetActive(useZed);
            }

            if (vfxObjects == null) return;

            foreach (var go in vfxObjects)
            {
                if (go == null) continue;

                var zed = go.GetComponent<ZEDPointCloudVFX>();
                if (zed != null && zed.enabled != useZed)
                {
                    Record(zed);
                    zed.enabled = useZed;
                }

                var kinect = go.GetComponent<KinectPointCloud>();
                if (kinect != null && kinect.enabled == useZed)
                {
                    Record(kinect);
                    kinect.enabled = !useZed;
                }
            }
        }

        static void Record(Object target)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.Undo.RecordObject(target, "Switch Depth Camera");
#endif
        }
    }
}
