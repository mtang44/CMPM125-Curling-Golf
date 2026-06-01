using System.Collections.Generic;
using UnityEngine;

public class TargetScoring : MonoBehaviour
{
    [SerializeField] private List<GameObject> collisions;
    private Vector3 target_center;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        target_center = transform.position;
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void OnTriggerEnter(Collider other)
    {
        collisions.Add(other.gameObject);
        UnityEngine.Debug.Log("Trigger Detected with " + other.gameObject.name);
       
    }
    void OnTriggerExit(Collider other)
    {
        collisions.Remove(other.gameObject);
        UnityEngine.Debug.Log("Trigger Exit Detected with " + other.gameObject.name);
    }
    GameObject Find_Closest_Collision(List<GameObject> collisionsList )
    {
        if(collisionsList.Count == 0)
        {
            return null;
        }
        else
        {
            GameObject closest = collisionsList[0];
            float closest_distance = Calculate_Distance_From_Center(closest.transform.position, target_center);
            foreach(GameObject currentCollision in collisionsList)
            {
                float current_distance = Calculate_Distance_From_Center(currentCollision.transform.position, target_center);
                if(current_distance < closest_distance)
                {
                    closest = currentCollision.gameObject;
                    closest_distance = current_distance;
                }
            }
            UnityEngine.Debug.Log("Closest Cllision is " + closest.name);
            return closest;
        }
    }
    float Calculate_Distance_From_Center(Vector3 stonePosition, Vector3 targetCenter)
    {
        return Vector3.Distance(stonePosition, targetCenter);
    }
    // function that calculates score of player 
    public int Calculate_Score(int current_player_number)
    {
        int score = 0; 
        List<GameObject> tempCollisions = new List<GameObject>(collisions);
        GameObject closestPlayer = Find_Closest_Collision(tempCollisions);
    while(closestPlayer != null)
        {
            Debug.Log("Closest Player is " + closestPlayer.GetComponent<Player>().player_number + "Current player name: " + current_player_number);
            if(closestPlayer.GetComponent<Player>().player_number == current_player_number)
            {
                Debug.Log("Adding 1 to player " + current_player_number + " Score");
                score++;
                tempCollisions.Remove(closestPlayer);
                closestPlayer = Find_Closest_Collision(tempCollisions);
            }
            else
            {
                break;
            }
        }
    return score;
    }
}
