using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(menuName = "Théatre")]
[Tooltip("Gère la configuration des scènes" +
    "Les scènes sont chargér et déchargé dands l'ordre d'assignation")]
public class ScenesLoadersData : ScriptableObject
{
    [Header("Main Scène")]
    [Tooltip("Scène qui sera considerer comme actif (pour les instantiates)")]
    [SerializeField] private SceneReferenceData _mainSceneReference;
    [Tooltip("Nom représentant le théatre")]
    [SerializeField] private string _theatreName;

    [Header("Additive Scène")]
    [Tooltip("Scène additive d'un' théatre")]
    [SerializeField] private SceneReferenceData[] _additiveSceneReference;

    [Header("Remove Scène")]
    [Tooltip("Scène a supprimé lors du chargement d'un théatre")]
    [SerializeField] private SceneReferenceData[] _removeSceneReference;

    public SceneReferenceData MainSceneReference => _mainSceneReference;
    public SceneReferenceData[] AdditiveSceneReference => _additiveSceneReference;
    public SceneReferenceData[] RemoveSceneReference => _removeSceneReference;
    public string TheatreName => _theatreName;
}

/// <summary>
/// Configuration des Scènes
/// Récupère l'accée de l'addresse de la scène
/// Récupère le nom de la scène
/// Récupère le FakeLoader
/// </summary>
[Serializable]
public class SceneReferenceData
{
    [Header("Scène Réfèrence")]
    [Tooltip("Addresse de la scène")]
    [SerializeField] private AssetReference _sceneReference;
    [SerializeField] private string _sceneName;
    [Tooltip("Défini si la scène reste pendant le changement d'un nouveau théatre")]
    [SerializeField] private bool _exception;

    [Header("Loader Content")]
    public FakeLoader[] _loadersContent;

    public AssetReference SceneReference => _sceneReference;
    public string SceneName => _sceneName;
    public bool Exception => _exception;
}

/// <summary>
/// Configuration du FakeLoader
/// Détermine le temps de chargement
/// Affiche un text dans le loadingAsync de changement de scène
/// </summary>
[Serializable]
public struct FakeLoader
{
    public float LoadProgress;
    public float Duration;
    public string Content;
}