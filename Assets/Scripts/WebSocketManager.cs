using UnityEngine;
using UnityEngine.UIElements;
using WebSocketSharp;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

public class WebSocketManager : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private string serverUrl = "wss://hisyam99-websockettest.deno.dev";
    [SerializeField] private string authToken = "12345";
    private UIElements _ui;
    private WebSocket _webSocket;
    private string _clientId = string.Empty;
    private string _currentRoomId = string.Empty;
    private string _username = string.Empty;
    private bool _isFirstTime = true;

    private class UIElements
    {
        public Label ConnectionStatus;
        public TextField MessageInput;
        public TextField TargetUserIdInput;
        public Button BroadcastButton;
        public Button PrivateMessageButton;
        public Button ChangeRoomButton;
        public ScrollView MessageLog;
        public VisualElement PopupDialog;
        public TextField PopupUsernameInput;
        public TextField PopupRoomIdInput;
        public Button PopupJoinButton;
        public Button PopupCancelButton;
        public VisualElement ExitPopupDialog;
        public Button ExitConfirmButton;
        public Button ExitCancelButton;
    }

    private void Awake()
    {
        ValidateUIDocument();
        InitializeUIReferences();
        SetupUIEventHandlers();
    }

    private void Start()
    {
        Application.runInBackground = true;
        if (_isFirstTime)
        {
            ShowPopupDialog();
        }
        else
        {
            ConnectToWebSocket();
        }
    }

    private void ValidateUIDocument()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[WebSocketManager] Dokumen UI tidak ditetapkan di Inspector!");
            enabled = false;
        }
    }

    private void InitializeUIReferences()
    {
        var root = uiDocument.rootVisualElement;
        _ui = new UIElements
        {
            ConnectionStatus = root.Q<Label>("ConnectionStatus"),
            MessageInput = root.Q<TextField>("MessageInput"),
            TargetUserIdInput = root.Q<TextField>("TargetUserIdInput"),
            BroadcastButton = root.Q<Button>("BroadcastButton"),
            PrivateMessageButton = root.Q<Button>("PrivateMessageButton"),
            ChangeRoomButton = root.Q<Button>("ChangeRoomButton"),
            MessageLog = root.Q<ScrollView>("MessageLog"),
            PopupDialog = root.Q<VisualElement>("popupDialog"),
            PopupUsernameInput = root.Q<TextField>("popupUsernameInput"),
            PopupRoomIdInput = root.Q<TextField>("popupRoomIdInput"),
            PopupJoinButton = root.Q<Button>("popupJoinButton"),
            PopupCancelButton = root.Q<Button>("popupCancelButton"),
            ExitPopupDialog = root.Q<VisualElement>("exitPopupDialog"),
            ExitConfirmButton = root.Q<Button>("exitConfirmButton"),
            ExitCancelButton = root.Q<Button>("exitCancelButton")
        };
    }

    private void SetupUIEventHandlers()
    {
        _ui.BroadcastButton.RegisterCallback<ClickEvent>(_ => SendBroadcastMessage(_ui.MessageInput.value));
        _ui.PrivateMessageButton.RegisterCallback<ClickEvent>(_ => SendPrivateMessage(_ui.TargetUserIdInput.value, _ui.MessageInput.value));
        _ui.ChangeRoomButton.RegisterCallback<ClickEvent>(_ => ShowPopupDialog());
        _ui.PopupJoinButton.RegisterCallback<ClickEvent>(_ => OnPopupJoinClicked());
        _ui.PopupCancelButton.RegisterCallback<ClickEvent>(_ => OnPopupCancelClicked());
        _ui.ExitConfirmButton.RegisterCallback<ClickEvent>(_ => OnExitConfirmClicked());
        _ui.ExitCancelButton.RegisterCallback<ClickEvent>(_ => OnExitCancelClicked());
    }

    private void ShowPopupDialog()
    {
        _ui.PopupDialog.style.visibility = Visibility.Visible;
    }

    private void HidePopupDialog()
    {
        _ui.PopupDialog.style.visibility = Visibility.Hidden;
    }

    private void ShowExitPopupDialog()
    {
        _ui.ExitPopupDialog.style.visibility = Visibility.Visible;
    }

    private void HideExitPopupDialog()
    {
        _ui.ExitPopupDialog.style.visibility = Visibility.Hidden;
    }

    private void OnPopupJoinClicked()
    {
        string newUsername = _ui.PopupUsernameInput.value;
        string newRoomId = _ui.PopupRoomIdInput.value;
        if (string.IsNullOrWhiteSpace(newUsername) || string.IsNullOrWhiteSpace(newRoomId))
        {
            LogMessage($"[{DateTime.Now:HH:mm:ss}] [KESALAHAN] Username dan Room ID harus diisi.");
            AddNewLineToMessageLog();
            return;
        }
        HidePopupDialog();
        _username = newUsername;
        _currentRoomId = newRoomId;
        _isFirstTime = false;
        if (_webSocket != null && _webSocket.ReadyState == WebSocketState.Open)
        {
            _webSocket.Close();
        }
        ConnectToWebSocket();
    }

    private void OnPopupCancelClicked()
    {
        HidePopupDialog();
        if (_isFirstTime)
        {
            LogMessage($"[{DateTime.Now:HH:mm:ss}] [KESALAHAN] Anda harus memasukkan Username dan Room ID untuk melanjutkan.");
            AddNewLineToMessageLog();
        }
    }

    private void OnExitConfirmClicked()
    {
        HideExitPopupDialog();
        Application.Quit();
    }

    private void OnExitCancelClicked()
    {
        HideExitPopupDialog();
    }

    private void Update()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ShowExitPopupDialog();
            }
        }
    }

    private void ConnectToWebSocket()
    {
        _webSocket = new WebSocket(serverUrl);
        ConfigureWebSocketEvents();
        EstablishConnection();
    }

    private void ConfigureWebSocketEvents()
    {
        _webSocket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
        _webSocket.OnOpen += HandleConnectionOpen;
        _webSocket.OnMessage += HandleIncomingMessage;
        _webSocket.OnError += HandleConnectionError;
        _webSocket.OnClose += HandleConnectionClosed;
    }

    private void EstablishConnection()
    {
        try
        {
            _webSocket.Connect();
        }
        catch (Exception ex)
        {
            UpdateConnectionStatus($"Koneksi gagal: {ex.Message}", Color.red);
        }
    }

    private void HandleConnectionOpen(object sender, EventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            UpdateConnectionStatus("Terhubung", Color.green);
            SendJoinRequest();
        });
    }

    private void HandleIncomingMessage(object sender, MessageEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            ProcessServerMessage(e.Data);
        });
    }

    private void HandleConnectionError(object sender, ErrorEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            UpdateConnectionStatus($"Kesalahan: {e.Message}", Color.red);
        });
    }

    private void HandleConnectionClosed(object sender, CloseEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            UpdateConnectionStatus("Terputus", Color.yellow);
        });
    }

    private void SendJoinRequest()
    {
        SendMessage("join", new
        {
            roomId = _currentRoomId,
            authToken = authToken,
            username = _username
        });
    }

    private void UpdateConnectionStatus(string status, Color color)
    {
        _ui.ConnectionStatus.text = status;
        _ui.ConnectionStatus.style.color = new StyleColor(color);
        LogMessage($"[{DateTime.Now:HH:mm:ss}] [STATUS] {status}");
        AddNewLineToMessageLog();
    }

    private void ProcessServerMessage(string message)
    {
        try
        {
            var messageArray = JsonConvert.DeserializeObject<object[]>(message);
            if (messageArray == null || messageArray.Length < 1)
            {
                LogMessage($"[{DateTime.Now:HH:mm:ss}] [KESALAHAN] Format pesan tidak valid.");
                AddNewLineToMessageLog();
                return;
            }
            string eventName = messageArray[0].ToString();
            object eventData = messageArray.Length > 1 ? messageArray[1] : null;
            HandleSpecificServerEvent(eventName, eventData);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Kesalahan pemrosesan pesan: {ex.Message}");
        }
    }

    private void HandleSpecificServerEvent(string eventName, object eventData)
    {
        switch (eventName)
        {
            case "welcome":
                HandleWelcomeEvent(eventData);
                break;
            case "message":
                HandleBroadcastMessage(eventData);
                break;
            case "privateMessage":
                HandlePrivateMessage(eventData);
                break;
            case "error":
                LogMessage($"[{DateTime.Now:HH:mm:ss}] [KESALAHAN] {eventData}");
                AddNewLineToMessageLog();
                break;
            default:
                // Ignore other events
                break;
        }
    }

    private void HandleWelcomeEvent(object eventData)
    {
        if (eventData is JObject welcomeData)
        {
            _clientId = welcomeData["id"]?.ToString();
            string welcomeMessage = welcomeData["message"]?.ToString();
            LogMessage($"[{DateTime.Now:HH:mm:ss}] [WELCOME] {welcomeMessage}");
            AddNewLineToMessageLog();
        }
    }

    private void HandleBroadcastMessage(object eventData)
    {
        if (eventData is JObject broadcastData)
        {
            string from = broadcastData["from"]?.ToString();
            string username = broadcastData["username"]?.ToString();
            string content = broadcastData["message"]?.ToString();
            string messageId = broadcastData["id"]?.ToString();
            long timestamp = broadcastData["timestamp"] != null ? (long)broadcastData["timestamp"] : 0;

            // Mengonversi timestamp dari server ke zona waktu lokal client
            DateTime serverDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(timestamp);
            DateTime localDateTime = serverDateTime.ToLocalTime();

            LogMessage($"[{localDateTime:HH:mm:ss}] [BROADCAST] - {username} ({from}): {content}");
            AddNewLineToMessageLog();
        }
    }

    private void HandlePrivateMessage(object eventData)
    {
        if (eventData is JObject privateMessageData)
        {
            string from = privateMessageData["from"]?.ToString();
            string username = privateMessageData["username"]?.ToString();
            string content = privateMessageData["message"]?.ToString();
            LogMessage($"[{DateTime.Now:HH:mm:ss}] [PRIBADI] - {username} ({from}): {content}");
            AddNewLineToMessageLog();
        }
    }

    private void LogMessage(string message)
    {
        // Memisahkan username, userId, dan pesan dari pesan
        string pattern = @"\[(\d{2}:\d{2}:\d{2})\] \[(\w+)\] - (.+?) \(([^)]+)\)";
        Match match = Regex.Match(message, pattern);

        if (match.Success)
        {
            string time = match.Groups[1].Value;
            string type = match.Groups[2].Value;
            string username = match.Groups[3].Value;
            string userId = match.Groups[4].Value;

            // Memisahkan bagian pesan dari sisa pesan
            string[] parts = message.Split(new[] { ": " }, 2, StringSplitOptions.None);
            string timeTypePart = $"[{time}] [{type}]";
            string userIdPart = $"{username} ({userId})";
            string contentPart = parts.Length > 1 ? parts[1] : string.Empty;

            // Membuat Label untuk waktu dan tipe
            var timeTypeLabel = new Label(timeTypePart)
            {
                style =
                {
                    marginTop = 5,
                    marginBottom = 5,
                    color = new StyleColor(Color.black),
                    unityFontStyleAndWeight = FontStyle.Normal,
                    whiteSpace = WhiteSpace.Normal,
                    maxWidth = Length.Percent(100)
                },
                pickingMode = PickingMode.Position
            };

            // Membuat TextField untuk username dan userId
            var userIdField = new TextField
            {
                value = userIdPart,
                style =
                {
                    marginTop = 5,
                    marginBottom = 5,
                    color = new StyleColor(Color.black),
                    unityFontStyleAndWeight = FontStyle.Normal,
                    maxWidth = Length.Percent(100),
                    // Menghilangkan border dan background agar terlihat seperti Label
                    borderBottomWidth = 0,
                    borderTopWidth = 0,
                    borderLeftWidth = 0,
                    borderRightWidth = 0,
                    backgroundColor = new StyleColor(Color.clear),
                    // Memastikan teks tidak dapat diedit
                    unityTextAlign = TextAnchor.MiddleLeft
                },
                isReadOnly = true,
                multiline = false,
                pickingMode = PickingMode.Position
            };

            // Membuat Label untuk isi pesan
            var messageLabel = new Label(contentPart)
            {
                style =
                {
                    marginTop = 5,
                    marginBottom = 5,
                    color = new StyleColor(Color.black),
                    unityFontStyleAndWeight = FontStyle.Normal,
                    whiteSpace = WhiteSpace.Normal, // Mengaktifkan wrap text
                    maxWidth = Length.Percent(100)
                },
                pickingMode = PickingMode.Position
            };

            // Membuat container untuk mengelompokkan Label untuk waktu dan tipe, TextField untuk username dan userId, dan Label untuk pesan
            var container = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    alignItems = Align.FlexStart
                }
            };

            container.Add(timeTypeLabel);
            container.Add(userIdField);
            container.Add(messageLabel);

            // Menambahkan container ke message log
            _ui.MessageLog.contentContainer.Add(container);

            // Scroll ke container terbaru
            _ui.MessageLog.ScrollTo(container);
        }
        else
        {
            // Jika format pesan tidak sesuai, tambahkan pesan keseluruhan sebagai TextField
            var fullMessageField = new TextField
            {
                value = message,
                style =
                {
                    marginTop = 5,
                    marginBottom = 5,
                    color = new StyleColor(Color.black),
                    unityFontStyleAndWeight = FontStyle.Normal,
                    maxWidth = Length.Percent(100),
                    // Menghilangkan border dan background agar terlihat seperti Label
                    borderBottomWidth = 0,
                    borderTopWidth = 0,
                    borderLeftWidth = 0,
                    borderRightWidth = 0,
                    backgroundColor = new StyleColor(Color.clear),
                    // Memastikan teks tidak dapat diedit
                    unityTextAlign = TextAnchor.MiddleLeft
                },
                isReadOnly = true,
                multiline = true,
                pickingMode = PickingMode.Position
            };

            _ui.MessageLog.contentContainer.Add(fullMessageField);
            _ui.MessageLog.ScrollTo(fullMessageField);
        }
    }

    private void AddNewLineToMessageLog()
    {
        var newline = new Label("\n")
        {
            style =
            {
                whiteSpace = WhiteSpace.Pre,
                unityFontStyleAndWeight = FontStyle.Normal
            },
            pickingMode = PickingMode.Ignore
        };
        _ui.MessageLog.contentContainer.Add(newline);
    }

    private new void SendMessage(string eventName, object data)
    {
        if (_webSocket == null || _webSocket.ReadyState != WebSocketState.Open)
        {
            LogMessage($"[{DateTime.Now:HH:mm:ss}] [KESALAHAN] WebSocket tidak terhubung. Tidak dapat mengirim pesan.");
            AddNewLineToMessageLog();
            return;
        }
        var message = new[] { eventName, data };
        string jsonMessage = JsonConvert.SerializeObject(message);
        _webSocket.Send(jsonMessage);
    }

    private void SendBroadcastMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        SendMessage("broadcast", new
        {
            message = message,
            authToken = authToken,
            roomId = _currentRoomId
        });
        _ui.MessageInput.value = string.Empty;
    }

    private void SendPrivateMessage(string targetId, string message)
    {
        if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(targetId))
        {
            LogMessage($"[{DateTime.Now:HH:mm:ss}] [KESALAHAN] Pesan dan ID target harus diisi.");
            AddNewLineToMessageLog();
            return;
        }
        SendMessage("privateMessage", new
        {
            targetId = targetId,
            message = message,
            authToken = authToken
        });
        LogMessage($"[{DateTime.Now:HH:mm:ss}] [PRIBADI] Pesan pribadi terkirim ke {targetId}: {message}");
        AddNewLineToMessageLog();
        _ui.MessageInput.value = string.Empty;
    }

    private void OnApplicationQuit()
    {
        if (_webSocket != null && _webSocket.ReadyState == WebSocketState.Open)
        {
            _webSocket.Close();
            LogMessage($"[{DateTime.Now:HH:mm:ss}] [STATUS] WebSocket ditutup.");
            AddNewLineToMessageLog();
        }
    }
}