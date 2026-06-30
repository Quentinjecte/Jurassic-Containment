using UnityEditor;
using UnityEngine;

namespace KINEMATION.KAnimationCore.Editor.Tools
{
    public class CopyBoneTool : IEditorTool
    {
        private Transform _root;
        private Transform _extractFrom;
        private Transform _extractTo;

        private AnimationClip _clip;
        private AnimationClip _refClip;

        private Vector3 _rotationOffset;
        private bool _isAdditive;

        private enum AxisDirection { PosX, NegX, PosY, NegY, PosZ, NegZ }

        private AxisDirection _sourceForward = AxisDirection.PosZ;
        private AxisDirection _sourceUp = AxisDirection.PosY;
        private AxisDirection _sourceRight = AxisDirection.PosX;

        private static int AxisIndex(AxisDirection dir) =>
            dir switch
            {
                AxisDirection.PosX or AxisDirection.NegX => 0,
                AxisDirection.PosY or AxisDirection.NegY => 1,
                AxisDirection.PosZ or AxisDirection.NegZ => 2,
                _ => 0
            };

        private static float AxisSign(AxisDirection dir) =>
            dir switch
            {
                AxisDirection.NegX or AxisDirection.NegY or AxisDirection.NegZ => -1f,
                _ => 1f
            };

        private Vector3 RemapVector(Vector3 input)
        {
            float[] arr = { input.x, input.y, input.z };
            return new Vector3(
                arr[AxisIndex(_sourceRight)] * AxisSign(_sourceRight),
                arr[AxisIndex(_sourceUp)] * AxisSign(_sourceUp),
                arr[AxisIndex(_sourceForward)] * AxisSign(_sourceForward)
            );
        }

        private Quaternion RemapQuaternion(Quaternion input)
        {
            float[] arr = { input.x, input.y, input.z };
            return new Quaternion(
                arr[AxisIndex(_sourceRight)] * AxisSign(_sourceRight),
                arr[AxisIndex(_sourceUp)] * AxisSign(_sourceUp),
                arr[AxisIndex(_sourceForward)] * AxisSign(_sourceForward),
                input.w
            );
        }

        private static Vector3 GetVectorValue(AnimationClip clip, EditorCurveBinding[] bindings, float time)
        {
            var curveX = AnimationUtility.GetEditorCurve(clip, bindings[0]);
            var curveY = AnimationUtility.GetEditorCurve(clip, bindings[1]);
            var curveZ = AnimationUtility.GetEditorCurve(clip, bindings[2]);

            if (curveX == null || curveY == null || curveZ == null)
            {
                Debug.LogError("One or more position curves are null!");
                return Vector3.zero;
            }

            return new Vector3(curveX.Evaluate(time), curveY.Evaluate(time), curveZ.Evaluate(time));
        }

        private static Quaternion GetQuatValue(AnimationClip clip, EditorCurveBinding[] bindings, float time)
        {
            var curveX = AnimationUtility.GetEditorCurve(clip, bindings[0]);
            var curveY = AnimationUtility.GetEditorCurve(clip, bindings[1]);
            var curveZ = AnimationUtility.GetEditorCurve(clip, bindings[2]);
            var curveW = AnimationUtility.GetEditorCurve(clip, bindings[3]);

            if (curveX == null || curveY == null || curveZ == null || curveW == null)
            {
                Debug.LogError("One or more rotation curves are null!");
                return Quaternion.identity;
            }

            return new Quaternion(curveX.Evaluate(time), curveY.Evaluate(time), curveZ.Evaluate(time), curveW.Evaluate(time));
        }

        private string GetBonePath(Transform targetBone, Transform root)
        {
            if (targetBone == null || root == null) return "";

            string path = targetBone.name;
            Transform current = targetBone.parent;

            while (current != null && current != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return (current == root) ? path : null;
        }

        private void ExtractAndSetAnimationData()
        {
            string fromPath = GetBonePath(_extractFrom, _root);
            if (string.IsNullOrEmpty(fromPath))
            {
                Debug.LogError("Cannot find path from root to source bone!");
                return;
            }

            // Get bindings from the REFERENCE animClip (where we copy FROM)
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(_refClip);

            EditorCurveBinding[] tBindings = new EditorCurveBinding[3];
            EditorCurveBinding[] rBindings = new EditorCurveBinding[4];

            foreach (var binding in bindings)
            {
                // Match the full path, not just the ending
                if (binding.path != fromPath) continue;

                string prop = binding.propertyName.ToLower();

                if (prop.Contains("m_localposition.x")) tBindings[0] = binding;
                else if (prop.Contains("m_localposition.y")) tBindings[1] = binding;
                else if (prop.Contains("m_localposition.z")) tBindings[2] = binding;
                else if (prop.Contains("m_localrotation.x")) rBindings[0] = binding;
                else if (prop.Contains("m_localrotation.y")) rBindings[1] = binding;
                else if (prop.Contains("m_localrotation.z")) rBindings[2] = binding;
                else if (prop.Contains("m_localrotation.w")) rBindings[3] = binding;
            }

            // Validate we found all curves
            if (tBindings[0].propertyName == null || rBindings[0].propertyName == null)
            {
                Debug.LogError($"Could not find animation curves for bone '{_extractFrom.name}' in reference animClip!");
                return;
            }

            Vector3 refTranslation = Vector3.zero;
            Quaternion refRotation = Quaternion.identity;

            if (_isAdditive)
            {
                refTranslation = GetVectorValue(_refClip, tBindings, 0f);
                refRotation = GetQuatValue(_refClip, rBindings, 0f) * Quaternion.Euler(_rotationOffset);
            }

            AnimationCurve tX = new AnimationCurve();
            AnimationCurve tY = new AnimationCurve();
            AnimationCurve tZ = new AnimationCurve();

            AnimationCurve rX = new AnimationCurve();
            AnimationCurve rY = new AnimationCurve();
            AnimationCurve rZ = new AnimationCurve();
            AnimationCurve rW = new AnimationCurve();

            float playLength = _refClip.length;
            float frameRate = 1f / _refClip.frameRate;
            float playBack = 0f;

            while (playBack <= playLength)
            {
                Vector3 translation = GetVectorValue(_refClip, tBindings, playBack);
                Quaternion rotation = GetQuatValue(_refClip, rBindings, playBack) * Quaternion.Euler(_rotationOffset);

                Vector3 deltaT = translation - refTranslation;
                Quaternion deltaR = Quaternion.Inverse(refRotation) * rotation;

                deltaT = RemapVector(deltaT);
                deltaR = RemapQuaternion(deltaR);

                tX.AddKey(playBack, deltaT.x);
                tY.AddKey(playBack, deltaT.y);
                tZ.AddKey(playBack, deltaT.z);

                rX.AddKey(playBack, deltaR.x);
                rY.AddKey(playBack, deltaR.y);
                rZ.AddKey(playBack, deltaR.z);
                rW.AddKey(playBack, deltaR.w);

                playBack += frameRate;
            }

            string toPath = GetBonePath(_extractTo, _root);
            if (string.IsNullOrEmpty(toPath))
            {
                Debug.LogError("Cannot find path from root to target bone!");
                return;
            }

            // Set curves on the TARGET animClip
            _clip.SetCurve(toPath, typeof(Transform), "m_LocalPosition.x", tX);
            _clip.SetCurve(toPath, typeof(Transform), "m_LocalPosition.y", tY);
            _clip.SetCurve(toPath, typeof(Transform), "m_LocalPosition.z", tZ);

            _clip.SetCurve(toPath, typeof(Transform), "m_LocalRotation.x", rX);
            _clip.SetCurve(toPath, typeof(Transform), "m_LocalRotation.y", rY);
            _clip.SetCurve(toPath, typeof(Transform), "m_LocalRotation.z", rZ);
            _clip.SetCurve(toPath, typeof(Transform), "m_LocalRotation.w", rW);

            EditorUtility.SetDirty(_clip);
            AssetDatabase.SaveAssets();

            Debug.Log($"Successfully copied animation from '{_extractFrom.name}' to '{_extractTo.name}'");
        }

        public void Init() { }

        public void Render()
        {
            if (!EditorGUIUtility.wideMode)
                EditorGUIUtility.wideMode = true;

            GUILayout.Label("Settings", EditorStyles.boldLabel);

            _refClip = (AnimationClip)EditorGUILayout.ObjectField("Reference Animation", _refClip, typeof(AnimationClip), true);
            _clip = (AnimationClip)EditorGUILayout.ObjectField("Target Animation", _clip, typeof(AnimationClip), true);

            _root = (Transform)EditorGUILayout.ObjectField("Root", _root, typeof(Transform), true);
            _extractFrom = (Transform)EditorGUILayout.ObjectField("From", _extractFrom, typeof(Transform), true);
            _extractTo = (Transform)EditorGUILayout.ObjectField("To", _extractTo, typeof(Transform), true);

            _rotationOffset = EditorGUILayout.Vector3Field("Rotation Offset", _rotationOffset);
            _isAdditive = EditorGUILayout.Toggle("Is Additive", _isAdditive);

            GUILayout.Space(10);
            GUILayout.Label("Axis Mapping", EditorStyles.boldLabel);

            _sourceForward = (AxisDirection)EditorGUILayout.EnumPopup("Source Forward Axis", _sourceForward);
            _sourceUp = (AxisDirection)EditorGUILayout.EnumPopup("Source Up Axis", _sourceUp);
            _sourceRight = (AxisDirection)EditorGUILayout.EnumPopup("Source Right Axis", _sourceRight);

            if (_clip == null)
            {
                EditorGUILayout.HelpBox("Please specify the Target Animation!", MessageType.Warning);
                return;
            }

            if (_refClip == null)
            {
                EditorGUILayout.HelpBox("Please specify the Reference Animation!", MessageType.Warning);
                return;
            }

            if (_root == null || _extractFrom == null || _extractTo == null)
            {
                EditorGUILayout.HelpBox("Please specify the bones!", MessageType.Warning);
                return;
            }

            if (GUILayout.Button("Copy Bone"))
            {
                ExtractAndSetAnimationData();
            }
        }

        public string GetToolCategory() => "Animation/";
        public string GetToolName() => "Copy Bone";
        public string GetDocsURL() => string.Empty;
        public string GetToolDescription() => "Copy curves from one bone to another, with optional additive blending and axis remapping.";
    }
}