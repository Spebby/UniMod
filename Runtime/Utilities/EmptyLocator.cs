using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;


namespace UniMod.Utilities {
    internal sealed class EmptyLocator : IResourceLocator {
        public static readonly EmptyLocator Instance = new();

        public IEnumerable<IResourceLocation> AllLocations => throw new NotImplementedException();

        public bool Locate(object key, Type type, out IList<IResourceLocation> locations) {
            locations = null;
            return false;
        }

        public string LocatorId => "__EmptyResourceLocator";
        public IEnumerable<object> Keys => Array.Empty<object>();
    }
}
