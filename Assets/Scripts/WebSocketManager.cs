using UnityEngine;
using UnityEngine.UIElements;
using WebSocketSharp;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

// Deklarasi kelas utama WebSocketManager yang mewarisi MonoBehaviour
public class WebSocketManager : MonoBehaviour
{
    // Variabel yang dapat disetel di Inspector Unity
    [SerializeField] private UIDocument uiDocument; // Referensi ke UIDocument yang berisi UI
    [SerializeField] private string serverUrl = "wss://hisyam99-websockettest.deno.dev"; // URL server WebSocket
    [SerializeField] private string authToken = "12345"; // Token otentikasi untuk koneksi WebSocket

    // Variabel privat untuk menyimpan referensi dan status
    private UIElements _ui; // Objek yang menyimpan referensi ke elemen-elemen UI
    private WebSocket _webSocket; // Objek WebSocket untuk koneksi jaringan
    private string _clientId = string.Empty; // ID klien yang diberikan oleh server
    private string _currentRoomId = string.Empty; // ID ruang yang sedang digunakan
    private string _username = string.Empty; // Nama pengguna yang digunakan dalam aplikasi
    private bool _isFirstTime = true; // Flag untuk menentukan apakah ini pertama kali aplikasi dijalankan

    // Kelas privat untuk mengelola elemen-elemen UI
    private class UIElements
    {
        public Label ConnectionStatus; // Label untuk menampilkan status koneksi
        public TextField MessageInput; // TextField untuk input pesan
        public TextField TargetUserIdInput; // TextField untuk input ID pengguna target
        public Button BroadcastButton; // Tombol untuk mengirim pesan broadcast
        public Button PrivateMessageButton; // Tombol untuk mengirim pesan pribadi
        public Button ChangeRoomButton; // Tombol untuk mengganti ruang
        public ScrollView MessageLog; // ScrollView untuk log pesan
        public VisualElement PopupDialog; // Elemen visual untuk dialog popup
        public TextField PopupUsernameInput; // TextField untuk input nama pengguna dalam popup
        public TextField PopupRoomIdInput; // TextField untuk input ID ruang dalam popup
        public Button PopupJoinButton; // Tombol untuk bergabung dalam popup
        public Button PopupCancelButton; // Tombol untuk membatalkan dalam popup
        public VisualElement ExitPopupDialog; // Elemen visual untuk dialog popup keluar
        public Button ExitConfirmButton; // Tombol konfirmasi keluar
        public Button ExitCancelButton; // Tombol batal keluar
    }

    // Metode yang dipanggil saat objek diinisialisasi
    private void Awake()
    {
        ValidateUIDocument(); // Memvalidasi UIDocument
        InitializeUIReferences(); // Menginisialisasi referensi UI
        SetupUIEventHandlers(); // Mengatur handler event untuk UI
    }

    // Metode yang dipanggil saat objek dimulai
    private void Start()
    {
        Application.runInBackground = true; // Mengizinkan aplikasi berjalan di latar belakang
        if (_isFirstTime)
        {
            ShowPopupDialog(); // Menampilkan dialog popup jika ini pertama kali
        }
        else
        {
            ConnectToWebSocket(); // Menghubungkan ke WebSocket jika bukan pertama kali
        }
    }

    // Memvalidasi apakah UIDocument telah disetel di Inspector
    private void ValidateUIDocument()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[WebSocketManager] Dokumen UI tidak ditetapkan di Inspector!");
            enabled = false; // Menonaktifkan skrip jika UIDocument tidak disetel
        }
    }

    // Menginisialisasi referensi ke elemen-elemen UI
    private void InitializeUIReferences()
    {
        var root = uiDocument.rootVisualElement; // Mendapatkan elemen visual root dari UIDocument
        _ui = new UIElements
        {
            ConnectionStatus = root.Q<Label>("ConnectionStatus"), // Mengambil Label untuk status koneksi
            MessageInput = root.Q<TextField>("MessageInput"), // Mengambil TextField untuk input pesan
            TargetUserIdInput = root.Q<TextField>("TargetUserIdInput"), // Mengambil TextField untuk input ID pengguna target
            BroadcastButton = root.Q<Button>("BroadcastButton"), // Mengambil tombol untuk mengirim pesan broadcast
            PrivateMessageButton = root.Q<Button>("PrivateMessageButton"), // Mengambil tombol untuk mengirim pesan pribadi
            ChangeRoomButton = root.Q<Button>("ChangeRoomButton"), // Mengambil tombol untuk mengganti ruang
            MessageLog = root.Q<ScrollView>("MessageLog"), // Mengambil ScrollView untuk log pesan
            PopupDialog = root.Q<VisualElement>("popupDialog"), // Mengambil elemen visual untuk dialog popup
            PopupUsernameInput = root.Q<TextField>("popupUsernameInput"), // Mengambil TextField untuk input nama pengguna dalam popup
            PopupRoomIdInput = root.Q<TextField>("popupRoomIdInput"), // Mengambil TextField untuk input ID ruang dalam popup
            PopupJoinButton = root.Q<Button>("popupJoinButton"), // Mengambil tombol untuk bergabung dalam popup
            PopupCancelButton = root.Q<Button>("popupCancelButton"), // Mengambil tombol untuk membatalkan dalam popup
            ExitPopupDialog = root.Q<VisualElement>("exitPopupDialog"), // Mengambil elemen visual untuk dialog popup keluar
            ExitConfirmButton = root.Q<Button>("exitConfirmButton"), // Mengambil tombol konfirmasi keluar
            ExitCancelButton = root.Q<Button>("exitCancelButton") // Mengambil tombol batal keluar
        };
    }

    // Mengatur handler event untuk elemen-elemen UI
    private void SetupUIEventHandlers()
    {
        _ui.BroadcastButton.RegisterCallback<ClickEvent>(_ => SendBroadcastMessage(_ui.MessageInput.value)); // Mengatur handler untuk tombol broadcast
        _ui.PrivateMessageButton.RegisterCallback<ClickEvent>(_ => SendPrivateMessage(_ui.TargetUserIdInput.value, _ui.MessageInput.value)); // Mengatur handler untuk tombol pesan pribadi
        _ui.ChangeRoomButton.RegisterCallback<ClickEvent>(_ => ShowPopupDialog()); // Mengatur handler untuk tombol ganti ruang
        _ui.PopupJoinButton.RegisterCallback<ClickEvent>(_ => OnPopupJoinClicked()); // Mengatur handler untuk tombol bergabung dalam popup
        _ui.PopupCancelButton.RegisterCallback<ClickEvent>(_ => OnPopupCancelClicked()); // Mengatur handler untuk tombol batalkan dalam popup
        _ui.ExitConfirmButton.RegisterCallback<ClickEvent>(_ => OnExitConfirmClicked()); // Mengatur handler untuk tombol konfirmasi keluar
        _ui.ExitCancelButton.RegisterCallback<ClickEvent>(_ => OnExitCancelClicked()); // Mengatur handler untuk tombol batal keluar
    }

    // Menampilkan dialog popup
    private void ShowPopupDialog()
    {
        _ui.PopupDialog.style.visibility = Visibility.Visible; // Mengatur visibilitas dialog popup menjadi terlihat
    }

    // Menyembunyikan dialog popup
    private void HidePopupDialog()
    {
        _ui.PopupDialog.style.visibility = Visibility.Hidden; // Mengatur visibilitas dialog popup menjadi tersembunyi
    }

    // Menampilkan dialog popup keluar
    private void ShowExitPopupDialog()
    {
        _ui.ExitPopupDialog.style.visibility = Visibility.Visible; // Mengatur visibilitas dialog popup keluar menjadi terlihat
    }

    // Menyembunyikan dialog popup keluar
    private void HideExitPopupDialog()
    {
        _ui.ExitPopupDialog.style.visibility = Visibility.Hidden; // Mengatur visibilitas dialog popup keluar menjadi tersembunyi
    }

    // Handler untuk klik tombol bergabung dalam popup
    private void OnPopupJoinClicked()
    {
        string newUsername = _ui.PopupUsernameInput.value; // Mendapatkan nama pengguna baru dari input
        string newRoomId = _ui.PopupRoomIdInput.value; // Mendapatkan ID ruang baru dari input
        if (string.IsNullOrWhiteSpace(newUsername) || string.IsNullOrWhiteSpace(newRoomId))
        {
            LogMessage($"[{DateTime.Now:HH:mm:ss}] [KESALAHAN] Username dan Room ID harus diisi."); // Menampilkan pesan kesalahan jika input kosong
            AddNewLineToMessageLog(); // Menambahkan baris baru ke log pesan
            return;
        }
        HidePopupDialog(); // Menyembunyikan dialog popup
        _username = newUsername; // Mengatur nama pengguna baru
        _currentRoomId = newRoomId; // Mengatur ID ruang baru
        _isFirstTime = false; // Mengatur flag pertama kali menjadi false
        if (_webSocket != null && _webSocket.ReadyState == WebSocketState.Open)
        {
            _webSocket.Close(); // Menutup koneksi WebSocket jika sedang terbuka
        }
        ConnectToWebSocket(); // Menghubungkan kembali ke WebSocket
    }

    // Handler untuk klik tombol batalkan dalam popup
    private void OnPopupCancelClicked()
    {
        HidePopupDialog(); // Menyembunyikan dialog popup
        if (_isFirstTime)
        {
            LogMessage($"[{DateTime.Now:HH:mm:ss}] [KESALAHAN] Anda harus memasukkan Username dan Room ID untuk melanjutkan."); // Menampilkan pesan kesalahan jika ini pertama kali dan input kosong
            AddNewLineToMessageLog(); // Menambahkan baris baru ke log pesan
        }
    }

    // Handler untuk klik tombol konfirmasi keluar
    private void OnExitConfirmClicked()
    {
        HideExitPopupDialog(); // Menyembunyikan dialog popup keluar
        Application.Quit(); // Menghentikan aplikasi
    }

    // Handler untuk klik tombol batal keluar
    private void OnExitCancelClicked()
    {
        HideExitPopupDialog(); // Menyembunyikan dialog popup keluar
    }

    // Metode yang dipanggil setiap frame
    private void Update()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ShowExitPopupDialog(); // Menampilkan dialog popup keluar jika tombol back ditekan di Android
            }
        }
    }

    // Menghubungkan ke WebSocket
    private void ConnectToWebSocket()
    {
        _webSocket = new WebSocket(serverUrl); // Membuat objek WebSocket baru dengan URL server
        ConfigureWebSocketEvents(); // Mengonfigurasi event WebSocket
        EstablishConnection(); // Menginisiasi koneksi
    }

    // Mengonfigurasi event WebSocket
    private void ConfigureWebSocketEvents()
    {
        _webSocket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12; // Mengatur protokol SSL yang diizinkan
        _webSocket.OnOpen += HandleConnectionOpen; // Menambahkan handler untuk event koneksi terbuka
        _webSocket.OnMessage += HandleIncomingMessage; // Menambahkan handler untuk event pesan masuk
        _webSocket.OnError += HandleConnectionError; // Menambahkan handler untuk event kesalahan koneksi
        _webSocket.OnClose += HandleConnectionClosed; // Menambahkan handler untuk event koneksi ditutup
    }

    // Menginisiasi koneksi WebSocket
    private void EstablishConnection()
    {
        try
        {
            _webSocket.Connect(); // Mencoba menghubungkan ke WebSocket
        }
        catch (Exception ex)
        {
            UpdateConnectionStatus($"Koneksi gagal: {ex.Message}", Color.red); // Memperbarui status koneksi jika gagal
        }
    }

    // Handler untuk event koneksi terbuka
    private void HandleConnectionOpen(object sender, EventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            UpdateConnectionStatus("Terhubung", Color.green); // Memperbarui status koneksi menjadi terhubung
            SendJoinRequest(); // Mengirim permintaan bergabung
        });
    }

    // Handler untuk event pesan masuk
    private void HandleIncomingMessage(object sender, MessageEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            ProcessServerMessage(e.Data); // Memproses pesan dari server
        });
    }

    // Handler untuk event kesalahan koneksi
    private void HandleConnectionError(object sender, ErrorEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            UpdateConnectionStatus($"Kesalahan: {e.Message}", Color.red); // Memperbarui status koneksi dengan pesan kesalahan
        });
    }

    // Handler untuk event koneksi ditutup
    private void HandleConnectionClosed(object sender, CloseEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            UpdateConnectionStatus("Terputus", Color.yellow); // Memperbarui status koneksi menjadi terputus
        });
    }

    // Mengirim permintaan bergabung ke server
    private void SendJoinRequest()
    {
        SendMessage("join", new
        {
            roomId = _currentRoomId, // ID ruang yang sedang digunakan
            authToken = authToken, // Token otentikasi
            username = _username // Nama pengguna
        });
    }

    // Memperbarui status koneksi di UI
    private void UpdateConnectionStatus(string status, Color color)
    {
        _ui.ConnectionStatus.text = status; // Mengatur teks status koneksi
        _ui.ConnectionStatus.style.color = new StyleColor(color); // Mengatur warna teks status koneksi
        LogMessage($"[{DateTime.Now:HH:mm:ss}] [STATUS] {status}"); // Mencatat status koneksi ke log
        AddNewLineToMessageLog(); // Menambahkan baris baru ke log pesan
    }

    // Memproses pesan dari server
    private void ProcessServerMessage(string message)
    {
        try
        {
            var messageArray = JsonConvert.DeserializeObject<object[]>(message); // Mendekode pesan JSON
            if (messageArray == null || messageArray.Length < 1)
            {
                LogMessage($"[{DateTime.Now:HH:mm:ss}] [KESALAHAN] Format pesan tidak valid."); // Menampilkan pesan kesalahan jika format pesan tidak valid
                AddNewLineToMessageLog(); // Menambahkan baris baru ke log pesan
                return;
            }
            string eventName = messageArray[0].ToString(); // Mendapatkan nama event dari pesan
            object eventData = messageArray.Length > 1 ? messageArray[1] : null; // Mendapatkan data event dari pesan
            HandleSpecificServerEvent(eventName, eventData); // Menangani event spesifik dari server
        }
        catch (Exception ex)
        {
            Debug.LogError($"Kesalahan pemrosesan pesan: {ex.Message}"); // Menampilkan pesan kesalahan jika terjadi kesalahan saat memproses pesan
        }
    }

    // Menangani event spesifik dari server
    private void HandleSpecificServerEvent(string eventName, object eventData)
    {
        switch (eventName)
        {
            case "welcome":
                HandleWelcomeEvent(eventData); // Menangani event selamat datang
                break;
            case "message":
                HandleBroadcastMessage(eventData); // Menangani event pesan broadcast
                break;
            case "privateMessage":
                HandlePrivateMessage(eventData); // Menangani event pesan pribadi
                break;
            case "error":
                LogMessage($"[{DateTime.Now:HH:mm:ss}] [KESALAHAN] {eventData}"); // Menampilkan pesan kesalahan dari server
                AddNewLineToMessageLog(); // Menambahkan baris baru ke log pesan
                break;
            default:
                // Mengabaikan event lain
                break;
        }
    }

    // Menangani event selamat datang
    private void HandleWelcomeEvent(object eventData)
    {
        if (eventData is JObject welcomeData)
        {
            _clientId = welcomeData["id"]?.ToString(); // Mendapatkan ID klien dari data event
            string welcomeMessage = welcomeData["message"]?.ToString(); // Mendapatkan pesan selamat datang dari data event
            LogMessage($"[{DateTime.Now:HH:mm:ss}] [WELCOME] {welcomeMessage}"); // Mencatat pesan selamat datang ke log
            AddNewLineToMessageLog(); // Menambahkan baris baru ke log pesan
        }
    }

    // Menangani event pesan broadcast
    private void HandleBroadcastMessage(object eventData)
    {
        if (eventData is JObject broadcastData)
        {
            string from = broadcastData["from"]?.ToString(); // Mendapatkan ID pengirim dari data event
            string username = broadcastData["username"]?.ToString(); // Mendapatkan nama pengirim dari data event
            string content = broadcastData["message"]?.ToString(); // Mendapatkan isi pesan dari data event
            string messageId = broadcastData["id"]?.ToString(); // Mendapatkan ID pesan dari data event
            long timestamp = broadcastData["timestamp"] != null ? (long)broadcastData["timestamp"] : 0; // Mendapatkan timestamp dari data event
            // Mengonversi timestamp dari server ke zona waktu lokal client
            DateTime serverDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(timestamp);
            DateTime localDateTime = serverDateTime.ToLocalTime();
            LogMessage($"[{localDateTime:HH:mm:ss}] [BROADCAST] - {username} ({from}): {content}"); // Mencatat pesan broadcast ke log
            AddNewLineToMessageLog(); // Menambahkan baris baru ke log pesan
        }
    }

    // Menangani event pesan pribadi
    private void HandlePrivateMessage(object eventData)
    {
        if (eventData is JObject privateMessageData)
        {
            string from = privateMessageData["from"]?.ToString(); // Mendapatkan ID pengirim dari data event
            string username = privateMessageData["username"]?.ToString(); // Mendapatkan nama pengirim dari data event
            string content = privateMessageData["message"]?.ToString(); // Mendapatkan isi pesan dari data event
            LogMessage($"[{DateTime.Now:HH:mm:ss}] [PRIBADI] - {username} ({from}): {content}"); // Mencatat pesan pribadi ke log
            AddNewLineToMessageLog(); // Menambahkan baris baru ke log pesan
        }
    }

    // Mencatat pesan ke log pesan
    private void LogMessage(string message)
    {
        // Memisahkan username, userId, dan pesan dari pesan
        string pattern = @"\[(\d{2}:\d{2}:\d{2})\] \[(\w+)\] - (.+?) \(([^)]+)\)";
        Match match = Regex.Match(message, pattern);
        if (match.Success)
        {
            string time = match.Groups[1].Value; // Mendapatkan waktu dari pesan
            string type = match.Groups[2].Value; // Mendapatkan tipe pesan
            string username = match.Groups[3].Value; // Mendapatkan nama pengguna dari pesan
            string userId = match.Groups[4].Value; // Mendapatkan ID pengguna dari pesan
            // Memisahkan bagian pesan dari sisa pesan
            string[] parts = message.Split(new[] { ": " }, 2, StringSplitOptions.None);
            string timeTypePart = $"[{time}] [{type}]"; // Menggabungkan waktu dan tipe pesan
            string userIdPart = $"{username} ({userId})"; // Menggabungkan nama pengguna dan ID pengguna
            string contentPart = parts.Length > 1 ? parts[1] : string.Empty; // Mendapatkan isi pesan
            // Membuat Label untuk waktu dan tipe
            var timeTypeLabel = new Label(timeTypePart)
            {
                style =
                {
                    marginTop = 5, // Margin atas
                    marginBottom = 5, // Margin bawah
                    color = new StyleColor(Color.black), // Warna teks
                    unityFontStyleAndWeight = FontStyle.Normal, // Gaya font
                    whiteSpace = WhiteSpace.Normal, // Ruang putih normal
                    maxWidth = Length.Percent(100) // Lebar maksimal 100%
                },
                pickingMode = PickingMode.Position // Mode picking
            };
            // Membuat TextField untuk username dan userId
            var userIdField = new TextField
            {
                value = userIdPart, // Nilai TextField
                style =
                {
                    marginTop = 5, // Margin atas
                    marginBottom = 5, // Margin bawah
                    color = new StyleColor(Color.black), // Warna teks
                    unityFontStyleAndWeight = FontStyle.Normal, // Gaya font
                    maxWidth = Length.Percent(100), // Lebar maksimal 100%
                    // Menghilangkan border dan background agar terlihat seperti Label
                    borderBottomWidth = 0,
                    borderTopWidth = 0,
                    borderLeftWidth = 0,
                    borderRightWidth = 0,
                    backgroundColor = new StyleColor(Color.clear),
                    // Memastikan teks tidak dapat diedit
                    unityTextAlign = TextAnchor.MiddleLeft
                },
                isReadOnly = true, // Hanya baca
                multiline = false, // Tidak multi-line
                pickingMode = PickingMode.Position // Mode picking
            };
            // Membuat Label untuk isi pesan
            var messageLabel = new Label(contentPart)
            {
                style =
                {
                    marginTop = 5, // Margin atas
                    marginBottom = 5, // Margin bawah
                    color = new StyleColor(Color.black), // Warna teks
                    unityFontStyleAndWeight = FontStyle.Normal, // Gaya font
                    whiteSpace = WhiteSpace.Normal, // Ruang putih normal
                    maxWidth = Length.Percent(100) // Lebar maksimal 100%
                },
                pickingMode = PickingMode.Position // Mode picking
            };
            // Membuat container untuk mengelompokkan Label untuk waktu dan tipe, TextField untuk username dan userId, dan Label untuk pesan
            var container = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column, // Arah flex column
                    alignItems = Align.FlexStart // Penyelarasan item flex
                }
            };
            container.Add(timeTypeLabel); // Menambahkan Label waktu dan tipe ke container
            container.Add(userIdField); // Menambahkan TextField username dan userId ke container
            container.Add(messageLabel); // Menambahkan Label pesan ke container
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
                value = message, // Nilai TextField
                style =
                {
                    marginTop = 5, // Margin atas
                    marginBottom = 5, // Margin bawah
                    color = new StyleColor(Color.black), // Warna teks
                    unityFontStyleAndWeight = FontStyle.Normal, // Gaya font
                    maxWidth = Length.Percent(100), // Lebar maksimal 100%
                    // Menghilangkan border dan background agar terlihat seperti Label
                    borderBottomWidth = 0,
                    borderTopWidth = 0,
                    borderLeftWidth = 0,
                    borderRightWidth = 0,
                    backgroundColor = new StyleColor(Color.clear),
                    // Memastikan teks tidak dapat diedit
                    unityTextAlign = TextAnchor.MiddleLeft
                },
                isReadOnly = true, // Hanya baca
                multiline = true, // Multi-line
                pickingMode = PickingMode.Position // Mode picking
            };
            _ui.MessageLog.contentContainer.Add(fullMessageField); // Menambahkan TextField ke message log
            _ui.MessageLog.ScrollTo(fullMessageField); // Scroll ke TextField terbaru
        }
    }

    // Menambahkan baris baru ke log pesan
    private void AddNewLineToMessageLog()
    {
        var newline = new Label("\n")
        {
            style =
            {
                whiteSpace = WhiteSpace.Pre, // Ruang putih preformatted
                unityFontStyleAndWeight = FontStyle.Normal // Gaya font
            },
            pickingMode = PickingMode.Ignore // Mode picking
        };
        _ui.MessageLog.contentContainer.Add(newline); // Menambahkan Label baris baru ke message log
    }

    // Mengirim pesan ke server
    private new void SendMessage(string eventName, object data)
    {
        if (_webSocket == null || _webSocket.ReadyState != WebSocketState.Open)
        {
            LogMessage($"[{DateTime.Now:HH:mm:ss}] [KESALAHAN] WebSocket tidak terhubung. Tidak dapat mengirim pesan."); // Menampilkan pesan kesalahan jika WebSocket tidak terhubung
            AddNewLineToMessageLog(); // Menambahkan baris baru ke log pesan
            return;
        }
        var message = new[] { eventName, data }; // Membuat array pesan dengan nama event dan data
        string jsonMessage = JsonConvert.SerializeObject(message); // Mengonversi pesan ke JSON
        _webSocket.Send(jsonMessage); // Mengirim pesan ke WebSocket
    }

    // Mengirim pesan broadcast
    private void SendBroadcastMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return; // Mengembalikan jika pesan kosong
        SendMessage("broadcast", new
        {
            message = message, // Isi pesan
            authToken = authToken, // Token otentikasi
            roomId = _currentRoomId // ID ruang
        });
        _ui.MessageInput.value = string.Empty; // Mengosongkan input pesan setelah mengirim
    }

    // Mengirim pesan pribadi
    private void SendPrivateMessage(string targetId, string message)
    {
        if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(targetId))
        {
            LogMessage($"[{DateTime.Now:HH:mm:ss}] [KESALAHAN] Pesan dan ID target harus diisi."); // Menampilkan pesan kesalahan jika input kosong
            AddNewLineToMessageLog(); // Menambahkan baris baru ke log pesan
            return;
        }
        SendMessage("privateMessage", new
        {
            targetId = targetId, // ID target
            message = message, // Isi pesan
            authToken = authToken // Token otentikasi
        });
        LogMessage($"[{DateTime.Now:HH:mm:ss}] [PRIBADI] Pesan pribadi terkirim ke {targetId}: {message}"); // Mencatat pesan pribadi ke log
        AddNewLineToMessageLog(); // Menambahkan baris baru ke log pesan
        _ui.MessageInput.value = string.Empty; // Mengosongkan input pesan setelah mengirim
    }

    // Handler untuk event aplikasi keluar
    private void OnApplicationQuit()
    {
        if (_webSocket != null && _webSocket.ReadyState == WebSocketState.Open)
        {
            _webSocket.Close(); // Menutup koneksi WebSocket jika terbuka
            LogMessage($"[{DateTime.Now:HH:mm:ss}] [STATUS] WebSocket ditutup."); // Mencatat status WebSocket ditutup ke log
            AddNewLineToMessageLog(); // Menambahkan baris baru ke log pesan
        }
    }
}