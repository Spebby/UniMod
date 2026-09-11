using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UniMod.Data;
using UniMod.Utilities;
using UniMod.Utilities.Pooling;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;


namespace UniMod.EmbeddedMods {
    public sealed class EmbeddedModLoader : IModLoader {
        static readonly IReadOnlyList<Assembly> EMPTY_ASSEMBLIES = new List<Assembly>(0).AsReadOnly();

        public readonly EmbeddedModConfig Config;

        public ModInfo Info { get; }
        public string Source { get; }
        public bool ContainsAssets { get; }
        public bool ContainsAssemblies { get; }
        public bool IsLoaded { get; set; }
        public IResourceLocator ResourceLocator { get; set; }
        public IReadOnlyList<Assembly> LoadedAssemblies { get; set; }

        readonly ModStartup _startup;
        readonly IReadOnlyList<Assembly> _loadedAssemblies;

        UniTaskCompletionSource _loadOperation;
        Sprite _thumbnail;

        public EmbeddedModLoader(EmbeddedModConfig config, string source = EmbeddedModSource.SOURCE_LABEL) {
            Config            = config;
            _startup          = config.startup;
            _loadedAssemblies = GetLoadedAssemblies(config).AsReadOnly();

            Info               = UniModUtility.CreateModInfoFromEmbeddedConfig(config);
            Source             = source;
            ContainsAssets     = config.ContainsAssets;
            ContainsAssemblies = _loadedAssemblies.Count > 0;
            // to simulate how local mods are loaded, lets not assign these properties yet even though the assets/assemblies are already loaded
            ResourceLocator    = EmptyLocator.Instance;
            LoadedAssemblies   = EMPTY_ASSEMBLIES;
        }

        public async UniTask LoadAsync(IMod mod) {
            if (_loadOperation != null) {
                await _loadOperation.Task;
                return;
            }

            _loadOperation = new UniTaskCompletionSource();

            try {
                await InternalLoadAsync(mod);
                _loadOperation.TrySetResult();
            } catch (Exception exception) {
                _loadOperation.TrySetException(exception);
                throw;
            }
        }

        public UniTask<Sprite> GetThumbnailAsync() {
            if (!Config.thumbnail)
                return UniTask.FromResult<Sprite>(null);

            _thumbnail ??= UniModUtility.CreateSpriteFromTexture(Config.thumbnail);
            return UniTask.FromResult(_thumbnail);
        }

        async UniTask InternalLoadAsync(IMod mod) {
            if (IsLoaded)
                return;

            // to simulate how local mods are loaded, we will assign now the properties to the already loaded assets/assemblies
            if (ContainsAssets)
                ResourceLocator = await EmbeddedModAssetsLocator.CreateAsync(Config.modId, Config.assets);
            if (ContainsAssemblies)
                LoadedAssemblies = _loadedAssemblies;

            // run startup script and methods
            if (_startup)
                await _startup.StartAsync(mod);

            await UniModUtility.RunStartupMethodsAsync(LoadedAssemblies, mod);

            IsLoaded = true;
        }

        static List<Assembly> GetLoadedAssemblies(EmbeddedModConfig config) {
            // find the assemblies config for the current platform
            foreach (EmbeddedModAssemblies configAssemblies in config.assemblies) {
                RuntimePlatform currentPlatform = Application.platform;

#if UNITY_EDITOR
                currentPlatform = currentPlatform switch {
                    RuntimePlatform.WindowsEditor => RuntimePlatform.WindowsPlayer,
                    RuntimePlatform.OSXEditor     => RuntimePlatform.OSXPlayer,
                    RuntimePlatform.LinuxEditor   => RuntimePlatform.LinuxPlayer,
                    _                             => currentPlatform
                };
#endif

                if (configAssemblies.platform != currentPlatform) continue;
                List<Assembly> results = new(configAssemblies.names.Count);

                // get all the domain loaded assemblies and register them by name
                using Pool<Dictionary<string, Assembly>>.Handle _ =
                    DictionaryPool<string, Assembly>.Get(out Dictionary<string, Assembly> assembliesByName);
                foreach (Assembly assembly in DomainAssemblies.Assemblies)
                    assembliesByName[assembly.GetName().Name] = assembly;

                // find the assemblies in the domain corresponding to the names in the config
                foreach (string assemblyName in configAssemblies.names) {
                    if (assembliesByName.TryGetValue(assemblyName, out Assembly assembly))
                        results.Add(assembly);
                }

                return results;
            }

            return new List<Assembly>(0);
        }
    }
}
