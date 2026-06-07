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
    public GameObject player1_stone;
    public GameObject player2_stone;
    //public GameObject ScoreBoard;
    //public GameObject WinnerText;
    // public GameObject RightArrow;
    // public GameObject LeftArrow;

    // public TextMeshProUGUI player1_score_text;
    // public TextMeshProUGUI player2_score_text;
    public GameObject spawnPosition; 
    
    
    public Camera main_camera;
    [SerializeField] private Player player1 = new Player(1);
    [SerializeField] private Player player2 = new Player(2);
    private GameObject CameraTarget;
    [SerializeField] private List<GameObject> scoring_targets;
    private bool currentHoleHasBeenScored = false;
    
    private Vector3 cameraOffset;
    private Vector3 CONST_CAMERA_STONE_OFFSET = new Vector3(0,7,8);
    private Vector3 CONST_CAMERA_SCOREBOARD_OFFSET = new Vector3(0,10,30);

    [SerializeField] private Player currentPlayer;
    // spawns player 1 and finds uo; 
    void Start()
    {
        // RightArrow = GameObject.Find("Curve Right");
        // LeftArrow = GameObject.Find("Curve Left");
        main_camera = Camera.main;
        cameraOffset = CONST_CAMERA_STONE_OFFSET;

        player1.curlingStone = Instantiate(player1_stone, spawnPosition.transform.position, Quaternion.identity);
        player1.alreadySpawned = true;
        currentPlayer = player1; 
        CameraTarget = player1.curlingStone;
   
    }

    // constantly updates camera's position to follow current player's stone and updates score text on scoreboard;
    void Update()
    {
        Debug.Log("Player 1 already entered scoring target: " + player1.alreadyEnteredScoringTarget);
        Debug.Log("Player 2 already entered scoring target: " + player2.alreadyEnteredScoringTarget);
        Debug.Log("Current Hole Has Been Scored: " + currentHoleHasBeenScored);
        if(player1.alreadyEnteredScoringTarget && player2.alreadyEnteredScoringTarget && !currentHoleHasBeenScored)
        {
            Debug.Log("Both players have entered scoring target, calculating score");
            currentHoleHasBeenScored = true;
            ScoreHole();
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
        Debug.Log("Player 1 Score: " + player1.score);
        Debug.Log("Player 2 Score: " + player2.score);
    }
    public void EndTurn()
    {
        if(player2.alreadySpawned == false ) // player 2 not yet spawned spawn player 2 stone and switch to player 2
        {
            player1.shotsTaken++;
         
            // spawn player 2 stone at beginning of course; 
            player2.curlingStone = Instantiate(player2_stone, spawnPosition.transform.position, Quaternion.identity);
            currentPlayer = player2;
            CameraTarget = player2.curlingStone;
            player2.alreadySpawned = true;
             
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
        if(currentPlayer.player_number == 1)
        {
            Debug.Log("SWITCHING TO PLAYER 2");
            player1.curlingStone.GetComponent<StoneController>().disableTurn();
            player2.curlingStone.GetComponent<StoneController>().reEnableTurn();
            currentPlayer = player2;
        }
        else
        {   

            Debug.Log("SWITCHING TO PLAYER 1");
            player1.curlingStone.GetComponent<StoneController>().reEnableTurn();
            player2.curlingStone.GetComponent<StoneController>().disableTurn();
            currentPlayer = player1;
            
        }
        CameraTarget = currentPlayer.curlingStone;
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
