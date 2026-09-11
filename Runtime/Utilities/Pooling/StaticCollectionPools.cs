using System;
using System.Collections.Generic;


// This file contains static pool classes for every generic collection in the System.Collections.Generic namespace.
namespace UniMod.Utilities.Pooling {
    /// <summary>
    /// Base class for static collection pools.
    /// </summary>
    class CollectionPool<TCollection, TItem>
        where TCollection : class, ICollection<TItem>, new() {
        [ThreadStatic] static Pool<TCollection> s_instance;

        public static Pool<TCollection> Instance => s_instance ??= CreatePool();

        public static int TotalCount => Instance.TotalCount;
        public static int PooledCount => Instance.PooledCount;
        public static int ActiveCount => Instance.ActiveCount;

        public static Pool<TCollection> CreatePool() => new(CreateNew, onRelease: collection => collection.Clear());
        public static TCollection Get() => Instance.Get();
        public static void Release(TCollection obj) => Instance.Release(obj);
        public static Pool<TCollection>.Handle Get(out TCollection obj) => Instance.Get(out obj);
        public static IEnumerable<TCollection> Get(int count) => Instance.Get(count);
        public static void Get(int count, ICollection<TCollection> collection) => Instance.Get(count, collection);
        public static void Release(IEnumerable<TCollection> objects) => Instance.Release(objects);
        public static void Clear() => Instance.Clear();
        static TCollection CreateNew() => new();
    }

    class ListPool<T> : CollectionPool<List<T>, T> { }

    class LinkedListPool<T> : CollectionPool<LinkedList<T>, T> { }

    class SortedListPool<TKey, TValue> : CollectionPool<SortedList<TKey, TValue>, KeyValuePair<TKey, TValue>> { }

    class HashSetPool<T> : CollectionPool<HashSet<T>, T> { }

    class SortedSetPool<T> : CollectionPool<SortedSet<T>, T> { }

    class DictionaryPool<TKey, TValue> : CollectionPool<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>> { }

    class SortedDictionaryPool<TKey, TValue> : CollectionPool<SortedDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>> { }

    // Stack and Queue collections don't inherit from any interface containing the Clear method. Microsoft should seriously consider in adding
    // the clear method to the ICollection non-generic interface.
    static class StackPool<T> {
        [ThreadStatic] static Pool<Stack<T>> s_instance;

        public static Pool<Stack<T>> Instance => s_instance ??= CreatePool();

        public static int TotalCount => Instance.TotalCount;
        public static int PooledCount => Instance.PooledCount;
        public static int ActiveCount => Instance.ActiveCount;

        public static Pool<Stack<T>> CreatePool() => new(CreateNew, onRelease: stack => stack.Clear());
        public static Stack<T> Get() => Instance.Get();
        public static void Release(Stack<T> obj) => Instance.Release(obj);
        public static Pool<Stack<T>>.Handle Get(out Stack<T> obj) => Instance.Get(out obj);
        public static IEnumerable<Stack<T>> Get(int count) => Instance.Get(count);
        public static void Get(int count, ICollection<Stack<T>> collection) => Instance.Get(count, collection);
        public static void Release(IEnumerable<Stack<T>> objects) => Instance.Release(objects);
        public static void Clear() => Instance.Clear();
        static Stack<T> CreateNew() => new();
    }

    static class QueuePool<T> {
        [ThreadStatic] static Pool<Queue<T>> s_instance;

        public static Pool<Queue<T>> Instance => s_instance ??= CreatePool();

        public static int TotalCount => Instance.TotalCount;
        public static int PooledCount => Instance.PooledCount;
        public static int ActiveCount => Instance.ActiveCount;

        public static Pool<Queue<T>> CreatePool() => new(CreateNew, onRelease: queue => queue.Clear());
        public static Queue<T> Get() => Instance.Get();
        public static void Release(Queue<T> obj) => Instance.Release(obj);
        public static Pool<Queue<T>>.Handle Get(out Queue<T> obj) => Instance.Get(out obj);
        public static IEnumerable<Queue<T>> Get(int count) => Instance.Get(count);
        public static void Get(int count, ICollection<Queue<T>> collection) => Instance.Get(count, collection);
        public static void Release(IEnumerable<Queue<T>> objects) => Instance.Release(objects);
        public static void Clear() => Instance.Clear();
        static Queue<T> CreateNew() => new();
    }
}