// Designed by KINEMATION, 2025.

using KINEMATION.FPSAnimationFramework.Runtime.Core;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Demo.Scripts.Runtime.AttachmentSystem
{
    public class BaseAttachment : MonoBehaviour
    {
        public List<FPSAnimatorLayerSettings> attachmentLayerSettings;
        public AudioClip fireSound;
        public Transform Canon 
            => transform.GetChild(0);
    }
}