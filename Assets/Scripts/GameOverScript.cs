using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public TextMeshProUGUI finalText;

    void Start()
    {
        gameObject.SetActive(false);
    }

    public void showGameOver(int finalScore)
    {
        gameObject.SetActive(true);
        finalText.text = "Final Score: " + finalScore.ToString();
    }
}
