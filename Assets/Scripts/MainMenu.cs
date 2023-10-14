using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenu : MonoBehaviour
{
    public float RefreshButtonRotateSpeed = 360f;
    public UIDocument MainMenuUI;
    public UIDocument LobbyMenuUI;
    public VisualTreeAsset LobbyRow;
    public VisualTreeAsset PlayerRow;
    public Color ReadyTint;
    public Color UnreadyTint;

    Button ButtonCreateLobby;
    Button ButtonJoinLobby;
    Button ButtonRefreshLobbies;
    Button ButtonLeaveLobby;
    Button ButtonReady;
    Button ButtonStartGame;
    Label labelLobbyCount;
    Label labelPlayerCount;
    Label labelPlayersReady;
    Label LabelJoinAction;
    Toggle toggleOption1;
    Toggle toggleOption2;
    EnumField dropdownOption3;
    TextField FieldName;
    ListView ListLobby;
    ListView ListPlayers;
    VisualElement containerLobbyOptions;
    List<Lobby> AvailableLobbies;
    Lobby currentLobby;
    Player player;
    bool EnableRefresh = true;
    bool QueryFinished = true;
    float HeartbeatTimer;
    float refreshTimer;
    string selectedLobbyId;

    const float HEARTBEAT_INTERVAL = 20f;
    const float REFRESH_DELAY = 0.5f;
    const float REFRESH_INTERVAL = 5.0f;

    private void OnEnable()
    {
        // Bind main menu
        VisualElement root = MainMenuUI.rootVisualElement;

        ButtonCreateLobby = root.Q<Button>("buttonCreateLobby");
        ButtonJoinLobby = root.Q<Button>("buttonJoinLobby");
        ButtonRefreshLobbies = root.Q<Button>("buttonRefreshLobbies");
        labelLobbyCount = root.Q<Label>("labelLobbyCount");
        ListLobby = root.Q<ListView>("listLobby");
        LabelJoinAction = root.Q<Label>("labelJoinAction");
        FieldName = root.Q<TextField>("fieldName");

        ButtonJoinLobby.SetEnabled(false);

        ButtonCreateLobby.clicked += ButtonCreateLobby_clicked;
        ButtonJoinLobby.clicked += ButtonJoinLobby_clicked;
        ButtonRefreshLobbies.clicked += ButtonRefreshLobbies_clicked;
        ListLobby.selectionChanged += ListLobby_selectionChanged;

        ListLobby.makeItem = () => LobbyRow.Instantiate();
        ListLobby.bindItem = BindLobbyRow;

        // Bind lobby menu
        root = LobbyMenuUI.rootVisualElement;

        containerLobbyOptions = root.Q<VisualElement>(nameof(containerLobbyOptions));
        labelPlayerCount = root.Q<Label>(nameof(labelPlayerCount));
        labelPlayersReady = root.Q<Label>(nameof(labelPlayersReady));
        toggleOption1 = root.Q<Toggle>(nameof(toggleOption1));
        toggleOption2 = root.Q<Toggle>(nameof(toggleOption2));
        dropdownOption3 = root.Q<EnumField>(nameof(dropdownOption3));
        ButtonLeaveLobby = root.Q<Button>("buttonLeaveLobby");
        ButtonReady = root.Q<Button>("buttonReady");
        ButtonStartGame = root.Q<Button>("buttonStartGame");
        ListPlayers = root.Q<ListView>("listPlayers");

        root.visible = false;

        ButtonLeaveLobby.clicked += ButtonLeaveLobby_clicked;
        ButtonStartGame.clicked += ButtonStartGame_clicked;
        ButtonReady.clicked += ButtonReady_clicked;
        toggleOption1.RegisterValueChangedCallback(e => SetOption(Keys.OPTION_1, e.newValue.ToString()));
        toggleOption2.RegisterValueChangedCallback(e => SetOption(Keys.OPTION_2, e.newValue.ToString()));
        dropdownOption3.RegisterValueChangedCallback(e => SetOption(Keys.OPTION_3, e.newValue.ToString()));

        ListPlayers.makeItem = () => PlayerRow.Instantiate();
        ListPlayers.bindItem = BindPlayerRow;

        ButtonStartGame.SetEnabled(false);
    }

    private async void Start()
    {
        var options = new InitializationOptions();
        options.SetProfile(UnityEngine.Random.Range(0, int.MaxValue).ToString());
        await UnityServices.InitializeAsync(options);

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log($"Signed in {AuthenticationService.Instance.PlayerId}");
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private async void FixedUpdate()
    {
        // Lobby heartbeat
        if (currentLobby != null && currentLobby.HostId == AuthenticationService.Instance.PlayerId)
        {
            HeartbeatTimer += Time.fixedUnscaledDeltaTime;
            if (HeartbeatTimer >= HEARTBEAT_INTERVAL)
            {
                HeartbeatTimer = 0;
                await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            }
        }

        // Rotate refresh button
        if (!EnableRefresh)
        {
            var newRotation = ButtonRefreshLobbies.style.rotate.value.angle;
            newRotation.value += RefreshButtonRotateSpeed * Time.fixedUnscaledDeltaTime;
            ButtonRefreshLobbies.style.rotate = new StyleRotate(new Rotate(newRotation));
        }

        // Refresh timer
        if(currentLobby == null && EnableRefresh)
        {
            refreshTimer += Time.fixedUnscaledDeltaTime;
            if(refreshTimer >= REFRESH_INTERVAL)
            {
                await RefreshLobbiesAsync();
            }
        }
    }

    #region Main Menu

    #region Lobby List

    private void BindLobbyRow(VisualElement element, int index)
    {
        var lobby = AvailableLobbies[index];
        element.Q<Label>("labelLobbyName").text = lobby.Name;
        element.Q<Label>("labelPlayerCount").text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";
    }

    private void ListLobby_selectionChanged(IEnumerable<object> obj)
    {
        ButtonJoinLobby.SetEnabled(ListLobby.selectedIndex > -1);
        selectedLobbyId = ((Lobby)ListLobby.selectedItem)?.Id;
    }

    IEnumerator RefreshDelay()
    {
        yield return new WaitForSecondsRealtime(REFRESH_DELAY);

        // Enable refreshing if query completed.
        if (QueryFinished) ShowResultsAndEnableRefresh();
        else QueryFinished = true;
    }

    void ShowResultsAndEnableRefresh()
    {
        refreshTimer = 0;
        labelLobbyCount.text = $"{AvailableLobbies.Count} lobb{(AvailableLobbies.Count == 1 ? "y" : "ies")} found";
        EnableRefresh = true;
        ButtonRefreshLobbies.transform.rotation = Quaternion.identity;
        if (AvailableLobbies.Any())
        {
            ListLobby.itemsSource = AvailableLobbies;
            var idx = AvailableLobbies.FindIndex(l => l.Id == selectedLobbyId);
            ListLobby.selectedIndex = idx;
        }
        else
        {
            ListLobby.itemsSource = null;
            selectedLobbyId = null;
            ButtonJoinLobby.SetEnabled(false);
        }
    }

    #endregion

    #region Refresh Lobbies

    private async void ButtonRefreshLobbies_clicked()
    {
        await RefreshLobbiesAsync();
    }

    public async Task RefreshLobbiesAsync()
    {
        if (!EnableRefresh) return;

        EnableRefresh = false;
        labelLobbyCount.text = "Refreshing...";

        // Re-enable refresh after 1 second minimum.
        QueryFinished = false;
        StartCoroutine(RefreshDelay());

        try
        {
            var response = await LobbyService.Instance.QueryLobbiesAsync();
            AvailableLobbies = response.Results;

            // Enable refreshing if > 1s passed.
            if (QueryFinished) ShowResultsAndEnableRefresh();

            QueryFinished = true;
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogWarning("Failed to load lobbies");
            Debug.LogWarning(ex);
        }
    }

    #endregion

    private async void ButtonJoinLobby_clicked()
    {
        if (ListLobby.selectedIndex > -1)
        {
            var lobby = (Lobby)ListLobby.selectedItem;
            LabelJoinAction.text = "Joining...";
            LabelJoinAction.style.color = new StyleColor(Color.white);
            try
            {
                var options = new JoinLobbyByIdOptions() { Player = GetPlayer() };
                currentLobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobby.Id, options);

                Debug.Log($"Joined lobby {currentLobby.LobbyCode} ({currentLobby.Players.Count}/{currentLobby.MaxPlayers} player{(currentLobby.Players.Count != 1 ? "s" : "")}");
                JoinedLobby(false);
            }
            catch (LobbyServiceException ex)
            {
                var msg = "Failed to join lobby";
                LabelJoinAction.text = msg;
                LabelJoinAction.style.color = new StyleColor(Color.red);
                Debug.LogWarning(msg);
                Debug.LogWarning(ex);
            }
        }
    }

    private async void ButtonCreateLobby_clicked()
    {
        try
        {
            string lobbyName = "MyLobby";
            int maxPlayers = 4;

            var options = new CreateLobbyOptions()
            {
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    { Keys.OPTION_1, new DataObject(DataObject.VisibilityOptions.Member, false.ToString()) },
                    { Keys.OPTION_2, new DataObject(DataObject.VisibilityOptions.Member, true.ToString()) },
                    { Keys.OPTION_3, new DataObject(DataObject.VisibilityOptions.Member, TextAlignment.Left.ToString()) },
                }
            };
            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

            Debug.Log($"Created lobby {currentLobby.LobbyCode}");
            JoinedLobby(true);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning($"Failed to create lobby:");
            Debug.LogWarning(e);
        }
    }

    #endregion

    #region Lobby Menu

    private void ButtonStartGame_clicked()
    {
        throw new System.NotImplementedException();
    }

    private async void ButtonLeaveLobby_clicked()
    {
        await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
        Debug.Log("Left the lobby");
        currentLobby = null;
        ToggleMenu();
    }

    private void BindPlayerRow(VisualElement element, int index)
    {
        var player = currentLobby.Players[index];
        element.Q<Label>("labelPlayerName").text = player.Data[Keys.PLAYER_NAME].Value;
        var kickButton = element.Q<Button>("buttonKickPlayer");
        var canKick = GetIsHost() && player.Id != AuthenticationService.Instance.PlayerId;
        if (canKick)
            kickButton.clicked += () => KickPlayer(player.Id);

        kickButton.visible = canKick;
    }

    private void UpdatePlayerLabels()
    {
        labelPlayerCount.text = $"{currentLobby.Players.Count} Player{(currentLobby.Players.Count == 1 ? "" : "s")}";
        labelPlayersReady.text = $"{currentLobby.Players.Count(p => bool.Parse(p.Data[Keys.READY].Value))}/{currentLobby.Players.Count}";
        ButtonStartGame.SetEnabled(currentLobby.Players.All(p => bool.Parse(p.Data[Keys.READY].Value)));
        for (int i = 0; i < currentLobby.Players.Count; i++)
        {
            var player = currentLobby.Players[i];
            var row = ListPlayers.GetRootElementForIndex(i);
            if(row != null)
                row.Q<VisualElement>("iconReady").style.unityBackgroundImageTintColor = bool.Parse(player.Data[Keys.READY].Value) ? ReadyTint : UnreadyTint;
        }
    }

    #endregion

    #region Lobby Events

    private async void HandleLobbyEvents(bool isHost)
    {
        if (currentLobby == null)
        {
            Debug.LogError("Attempted to handle events for a lobby that doesn't exist.");
            return;
        }

        var callbacks = new LobbyEventCallbacks();
        callbacks.PlayerJoined += Callbacks_PlayerJoined;
        callbacks.PlayerLeft += Callbacks_PlayerLeft;
        callbacks.PlayerDataChanged += Callbacks_PlayerDataChanged;
        callbacks.LobbyChanged += Callbacks_LobbyChanged;
        callbacks.KickedFromLobby += Callbacks_KickedFromLobby;

        try
        {
            await LobbyService.Instance.SubscribeToLobbyEventsAsync(currentLobby.Id, callbacks);
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to subscribe to lobby events.");
            Debug.LogError(ex);
        }
    }

    private void Callbacks_KickedFromLobby()
    {
        currentLobby = null;
        ToggleMenu();
    }

    private void Callbacks_LobbyChanged(ILobbyChanges obj)
    {
        if (obj.Data.Changed)
        {
            foreach ((var key, var value) in obj.Data.Value)
                if (value.Changed)
                {
                    currentLobby.Data[key] = value.Value;
                }

            SetLobbyOptions();
        }
    }

    private void Callbacks_PlayerDataChanged(Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> obj)
    {
        foreach ((var idx, var data) in obj)
            foreach ((var key, var value) in data)
                currentLobby.Players[idx].Data[key] = value.Value;
        UpdatePlayerLabels();
    }

    private void Callbacks_PlayerLeft(List<int> obj)
    {
        foreach (var idx in obj)
        {
            currentLobby.Players.RemoveAt(idx);
            Debug.Log($"Player {idx} left.");
        }
        UpdatePlayerLabels();
        ListPlayers.Rebuild();
    }

    private void Callbacks_PlayerJoined(List<LobbyPlayerJoined> obj)
    {
        foreach (LobbyPlayerJoined player in obj)
        {
            currentLobby.Players.Insert(player.PlayerIndex, player.Player);
            Debug.Log($"Player {player.PlayerIndex} joined.");
        }
        UpdatePlayerLabels();
        ListPlayers.Rebuild();
    }

    private async void ButtonReady_clicked()
    {
        bool isReady = !bool.Parse(player.Data[Keys.READY].Value);
        player.Data[Keys.READY].Value = isReady.ToString();
        ButtonReady.text = isReady ? "Ready" : "Unready";
        var options = new UpdatePlayerOptions { Data = player.Data };
        UpdatePlayerLabels();
        await LobbyService.Instance.UpdatePlayerAsync(currentLobby.Id, player.Id, options);
    }

    private async void SetOption(string key, string value)
    {
        if (GetIsHost())
        {
            currentLobby.Data[key] = new(DataObject.VisibilityOptions.Member, value);
            var options = new UpdateLobbyOptions { Data = currentLobby.Data };
            await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, options);
        }
    }

    private async void KickPlayer(string id)
    {
        await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, id);
        Debug.Log($"Kicked player {id}.");
    }

    #endregion

    private void JoinedLobby(bool isHost)
    {
        ListPlayers.itemsSource = currentLobby.Players;
        LabelJoinAction.text = string.Empty;
        ToggleMenu();
        SetLobbyOptions();
        HandleLobbyEvents(isHost);
        if (isHost)
        {
            ListPlayers.itemsSource = currentLobby.Players;
            HeartbeatTimer = 0;
            player = currentLobby.Players[0];
        }
        else
        {
            player = currentLobby.Players.First(p => p.Id == AuthenticationService.Instance.PlayerId);
        }
    }

    private void ToggleMenu()
    {
        var isInLobby = currentLobby != null;
        MainMenuUI.rootVisualElement.visible = !isInLobby;
        LobbyMenuUI.rootVisualElement.visible = isInLobby;
        if (isInLobby)
        {
            containerLobbyOptions.SetEnabled(GetIsHost());
            ButtonStartGame.SetEnabled(false);
            ButtonReady.text = "Unready";
            UpdatePlayerLabels();
            ButtonStartGame.visible = GetIsHost();
        }
    }

    private Player GetPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { Keys.PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, FieldName.text) },
                { Keys.READY, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, false.ToString()) }
            }
        };
    }

    private bool GetIsHost()
    {
        return currentLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    private void SetLobbyOptions()
    {
        toggleOption1.value = bool.Parse(currentLobby.Data[Keys.OPTION_1].Value);
        toggleOption2.value = bool.Parse(currentLobby.Data[Keys.OPTION_2].Value);
        dropdownOption3.value = Enum.Parse<TextAlignment>(currentLobby.Data[Keys.OPTION_3].Value);
    }
}
