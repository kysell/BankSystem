using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using BankSystem.Domain.Models;

namespace BankSystem.App.Services;

public sealed class MemoryWorker : IDisposable
{
    private readonly MemoryHandle _handle;
    private static long _totalAllocated;
    private static long _totalFreed;
    public static long TotalAllocated => Interlocked.Read(ref _totalAllocated);
    public static long TotalFreed => Interlocked.Read(ref _totalFreed);

    public MemoryWorker(int chunkSize)
    {
        if (chunkSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSize));
        }

        _handle = new MemoryHandle(chunkSize);
        Interlocked.Add(ref _totalAllocated, chunkSize);
    }

    public void DoWork() => ObjectDisposedException.ThrowIf(_handle.IsClosed, this);
    public void Dispose() => _handle.Dispose();
    // SafeHandle освободит память даже тогда, когда вызывающий код забыл Dispose.
    private sealed class MemoryHandle : SafeHandle
    {
        private readonly int _size;
        public MemoryHandle(int size) : base(IntPtr.Zero, true)
        {
            _size = size;
            SetHandle(Marshal.AllocHGlobal(Math.Max(size, 1)));
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(handle);
            Interlocked.Add(ref _totalFreed, _size);
            return true;
        }
    }
}

public sealed class ClientFilePipeline
{
    public async Task<IReadOnlyList<string>> WriteAsync(IAsyncEnumerable<Client> clients, string directory, long maxBytes, CancellationToken token = default)
    {
        if (maxBytes < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxBytes));
        }

        token.ThrowIfCancellationRequested();
        Directory.CreateDirectory(directory);
        var paths = new List<string>();
        FileStream? file = null;
        var batch = Guid.NewGuid().ToString("N");
        try
        {
            await foreach (var client in clients.WithCancellation(token))
            {
                var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(client) + "\n");
                if (bytes.Length > maxBytes)
                {
                    throw new ArgumentException("A single client exceeds the file size limit.", nameof(maxBytes));
                }

                if (file is null || file.Length + bytes.Length > maxBytes)
                {
                    if (file is not null)
                    {
                        await file.DisposeAsync();
                    }

                    var path = Path.Combine(directory, $"clients-{batch}-{paths.Count + 1}.jsonl");
                    file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, true);
                    paths.Add(path);
                }

                await file.WriteAsync(bytes, token);
            }
        }
        finally
        {
            if (file is not null)
            {
                await file.DisposeAsync();
            }
        }

        return paths;
    }

    public async Task<IReadOnlyList<Client>> ReadAsync(string path, CancellationToken token = default)
    {
        var clients = new List<Client>();
        using var reader = new StreamReader(path);
        while (true)
        {
            string? line = await reader.ReadLineAsync(token);
            if (line == null)
            {
                break;
            }
            Client? client = JsonSerializer.Deserialize<Client>(line);
            if (client == null)
            {
                throw new JsonException("Null client record.");
            }
            clients.Add(client);
        }

        return clients;
    }
}

public sealed class SimpleCalculator
{
    public int GetSum(int a, int b) => checked(a + b);
    public int GetSubtraction(int a, int b) => checked(a - b);
    public int GetMultiplication(int a, int b) => checked(a * b);
    public int GetDivision(int a, int b) => a / b;
}
