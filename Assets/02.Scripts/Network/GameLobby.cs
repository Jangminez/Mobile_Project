using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameLobby : MonoBehaviour
{
    public static GameLobby Instance {get; private set;}
    Lobby hostLobby;
    Lobby joinedLobby;
    float heartbeatTimer;
    float HandleLobbyTimer;
    string playerName;
    public const string KEY_START_GAME = "Relay Code";
    public event EventHandler<EventArgs> OnGameStarted;
    private List<Unity.Services.Lobbies.Models.Player> previousPlayerList;
    private bool isGameStart = false;


    [Header("MyUI")]
    public Button joinButton;
    public Text roomCode;
    public InputField inputField;

    private void Awake()
    {
        Instance = this;
    }

    private async void Start()
    {
        // 코드 비동기 실행
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };
        // 익명 로그인
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        joinButton.onClick.AddListener(JoinLobbyByCode);

        playerName = "Hero" + UnityEngine.Random.Range(0, 100);
    }

    void Update()
    {
        if (joinedLobby != null)
        {
            HandleLobbyHeartBeat();
            HandleLobbyPollForUpdates();
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        StartGame();
    }

    // 이 신호를 통해 로비가 활성화 되어있는지 확인
    private async void HandleLobbyHeartBeat()
    {
        if (hostLobby != null)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f)
            {
                float heartbeatTimerMax = 15f;
                heartbeatTimer = heartbeatTimerMax;

                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }

    // 로비의 정보가 업데이트 된다면 동기화를 위함
    private async void HandleLobbyPollForUpdates()
    {
        if (joinedLobby != null)
        {
            HandleLobbyTimer -= Time.deltaTime;
            if (HandleLobbyTimer < 0f)
            {
                float HandleLobbyTimerMax = 1.1f;
                HandleLobbyTimer = HandleLobbyTimerMax;

                joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);

                CheckPlayerCount();

                // 릴레이 코드가 추가 된다면 게임 시작
                if (joinedLobby.Data[KEY_START_GAME].Value != "0" && !isGameStart)
                {
                    ConnectRelay.Instance.JoinRelay(joinedLobby.Data[KEY_START_GAME].Value);
                    joinedLobby = null;
                    OnGameStarted?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }

    private void CheckPlayerCount()
    {
        if (previousPlayerList == null)
        {
            previousPlayerList = new List<Unity.Services.Lobbies.Models.Player>(joinedLobby.Players);
            return;
        }

        if (joinedLobby.Players.Count > previousPlayerList.Count)
        {
            if(IsLobbyHost())
                StartGame();
        }
    }

    public async void CreateLobby()
    {
        try
        {
            string lobbyName = "MyLobby";
            int maxPlayers = 2;

            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject> {
                    { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, "0")}
                }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);

            hostLobby = lobby;
            joinedLobby = hostLobby;

            roomCode.text = lobby.LobbyCode;

            Debug.Log("Create Lobby! " + lobby.LobbyCode);
        }

        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    // 현재 존재하는 로비 목록 출력
    private async void ListLobbies()
    {
        try
        {
            // 로비 검색 필터 설정
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter> {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder> {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };

            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryLobbiesOptions);

            foreach (Lobby lobby in queryResponse.Results)
            {
                Debug.Log(lobby.Name + " " + lobby.MaxPlayers);
            }
        }

        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    // 현재 존재하는 입장가능한 로비로 빠르게 입장
    public async void QuickJoinLobby()
    {
        try
        {
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            };

            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            joinedLobby = lobby;
        }

        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void JoinLobbyByCode()
    {
        try
        {
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            };

            Lobby lobby = await Lobbies.Instance.JoinLobbyByCodeAsync(inputField.text);
            joinedLobby = lobby;

            Debug.Log("Joined Lobby with code " + inputField.text);
        }

        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    // 로비에 존재하는 플레이어의 정보 가져오기
    private Unity.Services.Lobbies.Models.Player GetPlayer()
    {
        return new Unity.Services.Lobbies.Models.Player
        {
            Data = new Dictionary<string, PlayerDataObject> {
                        {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName)}
                    }
        };
    }

    // 로비에 존재하는 플레이어 출력
    [ContextMenu("PrintPlayer")]
    public void PrintPlayers()
    {
        Debug.Log("Players in Lobby " + joinedLobby.LobbyCode);
        foreach (var player in joinedLobby.Players)
        {
            Debug.Log(player.Id + " " + player.Data["PlayerName"].Value);
        }
    }

    // 로비 퇴장
    private async void LeaveLobby()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void MigrateLobbyHost()
    {
        try
        {
            hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                HostId = joinedLobby.Players[1].Id
            });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private bool IsLobbyHost()
    {
        if (joinedLobby.HostId == AuthenticationService.Instance.PlayerId)
            return true;
        else
            return false;
    }

    public async void StartGame()
    {
        if (IsLobbyHost() && !isGameStart)
        {
            try
            {
                isGameStart = true;
                Debug.Log("StartGame");

                string relayCode = await ConnectRelay.Instance.CreateRelay();

                Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject> {
                    { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, relayCode)}
                }
                });

                joinedLobby = lobby;

                NetworkManager.Singleton.SceneManager.LoadScene("StageScene", LoadSceneMode.Single);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }
}