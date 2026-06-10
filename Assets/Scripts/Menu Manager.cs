using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public GameObject currentCanvas;

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void SwitchCanvas(GameObject newCanvas)
    {
        currentCanvas.SetActive(false);
        newCanvas.SetActive(true);
        currentCanvas = newCanvas;
    }
}
