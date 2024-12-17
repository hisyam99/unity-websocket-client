using UnityEngine;
using UnityEngine.UIElements;
using WebSocketSharp;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

/// <summary>
/// Kelas utama untuk mengelola koneksi WebSocket dan interaksi antarmuka pengguna dalam aplikasi Unity.
/// Menangani komunikasi real-time, pesan broadcast, dan pesan pribadi.
/// </summary>
public class WebSocketManager : MonoBehaviour
{
    // Konfigurasi koneksi WebSocket
    [SerializeField] private UIDocument uiDocument; // Dokumen UI untuk referensi elemen antarmuka
    [SerializeField] private string serverUrl = "wss://hisyam99-websockettest.deno.dev"; // URL server WebSocket
    [SerializeField] private string defaultRoomId = "room1"; // Ruang default untuk bergabung
    [SerializeField] private string authToken = "12345"; // Token otentikasi untuk keamanan

    // Referensi internal untuk elemen UI dan koneksi
    private UIElements _ui; // Kelas bersarang untuk menyimpan referensi elemen UI
    private WebSocket _webSocket; // Objek WebSocket untuk koneksi
    private string _clientId = string.Empty; // Pengidentifikasi unik untuk klien

    /// <summary>
    /// Kelas bersarang untuk mengorganisir referensi elemen UI.
    /// Membantu dalam pengelolaan dan akses elemen antarmuka dengan lebih terstruktur.
    /// </summary>
    private class UIElements
    {
        public Label ConnectionStatus; // Label untuk menampilkan status koneksi
        public TextField MessageInput; // Bidang teks untuk input pesan
        public TextField TargetUserIdInput; // Bidang teks untuk ID pengguna target
        public Button BroadcastButton; // Tombol untuk mengirim pesan broadcast
        public Button PrivateMessageButton; // Tombol untuk mengirim pesan pribadi
        public ScrollView MessageLog; // Tampilan gulir untuk log pesan
    }

    /// <summary>
    /// Metode yang dipanggil saat inisialisasi script.
    /// Memvalidasi dokumen UI, menginisialisasi referensi, dan menyiapkan pengendali acara.
    /// </summary>
    private void Awake()
    {
        ValidateUIDocument(); // Memeriksa apakah dokumen UI valid
        InitializeUIReferences(); // Menginisialisasi referensi elemen UI
        SetupUIEventHandlers(); // Menyiapkan pengendali acara untuk tombol
    }

    /// <summary>
    /// Metode yang dipanggil setelah Awake.
    /// Mengatur mode latar belakang dan memulai koneksi WebSocket.
    /// </summary>
    private void Start()
    {
        Application.runInBackground = true; // Memungkinkan aplikasi berjalan di latar belakang
        ConnectToWebSocket(); // Memulai koneksi WebSocket
    }

    /// <summary>
    /// Memvalidasi keberadaan dokumen UI.
    /// Menonaktifkan script jika dokumen UI tidak ditetapkan.
    /// </summary>
    private void ValidateUIDocument()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[WebSocketManager] Dokumen UI tidak ditetapkan di Inspector!");
            enabled = false; // Menonaktifkan script jika dokumen UI hilang
        }
    }

    /// <summary>
    /// Menginisialisasi referensi untuk semua elemen UI yang diperlukan.
    /// Menggunakan metode Query (Q) untuk mengakses elemen dari dokumen UI.
    /// </summary>
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
            MessageLog = root.Q<ScrollView>("MessageLog")
        };
    }

    /// <summary>
    /// Menyiapkan pengendali acara untuk tombol broadcast dan pesan pribadi.
    /// Mendefinisikan tindakan yang akan dijalankan saat tombol diklik.
    /// </summary>
    private void SetupUIEventHandlers()
    {
        // Mengirim pesan broadcast saat tombol broadcast diklik
        _ui.BroadcastButton.clicked += () => SendBroadcastMessage(_ui.MessageInput.value);

        // Mengirim pesan pribadi saat tombol pesan pribadi diklik
        _ui.PrivateMessageButton.clicked += () =>
            SendPrivateMessage(_ui.TargetUserIdInput.value, _ui.MessageInput.value);
    }

    /// <summary>
    /// Memulai koneksi WebSocket dengan mengkonfigurasi dan membuat koneksi.
    /// </summary>
    private void ConnectToWebSocket()
    {
        _webSocket = new WebSocket(serverUrl); // Membuat instance WebSocket dengan URL server
        ConfigureWebSocketEvents(); // Menyiapkan event handler untuk WebSocket
        EstablishConnection(); // Memulai koneksi
    }

    /// <summary>
    /// Mengkonfigurasi protokol SSL dan event handler untuk koneksi WebSocket.
    /// Menyiapkan metode callback untuk berbagai kondisi koneksi.
    /// </summary>
    private void ConfigureWebSocketEvents()
    {
        // Mengatur protokol SSL untuk keamanan koneksi
        _webSocket.SslConfiguration.EnabledSslProtocols =
            System.Security.Authentication.SslProtocols.Tls12;

        // Mendaftarkan event handler untuk berbagai kondisi koneksi
        _webSocket.OnOpen += HandleConnectionOpen;
        _webSocket.OnMessage += HandleIncomingMessage;
        _webSocket.OnError += HandleConnectionError;
        _webSocket.OnClose += HandleConnectionClosed;
    }

    /// <summary>
    /// Mencoba membuat koneksi WebSocket.
    /// Menangkap dan mencatat kesalahan jika koneksi gagal.
    /// </summary>
    private void EstablishConnection()
    {
        try
        {
            _webSocket.Connect(); // Memulai koneksi WebSocket
        }
        catch (Exception ex)
        {
            // Memperbarui status koneksi dengan pesan kesalahan
            UpdateConnectionStatus($"Koneksi gagal: {ex.Message}", Color.red);
        }
    }

    /// <summary>
    /// Menangani event saat koneksi WebSocket berhasil dibuka.
    /// Memperbarui status koneksi dan mengirim permintaan bergabung.
    /// </summary>
    private void HandleConnectionOpen(object sender, EventArgs e)
    {
        // Memastikan pembaruan dilakukan di thread utama Unity
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            UpdateConnectionStatus("Terhubung", Color.green);
            SendJoinRequest(); // Mengirim permintaan untuk bergabung ke ruang
        });
    }

    /// <summary>
    /// Menangani pesan masuk dari server WebSocket.
    /// Memastikan pemrosesan pesan dilakukan di thread utama Unity.
    /// </summary>
    private void HandleIncomingMessage(object sender, MessageEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            ProcessServerMessage(e.Data);
        });
    }

    /// <summary>
    /// Menangani kesalahan koneksi WebSocket.
    /// Memperbarui status koneksi dengan informasi kesalahan.
    /// </summary>
    private void HandleConnectionError(object sender, ErrorEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            UpdateConnectionStatus($"Kesalahan: {e.Message}", Color.red);
        });
    }

    /// <summary>
    /// Menangani penutupan koneksi WebSocket.
    /// Memperbarui status koneksi sebagai terputus.
    /// </summary>
    private void HandleConnectionClosed(object sender, CloseEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            UpdateConnectionStatus("Terputus", Color.yellow);
        });
    }

    /// <summary>
    /// Mengirim permintaan bergabung ke ruang dengan token otentikasi.
    /// </summary>
    private void SendJoinRequest()
    {
        SendMessage("join", new Dictionary<string, string>
        {
            { "roomId", defaultRoomId },
            { "authToken", authToken }
        });
    }

    /// <summary>
    /// Memperbarui label status koneksi dengan warna dan pesan yang diberikan.
    /// Mencatat pesan status ke log.
    /// </summary>
    /// <param name="status">Pesan status koneksi</param>
    /// <param name="color">Warna label status</param>
    private void UpdateConnectionStatus(string status, Color color)
    {
        _ui.ConnectionStatus.text = status;
        _ui.ConnectionStatus.style.color = new StyleColor(color);
        LogMessage($"[STATUS] {status}");
    }

    /// <summary>
    /// Memproses pesan yang diterima dari server.
    /// Melakukan parsing JSON dan menangani event spesifik.
    /// </summary>
    /// <param name="message">Pesan JSON dari server</param>
    private void ProcessServerMessage(string message)
    {
        try
        {
            // Mengurai pesan JSON menjadi array objek
            var messageArray = JsonConvert.DeserializeObject<object[]>(message);
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
            Debug.LogError($"Kesalahan pemrosesan pesan: {ex.Message}");
        }
    }

    /// <summary>
    /// Menangani berbagai jenis event yang diterima dari server.
    /// </summary>
    /// <param name="eventName">Nama event</param>
    /// <param name="eventData">Data terkait event</param>
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
                LogMessage($"[KESALAHAN] {eventData}");
                break;
            default:
                LogMessage($"[TIDAK DIKENALI] Event: {eventName}, Data: {JsonConvert.SerializeObject(eventData)}");
                break;
        }
    }

    /// <summary>
    /// Menangani event selamat datang dari server.
    /// Menyimpan ID klien yang diberikan oleh server.
    /// </summary>
    /// <param name="eventData">Data selamat datang dari server</param>
    private void HandleWelcomeEvent(object eventData)
    {
        var welcomeData = JObject.Parse(eventData.ToString());
        _clientId = welcomeData["welcome"].ToString();
        LogMessage($"[SELAMAT DATANG] {_clientId}");
    }

    /// <summary>
    /// Menangani pesan broadcast yang diterima dari server.
    /// Mencatat pesan ke log dengan informasi pengirim.
    /// </summary>
    /// <param name="eventData">Data pesan broadcast</param>
    private void HandleBroadcastMessage(object eventData)
    {
        if (eventData is JObject broadcastData)
        {
            string from = broadcastData["from"]?.ToString();
            string content = broadcastData["message"]?.ToString();
            LogMessage($"[BROADCAST] {from}: {content}");
        }
    }

    /// <summary>
    /// Menangani pesan pribadi yang diterima dari server.
    /// Mencatat pesan ke log dengan informasi pengirim.
    /// </summary>
    /// <param name="eventData">Data pesan pribadi</param>
    private void HandlePrivateMessage(object eventData)
    {
        if (eventData is JObject privateMessageData)
        {
            string from = privateMessageData["from"]?.ToString();
            string content = privateMessageData["message"]?.ToString();
            LogMessage($"[PRIBADI] {from}: {content}");
        }
    }

    /// <summary>
    /// Mencatat pesan ke log UI dengan timestamp.
    /// Menambahkan entri log baru ke ScrollView dan menggulung ke bawah.
    /// </summary>
    /// <param name="message">Pesan yang akan dicatat</param>
    private void LogMessage(string message)
    {
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

        _ui.MessageLog.contentContainer.Add(logEntry);
        _ui.MessageLog.ScrollTo(logEntry);
        _ui.MessageLog.horizontalScroller.value = _ui.MessageLog.horizontalScroller.highValue;
    }

    /// <summary>
    /// Metode umum untuk mengirim pesan ke server WebSocket.
    /// Memastikan koneksi terbuka sebelum mengirim.
    /// </summary>
    /// <param name="eventName">Nama event</param>
    /// <param name="data">Data yang akan dikirim</param>
    private new void SendMessage(string eventName, object data)
    {
        if (_webSocket == null || _webSocket.ReadyState != WebSocketState.Open)
        {
            LogMessage("WebSocket tidak terhubung. Tidak dapat mengirim pesan.");
            return;
        }

        var message = new[] { eventName, data };
        string jsonMessage = JsonConvert.SerializeObject(message);
        _webSocket.Send(jsonMessage);
    }

    /// <summary>
    /// Mengirim pesan broadcast ke semua pengguna dalam ruang.
    /// Memvalidasi isi pesan sebelum mengirim.
    /// </summary>
    /// <param name="message">Isi pesan broadcast</param>
    private void SendBroadcastMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        SendMessage("broadcast", new
        {
            message = message,
            authToken = authToken
        });

        _ui.MessageInput.value = string.Empty;
    }

    private void SendPrivateMessage(string targetId, string message)
    {
        if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(targetId)) return;

        SendMessage("privateMessage", new
        {
            targetId = targetId,
            message = message,
            authToken = authToken
        });

        LogMessage($"[SENT] PrivateMessage to {targetId}: {message}");
        _ui.MessageInput.value = string.Empty;
    }

    private void OnApplicationQuit()
    {
        if (_webSocket != null && _webSocket.ReadyState == WebSocketState.Open)
        {
            _webSocket.Close();
            LogMessage("WebSocket closed.");
        }
    }
}