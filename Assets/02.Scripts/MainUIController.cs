using UnityEngine;
using UnityEngine.UI;

public class MainUIController : MonoBehaviour
{
    public Button startButton;

    void Start()
    {
        startButton.onClick.AddListener(GameManager.Instance.StartGame);
    }
}
