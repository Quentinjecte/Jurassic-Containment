using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.Netcode;

public class UIManager : MonoBehaviour
{
    [Header("Settingd")]
    public bool isPersistent;

    #region Singleton
    public static UIManager Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    #endregion

    private void Start()
    {
        UIStackManager.Instance.IsPersistent = isPersistent; 
    }

    public static bool IsPointerOverUI()
    => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

    public void Close(GameObject gameObject)
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        gameObject.SetActive(false);
    }

    public void SetLanguage(int i) => TranslationManager.instance.SetLanguage(i);

    #region Volume
    public void MasterVolum(Slider slider) => MasterVolumeManager.instance.MasterVolum(slider);
    public void EffectVolum(Slider slider) => MasterVolumeManager.instance.EffectVolum(slider);
    public void MusicVolum(Slider slider) => MasterVolumeManager.instance.MasterVolum(slider);
    public void EnvironnementVolum(Slider slider) => MasterVolumeManager.instance.EnvironnementVolum(slider);
    #endregion

    #region Scene Loader
    public void LoadMenu() => CoreManager.instance.Perform("Menu");
    public void LoadGame() => CoreManager.instance.Perform("Map");
    public void CreatLobby()
    {
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene("LobbyMenu", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
    public void JoinLobby()
    {
        NetworkManager.Singleton.StartClient();
    }
    #endregion

    public void QuitApp() => Application.Quit();
}