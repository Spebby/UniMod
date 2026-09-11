using Cysharp.Threading.Tasks;
using UniMod.Context;
using UniMod.EmbeddedMods;
using UnityEngine;
using UnityEngine.Events;


namespace UniMod {
    /// <summary>
    /// You can use this component for an automatic initialisation of the UniMod context with
    /// some configuration parameters exposed in the inspector.
    /// <br/><br/>
    /// You can optionally add an <see cref="EmbeddedModSource"/> component to the same GameObject to support embedded mods.
    /// </summary>
    [AddComponentMenu("UniMod/UniMod Context Initializer", 0)]
    [DisallowMultipleComponent]
    public sealed class UniModContextInitializer : MonoBehaviour {
        [Header("Configuration")] [SerializeField]
        string hostId = "com.company.name";

        [SerializeField] string hostVersion = "0.1.0";
        [SerializeField] bool supportStandaloneMods = true;
        [SerializeField] bool supportScriptingInMods;
        [SerializeField] bool supportModsCreatedForOtherHosts;

        [Header("Loading")] [Space(5)] [SerializeField]
        bool refreshContextOnStart;

        [SerializeField] bool loadAllModsOnStart;

        [Header("Events")] [Space(5)] public UnityEvent onContextInitialized = new();
        public UnityEvent onContextRefreshed = new();
        public UnityEvent onModsLoaded = new();

        void Awake() => InitializeContext();
        void Start() => StartAsync().Forget();

        void InitializeContext() {
            if (UniModRuntime.IsContextInitialized) {
                Debug.LogWarning("[UniMod] tried to initialize a UniMod context but it has already been initialized");
                return;
            }

            // initialize a mod host with the user configuration
            ModHost host = new(hostId, hostVersion) {
                SupportStandaloneMods           = supportStandaloneMods,
                SupportModsContainingAssemblies = supportScriptingInMods,
                SupportModsCreatedForOtherHosts = supportModsCreatedForOtherHosts
            };

            // initialize the UniMod context
            UniModRuntime.InitializeContext(host);

            // check if we have an embedded mod source component so we can add it to the context
            EmbeddedModSource embeddedModSource = GetComponent<EmbeddedModSource>();
            if (embeddedModSource)
                UniModRuntime.Context.AddSource(embeddedModSource);

            onContextInitialized.Invoke();
        }

        async UniTaskVoid StartAsync() {
            if (refreshContextOnStart) {
                await UniModRuntime.Context.RefreshAsync();
                onContextRefreshed.Invoke();
            }

            if (loadAllModsOnStart) {
                await UniModRuntime.Context.TryLoadAllModsAsync();
                onModsLoaded.Invoke();
            }
        }
    }
}
