using System;
using System.IO;
using UniMod.Context;
using UniMod.LocalMods;
using UnityEngine;


namespace UniMod {
    public static class UniModRuntime {
        public static IUniModContext Context => s_context ?? throw new Exception("UniMod context has not been initialized");
        public static bool IsContextInitialized => s_context is not null;

        static IUniModContext s_context;

        // UniMod package version. Needs to be manually updated
        public const string VERSION = "0.0.1";

        public const string INFO_FILE = "info.json";
        public const string MOD_FILE_EXTENSION_NO_DOT = "umod";
        public const string MOD_FILE_EXTENSION = "." + MOD_FILE_EXTENSION_NO_DOT;
        public const string THUMBNAIL_FILE = "thumbnail.png";
        public const string ADDRESSABLES_CATALOG_FILE_NAME = "catalog.json";
        public const string STARTUP_ADDRESS = "__mod_startup";
        public const string ASSEMBLIES_FOLDER = "Assemblies";
        public const string ASSETS_FOLDER = "Assets";

        public static readonly bool IsDebugBuild = Debug.isDebugBuild;

        const string LOCAL_INSTALLATION_FOLDER_NAME = "UniMods";

        const string ADDRESSABLES_LOAD_PATH = "{{UnityEngine.Application.persistentDataPath}}/" + LOCAL_INSTALLATION_FOLDER_NAME + "/";

        public static readonly string LocalInstallationFolder = Path.Combine(Application.persistentDataPath, LOCAL_INSTALLATION_FOLDER_NAME);

        /// <summary>
        /// Initializes the default UniMod context with the specified host ID and version. The UniMod context can be only initialized once.
        /// </summary>
        public static void InitializeContext(string hostId, string hostVersion) {
            if (s_context is not null) throw new Exception("UniMod context has already been initialized");
            InitializeContext(new ModHost(hostId, hostVersion));
        }

        /// <summary>
        /// Initializes the default UniMod context with the specified host. The UniMod context can be only initialized once.
        /// </summary>
        public static void InitializeContext(IModHost host) {
            if (s_context is not null) throw new Exception("UniMod context has already been initialized");

            string            installationFolder = LocalInstallationFolder;
            LocalModInstaller installer          = new(installationFolder);
            LocalModSource    localModSource     = new(installationFolder);
            UniModContext     context            = new(host, installer, localModSource);

            InitializeContext(context);
        }

        /// <summary>
        /// Initializes the UniMod context with the given context implementation. The UniMod context can be only initialized once.
        /// </summary>
        public static void InitializeContext(IUniModContext context) {
            if (s_context is not null) throw new Exception("UniMod context has already been initialized");
            s_context = context;
        }

        public static string GetAddressablesLoadPathForMod(string modId) => Path.Combine(ADDRESSABLES_LOAD_PATH, modId, ASSETS_FOLDER);
    }
}