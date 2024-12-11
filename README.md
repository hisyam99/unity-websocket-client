# Unity WebSocket Client

Aplikasi Unity WebSocket client yang dapat berkomunikasi dengan server WebSocket Deno.

## Konfigurasi

1. Jalankan server websocket dari repository berikut: https://github.com/hisyam99/deno-websocket-server
2. Buka file `Assets/WebSocketClient.cs` dan setting alamat server WebSocket yang ingin digunakan.
3. Setting token autentikasi yang digunakan untuk menghubungkan ke server WebSocket.

## Menjalankan Aplikasi

1. Jalankan aplikasi Unity WebSocket client dengan menekan tombol "Play" di Unity.
2. Aplikasi akan menghubungkan ke server WebSocket dan menampilkan console log.
3. Tekan tombol Spasi(Broadcast Pesan), atau Enter(PrivateMessage ke ID spesifik) untuk mengirimkan perintah ke server WebSocket. Ini menggunakan fungsi `Invoke` di `WebSocketClient.cs`.

## Fitur

* Menghubungkan ke server WebSocket
* Mengirimkan perintah ke server WebSocket
* Menerima pesan dari server WebSocket
* Menampilkan log komunikasi