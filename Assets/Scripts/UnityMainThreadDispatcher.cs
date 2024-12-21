// File 1: /Assets/Scripts/UnityMainThreadDispatcher.cs

using UnityEngine;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

/// <summary>
/// UnityMainThreadDispatcher adalah kelas utilitas yang digunakan untuk mengeksekusi aksi (Action) 
/// pada thread utama Unity. Hal ini sangat berguna ketika thread lain, seperti thread jaringan 
/// atau background task, ingin memperbarui elemen game atau antarmuka pengguna (UI) yang hanya dapat 
/// diakses pada thread utama.
/// 
/// Karena Unity membatasi perubahan pada elemen GameObject atau UI hanya dari thread utama, 
/// kelas ini berfungsi sebagai jembatan untuk menjalankan kode tersebut dengan aman.
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    // Instance tunggal dari UnityMainThreadDispatcher
    private static UnityMainThreadDispatcher _instance;

    // Antrian thread-safe yang menyimpan aksi-aksi yang akan dijalankan di thread utama
    private ConcurrentQueue<Action> _executionQueue = new ConcurrentQueue<Action>();

    /// <summary>
    /// Mengambil instance dari UnityMainThreadDispatcher. 
    /// Jika instance belum ada, maka akan dibuat secara otomatis.
    /// </summary>
    /// <returns>Instance tunggal dari UnityMainThreadDispatcher</returns>
    public static UnityMainThreadDispatcher Instance()
    {
        // Jika instance belum ada, buat GameObject baru untuk menyimpan komponen ini
        if (_instance == null)
        {
            var go = new GameObject("UnityMainThreadDispatcher");
            _instance = go.AddComponent<UnityMainThreadDispatcher>();

            // Menjaga GameObject agar tidak dihancurkan saat pindah antar scene
            DontDestroyOnLoad(go);
        }
        return _instance;
    }

    /// <summary>
    /// Update dipanggil setiap frame oleh Unity.
    /// Method ini akan mengeksekusi semua aksi yang ada di antrian (_executionQueue) 
    /// agar dijalankan di thread utama.
    /// </summary>
    private void Update()
    {
        // Mengeksekusi semua aksi di antrian secara berurutan
        while (_executionQueue.TryDequeue(out Action action))
        {
            action?.Invoke(); // Memanggil aksi jika tidak null
        }
    }

    /// <summary>
    /// Menambahkan aksi ke dalam antrian untuk dieksekusi di thread utama.
    /// </summary>
    /// <param name="action">Aksi yang akan dijalankan</param>
    public void Enqueue(Action action)
    {
        _executionQueue.Enqueue(action);
    }

    /// <summary>
    /// Menambahkan aksi ke antrian dengan dukungan async/await.
    /// Method ini memungkinkan kode asynchronous menunggu hingga aksi selesai dieksekusi di thread utama.
    /// </summary>
    /// <param name="action">Aksi yang akan dijalankan</param>
    /// <returns>Task yang dapat ditunggu (awaited) hingga aksi selesai</returns>
    public async Task EnqueueAsync(Action action)
    {
        // Membuat TaskCompletionSource untuk menyelesaikan task secara manual
        var tcs = new TaskCompletionSource<bool>();

        // Menambahkan aksi ke antrian. Setelah aksi selesai, tandai task sebagai selesai
        Enqueue(() =>
        {
            action?.Invoke(); // Menjalankan aksi jika tidak null
            tcs.SetResult(true); // Menandai TaskCompletionSource sebagai selesai
        });

        // Menunggu hingga task selesai
        await tcs.Task;
    }
}
