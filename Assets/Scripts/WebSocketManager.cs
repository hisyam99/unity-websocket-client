using UnityEngine;
using UnityEngine.UIElements;
using WebSocketSharp;
using System;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;

public class WebSocketManager : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    private WebSocket ws;
    private string authToken = "12345";

    // UI Elements
    private Label connectionStatusLabel;
    private TextField messageInputField;
    private Button broadcastButton;
    private Button privateMessageButton;
    private ScrollView messageLogView;

    private void Awake()
    {
        if (uiDocument == null)
        {
            Debug.LogError("UI Document is not assigned in the Inspector!");
            return;
        }
        SetupUI();
    }

    private void Start()
    {
        Application.runInBackground = true; // Ensure WebSocket runs in background
        ConnectToServer();
    }

    private void SetupUI()
    {
        var root = uiDocument.rootVisualElement;

        connectionStatusLabel = root.Q<Label>("ConnectionStatus");
        messageInputField = root.Q<TextField>("MessageInput");
        broadcastButton = root.Q<Button>("BroadcastButton");
        privateMessageButton = root.Q<Button>("PrivateMessageButton");
        messageLogView = root.Q<ScrollView>("MessageLog");

        broadcastButton.clicked += () => SendBroadcastMessage(messageInputField.value);
        privateMessageButton.clicked += () => SendPrivateMessage("some-target-id", messageInputField.value);
    }

    private void ConnectToServer()
    {
        ws = new WebSocket("ws://localhost:4000");

        ws.OnOpen += (sender, e) =>
        {
            UpdateConnectionStatus("Connected", Color.green);
            SendJoinRequest();
        };

        ws.OnMessage += (sender, e) =>
        {
            HandleServerMessage(e.Data);
        };

        ws.OnError += (sender, e) =>
        {
            UpdateConnectionStatus($"Error: {e.Message}", Color.red);
        };

        ws.OnClose += (sender, e) =>
        {
            UpdateConnectionStatus("Disconnected", Color.yellow);
        };

        ws.Connect();
    }

    private void SendJoinRequest()
    {
        Invoke("join", new Dictionary<string, string>
        {
            { "roomId", "room1" },
            { "authToken", authToken }
        });
    }

    private void UpdateConnectionStatus(string status, Color color)
{
    connectionStatusLabel.text = status;
    connectionStatusLabel.style.color = new StyleColor(color);
    StartCoroutine(AddMessageToLogCoroutine($"[STATUS] {status}"));
}


    private IEnumerator UpdateConnectionStatusCoroutine(string status, Color color)
    {
        yield return null;
        connectionStatusLabel.text = status;
        connectionStatusLabel.style.color = new StyleColor(color);
    }

    private void HandleServerMessage(string message)
{
    try
    {
        // Parsing pesan JSON dari server
        var messageArray = JsonConvert.DeserializeObject<object[]>(message);
        if (messageArray != null && messageArray.Length >= 1)
        {
            string eventName = messageArray[0]?.ToString();
            object eventData = messageArray.Length > 1 ? messageArray[1] : null;

            // Menambahkan pesan ke log
            string logMessage = $"[RECEIVED] Event: {eventName}, Data: {JsonConvert.SerializeObject(eventData)}";
            StartCoroutine(AddMessageToLogCoroutine(logMessage));
        }
        else
        {
            StartCoroutine(AddMessageToLogCoroutine("[ERROR] Received invalid message format."));
        }
    }
    catch (Exception ex)
    {
        StartCoroutine(AddMessageToLogCoroutine($"[ERROR] Failed to parse message: {ex.Message}"));
    }
}

private IEnumerator AddMessageToLogCoroutine(string message)
{
    yield return null; // Tunggu 1 frame untuk memastikan UI sudah siap

    // Buat elemen label baru untuk pesan
    var logEntry = new Label($"[{DateTime.Now:HH:mm:ss}] {message}")
    {
        style =
        {
            marginTop = 5,
            marginBottom = 5,
            color = new StyleColor(Color.white),
            unityFontStyleAndWeight = FontStyle.Normal
        }
    };

    // Tambahkan pesan ke ScrollView
    messageLogView.contentContainer.Add(logEntry);

    // Scroll otomatis ke pesan terbaru
    messageLogView.ScrollTo(logEntry);
}

    private void Invoke(string eventName, object data)
    {
        if (ws == null || ws.ReadyState != WebSocketState.Open)
        {
            StartCoroutine(AddMessageToLogCoroutine("WebSocket not ready to send message"));
            return;
        }

        var message = new[] { eventName, data };
        string jsonMessage = JsonConvert.SerializeObject(message);
        ws.Send(jsonMessage);
    }

    private void SendBroadcastMessage(string message)
{
    if (string.IsNullOrWhiteSpace(message)) return;

    Invoke("broadcast", new
    {
        message = message,
        authToken = authToken
    });

    StartCoroutine(AddMessageToLogCoroutine($"[SENT] Broadcast: {message}"));
    messageInputField.value = string.Empty;
}

private void SendPrivateMessage(string targetId, string message)
{
    if (string.IsNullOrWhiteSpace(message)) return;

    Invoke("privateMessage", new
    {
        targetId = targetId,
        message = message,
        authToken = authToken
    });

    StartCoroutine(AddMessageToLogCoroutine($"[SENT] PrivateMessage to {targetId}: {message}"));
    messageInputField.value = string.Empty;
}


    private void OnApplicationQuit()
    {
        if (ws != null)
        {
            ws.Close();
        }
    }
}
