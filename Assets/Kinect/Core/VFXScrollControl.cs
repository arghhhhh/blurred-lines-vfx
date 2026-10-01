using UnityEngine;
using UnityEngine.VFX;

namespace KinectVfx
{
    public class VFXScrollControl : MonoBehaviour
    {
        public VisualEffect vfxGraph;
        public bool useMouseScrollWheel = true;
        public bool useLeftRightArrowKeys = true;
        public bool useUpDownArrowKeys = true;
        public string scrollControlledProperty = "Focus Distance";
        public string leftRightProperty = "Clipping Plain Near";
        public string upDownProperty = "Clipping Plain Far";
        public float scrollSpeed = 0.5f;
        public float arrowSpeed = 0.5f;

        void Start()
        {
            if (vfxGraph == null)
                vfxGraph = GetComponent<VisualEffect>();
        }

        void Update()
        {
            if (vfxGraph == null) return;

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (useMouseScrollWheel && scroll != 0f)
            {
                float current = vfxGraph.GetFloat(scrollControlledProperty);
                current += scroll * scrollSpeed;
                vfxGraph.SetFloat(scrollControlledProperty, current);
            }

            if (useLeftRightArrowKeys && (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow)))
            {
                float delta = (Input.GetKey(KeyCode.RightArrow) ? 1f : -1f) * arrowSpeed * Time.deltaTime;
                float near = vfxGraph.GetFloat(leftRightProperty);
                vfxGraph.SetFloat(leftRightProperty, near + delta);
            }

            if (useUpDownArrowKeys && (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.UpArrow)))
            {
                float delta = (Input.GetKey(KeyCode.UpArrow) ? 1f : -1f) * arrowSpeed * Time.deltaTime;
                float far = vfxGraph.GetFloat(upDownProperty);
                vfxGraph.SetFloat(upDownProperty, far + delta);
            }
        }
    }
}
