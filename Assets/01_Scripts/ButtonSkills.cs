using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonSkills : MonoBehaviour
{
    public void QuitGame()
    {
        Application.Quit();
    }
    public void SceneChange(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
