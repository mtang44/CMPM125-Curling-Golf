using TMPro;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.InputSystem;


public class GameManager: MonoBehaviour
{
    public GameObject player1_stone_prefab;
    public int currentHoleNumber;
    public GameObject player2_stone_prefab;
    public Camera main_camera;
    private OrbitalCamera orbitalCamera;
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
     public GameObject leftClickIcon;

     

    [Header("Score Display Input")]
    [SerializeField] private float clickBufferAfterAutoAdvance = 0.2f;
    [SerializeField] private float clickBufferAfterManualAdvance = 0.08f;

    [SerializeField] private GameObject currentPlayer;
    [SerializeField]private GameObject otherPlayer;


    public static int player1_total_score ;
    public static int player2_total_score ;

    public TextMeshProUGUI p1_score_header;
    public TextMeshProUGUI p2_score_header;


    private float nextAllowedScoreClickTime;
    private bool waitForScoreClickRelease;
    // spawns player 1 and finds uo; 
    void Start()
    {
        currentHoleNumber = 0; 
        // RightArrow = GameObject.Find("Curve Right");
        // LeftArrow = GameObject.Find("Curve Left");
        main_camera = Camera.main;
        orbitalCamera = main_camera != null ? main_camera.GetComponent<OrbitalCamera>() : null;
        cameraOffset = CONST_CAMERA_STONE_OFFSET;

        
        player1 = Instantiate(player1_stone_prefab, holeSpawnPositions[currentHoleNumber].transform.position, Quaternion.identity);
        currentPlayer = player1; 
        otherPlayer = player2;
        cameraTarget = currentPlayer;
        UpdateCameraTarget();
   
    }

    // constantly updates camera's position to follow current player's stone and updates score text on scoreboard;
    void Update()
    {
        // Debug.Log("Current Player: " + currentPlayer.player_number);
        if (orbitalCamera == null && main_camera != null && cameraTarget != null)
        {
            main_camera.transform.position = Vector3.MoveTowards(main_camera.transform.position, cameraTarget.transform.position + cameraOffset, Time.deltaTime * 100);
        }
        p1_score_header.text = "Player 1 Score: " + player1_total_score;
        p2_score_header.text = "Player 2 Score: " + player2_total_score;
    }
    // function to score the hole and update player scores. 
    public IEnumerator ScoreHoleCoroutine()
    {
        // player1.score = scoring_target.GetComponent<TargetScoring>().Calculate_Score(player1.player_number);
        //  player2.score = scoring_target.GetComponent<TargetScoring>().Calculate_Score(player2.player_number);
        scoring_targets[currentHoleNumber].GetComponent<TargetScoring>().Calculate_Score();
        yield return StartCoroutine(DisplayScoreCoroutine());
        player1_score_text.SetActive(false);
        player2_score_text.SetActive(false);
        leftClickIcon.SetActive(false);
        beginNewHole();
    }
    public IEnumerator DisplayScoreCoroutine()
    {
        BeginScoreInputSession();

        TextMeshProUGUI p1Text = player1_score_text.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI p2Text = player2_score_text.GetComponent<TextMeshProUGUI>();

        p1Text.text = "";
        p2Text.text = "";

        player1_score_text.SetActive(true);
        leftClickIcon.SetActive(true);
        yield return StartCoroutine(DisplaySinglePlayerScore(player1, p1Text, "Player 1"));

        player1_score_text.SetActive(false);
        player2_score_text.SetActive(true);
        yield return StartCoroutine(DisplaySinglePlayerScore(player2, p2Text, "Player 2"));

        p1Text.text = "";
        p2Text.text = "";
    }

    private IEnumerator DisplaySinglePlayerScore(GameObject playerObject, TextMeshProUGUI scoreText, string label)
    {
        Player playerData = playerObject.GetComponent<Player>();
        scoreText.text = label + "\n";

        List<string> rewards = playerData.pointRewardList;
        if (rewards == null || rewards.Count == 0)
        {
            scoreText.text += "\nNo Points Awarded";
            yield return WaitForClickOrSeconds(1.2f);
        }
        else
        {
            for (int i = 0; i < rewards.Count; i++)
            {
                scoreText.text += rewards[i];
                yield return WaitForClickOrSeconds(2f);
            }
        }

        scoreText.text += "\n\nTotal: " + playerData.score + " Points";
        yield return WaitForClick();
    }

    private IEnumerator WaitForClickOrSeconds(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            if (TryConsumeScoreAdvanceClick(clickBufferAfterManualAdvance))
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        LockScoreClicks(clickBufferAfterAutoAdvance);
    }

    private IEnumerator WaitForClick()
    {
        while (!TryConsumeScoreAdvanceClick(clickBufferAfterManualAdvance))
        {
            yield return null;
        }
    }

    private void BeginScoreInputSession()
    {
        waitForScoreClickRelease = IsAnyScoreAdvanceButtonHeld();
        nextAllowedScoreClickTime = Time.time + clickBufferAfterAutoAdvance;
    }

    private void LockScoreClicks(float duration)
    {
        nextAllowedScoreClickTime = Time.time + Mathf.Max(0f, duration);
        waitForScoreClickRelease = IsAnyScoreAdvanceButtonHeld();
    }

    private bool TryConsumeScoreAdvanceClick(float postConsumeLock)
    {
        if (Time.time < nextAllowedScoreClickTime)
        {
            return false;
        }

        if (waitForScoreClickRelease)
        {
            if (IsAnyScoreAdvanceButtonHeld())
            {
                return false;
            }

            waitForScoreClickRelease = false;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return false;
        }

        bool clicked = mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame;
        if (!clicked)
        {
            return false;
        }

        LockScoreClicks(postConsumeLock);
        return true;
    }

    private bool IsAnyScoreAdvanceButtonHeld()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return false;
        }

        return mouse.leftButton.isPressed || mouse.rightButton.isPressed;
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
            UpdateCameraTarget();
            StartCoroutine(ScoreHoleCoroutine());
            return;
        }

        SwitchPlayer();
    }
    
    public void beginNewHole()
    {
        currentHoleNumber++;
        Debug.Log(currentHoleNumber);
        Debug.Log(holeSpawnPositions.Count);
        if(currentHoleNumber >=  holeSpawnPositions.Count)
        {
            EndGame();
            return;
        }
        else
        {
            currentHoleHasBeenScored = false;
            player1_total_score += player1.GetComponent<Player>().score;
            player2_total_score += player2.GetComponent<Player>().score;
            Destroy(player1);
            Destroy(player2);
            player1 = Instantiate(player1_stone_prefab, holeSpawnPositions[currentHoleNumber].transform.position, Quaternion.identity);
            currentPlayer = player1;
            cameraTarget = currentPlayer;
            UpdateCameraTarget();
        }
       
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
        UpdateCameraTarget();
    }

    private void UpdateCameraTarget()
    {
        if (orbitalCamera != null)
        {
            Transform newTarget = cameraTarget != null ? cameraTarget.transform : null;

            if (newTarget != null && cameraTarget == currentPlayer)
            {
                Vector3 lookDirection = Vector3.forward;
                if (currentHoleNumber >= 0 && currentHoleNumber < scoring_targets.Count && scoring_targets[currentHoleNumber] != null)
                {
                    lookDirection = scoring_targets[currentHoleNumber].transform.position - newTarget.position;
                }

                orbitalCamera.SetTargetAndLookDirection(newTarget, lookDirection);
                return;
            }

            orbitalCamera.SetTarget(newTarget);
        }
    }

    public void EndGame()
    {
        
            // RightArrow.SetActive(false);
            // LeftArrow.SetActive(false);
            // WinnerText.SetActive(true);
            // // end of game logic here 
            if(player1_total_score >= player2_total_score)
            {
                // WinnerText.GetComponentInChildren<TextMeshProUGUI>().text = "Player 1 Wins!";
                // WinnerText.GetComponentInChildren<TextMeshProUGUI>().color = Color.red;
            }
            else if (player2_total_score > player1_total_score)
            {
                // WinnerText.GetComponentInChildren<TextMeshProUGUI>().text = "Player 2 Wins!";
                // WinnerText.GetComponentInChildren<TextMeshProUGUI>().color = Color.blue;
            }
            else
            {
                // WinnerText.GetComponentInChildren<TextMeshProUGUI>().text = "Tie Game!";
                // WinnerText.GetComponentInChildren<TextMeshProUGUI>().color = Color.grey;
            }
            // cameraOffset = CONST_CAMERA_SCOREBOARD_OFFSET;
            // cameraTarget = scoreBoard;
    }
}
