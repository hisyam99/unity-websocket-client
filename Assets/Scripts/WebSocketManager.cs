using UnityEngine;
using UnityEngine.UIElements;
using WebSocketSharp;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

public class WebSocketManager : MonoBehaviour
{
    // Variabel untuk menyimpan referensi ke dokumen UI
    [SerializeField] private UIDocument uiDocument;
    // URL server WebSocket
    [SerializeField] private string serverUrl = "wss://hisyam99-websockettest.deno.dev";
    // Token autentikasi untuk keamanan
    [SerializeField] private string authToken = "12345";
    // Objek untuk menyimpan referensi elemen UI
    private UIElements _ui;
    // Objek WebSocket untuk koneksi
    private WebSocket _webSocket;
    // ID klien yang diberikan oleh server
    private string _clientId = string.Empty;
    // ID ruang saat ini, default adalah "room1"
    private string _currentRoomId = "room1"; // Default room ID

    // Kelas bersarang untuk mengorganisir referensi elemen UI
    private class UIElements
    {
        // Label untuk menampilkan status koneksi
        public Label ConnectionStatus;
        // Bidang teks untuk input pesan
        public TextField MessageInput;
        // Bidang teks untuk ID pengguna target
        public TextField TargetUserIdInput;
        // Bidang teks untuk input ID ruang
        public TextField RoomIdInput; // New input for room ID
        // Tombol untuk mengirim pesan broadcast
        public Button BroadcastButton;
        // Tombol untuk mengirim pesan pribadi
        public Button PrivateMessageButton;
        // Tombol untuk bergabung ke ruang
        public Button JoinRoomButton; // New button to join a room
        // Tampilan gulir untuk log pesan
        public ScrollView MessageLog;
    }

    // Metode yang dipanggil saat inisialisasi script
    private void Awake()
    {
        // Memvalidasi dokumen UI
        ValidateUIDocument();
        // Menginisialisasi referensi elemen UI
        InitializeUIReferences();
        // Menyiapkan pengendali acara untuk tombol
        SetupUIEventHandlers();
    }

    // Metode yang dipanggil setelah Awake
    private void Start()
    {
        // Mengatur aplikasi agar dapat berjalan di latar belakang
        Application.runInBackground = true;
        // Memulai koneksi WebSocket
        ConnectToWebSocket();
    }

    // Memvalidasi keberadaan dokumen UI
    private void ValidateUIDocument()
    {
        // Jika dokumen UI null, tampilkan pesan error dan nonaktifkan script
        if (uiDocument == null)
        {
            Debug.LogError("[WebSocketManager] Dokumen UI tidak ditetapkan di Inspector!");
            enabled = false;
        }
    }

    // Menginisialisasi referensi untuk semua elemen UI yang diperlukan
    private void InitializeUIReferences()
    {
        // Mendapatkan elemen root dari dokumen UI
        var root = uiDocument.rootVisualElement;
        // Membuat objek UIElements dan mengisi referensi elemen UI
        _ui = new UIElements
        {
            ConnectionStatus = root.Q<Label>("ConnectionStatus"),
            MessageInput = root.Q<TextField>("MessageInput"),
            TargetUserIdInput = root.Q<TextField>("TargetUserIdInput"),
            RoomIdInput = root.Q<TextField>("RoomIdInput"), // New input for room ID
            BroadcastButton = root.Q<Button>("BroadcastButton"),
            PrivateMessageButton = root.Q<Button>("PrivateMessageButton"),
            JoinRoomButton = root.Q<Button>("JoinRoomButton"), // New button to join a room
            MessageLog = root.Q<ScrollView>("MessageLog")
        };
    }

    // Menyiapkan pengendali acara untuk tombol
    private void SetupUIEventHandlers()
    {
        // Mengirim pesan broadcast saat tombol broadcast diklik
        _ui.BroadcastButton.clicked += () => SendBroadcastMessage(_ui.MessageInput.value);
        // Mengirim pesan pribadi saat tombol pesan pribadi diklik
        _ui.PrivateMessageButton.clicked += () => SendPrivateMessage(_ui.TargetUserIdInput.value, _ui.MessageInput.value);
        // Bergabung ke ruang saat tombol join room diklik
        _ui.JoinRoomButton.clicked += () => JoinRoom(_ui.RoomIdInput.value); // New handler for joining a room
    }

    // Memulai koneksi WebSocket
    private void ConnectToWebSocket()
    {
        // Membuat instance WebSocket dengan URL server
        _webSocket = new WebSocket(serverUrl);
        // Mengkonfigurasi event handler untuk WebSocket
        ConfigureWebSocketEvents();
        // Memulai koneksi
        EstablishConnection();
    }

    // Mengkonfigurasi protokol SSL dan event handler untuk koneksi WebSocket
    private void ConfigureWebSocketEvents()
    {
        // Mengatur protokol SSL untuk keamanan koneksi
        _webSocket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
        // Mendaftarkan event handler untuk berbagai kondisi koneksi
        _webSocket.OnOpen += HandleConnectionOpen;
        _webSocket.OnMessage += HandleIncomingMessage;
        _webSocket.OnError += HandleConnectionError;
        _webSocket.OnClose += HandleConnectionClosed;
    }

    // Mencoba membuat koneksi WebSocket
    private void EstablishConnection()
    {
        try
        {
            // Memulai koneksi WebSocket
            _webSocket.Connect();
        }
        catch (Exception ex)
        {
            // Memperbarui status koneksi dengan pesan kesalahan
            UpdateConnectionStatus($"Koneksi gagal: {ex.Message}", Color.red);
        }
    }

    // Menangani event saat koneksi WebSocket berhasil dibuka
    private void HandleConnectionOpen(object sender, EventArgs e)
    {
        // Memastikan pembaruan dilakukan di thread utama Unity
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            // Memperbarui status koneksi menjadi terhubung
            UpdateConnectionStatus("Terhubung", Color.green);
            // Mengirim permintaan bergabung ke ruang
            SendJoinRequest();
        });
    }

    // Menangani pesan masuk dari server WebSocket
    private void HandleIncomingMessage(object sender, MessageEventArgs e)
    {
        // Memastikan pemrosesan pesan dilakukan di thread utama Unity
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            // Memproses pesan dari server
            ProcessServerMessage(e.Data);
        });
    }

    // Menangani kesalahan koneksi WebSocket
    private void HandleConnectionError(object sender, ErrorEventArgs e)
    {
        // Memastikan pembaruan dilakukan di thread utama Unity
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            // Memperbarui status koneksi dengan pesan kesalahan
            UpdateConnectionStatus($"Kesalahan: {e.Message}", Color.red);
        });
    }

    // Menangani penutupan koneksi WebSocket
    private void HandleConnectionClosed(object sender, CloseEventArgs e)
    {
        // Memastikan pembaruan dilakukan di thread utama Unity
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            // Memperbarui status koneksi menjadi terputus
            UpdateConnectionStatus("Terputus", Color.yellow);
        });
    }

    // Mengirim permintaan bergabung ke ruang
    private void SendJoinRequest()
    {
        // Mengirim pesan "join" ke server dengan data ruang dan token autentikasi
        SendMessage("join", new Dictionary<string, string>
        {
            { "roomId", _currentRoomId },
            { "authToken", authToken }
        });
    }

    // Memperbarui label status koneksi
    private void UpdateConnectionStatus(string status, Color color)
    {
        // Mengatur teks status koneksi
        _ui.ConnectionStatus.text = status;
        // Mengatur warna teks status koneksi
        _ui.ConnectionStatus.style.color = new StyleColor(color);
        // Mencatat pesan status ke log
        LogMessage($"[STATUS] {status}");
    }

    // Memproses pesan yang diterima dari server
    private void ProcessServerMessage(string message)
    {
        try
        {
            // Mengurai pesan JSON menjadi array objek
            var messageArray = JsonConvert.DeserializeObject<object[]>(message);
            // Jika array null atau kosong, tampilkan pesan kesalahan
            if (messageArray == null || messageArray.Length < 1)
            {
                LogMessage("[KESALAHAN] Format pesan tidak valid.");
                return;
            }
            // Ekstrak nama event dan data
            string eventName = messageArray[0].ToString();
            object eventData = messageArray.Length > 1 ? messageArray[1] : null;
            // Tangani event spesifik
            HandleSpecificServerEvent(eventName, eventData);
        }
        catch (Exception ex)
        {
            // Tampilkan pesan kesalahan jika terjadi kesalahan saat memproses pesan
            Debug.LogError($"Kesalahan pemrosesan pesan: {ex.Message}");
        }
    }

    // Menangani berbagai jenis event yang diterima dari server
    private void HandleSpecificServerEvent(string eventName, object eventData)
    {
        switch (eventName)
        {
            case "welcome":
                // Menangani event selamat datang
                HandleWelcomeEvent(eventData);
                break;
            case "message":
                // Menangani pesan broadcast
                HandleBroadcastMessage(eventData);
                break;
            case "privateMessage":
                // Menangani pesan pribadi
                HandlePrivateMessage(eventData);
                break;
            case "error":
                // Mencatat pesan kesalahan ke log
                LogMessage($"[KESALAHAN] {eventData}");
                break;
            default:
                // Mengabaikan event lainnya
                // Ignore other events
                break;
        }
    }

    // Menangani event selamat datang dari server
    private void HandleWelcomeEvent(object eventData)
    {
        // Mengurai data selamat datang menjadi objek JSON
        var welcomeData = JObject.Parse(eventData.ToString());
        // Menyimpan ID klien yang diberikan oleh server
        _clientId = welcomeData["welcome"].ToString();
        // Mencatat pesan selamat datang ke log
        LogMessage($"Selamat datang! ID Anda: {_clientId}");
    }

    // Menangani pesan broadcast yang diterima dari server
    private void HandleBroadcastMessage(object eventData)
    {
        // Jika data adalah objek JSON
        if (eventData is JObject broadcastData)
        {
            // Ekstrak pengirim dan isi pesan
            string from = broadcastData["from"]?.ToString();
            string content = broadcastData["message"]?.ToString();
            // Mencatat pesan broadcast ke log
            LogMessage($"[BROADCAST] {from}: {content}");
        }
    }

    // Menangani pesan pribadi yang diterima dari server
    private void HandlePrivateMessage(object eventData)
    {
        // Jika data adalah objek JSON
        if (eventData is JObject privateMessageData)
        {
            // Ekstrak pengirim dan isi pesan
            string from = privateMessageData["from"]?.ToString();
            string content = privateMessageData["message"]?.ToString();
            // Mencatat pesan pribadi ke log
            LogMessage($"[PRIBADI] {from}: {content}");
        }
    }

    // Mencatat pesan ke log UI dengan timestamp
    private void LogMessage(string message)
    {
        // Membuat label untuk entri log
        var logEntry = new Label($"[{DateTime.Now:HH:mm:ss}] {message}")
        {
            style =
            {
                marginTop = 5,
                marginBottom = 5,
                color = new StyleColor(Color.white),
                unityFontStyleAndWeight = FontStyle.Normal
            },
            pickingMode = PickingMode.Position
        };
        // Menambahkan entri log ke ScrollView
        _ui.MessageLog.contentContainer.Add(logEntry);
        // Menggulung ScrollView ke entri log terbaru
        _ui.MessageLog.ScrollTo(logEntry);
        // Mengatur nilai scroller horizontal ke nilai maksimum
        _ui.MessageLog.horizontalScroller.value = _ui.MessageLog.horizontalScroller.highValue;
    }

    // Metode umum untuk mengirim pesan ke server WebSocket
    private void SendMessage(string eventName, object data)
    {
        // Jika WebSocket null atau tidak terhubung, tampilkan pesan kesalahan
        if (_webSocket == null || _webSocket.ReadyState != WebSocketState.Open)
        {
            LogMessage("WebSocket tidak terhubung. Tidak dapat mengirim pesan.");
            return;
        }
        // Membuat array pesan dengan nama event dan data
        var message = new[] { eventName, data };
        // Mengubah array pesan menjadi JSON
        string jsonMessage = JsonConvert.SerializeObject(message);
        // Mengirim pesan ke server
        _webSocket.Send(jsonMessage);
    }

    // Mengirim pesan broadcast ke semua pengguna dalam ruang
    private void SendBroadcastMessage(string message)
    {
        // Jika pesan kosong, kembalikan tanpa melakukan apa-apa
        if (string.IsNullOrWhiteSpace(message)) return;
        // Mengirim pesan broadcast ke server dengan data yang diperlukan
        SendMessage("broadcast", new
        {
            message = message,
            authToken = authToken,
            roomId = _currentRoomId
        });
        // Mengosongkan bidang input pesan
        _ui.MessageInput.value = string.Empty;
    }

    // Mengirim pesan pribadi ke pengguna tertentu
    private void SendPrivateMessage(string targetId, string message)
    {
        // Jika pesan atau ID target kosong, kembalikan tanpa melakukan apa-apa
        if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(targetId)) return;
        // Mengirim pesan pribadi ke server dengan data yang diperlukan
        SendMessage("privateMessage", new
        {
            targetId = targetId,
            message = message,
            authToken = authToken
        });
        // Mencatat pesan pribadi yang dikirim ke log
        LogMessage($"Pesan pribadi terkirim ke {targetId}: {message}");
        // Mengosongkan bidang input pesan
        _ui.MessageInput.value = string.Empty;
    }

    // Bergabung ke ruang yang ditentukan
    private void JoinRoom(string roomId)
    {
        // Jika ID ruang kosong, kembalikan tanpa melakukan apa-apa
        if (string.IsNullOrWhiteSpace(roomId)) return;
        // Mengatur ID ruang saat ini
        _currentRoomId = roomId;
        // Mengirim permintaan bergabung ke ruang
        SendJoinRequest();
        // Mencatat pesan bergabung ke ruang ke log
        LogMessage($"Bergabung ke ruang: {roomId}");
    }

    // Metode yang dipanggil saat aplikasi ditutup
    private void OnApplicationQuit()
    {
        // Jika WebSocket terhubung, tutup koneksi
        if (_webSocket != null && _webSocket.ReadyState == WebSocketState.Open)
        {
            _webSocket.Close();
            // Mencatat pesan penutupan WebSocket ke log
            LogMessage("WebSocket ditutup.");
        }
    }
}