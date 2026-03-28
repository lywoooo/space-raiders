using UnityEngine;

public class QuitManager : MonoBehaviour
{
    public void QuitGame()
    {
        Application.Quit();

        Debug.Log("Quit Game button clicked!");
    }
}
