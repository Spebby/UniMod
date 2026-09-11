using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniMod.Utilities;
using UniMod.Utilities.Pooling;


namespace UniMod.Context {
    /// <summary>
    /// Mod source group implementation that also implements a mod source so it can be handled as a single mod source.
    /// </summary>
    public sealed class ModSourceGroup : IModSource, IModSourceGroup {
        public IReadOnlyList<IModSource> Sources { get; }
        readonly List<IModSource> _sources;
        readonly List<ModSourceEntry> _entries;
        readonly AsyncMethodController _fetchController;

        public ModSourceGroup() {
            _sources         = new List<IModSource>();
            _entries         = new List<ModSourceEntry>();
            Sources          = _sources.AsReadOnly();
            _fetchController = new AsyncMethodController();
        }

        public ModSourceGroup(params IModSource[] sources) : this(sources as IEnumerable<IModSource>) { }

        public ModSourceGroup(IEnumerable<IModSource> sources) {
            IModSource[] validSources = sources.Where(source => source is not null).ToArray();
            _sources         = new List<IModSource>(validSources.Length);
            _entries         = new List<ModSourceEntry>(validSources.Length);
            Sources          = _sources.AsReadOnly();
            _fetchController = new AsyncMethodController();

            foreach (IModSource t in validSources) {
                _sources.Add(t);
                _entries.Add(new ModSourceEntry(t));
            }
        }

        public bool AddSource(IModSource source) {
            if (source is null || _sources.Contains(source)) return false;
            _sources.Add(source);
            _entries.Add(new ModSourceEntry(source));
            return true;
        }

        public bool AddSource(IModSource source, int insertAtIndex) {
            if (insertAtIndex >= _sources.Count) return AddSource(source);
            if (source is null || _sources.Contains(source)) return false;
            if (insertAtIndex < 0) insertAtIndex = 0;

            _sources.Insert(insertAtIndex, source);
            _entries.Insert(insertAtIndex, new ModSourceEntry(source));
            return true;
        }

        public void AddSources(IEnumerable<IModSource> sources) {
            foreach (IModSource source in sources) AddSource(source);
        }

        public void AddSources(IEnumerable<IModSource> sources, int insertAtIndex) {
            if (insertAtIndex < 0) insertAtIndex = 0;

            foreach (IModSource source in sources) {
                if (AddSource(source, insertAtIndex)) ++insertAtIndex;
            }
        }

        public bool RemoveSource(IModSource source) {
            int index = _sources.IndexOf(source);
            if (index < 0 || index >= _sources.Count) return false;

            RemoveSourceAt(index);
            return true;
        }

        public void RemoveSourceAt(int index) {
            if (index < 0 || index >= _sources.Count) return;

            _sources.RemoveAt(index);
            _entries.RemoveAt(index);
        }

        public void RemoveSources(IEnumerable<IModSource> sources) {
            foreach (IModSource source in sources) RemoveSource(source);
        }

        public void ClearSources() {
            _fetchController.Cancel();
            _sources.Clear();
            _entries.Clear();
        }

        public async UniTask FetchAsync() {
            CancellationToken cancellationToken = _fetchController.Invoke();
            HashSet<string>   allIds            = HashSetPool<string>.Get();

            try {
                await UniTaskUtility.WhenAll(_entries.Select(async entry => {
                    entry.Ids.Clear();

                    await entry.Source.FetchAsync();
                    cancellationToken.ThrowIfCancellationRequested();
                    await entry.Source.GetAllIdsAsync(entry.Ids);
                    cancellationToken.ThrowIfCancellationRequested();
                }));

                cancellationToken.ThrowIfCancellationRequested();

                foreach (ModSourceEntry entry in _entries) {
                    // make it so there are no duplicate ids between sources (first sources will have priority)
                    entry.Ids.ExceptWith(allIds);
                    allIds.UnionWith(entry.Ids);
                }
            } finally {
                HashSetPool<string>.Release(allIds);
                _fetchController.Finish();
            }
        }

        public UniTask GetAllIdsAsync(ICollection<string> results) {
            foreach (string id in _entries.SelectMany(entry => entry.Ids)) {
                results.Add(id);
            }

            return UniTask.CompletedTask;
        }

        public UniTask<IModLoader> GetLoaderAsync(string id) {
            foreach (ModSourceEntry entry in _entries.Where(entry => entry.Ids.Contains(id))) {
                return entry.Source.GetLoaderAsync(id);
            }

            throw new Exception($"Couldn't find loader for ID {id}");
        }

        public async UniTask GetLoadersAsync(IEnumerable<string> ids, ICollection<IModLoader> results) {
            (IModLoader[] loaders, Exception exception) =
                await UniTaskUtility.WhenAllNoThrow(ids.Select(GetLoaderAsync));

            foreach (IModLoader loader in loaders) {
                if (loader is not null) results.Add(loader);
            }

            if (exception is not null) throw exception;
        }

        public UniTask GetAllLoadersAsync(ICollection<IModLoader> results) => UniTaskUtility.WhenAll(_entries.Select(entry => entry.Source.GetLoadersAsync(entry.Ids, results)));

        struct ModSourceEntry {
            public readonly IModSource Source;
            public readonly HashSet<string> Ids;

            public ModSourceEntry(IModSource source) {
                Source = source;
                Ids    = new HashSet<string>();
            }
        }
    }
}