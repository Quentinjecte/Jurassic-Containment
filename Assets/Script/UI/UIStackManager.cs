using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gère une pile d'interfaces UI (CanvasGroup)
/// </summary>
public class UIStackManager : MonoBehaviour
{

    #region Singleton
    public static UIStackManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #endregion

    [SerializeField]
    private Stack<GameObject> stackUI = new();
    private CanvasGroup previousOverlay;

    public bool IsPersistent;

    public event Action OnInterfaceChanged;
    public event Action OnPop;

    #region Unity Lifecycle
    private void OnEnable()
    {
        OnInterfaceChanged += UpdateCursorState;
    }

    private void OnDisable()
    {
        OnInterfaceChanged -= UpdateCursorState;
    }
    #endregion

    #region Public API
    /// <summary>
    /// Retire puis ajoute une UI dans la stack
    /// </summary>
    public void PopPush(GameObject go)
    {
        if (go == null) return;

        Pop();

        // Désactive l'UI actuelle (si existe)
        if (stackUI.Count > 0)
        {
            var currentTop = GetCanvasGroup(stackUI.Peek());
            if (currentTop != null)
                SetActive(currentTop, false);
        }

        stackUI.Push(go);

        var cg = GetCanvasGroup(go);
        if (cg == null) return;

        SetActive(cg, true);
        OnInterfaceChanged?.Invoke();
    }

    /// <summary>
    /// Ajoute une UI dans la stack
    /// </summary>
    public void Push(GameObject go)
    {
        if (go == null) return;

        stackUI.Push(go);

        var cg = GetCanvasGroup(go);
        if (cg == null) return;

        SetActive(cg, true);
        OnInterfaceChanged?.Invoke();
    }

    /// <summary>
    /// Retire l'UI du dessus
    /// </summary>
    public void Pop()
    {
        if (stackUI.Count == 0)
            return;

        GameObject top = stackUI.Pop();

        var cg = GetCanvasGroup(top);
        if (cg != null)
            SetActive(cg, false);

        OnPop?.Invoke();

        // Réactive l'UI précédente si elle existe
        if (stackUI.Count > 0)
        {
            var next = GetCanvasGroup(stackUI.Peek());
            if (next != null)
                SetActive(next, true);
        }

        OnInterfaceChanged?.Invoke();
    }

    /// <summary>
    /// Vérifie si la stack contient des UI
    /// </summary>
    public bool HasStack() => stackUI.Count > 0;

    /// <summary>
    /// Switch un overlay indépendant de la stack
    /// </summary>
    public void SwitchOverlay(GameObject go)
    {
        if (go == null) return;

        var group = GetCanvasGroup(go);
        if (group == null) return;

        if (previousOverlay != null)
            SetActive(previousOverlay, false);

        previousOverlay = group;
        SetActive(group, true);
    }

    #endregion

    #region Private Helpers

    private CanvasGroup GetCanvasGroup(GameObject go)
    {
        if (!go.TryGetComponent(out CanvasGroup cg))
        {
            Debug.LogWarning($"[UIStackManager] Pas de CanvasGroup sur {go.name}");
            return null;
        }

        return cg;
    }

    private void SetActive(CanvasGroup cg, bool active)
    {
        cg.alpha = active ? 1f : 0f;
        cg.interactable = active;
        cg.blocksRaycasts = active;
    }

    private void UpdateCursorState()
    {
        if (HasStack())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (!IsPersistent)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    #endregion
}
