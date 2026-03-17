using NUnit.Framework.Constraints;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public GameObject OptionPanel;
    public GameObject HostPanel;
    public void StartSinglePlayer()
    {
        SceneManager.LoadScene("Map");
    }
    public void MultiPlayer()
    {
        HostPanel.SetActive(true);
    }
    public void CloseMultiPlayer()
    {
        HostPanel.SetActive(false);
    }
    public void Option()
    {
        OptionPanel.SetActive(true);
    }
    public void CloseOption()
    {
        OptionPanel.SetActive(false);
    }
    public void Quit()
    {
        Application.Quit();
    }
    
}
