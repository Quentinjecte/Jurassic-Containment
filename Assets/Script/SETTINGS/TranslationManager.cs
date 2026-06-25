using System;
using System.Collections.Generic;
using UnityEngine;

public class TranslationManager : MonoBehaviour
{
    public static TranslationManager instance;

    public int index;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // <- important !
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetLanguage(int index)
    {
        this.index = index;
        // Mettre à jour tous les textes
        foreach (TranslationText text in FindObjectsByType<TranslationText>(FindObjectsSortMode.None))
        {
            text.UpdateText();
        }
    }

    public int GetLanguageIndex() => index;
}

