using System;
using System.Collections.Generic;


namespace UniMod.EmbeddedMods {
    [Serializable]
    public struct EmbeddedModAsset {
        public string guid;
        public List<string> labels;
    }
}
