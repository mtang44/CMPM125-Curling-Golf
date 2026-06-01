using UnityEngine;

public class Player : MonoBehaviour
{
    public int player_number;
    public int score;
    public int stones_remaining;

    public Player(int current_player_number)
    {
        player_number = current_player_number;
        score = 0;
        stones_remaining = 4;
    }
}
