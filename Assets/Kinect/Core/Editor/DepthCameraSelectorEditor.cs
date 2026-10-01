using UnityEditor;
using UnityEditor.SceneManagement;

namespace KinectVfx
{
    [CustomEditor(typeof(DepthCameraSelector))]
    public class DepthCameraSelectorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("source"));
            bool sourceChanged = EditorGUI.EndChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("vfxObjects"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("zedRig"));

            serializedObject.ApplyModifiedProperties();

            if (sourceChanged)
            {
                var selector = (DepthCameraSelector)target;
                selector.Apply();
                if (!EditorApplication.isPlaying)
                    EditorSceneManager.MarkSceneDirty(selector.gameObject.scene);
            }
        }
    }
}
