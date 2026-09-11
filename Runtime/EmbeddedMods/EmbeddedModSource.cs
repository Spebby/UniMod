using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UniMod.Utilities;
using UnityEngine;


namespace UniMod.EmbeddedMods {
    [AddComponentMenu("UniMod/Embedded Mod Source")]
    [DisallowMultipleComponent]
    public sealed class EmbeddedModSource : MonoBehaviour, IModSource {
        public const string SOURCE_LABEL = "Embedded";

        [SerializeField] List<EmbeddedModConfig> configs;

        readonly Dictionary<string, EmbeddedModLoader> _loaders = new();
        readonly Dictionary<string, EmbeddedModConfig> _configsById = new();

        public UniTask FetchAsync() {
            _configsById.Clear();

            foreach (EmbeddedModConfig config in configs.Where(config => config)) {
                _configsById[config.modId] = config;
            }

            return UniTask.CompletedTask;
        }

        public UniTask GetAllIdsAsync(ICollection<string> results) {
            foreach (string id in _configsById.Keys) results.Add(id);
            return UniTask.CompletedTask;
        }

        public UniTask<IModLoader> GetLoaderAsync(string id) {
            if (string.IsNullOrEmpty(id))
                throw new Exception("Null or empty mod ID");
            if (!_configsById.TryGetValue(id, out EmbeddedModConfig config))
                throw new Exception($"Couldn't find loader for ID {id}");

            if (!_loaders.TryGetValue(id, out EmbeddedModLoader loader))
                _loaders[id] = loader = new EmbeddedModLoader(config);

            return UniTask.FromResult<IModLoader>(loader);
        }

        public async UniTask GetLoadersAsync(IEnumerable<string> ids, ICollection<IModLoader> results) {
            (IModLoader[] loaders, Exception exception) =
                await UniTaskUtility.WhenAllNoThrow(ids.Select(GetLoaderAsync));

            foreach (IModLoader loader in loaders) {
                if (loader is not null) results.Add(loader);
            }

            if (exception is not null) throw exception;
        }

        public UniTask GetAllLoadersAsync(ICollection<IModLoader> results) =>
            GetLoadersAsync(_configsById.Keys, results);
    }
}