using System;
using UnityEngine;


namespace Assets.Script.Scene
{
    public class SceneLoader : MonoBehaviour
    {
        public bool isActive;
        public string sceneToLoad;

        public void ForceLoad(string v)
        {
            if (CoreManager.instance.ScenePlan.TryGetValue(v, out var scene))
                scene().Perform();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isActive) return;
            if (!other.gameObject.CompareTag("Player")) return;

            if (CoreManager.instance.ScenePlan.TryGetValue(sceneToLoad, out var scene))
                scene().Perform();
        }
    }
}