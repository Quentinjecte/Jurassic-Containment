using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TranslationText : MonoBehaviour
{
    public GameObject[] value; // Identifiant unique du texte
    public string[] valuetxt; // Identifiant unique du texte
    private void Start()
    {
        UpdateText(); // met à jour dès que le texte est instancié
    }

    public void UpdateText()
    {
        int i = TranslationManager.instance.GetLanguageIndex();

        if (value.Length == 0)
        {
            if (!TryGetComponent(out TextMeshProUGUI textUGUI))
                return;
            textUGUI.text = valuetxt[i];
            return;
        }

        value[i].SetActive(true);
        int v = (i - 1 + value.Length) % value.Length;
        value[v].SetActive(false);

    }
}
