using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class EndGameScoring : MonoBehaviour
{
    public GameObject WinnerText;
    public TextMeshProUGUI p1_Score_Text;
    public TextMeshProUGUI p2_Score_Text;
    public int p1_score;
    public int p2_score;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        p1_score = GameManager.player1_total_score;
        p2_score = GameManager.player2_total_score;
    }

    // Update is called once per frame
    void Update()
    {
            p1_Score_Text.text = "" + p1_score;
            p2_Score_Text.text = "" + p2_score;
            if(p1_score >= p2_score)
            {
                WinnerText.GetComponentInChildren<TextMeshProUGUI>().text = "Player 1 Wins!";
                WinnerText.GetComponentInChildren<TextMeshProUGUI>().color = Color.red;
            }
            else if (p2_score > p1_score)
            {
                WinnerText.GetComponentInChildren<TextMeshProUGUI>().text = "Player 2 Wins!";
                WinnerText.GetComponentInChildren<TextMeshProUGUI>().color = Color.blue;
            }
            else
            {
                WinnerText.GetComponentInChildren<TextMeshProUGUI>().text = "Tie Game!";
                WinnerText.GetComponentInChildren<TextMeshProUGUI>().color = Color.grey;
            }
    }
    public void returnMainMenu()
    {
        SceneManager.LoadScene(0);
    }
    
}
