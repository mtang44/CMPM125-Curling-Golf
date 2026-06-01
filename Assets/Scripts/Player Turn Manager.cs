using TMPro;
using Unity.VectorGraphics;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class PlayerTurnManager : MonoBehaviour
{
    public GameObject player1_stone;
    public GameObject player2_stone;
    public GameObject ScoreBoard;
    public GameObject WinnerText;
    public GameObject RightArrow;
    public GameObject LeftArrow;

    public TextMeshProUGUI player1_score_text;
    public TextMeshProUGUI player2_score_text;
    
    
    public Camera main_camera;
    private Player player1 = new Player(1);
    private Player player2 = new Player(2);
    private GameObject CameraTarget;

    private GameObject scoring_target;
    
    private Vector3 cameraOffset;
   

    

    
    [SerializeField] private int currentPlayerIndex = 1;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RightArrow = GameObject.Find("Curve Right");
        LeftArrow = GameObject.Find("Curve Left");
        CameraTarget = Instantiate(player1_stone, new Vector3(0,1,0), Quaternion.identity);
        scoring_target = GameObject.Find("Scoring Target");
        cameraOffset = new Vector3(0,7,8);
        main_camera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        main_camera.transform.position = Vector3.MoveTowards(main_camera.transform.position, CameraTarget.transform.position + cameraOffset, Time.deltaTime * 100);
        player1_score_text.text = "" + player1.score;
        player2_score_text.text = "" + player2.score;
    }
    public void EndTurn()
    {
        UpdatePlayerScores();
        if(currentPlayerIndex == 1 && player1.stones_remaining > 0 ) // > 1 since player starts with stone already on field
        {
            player1.stones_remaining--;
            cameraOffset = new Vector3(0,7,8);
            CameraTarget = Instantiate(player2_stone, new Vector3(0,.2f,0), Quaternion.identity);
            
            currentPlayerIndex = 2;
        }
        else if(currentPlayerIndex == 2 && player2.stones_remaining > 0)
        {
            player2.stones_remaining--;
            cameraOffset = new Vector3(0,7,8);
            CameraTarget = Instantiate(player1_stone, new Vector3(0,.2f,0), Quaternion.identity);
            
        
            currentPlayerIndex = 1;
        }
        else
        {
            RightArrow.SetActive(false);
            LeftArrow.SetActive(false);
            WinnerText.SetActive(true);
            // end of game logic here 
            if(player1.score > player2.score)
            {
                WinnerText.GetComponentInChildren<TextMeshProUGUI>().text = "Player 1 Wins!";
                WinnerText.GetComponentInChildren<TextMeshProUGUI>().color = Color.red;
            }
            else if (player2.score > player1.score)
            {
                WinnerText.GetComponentInChildren<TextMeshProUGUI>().text = "Player 2 Wins!";
                WinnerText.GetComponentInChildren<TextMeshProUGUI>().color = Color.blue;
            }
            else
            {
                WinnerText.GetComponentInChildren<TextMeshProUGUI>().text = "Tie Game!";
                WinnerText.GetComponentInChildren<TextMeshProUGUI>().color = Color.grey;
            }
            cameraOffset = new Vector3(5,10,30);
            CameraTarget = ScoreBoard;
        }
    }
    public void UpdatePlayerScores()
    {
        player1.score = scoring_target.GetComponent<TargetScoring>().Calculate_Score(player1.player_number);
        player2.score = scoring_target.GetComponent<TargetScoring>().Calculate_Score(player2.player_number);
        Debug.Log("Player 1 Score: " + player1.score);
        Debug.Log("Player 2 Score: " + player2.score);
    }
    public void resetScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
