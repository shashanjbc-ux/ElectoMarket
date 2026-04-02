using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ElectoMarket.Tests
{
  public class FakeSessionPro : ISession
  {
    private readonly Dictionary<string, byte[]> _store = new();
    public string Id => Guid.NewGuid().ToString();
    public bool IsAvailable => true;
    public IEnumerable<string> Keys => _store.Keys;
    public void Clear() => _store.Clear();
    public void Remove(string key) => _store.Remove(key);
    public void Set(string key, byte[] value) => _store[key] = value;
    public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
    public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken ct = default) => Task.CompletedTask;

    // Helpers
    public void SetInt32(string key, int value) => Set(key, BitConverter.GetBytes(value));
    public void SetString(string key, string value) => Set(key, Encoding.UTF8.GetBytes(value));
    public string? GetString(string key) => _store.TryGetValue(key, out var b) ? Encoding.UTF8.GetString(b) : null;
  }
}
