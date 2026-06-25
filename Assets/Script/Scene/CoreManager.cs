using System;
using System.Collections.Generic;
using UnityEngine;
using static SceneController;

public class CoreManager : MonoBehaviour
{
    #region Singleton
    public static CoreManager instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (var t in _theatre)
            Theatre[t.TheatreName] = t;
    }
    #endregion

    public Dictionary<string, Func<SceneTransitionPlan>> ScenePlan { get; private set; } = new();
    public Dictionary<string, ScenesLoadersData> Theatre { get; private set; } = new();
    public event Action ScenePlanChanged;

    [SerializeField] private ScenesLoadersData[] _theatre;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Crée toutes les Funcs (un par theatre)
        foreach (var t in Theatre)
            ScenePlan[t.Key] = new Func<SceneTransitionPlan>(SceneController.instance.NewTransitionPlan);

        //Assigne au Funcs les valeur du théatre.
        foreach (var plan in ScenePlan)
            plan.Value.Invoke().LoadTheatre(plan.Key, Theatre[plan.Key]);

        //Active le théatre par defaut (le lobby)
        Perform("MainMenu");
    }
    public void Perform(string theatre) => ScenePlan[theatre]().Perform();

   /* public ScenesLoadersData GetTheatre(string theatreName) => Theatre[theatreName];

    public SceneTransitionPlan LoadMenu() =>// Load Map one
        SceneController.instance.
            NewTransitionPlan()                                                             // Create a new plan
            .LoadTheatre(GetTheatre("MainMenu").TheatreName, GetTheatre("MainMenu"))        // Load New Théatre
            .WithClearUnusedAssets()                                                        // Clear any asset not used
            .WithOverlay()                                                                  // Show loading Screen
            .WithPlayerCall();                                                              // Waiting any action by player


    public SceneTransitionPlan LoadSHop() =>// Load Shop
        SceneController.instance.
            NewTransitionPlan()                                                                         // Create a new plan
            .Load(SceneDataBase.Slots.Shop, SceneDataBase.Scenes.Shop, setActive: true)                 // Load player
            .UnloadAll()
            .WithClearUnusedAssets();                                                                   // Clear any asset not used

    
    public SceneTransitionPlan LoadMenu() =>
        SceneController.instance.
            NewTransitionPlan()
            .Load(SceneDataBase.Slots.Menu, SceneDataBase.Scenes.MainMenu, setActive: true)
            .UnloadAll()
            .WithClearUnusedAssets();*/
}
