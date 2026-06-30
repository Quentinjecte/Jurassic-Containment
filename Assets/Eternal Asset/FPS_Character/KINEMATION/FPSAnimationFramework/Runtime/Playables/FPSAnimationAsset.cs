// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.KAnimationCore.Runtime.Rig;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace KINEMATION.FPSAnimationFramework.Runtime.Playables
{
    [HelpURL("https://kinemation.gitbook.io/scriptable-animation-system/workflow/playing-animations")]
    [CreateAssetMenu(fileName = "NewAnimation", menuName = FPSANames.FileMenuGeneral + "Animation Asset")]
    public class FPSAnimationAsset : ScriptableObject, IRigUser
    {
        [Tooltip("Target rig asset.")]
        public KRig rigAsset;

        [Header("Audio")]
        [Tooltip("Select your sound effect.")]
        public AudioClip audioClip;
        
        [Header("Animation")]
        [Tooltip("Select your animation.")]
        public AnimationClip animClip;

        [Tooltip("What bones will be animated.")]
        public AvatarMask mask;
        
        [Tooltip("This mask will define what parts will be excluded from additive motion.")]
        public AvatarMask overrideMask;
        public bool isAdditive;

        [Header("Blend In/Out")]
        [Tooltip("Smooth blend in/out parameters.")]
        public BlendTime blendTime = new BlendTime(0.15f, 0.15f);
        public List<AnimCurve> curves;

        public static bool IsValid(FPSAnimationAsset asset)
        {
            return asset != null && asset.animClip != null;
        }

        public float GetTimeAtFrame(int frame)
        {
            if (animClip == null)
            {
                return 0f;
            }

            frame = frame < 0 ? frame * -1 : frame;
            return frame / (animClip.frameRate * animClip.length);
        }

        public float GetNormalizedTimeAtFrame(int frame)
        {
            if (animClip == null)
            {
                return 0f;
            }
            
            return GetTimeAtFrame(frame) / animClip.length;
        }

        public KRig GetRigAsset()
        {
            return rigAsset;
        }
    }
}