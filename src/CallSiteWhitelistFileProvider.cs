// Copyright (c) 2023-present EF Core N+1 Guard contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Options;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// Hot-reloads a <see cref="CallSiteWhitelist"/> from a JSON file on disk, exposing it as an
/// <see cref="IOptionsMonitor{TOptions}"/> so a new entry can be added to the whitelist file
/// without requiring the observed application to restart. Backed by a <see cref="FileSystemWatcher"/>;
/// reload failures (e.g. the file is mid-write) are swallowed and the previous value is retained.
/// </summary>
public sealed class CallSiteWhitelistFileProvider : IOptionsMonitor<CallSiteWhitelist>, IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly string _filePath;
    private readonly object _reloadLock = new();
    private CallSiteWhitelist _current;
    private readonly System.Collections.Generic.List<Action<CallSiteWhitelist, string?>> _listeners = new();
    private Timer? _debounceTimer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CallSiteWhitelistFileProvider"/> class,
    /// performing an initial load of <paramref name="filePath"/> and starting a file watcher
    /// for subsequent changes.
    /// </summary>
    /// <param name="filePath">Path to the JSON whitelist file to watch and hot-reload.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is null or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when <paramref name="filePath"/> does not exist on disk.</exception>
    public CallSiteWhitelistFileProvider(string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        var fullPath = Path.GetFullPath(filePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Whitelist file not found.", fullPath);

        _filePath = fullPath;
        _current = LoadFromDisk(fullPath);

        var directory = Path.GetDirectoryName(fullPath)!;
        _watcher = new FileSystemWatcher(directory, Path.GetFileName(fullPath))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime
        };
        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _watcher.Renamed += OnFileChanged;
        _watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// Gets the most recently loaded <see cref="CallSiteWhitelist"/>.
    /// </summary>
    public CallSiteWhitelist CurrentValue => Volatile.Read(ref _current!);

    /// <summary>
    /// Gets the whitelist. The <paramref name="name"/> parameter is ignored; a single
    /// unnamed instance is tracked per file.
    /// </summary>
    /// <param name="name">Ignored; present to satisfy <see cref="IOptionsMonitor{TOptions}"/>.</param>
    /// <returns>The current <see cref="CallSiteWhitelist"/>.</returns>
    public CallSiteWhitelist Get(string? name) => CurrentValue;

    /// <summary>
    /// Registers a callback invoked whenever the whitelist file is successfully reloaded.
    /// </summary>
    /// <param name="listener">The callback to invoke with the new whitelist and (ignored) name.</param>
    /// <returns>A token that unregisters the callback when disposed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="listener"/> is null.</exception>
    public IDisposable OnChange(Action<CallSiteWhitelist, string?> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);

        lock (_reloadLock)
        {
            _listeners.Add(listener);
        }

        return new Unsubscriber(this, listener);
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce: editors typically issue multiple write events for a single save.
        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(_ => Reload(), null, TimeSpan.FromMilliseconds(200), Timeout.InfiniteTimeSpan);
    }

    private void Reload()
    {
        Action<CallSiteWhitelist, string?>[] listenersSnapshot;
        CallSiteWhitelist reloaded;

        try
        {
            reloaded = LoadFromDisk(_filePath);
        }
        catch (IOException)
        {
            // File is mid-write or locked; keep the previous value and try again on the next event.
            return;
        }
        catch (System.Text.Json.JsonException)
        {
            // Malformed JSON mid-save; keep the previous value.
            return;
        }

        lock (_reloadLock)
        {
            Volatile.Write(ref _current!, reloaded);
            listenersSnapshot = _listeners.ToArray();
        }

        foreach (var listener in listenersSnapshot)
            listener(reloaded, null);
    }

    private static CallSiteWhitelist LoadFromDisk(string fullPath)
    {
        var json = File.ReadAllText(fullPath);
        return CallSiteWhitelistJsonExtensions.FromJson(json) ?? new CallSiteWhitelist();
    }

    /// <summary>
    /// Stops watching the file and releases underlying resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _watcher.EnableRaisingEvents = false;
        _watcher.Changed -= OnFileChanged;
        _watcher.Created -= OnFileChanged;
        _watcher.Renamed -= OnFileChanged;
        _watcher.Dispose();
        _debounceTimer?.Dispose();
    }

    private sealed class Unsubscriber : IDisposable
    {
        private readonly CallSiteWhitelistFileProvider _owner;
        private readonly Action<CallSiteWhitelist, string?> _listener;

        public Unsubscriber(CallSiteWhitelistFileProvider owner, Action<CallSiteWhitelist, string?> listener)
        {
            _owner = owner;
            _listener = listener;
        }

        public void Dispose()
        {
            lock (_owner._reloadLock)
            {
                _owner._listeners.Remove(_listener);
            }
        }
    }
}
