using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneController : MonoBehaviour
{
    #region Singleton
    public static SceneController instance;

    private void Awake()
    {
        if(instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #endregion

    [SerializeField] private LoadingOverlay loadingOverlay;
    //private Dictionary<string, string> loadedScene = new(); --- olf
    private Dictionary<string, AsyncOperationHandle<SceneInstance>> loadedScene = new();
    //private List<AsyncOperation> ops = new List<AsyncOperation>(); --- old
    private List<AsyncOperationHandle<SceneInstance>> ops = new();
    [SerializeField] private Slider slider; // Overlay, % 
    [SerializeField] private TextMeshProUGUI progessTxt; // Overlay, % du chargement
    [SerializeField] private TextMeshProUGUI LoadingTxt; // Overlay, text
    private bool isBusy = false;

    // ─── API ─────────────────────────────────────────────────────────
    public SceneTransitionPlan NewTransitionPlan()
    {
        return new SceneTransitionPlan();
    }

    private Coroutine ExecutePlan(SceneTransitionPlan plan)
    {
        if (isBusy)
        {
            Debug.LogWarning("Scene change already in progress.");
            return null;
        }
        isBusy = true;
        ops.Clear();

        return StartCoroutine(ChangeSceneRoutine(plan));
    }

    private IEnumerator ChangeSceneRoutine(SceneTransitionPlan plan)
    {
        slider.value = 0;
        progessTxt.text = 00.0 + "%";

        if (plan.Overlay)
        {
            LoadingTxt.text = "Loading...";
            yield return loadingOverlay.FadeIn();
            yield return new WaitForSeconds(0.5f);
        }
        foreach(var Key in plan.TheatreToLoad.RemoveSceneReference)
        {
            yield return UnloadSceneRoutine(Key.SceneName);
        }
        if (plan.ClearUnusedAssets) yield return CleanupUnusedAssetsRoutine();
        if (loadedScene.ContainsKey(plan.TheatreToLoad.TheatreName))
        {
            yield return UnloadSceneRoutine(plan.TheatreToLoad.TheatreName);

            yield return LoadSceneDeferredRoutine(plan.TheatreToLoad.MainSceneReference, setActive: true);
            foreach (var Key in plan.TheatreToLoad.AdditiveSceneReference)
                yield return LoadSceneDeferredRoutine(Key);

            if (plan.Overlay)
            {
                LoadingTxt.text = "Game loaded. Press any key for start.";
                if (plan.PlayerCall)//<----- New option
                    while (!Keyboard.current.anyKey.wasPressedThisFrame &&
                            !Mouse.current.leftButton.wasPressedThisFrame &&
                            !Gamepad.current?.buttonSouth.wasPressedThisFrame == true) //<----- Wait any input press
                        yield return null;

                yield return loadingOverlay.FadeOut();
            }

            foreach (var op in ops) // Finish to load all scene : 90% -> 100%
                ActivateScene(op);

            isBusy = false;
        }
    }

    private IEnumerator CleanupUnusedAssetsRoutine()
    {
        AsyncOperation cleanupOp = Resources.UnloadUnusedAssets();
        while(!cleanupOp.isDone) yield return null;
    }

    private IEnumerator UnloadSceneRoutine(string slotKey)
    {
        if(!loadedScene.TryGetValue(slotKey, out var result)) yield break;

        AsyncOperationHandle<SceneInstance> oldHandle = default;
        try
        {
            oldHandle = Addressables.UnloadSceneAsync(result);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"An error occurred loading {result} : " + e);
            yield break;
        }

        //if (string.IsNullOrEmpty(result)) yield break;
        //AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(result);
        //while (!Handle.IsDone) yield return null;

        yield return oldHandle;

        if(oldHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogWarning($"An error occurred unloading {oldHandle.OperationException}");
            yield break;
        }

        loadedScene.Remove(slotKey);
    }

    private IEnumerator LoadSceneDeferredRoutine(SceneReferenceData Scene, bool setActive = false)
    {
        // Charger sans activer
        var handle = Scene.SceneReference.LoadSceneAsync(
            LoadSceneMode.Additive,
            activateOnLoad: false
        );

        float displayProgress = 0f;
        float timer = 0f;

        while (displayProgress < 0.99f)
        {
            // progression réelle (0 → 0.9)
            float realProgress = Mathf.Clamp01(handle.PercentComplete / 0.9f);
            // progression artificielle (ralentie)
            timer += Time.deltaTime;

            FakeLoader loader = Scene._loadersContent.FirstOrDefault(l => l.Duration == realProgress);
            float loadDuration = loader.Duration;
            float fakeProgress = Mathf.Clamp01(timer / loadDuration);

            // on affiche la plus petite des deux
            displayProgress = Mathf.Min(fakeProgress, realProgress);

            slider.value = displayProgress;
            progessTxt.text = (displayProgress * 100f) + "%";
            LoadingTxt.text = loader.Content;

            yield return null;
            //loadOp.allowSceneActivation = true; // la scène peut maintenant s'activer
        }

        yield return new WaitForSeconds(.5f);

        slider.value = 1;
        progessTxt.text = 100 + "%";

        ops.Add(handle); // Add to AOp list

        if (setActive)
        {
            Scene newScene = SceneManager.GetSceneByName(handle.Result.Scene.name);
            if (newScene.IsValid() && newScene.isLoaded)
            {
                SceneManager.SetActiveScene(newScene);
            }
        }

        loadedScene[Scene.SceneName] = handle;
    }

    private IEnumerator ActivateScene(AsyncOperationHandle<SceneInstance> handle)
    {
        yield return handle.Result.ActivateAsync();
    }

    public class SceneTransitionPlan
    {
        public ScenesLoadersData TheatreToLoad { get; set; } = new();
        //public string ActiveSceneName { get; private set; } = "";
        public bool ClearUnusedAssets { get; private set; } = false;
        public bool Overlay { get; private set; } = false;
        public bool PlayerCall { get; private set; } = false;

        public SceneTransitionPlan LoadTheatre(string slotKey, ScenesLoadersData theatre)
        {
            if (SceneController.instance.loadedScene.ContainsKey(slotKey))
                return this;

            TheatreToLoad = theatre;
            //if (setActive) ActiveSceneName = slotKey;
            return this;
        }

        /*public SceneTransitionPlan Unload(string slotKey)
        {
            ScenesToUnload.Add(slotKey);
            return this;
        }
        public SceneTransitionPlan UnloadAll()
        {
            string[] key = SceneController.instance.loadedScene
                .Where(w => w.Key != SceneDataBase.Slots.Menu &&
                w.Key != SceneDataBase.Scenes.MainMenu)
                .Select(w => w.Key)
                .ToArray();

            foreach(var slotKey in key)
                ScenesToUnload.Add(slotKey);

            return this;
        }
        public SceneTransitionPlan UnloadAllException(string exeption)
        {
            string[] keys = SceneController.instance.loadedScene
                .Where(w => w.Key != exeption)
                .Select(w => w.Key)
                .ToArray();

            foreach (var slotKey in keys)
            {
                ScenesToUnload.Add(slotKey);
            }

            return this;
        }*/

        public SceneTransitionPlan WithOverlay()
        {
            Overlay = true; 
            return this;
        }
        public SceneTransitionPlan WithPlayerCall() //<----- New option, define if the player need to press for continue or not
        {
            PlayerCall = true; 
            return this;
        }
        public SceneTransitionPlan WithClearUnusedAssets()
        {
            ClearUnusedAssets = true;
            return this;
        }
        public Coroutine Perform() => SceneController.instance.ExecutePlan(this);
    }
}
