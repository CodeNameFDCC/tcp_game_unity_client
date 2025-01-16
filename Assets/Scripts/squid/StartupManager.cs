// StartupManager.cs


using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.SceneManagement;

namespace squid_game
{


    public class StartupManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField ipAddressInput;
        [SerializeField] private TMP_InputField portInput;
        [SerializeField] private Button connectButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject connectPanel;
        [SerializeField] private GameObject loadingPanel;

        [Header("Connection Settings")]
        [SerializeField] private string defaultIpAddress = "127.0.0.1";
        [SerializeField] private int defaultPort = 3000;

        private void Start()
        {
            // UI 초기값 설정
            ipAddressInput.text = defaultIpAddress;
            portInput.text = defaultPort.ToString();

            // 버튼 이벤트 연결
            connectButton.onClick.AddListener(ConnectToServer);

            // 초기 UI 상태
            connectPanel.SetActive(true);
            loadingPanel.SetActive(false);
            statusText.text = "서버 정보를 입력하세요";
        }

        private async void ConnectToServer()
        {
            try
            {
                // UI 상태 업데이트
                connectButton.interactable = false;
                loadingPanel.SetActive(true);
                statusText.text = "서버에 연결 중...";

                string ip = ipAddressInput.text;
                int port = int.Parse(portInput.text);

                // NetworkManager를 통해 서버 연결 시도
                await NetworkManager.Instance.ConnectToServer(ip, port);

                // 연결 성공 - 게임 씬으로 전환
                //SceneManager.LoadScene("GameScene");
                connectPanel.SetActive(false);
                loadingPanel.SetActive(false);
            }
            catch (Exception e)
            {
                Debug.LogError($"Server connection failed: {e.Message}");
                statusText.text = "연결 실패: " + e.Message;

                // UI 복구
                connectButton.interactable = true;
                loadingPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            // 이벤트 리스너 제거
            if (connectButton != null)
                connectButton.onClick.RemoveListener(ConnectToServer);
        }
    }
}