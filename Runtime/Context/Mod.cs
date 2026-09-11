using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UniMod.Data;
using UniMod.Utilities;
using UniMod.Utilities.Pooling;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;


namespace UniMod.Context {
    /// <summary>
    /// Default implementation for mods that uses a mod loader for the information and loading. It can only be obtained by resolving a closure with
    /// the ResolveClosure static method since this implementation is intended to be used within a mod closure.
    /// </summary>
    public sealed class Mod : IMod {
        public readonly IModLoader Loader;

        public string Id => Loader.Info.Id;
        public string Version => Loader.Info.Version;
        public string DisplayName => Loader.Info.DisplayName;
        public string Description => Loader.Info.Description;
        public string Source => Loader.Source;
        public bool ContainsAssets => Loader.ContainsAssets;
        public bool ContainsAssemblies => Loader.ContainsAssemblies;
        public ModTargetInfo Target => Loader.Info.Target;

        public ModIssues Issues { get; set; }
        public IReadOnlyCollection<Mod> Dependencies { get; }
        public IReadOnlyCollection<ModReference> MissingDependencies => _missingDependencies;

        public bool IsLoaded => Loader.IsLoaded;
        public IResourceLocator ResourceLocator => Loader.ResourceLocator;
        public IReadOnlyList<Assembly> LoadedAssemblies => Loader.LoadedAssemblies;

        readonly List<Mod> _dependencies;
        readonly HashSet<ModReference> _missingDependencies;
        readonly Dictionary<ModIssues, List<Mod>> _modsByIssue;

        bool _resolved;

        public static Dictionary<string, Mod> ResolveClosure(IEnumerable<IModLoader> loaders, IModHost host) {
            Dictionary<string, Mod> mods = new();

            // create the Mod instances from the loaders and register them by ID
            foreach (IModLoader loader in loaders) {
                if (loader != null) mods[loader.Info.Id] = new Mod(loader);
            }

            // resolve all created mod instances
            foreach (Mod mod in mods.Values) mod.Resolve(mods, host);
            return mods;
        }

        Mod(IModLoader loader) {
            Loader = loader ?? throw new NullReferenceException("Null mod loader");

            _dependencies        = new List<Mod>(Loader.Info.Dependencies?.Count ?? 0);
            _missingDependencies = new HashSet<ModReference>();
            _modsByIssue         = new Dictionary<ModIssues, List<Mod>>();

            Dependencies = _dependencies.AsReadOnly();
        }

        public UniTask LoadAsync() => Loader.LoadAsync(this);
        public UniTask<Sprite> GetThumbnailAsync() => Loader.GetThumbnailAsync();

        public void GetDependenciesRelatedToIssues(ModIssues issues, ICollection<Mod> results) {
            ModIssues[] allIssues = Enum.GetValues(typeof(ModIssues)) as ModIssues[];
            if (allIssues is null) return;

            foreach (ModIssues issue in allIssues) {
                if ((issues & issue) != issue || !_modsByIssue.TryGetValue(issue, out List<Mod> mods)) continue;
                foreach (Mod mod in mods) results.Add(mod);
            }
        }

        void Resolve(IReadOnlyDictionary<string, Mod> mods, IModHost host) {
            if (_resolved) return;
            _resolved = true;

            // get any possible host support issues with the mod
            Issues = host.GetModIssues(this);

            // if the mod contains assemblies, check if we are currently using the Mono scripting backend
            if (!Issues.HasFlag(ModIssues.UnsupportedContent) && Loader.ContainsAssemblies &&
                !UniModUtility.IS_MOD_SCRIPTING_SUPPORTED) {
                Debug.LogWarning($"[UniMod] the mod {Id} contains assemblies but the project is using the IL2CPP backend. Please switch to Mono if you want scripting support for mods");
                Issues |= ModIssues.UnsupportedContent;
            }

            if (Loader.Info.Dependencies is null)
                return;

            // resolve all direct dependencies, this will solve the entire graph recursively
            foreach ((string id, string version) in Loader.Info.Dependencies) {
                if (!mods.TryGetValue(id, out Mod dependency)) {
                    // mark as missing dependency
                    Issues |= ModIssues.MissingDependencies;
                    _missingDependencies.Add(new ModReference { id = id, version = version });
                    continue;
                }

                _dependencies.Add(dependency);
                dependency.Resolve(mods, host);

                // check version support
                if (!UniModUtility.IsSemanticVersionSupportedByCurrent(version, dependency.Version)) {
                    Issues |= ModIssues.UnsupportedDependenciesVersion;
                    LinkModWithIssue(dependency, ModIssues.UnsupportedDependenciesVersion);
                }

                // check any other issues
                if (dependency.Issues == 0) continue;
                Issues |= ModIssues.DependenciesWithIssues;
                LinkModWithIssue(dependency, ModIssues.DependenciesWithIssues);
            }

            // check for cyclic dependencies
            if (HasCyclicDependencies())
                Issues |= ModIssues.CyclicDependencies;
        }

        bool HasCyclicDependencies() {
            return _dependencies.Any(HasCyclicDependencies);
        }

        bool HasCyclicDependencies(Mod dependency) {
            // if the dependency that we are checking is this one or has cyclic dependencies then return true
            if (dependency == this ||
                (dependency.Issues & ModIssues.CyclicDependencies) == ModIssues.CyclicDependencies)
                return true;

            // recursively check the transient dependencies for cyclic dependencies
            return dependency.Dependencies.Any(HasCyclicDependencies);
        }

        void LinkModWithIssue(Mod mod, ModIssues issue) {
            if (!_modsByIssue.TryGetValue(issue, out List<Mod> mods))
                _modsByIssue[issue] = mods = new List<Mod>();

            mods.Add(mod);
        }

        #region IMod Overrides
        IReadOnlyCollection<IMod> IMod.Dependencies => Dependencies;

        public void GetDependenciesRelatedToIssues(ModIssues issues, ICollection<IMod> results) {
            if (results is null) return;

            using Pool<List<Mod>>.Handle _ = ListPool<Mod>.Get(out List<Mod> modResults);
            GetDependenciesRelatedToIssues(issues, modResults);

            foreach (Mod mod in modResults) results.Add(mod);
        }
        #endregion
    }
}