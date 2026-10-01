using UnityEngine;
using UnityEngine.SceneManagement;

namespace KinectVfx
{
    /// <summary>
    /// Press R to reload the entire scene. This is the safest way to
    /// recover from a ZED camera hiccup without fighting the SDK's
    /// internal threading.
    /// </summary>
    public class ZEDCameraReset : MonoBehaviour
    {
        public KeyCode resetKey = KeyCode.R;

        void Update()
        {
            if (Input.GetKeyDown(resetKey))
            {
                Debug.Log("[ZEDCameraReset] Reloading scene...");
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
    }
}
