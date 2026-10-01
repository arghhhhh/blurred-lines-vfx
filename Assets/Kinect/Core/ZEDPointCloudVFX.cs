using UnityEngine;
using UnityEngine.VFX;
using sl;

namespace KinectVfx
{
    /// <summary>
    /// Bridges ZED SDK textures to VFX Graph by copying the ZED's auto-updated
    /// XYZ position and color textures into RenderTextures each frame.
    /// Attach to the same GameObject as the VisualEffect component.
    /// </summary>
    public class ZEDPointCloudVFX : MonoBehaviour
    {
        [Header("ZED")]
        public ZEDManager zedManager;

        [Header("VFX Graph Bindings")]
        public VisualEffect vfxGraph;

        [Tooltip("Name of the Texture2D property in the VFX Graph for position data.")]
        public string positionMapProperty = "PositionMap";

        [Tooltip("Name of the Texture2D property in the VFX Graph for color data.")]
        public string colorMapProperty = "ColorMap";

        [Tooltip("Name of the int property in the VFX Graph for total point count.")]
        public string pointCountProperty = "PointCount";

        [Header("Shaders")]
        public Shader flipPositionShader;
        public Shader flipColorShader;

        private sl.ZEDCamera zed;
        private Texture2D xyzTexture;
        private Texture2D colorTexture;
        private RenderTexture positionRT;
        private RenderTexture colorRT;
        private Material flipPositionMat;
        private Material flipColorMat;
        private bool initialized;

        void Start()
        {
            if (zedManager == null)
                zedManager = FindFirstObjectByType<ZEDManager>();

            if (vfxGraph == null)
                vfxGraph = GetComponent<VisualEffect>();

            zedManager.OnZEDDisconnected += OnDisconnected;
            zedManager.OnZEDReady += OnReconnected;
        }

        void Update()
        {
            if (zedManager == null) return;

            zed = zedManager.zedCamera;
            if (zed == null || !zed.IsCameraReady) return;

            if (!initialized)
            {
                Initialize();
            }

            if (initialized)
            {
                // Only blit when the grab succeeded — on failure, the last good
                // frame stays in the RenderTextures instead of going black.
                if (zedManager.ZEDGrabError != sl.ERROR_CODE.SUCCESS) return;

                Graphics.Blit(xyzTexture, positionRT, flipPositionMat);
                Graphics.Blit(colorTexture, colorRT, flipColorMat);
            }
        }

        private void OnDisconnected()
        {
            Debug.LogWarning("[ZEDPointCloudVFX] Camera disconnected — holding last frame.");
            Cleanup();
        }

        private void OnReconnected()
        {
            Debug.Log("[ZEDPointCloudVFX] Camera reconnected — reinitializing.");
        }

        private void Cleanup()
        {
            initialized = false;
            xyzTexture = null;
            colorTexture = null;

            if (positionRT != null)
            {
                positionRT.Release();
                Destroy(positionRT);
                positionRT = null;
            }

            if (colorRT != null)
            {
                colorRT.Release();
                Destroy(colorRT);
                colorRT = null;
            }
        }

        private void Initialize()
        {
            int width = zed.ImageWidth;
            int height = zed.ImageHeight;

            // Get fresh auto-updating GPU textures from the ZED SDK
            xyzTexture = zed.CreateTextureMeasureType(sl.MEASURE.XYZ);
            colorTexture = zed.CreateTextureImageType(sl.VIEW.LEFT);

            if (xyzTexture == null || colorTexture == null) return;

            // Create blit materials once
            if (flipPositionMat == null)
                flipPositionMat = new Material(flipPositionShader);
            if (flipColorMat == null)
                flipColorMat = new Material(flipColorShader);

            // Create RenderTextures matching ZED resolution
            positionRT = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
            positionRT.filterMode = FilterMode.Point;
            positionRT.wrapMode = TextureWrapMode.Clamp;
            positionRT.Create();

            colorRT = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            colorRT.filterMode = FilterMode.Point;
            colorRT.wrapMode = TextureWrapMode.Clamp;
            colorRT.Create();

            // Bind to VFX Graph
            if (vfxGraph != null)
            {
                vfxGraph.SetTexture(positionMapProperty, positionRT);
                vfxGraph.SetTexture(colorMapProperty, colorRT);
                vfxGraph.SetInt(pointCountProperty, width * height);
                Debug.Log($"[ZEDPointCloudVFX] Initialized: {width}x{height} = {width * height} points");
            }

            initialized = true;
        }

        private void OnDestroy()
        {
            if (zedManager != null)
            {
                zedManager.OnZEDDisconnected -= OnDisconnected;
                zedManager.OnZEDReady -= OnReconnected;
            }

            Cleanup();

            if (flipPositionMat != null) Destroy(flipPositionMat);
            if (flipColorMat != null) Destroy(flipColorMat);
        }
    }
}
