using System.Collections.Generic;
using UnityEngine;

public class TargetScoring : MonoBehaviour
{
    [SerializeField] private List<GameObject> collisions;
    // public GameObject testSquare;
    private Vector3 target_center;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        target_center = transform.position;
        Debug.Log("Target Center = " + target_center);
    }

    // Update is called once per frame
    void Update()
    {
        //  Debug.Log(Calculate_Distance_From_Center(testSquare.transform.position, target_center));
    }
    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            collisions.Add(other.gameObject);
            other.gameObject.GetComponent<Player>().alreadyEnteredScoringTarget = true; // reset score of player whose stone just entered target
            UnityEngine.Debug.Log("Trigger Detected with " + other.gameObject.name);
        }
        
       
    }
    void OnTriggerExit(Collider other)
    {
         if(other.CompareTag("Player"))
        {
        collisions.Remove(other.gameObject);
        UnityEngine.Debug.Log("Trigger Exit Detected with " + other.gameObject.name);
        }
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
            UnityEngine.Debug.Log("Closest Collision is " + closest.name);
            return closest;
        }
    }
    float Calculate_Distance_From_Center(Vector3 stonePosition, Vector3 targetCenter)
    {
        return Vector3.Distance(stonePosition, targetCenter);
    }
    // function that calculates score of player 
    public void Calculate_Score()
    {
        Debug.Log("Calculating Score function for Target");
        List<GameObject> tempCollisions = new List<GameObject>(collisions);
        GameObject closestPlayer = Find_Closest_Collision(tempCollisions);
        // if at least 1 player is still in zone upon scoring award points
        if(closestPlayer != null)
        {
            Debug.Log("Closest Player to center is player " + closestPlayer.gameObject.GetComponent<Player>().player_number);
            closestPlayer.gameObject.GetComponent<Player>().score += 100;  // bonus points for being closest
            if(tempCollisions.Count > 1) // both players in zone, and award bonus points to player, with least shots taken to get to zone
            {
                Debug.Log("Both players in target, awarding bonus points to player with least shots taken");
                // check who has least amount of shots taken and give them bonus points,
                if(tempCollisions[0].gameObject.GetComponent<Player>().shotsTaken == tempCollisions[1].gameObject.GetComponent<Player>().shotsTaken)
                {
                    Debug.Log("No shot bonus points awarded");
                    // both player shot counts are same, neither player recieves points. 
                }
                else if(tempCollisions[0].gameObject.GetComponent<Player>().shotsTaken < tempCollisions[1].gameObject.GetComponent<Player>().shotsTaken)
                {
                    Debug.Log("50 bonus points awarded to player"  +  tempCollisions[0].gameObject.GetComponent<Player>().player_number + " for least amount of shots");
                    tempCollisions[0].gameObject.GetComponent<Player>().score += 50; // bonus points for having fewest shots
                }
                else // 
                {
                    tempCollisions[1].gameObject.GetComponent<Player>().score += 50; // bonus points for having fewest shots
                    Debug.Log("50 bonus points awarded to player"  +  tempCollisions[1].gameObject.GetComponent<Player>().player_number + " for least amount of shots");
                }
            }
            else
            {
                Debug.Log("Only 1 player in goal awarding player "+ tempCollisions[0].gameObject.GetComponent<Player>().player_number + " for least amount of shots");
                closestPlayer.gameObject.GetComponent<Player>().score += 50; // bonus points awarded to closest player for having least shots taken in zone
            }
            // award points for each player in target based on distance from center.
            foreach(GameObject currentPlayer in tempCollisions)
            {
                Debug.Log(currentPlayer.transform.position);
                float current_DistanceAway = Calculate_Distance_From_Center(currentPlayer.transform.position, target_center);
                Debug.Log("Current Collision Distance from Center: of player " + +currentPlayer.GetComponent<Player>().player_number  + " : " + current_DistanceAway);
                if(current_DistanceAway < 1)
                {
                    currentPlayer.gameObject.GetComponent<Player>().score += 50;
                }
                else if(current_DistanceAway < 2)
                {
                    currentPlayer.gameObject.GetComponent<Player>().score += 40;
                }
                else if(current_DistanceAway < 3)
                {
                    currentPlayer.gameObject.GetComponent<Player>().score += 30;
                }
                else if(current_DistanceAway < 4)
                {
                    currentPlayer.gameObject.GetComponent<Player>().score += 20;
                }
                else if(current_DistanceAway < 7)
                {
                    currentPlayer.gameObject.GetComponent<Player>().score += 10;
                }
            }
        }
        else
        {
            Debug.Log("No players in target, no points awarded");
            return; // if no stones in target, return without updating score
        }
           
    }
}
