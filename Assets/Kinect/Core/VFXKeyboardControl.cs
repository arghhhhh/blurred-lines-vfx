using System;
using UnityEngine;
using UnityEngine.VFX;

namespace KinectVfx
{
    /// <summary>
    /// Live keyboard/mouse control of exposed VFX Graph floats. Each binding
    /// nudges one property on every target graph that exposes it, so a single
    /// key pair can drive the same setting on several effects at once.
    /// </summary>
    public class VFXKeyboardControl : MonoBehaviour
    {
        [Serializable]
        public class Binding
        {
            public string property;
            public KeyCode decrease = KeyCode.None;
            public KeyCode increase = KeyCode.None;
            [Tooltip("Binding only fires while Shift is held; otherwise only while Shift is not held.")]
            public bool shift;
            [Tooltip("Units per second while a key is held.")]
            public float keySpeed = 1f;
            [Tooltip("Units per mouse wheel notch. 0 = not on the scroll wheel.")]
            public float scrollStep;
            public float min = float.NegativeInfinity;
            public float max = float.PositiveInfinity;
        }

        public VisualEffect[] targets;

        public Binding[] bindings =
        {
            new Binding { property = "Clipping Plain Near", decrease = KeyCode.LeftArrow, increase = KeyCode.RightArrow, keySpeed = 1f, min = 0f },
            new Binding { property = "Clipping Plain Far", decrease = KeyCode.LeftArrow, increase = KeyCode.RightArrow, shift = true, keySpeed = 3f, min = 0f },
            new Binding { property = "Focus Distance", decrease = KeyCode.DownArrow, increase = KeyCode.UpArrow, shift = true, keySpeed = 2f, scrollStep = 0.5f, min = 0f },
            new Binding { property = "Lines Brightness Multiplier", decrease = KeyCode.DownArrow, increase = KeyCode.UpArrow, keySpeed = 15f, min = 0f },
        };

        [Tooltip("Keeps the near plane at least this far in front of the far plane.")]
        public float minClipGap = 0.1f;
        public string nearProperty = "Clipping Plain Near";
        public string farProperty = "Clipping Plain Far";

        void Update()
        {
            if (targets == null) return;

            bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            // One wheel notch reads as 0.1 on the legacy axis.
            float notches = Input.GetAxis("Mouse ScrollWheel") * 10f;

            foreach (var b in bindings)
            {
                float delta = 0f;

                if (b.shift == shiftHeld)
                {
                    if (b.increase != KeyCode.None && Input.GetKey(b.increase)) delta += b.keySpeed * Time.deltaTime;
                    if (b.decrease != KeyCode.None && Input.GetKey(b.decrease)) delta -= b.keySpeed * Time.deltaTime;
                }

                if (b.scrollStep != 0f) delta += notches * b.scrollStep;

                if (delta != 0f) Nudge(b, delta);
            }
        }

        void Nudge(Binding b, float delta)
        {
            foreach (var vfx in targets)
            {
                if (vfx == null || !vfx.HasFloat(b.property)) continue;

                float value = Mathf.Clamp(vfx.GetFloat(b.property) + delta, b.min, b.max);

                if (b.property == nearProperty && vfx.HasFloat(farProperty))
                    value = Mathf.Min(value, vfx.GetFloat(farProperty) - minClipGap);
                else if (b.property == farProperty && vfx.HasFloat(nearProperty))
                    value = Mathf.Max(value, vfx.GetFloat(nearProperty) + minClipGap);

                vfx.SetFloat(b.property, value);
            }
        }
    }
}
