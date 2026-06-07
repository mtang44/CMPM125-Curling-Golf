using UnityEngine;

[System.Serializable]
public class Player
{
    public int player_number;
    public int score;
    public int shotsTaken;
    public bool alreadySpawned;
    public GameObject curlingStone;
  

    public Player(int current_player_number)
    {
        player_number = current_player_number;
        score = 0;
        shotsTaken = 0;
        alreadySpawned = false;  
    }
}
