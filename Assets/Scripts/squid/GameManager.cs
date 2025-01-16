using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace squid_game
{
    public class GameManager : MonoBehaviour
    {
        // 싱글톤 인스턴스
        private static GameManager instance;
        public static GameManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<GameManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        instance = go.AddComponent<GameManager>();
                    }
                }
                return instance;
            }
        }

        [SerializeField]
        private PlayerController playerController;  // 로컬 플레이어 컨트롤러
        [SerializeField]
        private GameObject remotePlayerPrefab;      // 원격 플레이어 프리팹

        private bool isGameStarted;                 // 게임 시작 여부
        private bool isLooking;                     // 무궁화 꽃이 진행중인지 여부
        private int localPlayerId = -1;             // 로컬 플레이어 ID
        private Dictionary<int, PlayerState> playerStates = new Dictionary<int, PlayerState>();  // 플레이어 상태 정보

        public int LocalPlayerId => localPlayerId;  // 로컬 플레이어 ID 프로퍼티

        [System.Serializable]
        public class PlayerState
        {
            public string state;              // 플레이어 상태 (alive, dead, winner)
            public GameObject playerObject;    // 플레이어 게임 오브젝트
            public PlayerController controller;// 플레이어 컨트롤러
            public bool isLocal;              // 로컬 플레이어 여부
            public Vector3 lastPosition;       // 마지막 위치
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        // 게임 시작
        public void StartGame()
        {
            isGameStarted = true;
            if (playerController != null)
            {
                playerController.EnableMovement();
            }
        }

        // 턴 상태 업데이트 (무궁화 꽃이 피었습니다)
        public void UpdateTurnState(bool looking)
        {
            isLooking = looking;
            if (looking)
            {
                StartCoroutine(CheckPlayerMovement());
                UpdateGameUI(true);
            }
            else
            {
                UpdateGameUI(false);
            }
        }

        // 로컬 플레이어 ID 설정
        public void SetLocalPlayerId(int id)
        {
            if (id < 0)
            {
                Debug.LogError("Invalid player ID");
                return;
            }

            localPlayerId = id;
            Debug.Log($"Local player ID set to: {id}");

            if (!playerStates.ContainsKey(id) && playerController != null)
            {
                playerStates[id] = new PlayerState
                {
                    state = "alive",
                    playerObject = playerController.gameObject,
                    controller = playerController,
                    isLocal = true,
                    lastPosition = playerController.transform.position
                };
            }
        }

        // 모든 플레이어 ID 가져오기
        public List<int> GetAllPlayerIds()
        {
            return new List<int>(playerStates.Keys);
        }

        // 원격 플레이어 생성
        public void CreateRemotePlayer(int playerId)
        {
            if (playerId != localPlayerId && !playerStates.ContainsKey(playerId))
            {
                if (remotePlayerPrefab != null)
                {
                    GameObject playerObject = Instantiate(remotePlayerPrefab);
                    PlayerController controller = playerObject.GetComponent<PlayerController>();

                    playerStates[playerId] = new PlayerState
                    {
                        state = "alive",
                        playerObject = playerObject,
                        controller = controller,
                        isLocal = false,
                        lastPosition = Vector3.zero
                    };

                    UpdatePlayerListUI();
                    Debug.Log($"Remote player {playerId} created");
                }
                else
                {
                    Debug.LogError("Remote player prefab is not assigned!");
                }
            }
        }

        // 플레이어 상태 업데이트
        public void UpdatePlayerState(int playerId, string newState)
        {
            if (!playerStates.ContainsKey(playerId))
            {
                if (playerId != localPlayerId)
                {
                    CreateRemotePlayer(playerId);
                }
                else
                {
                    Debug.LogError($"Cannot update state for local player {playerId}: PlayerState not initialized");
                    return;
                }
            }

            var playerState = playerStates[playerId];
            playerState.state = newState;

            switch (newState)
            {
                case "dead":
                    if (playerState.isLocal)
                    {
                        playerController?.DisableMovement();
                        ShowDeathEffect();
                        Debug.Log("플레이어 사망");
                    }
                    else
                    {
                        playerState.controller?.DisableMovement();
                        ShowDeathEffect(playerState.playerObject);
                    }
                    break;

                case "winner":
                    if (playerState.isLocal)
                    {
                        ShowVictoryScreen();
                        Debug.Log("플레이어 승리");
                    }
                    break;
            }

            UpdatePlayerListUI();
        }

        // 플레이어 존재 여부 확인
        public bool HasPlayer(int playerId)
        {
            return playerStates.ContainsKey(playerId);
        }

        // 플레이어 위치 업데이트
        public void UpdatePlayerPosition(int playerId, Vector3 position)
        {
            if (playerStates.TryGetValue(playerId, out PlayerState playerState))
            {
                if (playerState.playerObject != null)
                {
                    // 부드러운 이동을 위한 보간 처리
                    StartCoroutine(SmoothMovePlayer(playerState, position));
                }
            }
            else
            {
                Debug.LogWarning($"Tried to update position for non-existent player: {playerId}");
            }
        }

        // 부드러운 이동을 위한 코루틴
        private IEnumerator SmoothMovePlayer(PlayerState playerState, Vector3 targetPosition)
        {
            float elapsedTime = 0;
            Vector3 startPosition = playerState.playerObject.transform.position;
            float moveTime = 0.1f; // 보간 시간

            while (elapsedTime < moveTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / moveTime;
                playerState.playerObject.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            playerState.playerObject.transform.position = targetPosition;
        }

        // 플레이어 제거
        public void RemovePlayer(int playerId)
        {
            if (playerStates.TryGetValue(playerId, out PlayerState playerState))
            {
                if (playerState.playerObject != null)
                {
                    Destroy(playerState.playerObject);
                }
                playerStates.Remove(playerId);
                UpdatePlayerListUI();
                Debug.Log($"Player {playerId} removed");
            }
        }

        // 게임 종료 처리
        public void HandleGameEnd(int winnerId)
        {
            isGameStarted = false;

            if (winnerId == localPlayerId)
            {
                ShowVictoryScreen();
            }
            else
            {
                ShowDefeatScreen();
            }

            // 모든 플레이어 이동 비활성화
            foreach (var playerState in playerStates.Values)
            {
                playerState.controller?.DisableMovement();
            }
        }

        // 패배 화면 표시
        private void ShowDefeatScreen()
        {
            var defeatScreen = GameObject.Find("DefeatScreen");
            if (defeatScreen != null)
            {
                defeatScreen.SetActive(true);
            }
        }

        private IEnumerator CheckPlayerMovement()
        {
            yield return new WaitForSeconds(0.5f);

            if (playerController != null && playerController.IsMoving && isLooking)
            {
                NetworkManager.Instance.SendPlayerState("dead").ContinueWith(task =>
                {
                    if (task.Exception != null)
                    {
                        Debug.LogError($"Failed to send player state: {task.Exception.InnerException.Message}");
                    }
                });
            }
        }

        private void ShowDeathEffect(GameObject target = null)
        {
            GameObject targetObj = target ?? playerController?.gameObject;
            if (targetObj == null) return;

            var deathEffect = targetObj.GetComponentInChildren<ParticleSystem>();
            deathEffect?.Play();

            var audioSource = targetObj.GetComponent<AudioSource>();
            audioSource?.Play();
        }

        private void ShowVictoryScreen()
        {
            var victoryUI = GameObject.Find("VictoryScreen");
            if (victoryUI != null)
            {
                victoryUI.SetActive(true);
            }
        }

        private void UpdateGameUI(bool isLooking)
        {
            var turnText = GameObject.Find("TurnText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (turnText != null)
            {
                turnText.text = isLooking ? "무궁화 꽃이 피었습니다!" : "";
            }
        }

        public void UpdateWaitingTime(int remainingSeconds)
        {
            var waitingText = GameObject.Find("WaitingTimeText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (waitingText != null)
            {
                waitingText.text = $"게임 시작까지: {remainingSeconds}초";
            }
        }

        private void UpdatePlayerListUI()
        {
            var playerList = GameObject.Find("PlayerList")?.GetComponent<PlayerListUI>();
            playerList?.UpdateList(playerStates);
        }

        // 게임 재시작을 위한 상태 초기화
        public void ResetGame()
        {
            isGameStarted = false;
            isLooking = false;

            // UI 초기화
            var victoryUI = GameObject.Find("VictoryScreen");
            if (victoryUI != null) victoryUI.SetActive(false);

            var defeatScreen = GameObject.Find("DefeatScreen");
            if (defeatScreen != null) defeatScreen.SetActive(false);

            // 모든 플레이어 상태 리셋
            foreach (var playerState in playerStates.Values)
            {
                playerState.state = "alive";
                playerState.controller?.EnableMovement();
            }

            UpdatePlayerListUI();
        }
    }
}