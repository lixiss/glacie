using System;
using System.Runtime.CompilerServices;

using Glacie.Data.Arz;

namespace Glacie.GX1.Abstractions
{
    /// <summary>
    /// Represents game module.
    /// </summary>
    public abstract class Module
    {
        private readonly string? _name;
        private bool _disposed;

        private ArzDatabase? _database;

        protected Module(string? name)
        {
            _name = EmptyToNull(name);
        }

        protected void DisposeInternal()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _database?.Dispose();
                }

                _database = null;

                _disposed = true;
            }
        }

        protected void ThrowIfDisposed()
        {
            if (_disposed) throw Error.ObjectDisposed(GetType().FullName);
        }

        protected bool Disposed => _disposed;

        protected object SyncRoot => this;

        public string? Name => _name;

        public bool IsRoot => _name == null;

        public abstract bool IsReadOnly { get; }

        public abstract bool IsSource { get; }

        public bool IsTarget => !IsSource;

        public bool HasDatabase => throw Error.NotImplemented();
        public bool HasResources => throw Error.NotImplemented();

        public ArzDatabase Database
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_database != null) return _database;
                return OpenDatabaseInternal();
            }
        }

        // TODO: Link it to ProjectContext?
        // I want if they can outlive project context.
        // read-only SourceModule should be shareable between project contexts.
        // however, for this purposes they can't be linked to project context,
        // and it is not clear how to route lazy operation results.
        // probably add:
        // bool TryOpen(IReporting... );
        // bool IsOpened { get; }
        // bool IsShareable => IsReadOnly && IsSource;
        // bool IsFaulted { get; }

        // TODO: string? PhysicalPath

        // TODO: List of Resource Bundles
        // TODO: ResourceProvider

        // TODO: List of languages

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ArzDatabase OpenDatabaseInternal()
        {
            lock (SyncRoot)
            {
                ThrowIfDisposed();

                if (_database != null) return _database;
                return _database = OpenDatabaseCore();
            }
        }

        protected abstract ArzDatabase OpenDatabaseCore();

        private static string? EmptyToNull(string? value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            return value;
        }
    }
}
