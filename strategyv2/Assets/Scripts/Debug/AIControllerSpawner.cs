using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIControllerSpawner : MonoBehaviour
{
    [SerializeField]
    GameObject AIPlayerControllerPrefab;
    [SerializeField]
    GameObject AIDivisionController;
    [SerializeField]
    int numAIPlayersToSpawn = 5;
    // Start is called before the first frame update
    void Start()
    {
        if(enabled)
        {
            SpawnControllers();
        }
    }

    void SpawnControllers()
    {
        for(int i = 0; i < numAIPlayersToSpawn; i++)
        {
            AIPlayerController AIPlayer = Instantiate(AIPlayerControllerPrefab).GetComponent<AIPlayerController>();
            AIPlayer.name = $"Player {AIPlayer.TeamId}";
            AIDivisionController newDivision = Instantiate(DivisionControllerManager.Instance.DivisionPrefab).GetComponent<AIDivisionController>();
            newDivision.Controller = AIPlayer;
            newDivision.name = $"Division {newDivision.AttachedDivision.DivisionId}";
            newDivision.AttachedDivision.TeamId = AIPlayer.TeamId;
            float width = MapManager.Instance.MapGen.MapSize;
            float height = MapManager.Instance.MapGen.MapSize;

            newDivision.transform.position = new Vector3(Random.Range(0, width-1), Random.Range(0, height-1));
            //newDivision.Controller = AIPlayer;
        }
    }
}
