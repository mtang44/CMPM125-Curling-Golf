using UnityEngine;

public class Player: MonoBehaviour
{
    public int player_number;
    public int score;
    public int shotsTaken;
    public bool alreadySpawned;
    public bool alreadyEnteredScoringTarget;
    public bool hasScoredThisTurn;
    
  

    public Player(int current_player_number)
    {
        player_number = current_player_number;
        score = 0;
        shotsTaken = 0;
        alreadyEnteredScoringTarget = false;
        alreadySpawned = false;  
    }
}
