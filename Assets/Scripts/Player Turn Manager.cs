using TMPro;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;


public class GameManager: MonoBehaviour
{
    public GameObject player1_stone_prefab;
    public int currentHoleNumber;
    public GameObject player2_stone_prefab;
    public Camera main_camera;
    private GameObject player1;
    private GameObject player2;
    private GameObject cameraTarget;
    [SerializeField] private List<GameObject> scoring_targets;
    [SerializeField] private List<GameObject> holeSpawnPositions;
    private bool currentHoleHasBeenScored = false;
    private Vector3 cameraOffset;
    private Vector3 CONST_CAMERA_STONE_OFFSET = new Vector3(0,7,8);
    private Vector3 CONST_CAMERA_SCOREBOARD_OFFSET = new Vector3(0,10,30);
    //public GameObject ScoreBoard;
    //public GameObject WinnerText;
    //public GameObject RightArrow;
    //public GameObject LeftArrow;

     public GameObject player1_score_text;
     public GameObject player2_score_text;

    [SerializeField] private GameObject currentPlayer;
    [SerializeField]private GameObject otherPlayer;
    // spawns player 1 and finds uo; 
    void Start()
    {
        currentHoleNumber = 0; 
        // RightArrow = GameObject.Find("Curve Right");
        // LeftArrow = GameObject.Find("Curve Left");
        main_camera = Camera.main;
        cameraOffset = CONST_CAMERA_STONE_OFFSET;

        
        player1 = Instantiate(player1_stone_prefab, holeSpawnPositions[currentHoleNumber].transform.position, Quaternion.identity);
        currentPlayer = player1; 
        otherPlayer = player2;
        cameraTarget = currentPlayer;;
   
    }

    // constantly updates camera's position to follow current player's stone and updates score text on scoreboard;
    void Update()
    {
        // Debug.Log("Current Player: " + currentPlayer.player_number);
        main_camera.transform.position = Vector3.MoveTowards(main_camera.transform.position, cameraTarget.transform.position + cameraOffset, Time.deltaTime * 100);
        // player1_score_text.text = "Player 1 Score: " + player1.score;
        // player2_score_text.text = "Player 2 Score: " + player2.score;
    }
    // function to score the hole and update player scores. 
    public IEnumerator ScoreHoleCoroutine()
    {
        // player1.score = scoring_target.GetComponent<TargetScoring>().Calculate_Score(player1.player_number);
        //  player2.score = scoring_target.GetComponent<TargetScoring>().Calculate_Score(player2.player_number);
        scoring_targets[currentHoleNumber].GetComponent<TargetScoring>().Calculate_Score();
        StartCoroutine(DisplayScoreCoroutine());
        yield return new WaitForSeconds(10f);
        player1_score_text.SetActive(false);
        player2_score_text.SetActive(false);
        beginNewHole();
    }
    public IEnumerator DisplayScoreCoroutine()
    {

        player1_score_text.SetActive(true);
       
        foreach(string pointAward in player1.GetComponent<Player>().pointRewardList)
        {
            player1_score_text.GetComponent<TextMeshProUGUI>().text = pointAward; 
            yield return new WaitForSeconds(2f);
        }
        yield return new WaitForSeconds(5f);
         player1_score_text.SetActive(false);
         player2_score_text.SetActive(true);
        foreach(string pointAward in player2.GetComponent<Player>().pointRewardList)
        {
            player2_score_text.GetComponent<TextMeshProUGUI>().text = pointAward; 
            yield return new WaitForSeconds(2f);
        }
        yield return null;
    }
    public void EndTurn()
    {
        Debug.Log("Ending Turn");

        if(!player2) // if player 2 does not exist create player 2
        {
            player2 = Instantiate(player2_stone_prefab, holeSpawnPositions[currentHoleNumber].transform.position, Quaternion.identity);
            otherPlayer = player2;
        }
        // if both players have entered scoring zone begin scoring else switch to next player 
        if(player1.GetComponent<Player>().alreadyEnteredScoringTarget && player2.GetComponent<Player>().alreadyEnteredScoringTarget && !currentHoleHasBeenScored)
        {
            Debug.Log("Both players have entered scoring target, calculating score");
            currentHoleHasBeenScored = true;
            cameraTarget = scoring_targets[currentHoleNumber];
            StartCoroutine(ScoreHoleCoroutine());
            return;
        }

        SwitchPlayer();
    }
    
    public void beginNewHole()
    {
        currentHoleNumber++;
        currentHoleHasBeenScored = false;
        Destroy(player1);
        Destroy(player2);
        player1 = Instantiate(player1_stone_prefab, holeSpawnPositions[currentHoleNumber].transform.position, Quaternion.identity);
        currentPlayer = player1;
        cameraTarget = currentPlayer;
    }
    public void resetScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void SwitchPlayer()
    {
        Debug.Log("Switching Player");
        if(otherPlayer.GetComponent<Player>().alreadyEnteredScoringTarget)
        {
            Debug.Log("Other player already in score zone reEnabling current player");
            currentPlayer.GetComponent<StoneController>().reEnableTurn();
            otherPlayer.GetComponent<StoneController>().disableTurn();
        }
        else if(currentPlayer.GetComponent<Player>().player_number == 1)
        {
           
                Debug.Log("SWITCHING TO PLAYER 2");
                player1.GetComponent<StoneController>().disableTurn();
                player2.GetComponent<StoneController>().reEnableTurn();
                currentPlayer = player2;
                otherPlayer = player1;  
        }
        else
        {   
                Debug.Log("SWITCHING TO PLAYER 1");
                player1.GetComponent<StoneController>().reEnableTurn();
                player2.GetComponent<StoneController>().disableTurn();
                currentPlayer = player1;
                otherPlayer = player2;
        }
        cameraTarget = currentPlayer;
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
