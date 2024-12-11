using UnityEngine;
using WebSocketSharp;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/**
 * Kelas WsClient menghandle koneksi WebSocket ke server dan mengirimkan pesan.
 */
public class WsClient : MonoBehaviour
{
    // Variabel untuk menyimpan objek WebSocket
    private WebSocket ws;
    
    // Token autentikasi yang digunakan untuk koneksi ke server
    private string authToken = "12345"; // sebagai contoh penerapan token

    /**
     * Fungsi Start dipanggil ketika skrip dijalankan.
     * Menghubungkan koneksi ke server.
     */
    private void Start()
    {
        ConnectToServer();
    }

    /**
     * Fungsi ConnectToServer menghubungkan koneksi ke server.
     * Mengatur event handler untuk koneksi terbuka, pesan diterima, dan error.
     */
    private void ConnectToServer()
    {
        // Membuat objek WebSocket dengan alamat server
        ws = new WebSocket("ws://localhost:4000");
        
        // Mengatur event handler untuk koneksi terbuka
        ws.OnOpen += (sender, e) =>
        {
            // Mencetak log ketika koneksi terbuka
            Debug.Log("WebSocket connection established.");
            // Mengundang fungsi join dengan penundaan
            Invoke("join", new { roomId = "room1", authToken });
        };

        // Mengatur event handler untuk pesan diterima
        ws.OnMessage += (sender, e) =>
        {
            // Mencetak log pesan yang diterima
            Debug.Log($"Raw Message Received: {e.Data}");
            // Menghandle pesan yang diterima
            HandleServerMessage(e.Data);
        };

        // Mengatur event handler untuk error
        ws.OnError += (sender, e) =>
        {
            // Mencetak log error
            Debug.LogError($"WebSocket Error: {e.Message}");
        };

        // Menghubungkan koneksi ke server
        ws.Connect();
    }

    /**
     * Fungsi HandleServerMessage menghandle pesan yang diterima dari server.
     * @param message pesan yang diterima dari server
     */
    private void HandleServerMessage(string message)
    {
        try
        {
            // Mengparse pesan sebagai array sederhana
            var messageArray = JsonConvert.DeserializeObject<object[]>(message);
            if (messageArray != null && messageArray.Length >= 1)
            {
                // Mengambil nama event dari array
                string eventName = messageArray[0].ToString();
                // Mengambil data event dari array
                object eventData = messageArray.Length > 1 ? messageArray[1] : null;

                // Menghandle event berdasarkan nama event
                switch (eventName)
                {
                    case "welcome":
                        // Mencetak log welcome
                        Debug.Log($"Server Welcome: {eventData}");
                        break;
                    case "message": // Broadcast message
                        // Menghandle pesan broadcast
                        if (eventData is JObject dataObj)
                        {
                            // Mencetak log pesan broadcast
                            Debug.Log($"Broadcast from {dataObj["from"]}: {dataObj["message"]}");
                        }
                        break;
                    case "privateMessage":
                        // Menghandle pesan private
                        if (eventData is JObject privateDataObj)
                        {
                            // Mencetak log pesan private
                            Debug.Log($"Private message from {privateDataObj["from"]}: {privateDataObj["message"]}");
                        }
                        break;
                    case "error":
                        // Mencetak log error
                        Debug.LogError($"Server Error: {eventData}");
                        break;
                    default:
                        // Mencetak log event yang tidak dihandle
                        Debug.Log($"Unhandled event: {eventName}, Data: {eventData}");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            // Mencetak log error parsing
            Debug.LogError($"Error parsing server message: {ex.Message}");
        }
    }

    /**
     * Fungsi Invoke mengirimkan pesan ke server.
     * @param eventName nama event yang dikirimkan
     * @param data data yang dikirimkan
     */
    private void Invoke(string eventName, object data)
    {
        // Mengcek apakah koneksi siap untuk mengirimkan pesan
        if (ws == null || ws.ReadyState != WebSocketState.Open)
        {
            // Mencetak log peringatan jika koneksi tidak siap
            Debug.LogWarning("WebSocket not ready to send message");
            return;
        }

        // Membuat array pesan dengan nama event dan data
        var message = new[] { eventName, data };
        // Mengubah array menjadi string JSON
        string jsonMessage = JsonConvert.SerializeObject(message);
        // Mengirimkan pesan ke server
        ws.Send(jsonMessage);
    }

    /**
     * Fungsi SendBroadcastMessage mengirimkan pesan broadcast ke server.
     * @param message pesan yang dikirimkan
     */
    private void SendBroadcastMessage(string message)
    {
        // Mengirimkan pesan broadcast dengan nama event "broadcast"
        Invoke("broadcast", new
        {
            message = message,
            authToken = authToken
        });
    }

    /**
     * Fungsi SendPrivateMessage mengirimkan pesan private ke server.
     * @param targetId ID target pesan private
     * @param message pesan yang dikirimkan
     */
    private void SendPrivateMessage(string targetId, string message)
    {
        // Mengirimkan pesan private dengan nama event "privateMessage"
        Invoke("privateMessage", new
        {
            targetId = targetId,
            message = message,
            authToken = authToken
        });
    }

    /**
     * Fungsi Update dipanggil setiap frame.
     * Menghandle input pengguna untuk mengirimkan pesan.
     */
    private void Update()
    {
        // Menghandle input pengguna untuk mengirimkan pesan broadcast
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SendBroadcastMessage("Hai semua dari Unity :)");
        }

        // Menghandle input pengguna untuk mengirimkan pesan private
        if (Input.GetKeyDown(KeyCode.Return))
        {
            SendPrivateMessage("some-target-id", "Hai dari Unity.");
        }
    }

    /**
     * Fungsi OnApplicationQuit dipanggil ketika aplikasi dihentikan.
     * Menghentikan koneksi WebSocket.
     */
    private void OnApplicationQuit()
    {
        // Menghentikan koneksi WebSocket jika tidak null
        if (ws != null)
        {
            ws.Close();
        }
    }
}