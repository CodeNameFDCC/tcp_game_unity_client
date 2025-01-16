using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;


namespace squid_game
{
    public class PlayerListUI : MonoBehaviour
    {
        [SerializeField]
        private GameObject playerEntryPrefab;  // 플레이어 항목 UI 프리팹
        [SerializeField]
        private Transform contentParent;       // 스크롤뷰의 content transform

        private Dictionary<int, GameObject> playerEntries = new Dictionary<int, GameObject>();

        public void UpdateList(Dictionary<int, GameManager.PlayerState> playerStates)
        {
            // 새로운 플레이어 추가 및 기존 플레이어 업데이트
            foreach (var playerState in playerStates)
            {
                if (!playerEntries.ContainsKey(playerState.Key))
                {
                    // 새 플레이어 항목 생성
                    GameObject entry = Instantiate(playerEntryPrefab, contentParent);
                    playerEntries[playerState.Key] = entry;
                }

                // 플레이어 정보 업데이트
                UpdatePlayerEntry(playerState.Key, playerState.Value);
            }

            // 게임에서 나간 플레이어의 항목 제거
            var removedPlayers = playerEntries.Keys
                .Where(id => !playerStates.ContainsKey(id))
                .ToList();

            foreach (int playerId in removedPlayers)
            {
                Destroy(playerEntries[playerId]);
                playerEntries.Remove(playerId);
            }
        }

        private void UpdatePlayerEntry(int playerId, GameManager.PlayerState state)
        {
            GameObject entry = playerEntries[playerId];

            // 플레이어 이름 텍스트 업데이트
            TextMeshProUGUI nameText = entry.transform.Find("PlayerName").GetComponent<TextMeshProUGUI>();
            nameText.text = state.isLocal ? $"Player {playerId} (You)" : $"Player {playerId}";

            // 상태 텍스트 업데이트
            TextMeshProUGUI stateText = entry.transform.Find("PlayerState").GetComponent<TextMeshProUGUI>();
            stateText.text = state.state;

            // 상태에 따른 색상 변경
            Color stateColor = GetStateColor(state.state);
            stateText.color = stateColor;

            // 로컬 플레이어 강조
            if (state.isLocal)
            {
                entry.GetComponent<Image>().color = new Color(0.9f, 0.9f, 1f);
            }
        }

        private Color GetStateColor(string state)
        {
            switch (state.ToLower())
            {
                case "alive":
                    return Color.green;
                case "dead":
                    return Color.red;
                case "winner":
                    return Color.yellow;
                default:
                    return Color.white;
            }
        }
    }

}