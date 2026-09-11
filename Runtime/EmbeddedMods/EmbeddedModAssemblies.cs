using System;
using System.Collections.Generic;
using UnityEngine;


namespace UniMod.EmbeddedMods {
    [Serializable]
    public struct EmbeddedModAssemblies {
        public RuntimePlatform platform;
        public List<string> names;
    }
}
