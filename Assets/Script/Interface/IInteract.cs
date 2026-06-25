using UnityEngine;

public interface IInteract
{
    string Prompt { get; }           // Texte ou info à afficher
    void Interact(GameObject Owner = null);
    void HoldInteract(GameObject owner = null);
    void EndInteract(GameObject owner = null);
    void Show();
}
