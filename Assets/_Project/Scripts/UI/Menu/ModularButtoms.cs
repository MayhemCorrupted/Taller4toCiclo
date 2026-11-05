using UnityEngine;
using UnityEngine.SceneManagement;

public class ModularButtoms : MonoBehaviour
{
    public enum ButtonAction
    {
        ChangeScene,
        QuitGame,
        PauseGame,
        ResumeGame
    }

    public ButtonAction actionType;
    public string sceneName;

    public void ExecuteAction()
    {
        switch (actionType)
        {
            case ButtonAction.ChangeScene:
                SceneManager.LoadScene(sceneName);
                break;

            case ButtonAction.QuitGame:
                Debug.Log ("Quitting the game...");
                Application.Quit();
                break;

            case ButtonAction.PauseGame:
                Time.timeScale = 0f;
                break;

            case ButtonAction.ResumeGame:
                Time.timeScale = 1f;
                break;
        }
    }
}