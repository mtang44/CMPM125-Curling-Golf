using TMPro;
using Unity.VectorGraphics;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using System.Collections.Generic;
using UnityEditor.Build.Content;

public class PlayerTurnManager : MonoBehaviour
{
    public GameObject player1_stone_prefab;
    public GameObject player2_stone_prefab;
    //public GameObject ScoreBoard;
    //public GameObject WinnerText;
    // public GameObject RightArrow;
    // public GameObject LeftArrow;

    // public TextMeshProUGUI player1_score_text;
    // public TextMeshProUGUI player2_score_text;
    public GameObject spawnPosition; 
    
    
    public Camera main_camera;
    private GameObject player1;
    private GameObject player2;
    // [SerializeField] private Player player1 = new Player(1);
    // [SerializeField] private Player player2 = new Player(2);
    private GameObject CameraTarget;
    [SerializeField] private List<GameObject> scoring_targets;
    private bool currentHoleHasBeenScored = false;
    
    private Vector3 cameraOffset;
    private Vector3 CONST_CAMERA_STONE_OFFSET = new Vector3(0,7,8);
    private Vector3 CONST_CAMERA_SCOREBOARD_OFFSET = new Vector3(0,10,30);

    [SerializeField] private GameObject currentPlayer;
    // spawns player 1 and finds uo; 
    void Start()
    {
        // RightArrow = GameObject.Find("Curve Right");
        // LeftArrow = GameObject.Find("Curve Left");
        main_camera = Camera.main;
        cameraOffset = CONST_CAMERA_STONE_OFFSET;

        
        player1 = Instantiate(player1_stone_prefab, spawnPosition.transform.position, Quaternion.identity);
        player1.GetComponent<Player>().alreadySpawned = true;
        currentPlayer = player1; 
        CameraTarget = player1;
   
    }

    // constantly updates camera's position to follow current player's stone and updates score text on scoreboard;
    void Update()
    {
       
        if(player1 && player2)
        {
            
        }
      
        main_camera.transform.position = Vector3.MoveTowards(main_camera.transform.position, CameraTarget.transform.position + cameraOffset, Time.deltaTime * 100);
        // player1_score_text.text = "Player 1 Score: " + player1.score;
        // player2_score_text.text = "Player 2 Score: " + player2.score;
    }
    // function to score the hole and update player scores. 
    public void ScoreHole()
    {
        // player1.score = scoring_target.GetComponent<TargetScoring>().Calculate_Score(player1.player_number);
        //  player2.score = scoring_target.GetComponent<TargetScoring>().Calculate_Score(player2.player_number);
        scoring_targets[0].GetComponent<TargetScoring>().Calculate_Score();
        Debug.Log("Player 1 Score: " + player1.GetComponent<Player>().score);
        Debug.Log("Player 2 Score: " + player2.GetComponent<Player>().score);
    }
    public void EndTurn()
    {
        Debug.Log("Ending Turn");

        if(!player2)
        {
            player2 = Instantiate(player2_stone_prefab, spawnPosition.transform.position, Quaternion.identity);
        }
        if(player1.GetComponent<Player>().alreadyEnteredScoringTarget && player2.GetComponent<Player>().alreadyEnteredScoringTarget && !currentHoleHasBeenScored)
        {
                Debug.Log("Both players have entered scoring target, calculating score");
                currentHoleHasBeenScored = true;
                ScoreHole();
        }
       
        else if(player2) // player 2 not yet spawned spawn player 2 stone and switch to player 2
        {
         
            // spawn player 2 stone at beginning of course; 
           
            currentPlayer = player2;
            CameraTarget = player2;
            player2.GetComponent<Player>().alreadySpawned = true;
             
        }
        else // all players already spawned in game switch to other player
        {
            SwitchPlayer();
        }
    }
    
    public void resetScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void SwitchPlayer()
    {
       
        if(currentPlayer.GetComponent<Player>().player_number == 1)
        {
            Debug.Log("SWITCHING TO PLAYER 2");
            player1.GetComponent<StoneController>().disableTurn();
            player2.GetComponent<StoneController>().reEnableTurn();
            currentPlayer = player2;
        }
        else
        {   

            Debug.Log("SWITCHING TO PLAYER 1");
            player1.GetComponent<StoneController>().reEnableTurn();
            player2.GetComponent<StoneController>().disableTurn();
            currentPlayer = player1;
            
        }
        CameraTarget = currentPlayer;
    }
    
    // public void EndGame()
    // {
    //         RightArrow.SetActive(false);
    //         LeftArrow.SetActive(false);
    //         WinnerText.SetActive(true);
    //         // end of game logic here 
    //         if(player1.score > player2.score)
    //         {
    //             WinnerText.GetComponentInChildren<TextMeshProUGUI>().text = "Player 1 Wins!";
    //             WinnerText.GetComponentInChildren<TextMeshProUGUI>().color = Color.red;
    //         }
    //         else if (player2.score > player1.score)
    //         {
    //             WinnerText.GetComponentInChildren<TextMeshProUGUI>().text = "Player 2 Wins!";
    //             WinnerText.GetComponentInChildren<TextMeshProUGUI>().color = Color.blue;
    //         }
    //         else
    //         {
    //             WinnerText.GetComponentInChildren<TextMeshProUGUI>().text = "Tie Game!";
    //             WinnerText.GetComponentInChildren<TextMeshProUGUI>().color = Color.grey;
    //         }
    //         cameraOffset = CONST_CAMERA_SCOREBOARD_OFFSET;
    //         CameraTarget = ScoreBoard;
    // }
}
