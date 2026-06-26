using System;
using UnityEngine;

namespace Assets.Script.Utils
{
    public static class ComponentExtensions
    {
        public static T GetComponentSafe<T>(this Component component, string msg) where T : Component
        {
            try
            {
                return component.GetComponent<T>();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting component of type {typeof(T)}: {e.Message} + {msg}");
                return default;
            }
        }
    }
}
