// PlayerManager.cs
using UnityEngine;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    public GameObject playerPrefab;
    private GameClient gameClient;
    private Dictionary<int, GameObject> players = new Dictionary<int, GameObject>();
    private int localPlayerId = -1;

    private void Start()
    {
        gameClient = GetComponent<GameClient>();
        SetupEventHandlers();
    }

    private void SetupEventHandlers()
    {
        gameClient.handler.OnPlayerSpawn += HandlePlayerSpawn;
        gameClient.handler.OnPlayerDespawn += HandlePlayerDespawn;
        gameClient.handler.OnPlayerPositionUpdate += HandlePlayerPositionUpdate;
        gameClient.handler.OnPlayerRotationUpdate += HandlePlayerRotationUpdate;
    }

    private void HandlePlayerSpawn(int playerId, Vector3 position, Quaternion rotation)
    {
        if (!players.ContainsKey(playerId))
        {
            GameObject playerObj = Instantiate(playerPrefab, position, rotation);
            players.Add(playerId, playerObj);

            if (localPlayerId == -1)
            {
                // First spawn is local player
                localPlayerId = playerId;
                playerObj.AddComponent<PlayerController>();
                Debug.Log($"Local player spawned with ID: {playerId}");
            }
            else
            {
                Debug.Log($"Remote player spawned with ID: {playerId}");
            }
        }
    }

    private void HandlePlayerDespawn(int playerId)
    {
        if (players.TryGetValue(playerId, out GameObject playerObj))
        {
            Destroy(playerObj);
            players.Remove(playerId);
            Debug.Log($"Player {playerId} despawned");
        }
    }

    private void HandlePlayerPositionUpdate(int playerId, Vector3 position)
    {
        if (playerId != localPlayerId && players.TryGetValue(playerId, out GameObject playerObj))
        {
            playerObj.transform.position = position;
        }
    }

    private void HandlePlayerRotationUpdate(int playerId, Quaternion rotation)
    {
        if (playerId != localPlayerId && players.TryGetValue(playerId, out GameObject playerObj))
        {
            playerObj.transform.rotation = rotation;
        }
    }
}

