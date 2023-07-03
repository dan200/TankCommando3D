using Dan200.Core.Async;
using Dan200.Core.Main;
using Dan200.Core.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Dan200.Core.Assets
{
    internal struct AssetLoadEventArgs
    {
		public readonly HashSet<string> Paths;

        public AssetLoadEventArgs(HashSet<string> paths)
        {
            Paths = paths;
        }
    }

    internal delegate object LoadDataDelegate(Stream stream, string path);

    internal static class Assets
    {
        private class RegisteredType
        {
            public Type Type;
            public ConstructorInfo Constructor;
            public LoadDataDelegate DataLoader;
            public string FallbackPath;
			public object FallbackData; // Will be a List<object> for compound assets
            public bool IsCompound;
        }

        private class LoadedAsset
        {
            public readonly RegisteredType Type;
            public readonly string Path;
            public readonly IAsset Asset;
            public List<AssetSource> Sources;
            public bool IsFallback;

            public LoadedAsset(RegisteredType type, string path, IAsset asset)
            {
                Type = type;
                Path = path;
                Asset = asset;
                Sources = null;
                IsFallback = false;
            }
        }

        private static Dictionary<string, RegisteredType> s_extensionToRegisteredType = new Dictionary<string, RegisteredType>();
        private static Dictionary<Type, RegisteredType> s_typeToRegisteredType = new Dictionary<Type, RegisteredType>();

        private static List<AssetSource> s_assetSources = new List<AssetSource>();
        private static Dictionary<string, LoadedAsset> s_loadedAssets = new Dictionary<string, LoadedAsset>();

        private class BasicAssetAsyncLoad
        {
            public RegisteredType Type;
            public string Path;
            public AssetSource Source;
            public Promise<object> DataPromise;
            public Promise Promise;
        }

        private class CompoundAssetAsyncLoad
        {
            public RegisteredType Type;
            public string Path;
            public List<AssetSource> Sources;
            public Promise<List<object>> DataPromise;
            public Promise Promise;
        }

        private static Queue<BasicAssetAsyncLoad> s_basicAssetLoadRequests = new Queue<BasicAssetAsyncLoad>();
        private static Queue<CompoundAssetAsyncLoad> s_compoundAssetLoadRequests = new Queue<CompoundAssetAsyncLoad>();

        public static int Count
        {
            get
            {
                return s_loadedAssets.Count;
            }
        }

        public static IReadOnlyList<AssetSource> Sources
        {
            get
            {
                return s_assetSources;
            }
        }

        public static event StaticStructEventHandler<AssetLoadEventArgs> OnAssetsLoaded;
        public static event StaticStructEventHandler<AssetLoadEventArgs> OnAssetsReloaded;
        public static event StaticStructEventHandler<AssetLoadEventArgs> OnAssetsUnloaded;

        // SETUP

		public static void RegisterType<TAssetType>(string extension) where TAssetType : class, IAsset
        {
            // Check valid constructor exists
            var assetType = typeof(TAssetType);
            bool compound;
            ConstructorInfo constructor;
            if (typeof(IBasicAsset).IsAssignableFrom(assetType))
            {
                compound = false;
                constructor = assetType.GetConstructor(new Type[] {
                    typeof( string ),
					typeof( object )
                });
                if (constructor == null)
                {
                    throw new ArgumentException("Type " + assetType.Name + " does not have required constructor (string, object)");
                }
            }
            else if (typeof(ICompoundAsset).IsAssignableFrom(assetType))
            {
                compound = true;
                constructor = assetType.GetConstructor(new Type[] {
                    typeof( string )
                });
                if (constructor == null)
                {
                    throw new ArgumentException("Type " + assetType.Name + " does not have required constructor (string)");
                }
            }
            else
            {
                throw new ArgumentException("Type " + assetType.Name + " is neither " + typeof(IBasicAsset).Name + " nor " + typeof(ICompoundAsset).Name);
            }

			var loadDataMethod = assetType.GetMethod("LoadData", new Type[] {
				typeof( Stream ),
				typeof( string )
			});
			if (loadDataMethod == null || !loadDataMethod.IsStatic || loadDataMethod.ReturnType != typeof(object))
			{
				throw new ArgumentException("Type " + assetType.Name + " does not have required static method object LoadData(Stream, string)");
			}
			var loadDataDelegate = (LoadDataDelegate)Delegate.CreateDelegate(typeof(LoadDataDelegate), loadDataMethod);

			// Check the extension is not already registered
            RegisteredType existingType;
	        if (s_extensionToRegisteredType.TryGetValue(extension, out existingType))
	        {
	            throw new ArgumentException("Extension " + extension + " is already registered to type " + existingType.Type.Name);
	        }

			// Check the type is not already registered
			if (s_typeToRegisteredType.TryGetValue(assetType, out existingType))
			{
                throw new ArgumentException("Type " + assetType.Name + " is already registered");
			}

            // Register the type
            var registeredType = new RegisteredType();
            registeredType.Type = assetType;
            registeredType.Constructor = constructor;
            registeredType.DataLoader = loadDataDelegate;
            registeredType.FallbackPath = "defaults/default." + extension;
            registeredType.FallbackData = null;
            registeredType.IsCompound = compound;
            s_typeToRegisteredType.Add(assetType, registeredType);
            s_extensionToRegisteredType.Add(extension, registeredType);
        }

        public static void UnregisterAllTypes()
        {
            App.Assert(s_loadedAssets.Count == 0);
            s_typeToRegisteredType.Clear();
            s_extensionToRegisteredType.Clear();
        }

        public static void AddSource(AssetSource source)
        {
            if (!s_assetSources.Contains(source))
            {
                App.Log("Adding {0} asset source", source.Name);
                s_assetSources.Add(source);
            }
        }

        public static void RemoveSource(AssetSource source)
        {
            if (s_assetSources.Remove(source))
            {
                App.Log("Removed {0} asset source", source.Name);
            }
        }

        public static void RemoveAllSources()
        {
            s_assetSources.Clear();
        }

        // LOAD

        public static void Load(string path)
        {
            var existing = LookupAsset(path);
            if (existing == null ||
                existing.IsFallback ||
                (existing.Type.IsCompound ?
                 !CompareSources(existing.Sources, CalculateCompoundSources(path)) :
                  existing.Sources[0] != CalculateBasicSource(path)))
            {
                LoadAsset(path);
                FireAssetLoadEvents();
            }
        }

        public static void LoadAll()
        {
            var list = BuildLoadAllList(false);
            if (list.Count > 0)
            {
                foreach (var path in list)
                {
                    LoadAsset(path);
                }
                App.Log("Loaded {0} assets", list.Count);
                FireAssetLoadEvents();
            }
        }

        public static CompoundPromise LoadAllAsync(TaskQueue queue)
        {
            var list = BuildLoadAllList(false);
            var promises = new List<Promise>(list.Count);
            foreach (var path in list)
            {
                var promise = LoadAssetAsync(path, queue);
                if (promise != null)
                {
                    promises.Add(promise);
                }
            }
            App.Log("Requested load of {0} assets", list.Count);
            return new CompoundPromise(promises);
        }

        public static void Reload(string path)
        {
            LoadAsset(path);
            FireAssetLoadEvents();
        }

        public static void ReloadAll()
        {
            var list = BuildLoadAllList(true);
            if (list.Count > 0)
            {
                foreach (var path in list)
                {
                    LoadAsset(path);
                }
                FireAssetLoadEvents();
                App.Log("Reloaded {0} assets", list.Count);
            }
        }

        public static CompoundPromise ReloadAllAsync(TaskQueue queue)
        {
            var list = BuildLoadAllList(true);
            var promises = new List<Promise>(list.Count);
            foreach (var path in list)
            {
                var promise = LoadAssetAsync(path, queue);
                if (promise != null)
                {
                    promises.Add(promise);
                }
            }
            App.Log("Requested reload of {0} assets", list.Count);
            return new CompoundPromise(promises);
        }

        private static List<string> BuildLoadAllList(bool reloadIfSourceUnchanged)
        {
            var results = new List<string>();
            var pathsSeen = new HashSet<string>();
            for (int i = s_assetSources.Count - 1; i >= 0; --i)
            {
                var source = s_assetSources[i];
                foreach (var path in source.FileStore.EnumerateFiles())
                {
                    if (!pathsSeen.Contains(path))
                    {
                        var extension = AssetPath.GetExtension(path);
                        var type = LookupType(extension);
                        if (type != null)
                        {
                            if (type.IsCompound)
                            {
                                // Reload compound assets if the source combination has changed
                                var existing = LookupAsset(path);
                                if (existing == null ||
                                    existing.IsFallback ||
                                    reloadIfSourceUnchanged ||
                                    !CompareSources(existing.Sources, CalculateCompoundSources(path)))
                                {
                                    results.Add(path);
                                }
                            }
                            else
                            {
                                // Reload basic assets if the source has change
                                var existing = LookupAsset(path);
                                if (existing == null ||
                                    existing.IsFallback ||
                                    reloadIfSourceUnchanged ||
                                    existing.Sources[0] != source)
                                {
                                    results.Add(path);
                                }
                            }
                        }
                        pathsSeen.Add(path);
                    }
                }
            }
            return results;
        }

        private static object LoadData(AssetSource source, LoadDataDelegate loader, string path)
        {
            using (var stream = source.FileStore.OpenFile(path))
            {
                return loader.Invoke(stream, path);
            }
        }

        private static List<object> LoadDatas(List<AssetSource> sources, LoadDataDelegate loader, string path)
        {
            var datas = new List<object>(sources.Count);
            foreach (AssetSource source in sources)
            {
                datas.Add(LoadData(source, loader, path));
            }
            return datas;
        }

        private static void LoadAsset(string path)
        {
            var extension = AssetPath.GetExtension(path);
            var type = LookupType(extension);
            if (type != null)
            {
                if (type.IsCompound)
                {
                    // Load a compound asset
                    var sources = CalculateCompoundSources(path);
                    if (sources.Count > 0)
                    {
						List<object> datas;
						try
						{
							datas = LoadDatas(sources, type.DataLoader, path);
						}
						catch (Exception e)
						{
							App.LogError("Error loading {0}: {1}", path, e.Message);
							return;
						}
                        LoadCompoundAsset(type, path, sources, datas);
                    }
                }
                else
                {
                    // Load a basic asset
                    var source = CalculateBasicSource(path);
                    if (source != null)
                    {
						object data;
						try
						{
							data = LoadData(source, type.DataLoader, path);
						}
						catch (Exception e)
						{
							App.LogError("Error loading {0}: {1}", path, e.Message);
							return;
						}
                        LoadBasicAsset(type, path, source, data);
                    }
                }
            }
        }

        private static Promise LoadAssetAsync(string path, TaskQueue queue)
        {
            var extension = AssetPath.GetExtension(path);
            var type = LookupType(extension);
            if (type != null)
            {
                if (type.IsCompound)
                {
                    // Load a compound asset
                    var sources = CalculateCompoundSources(path);
                    if (sources.Count > 0)
                    {
                        var request = new CompoundAssetAsyncLoad();
                        request.Type = type;
                        request.Path = path;
                        request.Sources = sources;
                        request.DataPromise = new Promise<List<object>>();
                        queue.AddTask(delegate
                        {
							try
							{
								var data = LoadDatas(sources, type.DataLoader, path);
                                request.DataPromise.Succeed(data);
							}
							catch (Exception e)
							{
                                request.DataPromise.Fail(e);
							}
                        });
                        request.Promise = new Promise();
                        s_compoundAssetLoadRequests.Enqueue(request);
                        return request.Promise;
                    }
                }
                else
                {
                    // Load a basic asset
                    var source = CalculateBasicSource(path);
                    if (source != null)
                    {
                        var request = new BasicAssetAsyncLoad();
                        request.Type = type;
                        request.Path = path;
                        request.Source = source;
                        request.DataPromise = new Promise<object>();
                        queue.AddTask(delegate
                        {
							try
							{
                                var data = LoadData(source, type.DataLoader, path);
                                request.DataPromise.Succeed(data);
							}
							catch (Exception e)
							{
                                request.DataPromise.Fail(e);
							}
                        });
                        request.Promise = new Promise();
                        s_basicAssetLoadRequests.Enqueue(request);
                        return request.Promise;
                    }
                }
            }
            return null;
        }

        public static void CompleteAsyncLoads(TimeSpan maxDuration)
        {
            // Complete basic asset loads
            var startTime = DateTime.UtcNow;
            while (s_basicAssetLoadRequests.Count > 0 && (DateTime.UtcNow - startTime) < maxDuration)
            {
                BasicAssetAsyncLoad request = s_basicAssetLoadRequests.Peek();
                if (request.DataPromise.IsReady)
                {
                    try
                    {
                        var result = request.DataPromise.Complete();
                        LoadBasicAsset(request.Type, request.Path, request.Source, result);
                        request.Promise.Succeed();
                    }
                    catch (Exception e)
                    {
                        App.LogError("{0}", e.Message);
                        request.Promise.Fail(e);
                    }
                    finally
                    {
                        s_basicAssetLoadRequests.Dequeue();
                    }
                }
                else
                {
                    break;
                }
            }

            // Complete compound asset loads
            while (s_compoundAssetLoadRequests.Count > 0 && (DateTime.UtcNow - startTime) < maxDuration)
            {
                CompoundAssetAsyncLoad request = s_compoundAssetLoadRequests.Peek();
                if (request.DataPromise.IsReady)
                {
                    try
                    {
                        var result = request.DataPromise.Complete();
                        s_compoundAssetLoadRequests.Dequeue();
                        LoadCompoundAsset(request.Type, request.Path, request.Sources, result);
                        request.Promise.Succeed();
                    }
                    catch(Exception e)
                    {
                        s_compoundAssetLoadRequests.Dequeue();
                        request.Promise.Fail(e);
                    }
                }
                else
                {
                    break;
                }
            }

            // Fire events
            FireAssetLoadEvents();
        }

        private static AssetSource CalculateBasicSource(string path)
        {
            for (int i = s_assetSources.Count - 1; i >= 0; --i)
            {
                var source = s_assetSources[i];
                if (source.FileStore.FileExists(path))
                {
                    return source;
                }
            }
            return null;
        }

        private static IBasicAsset ConstructBasicAsset(RegisteredType type, string path, object data)
        {
            App.Assert(!type.IsCompound);
            try
            {
                return (IBasicAsset)type.Constructor.Invoke(new object[] {
                    path, data
                });
            }
            catch (TargetInvocationException e)
            {
                throw App.Rethrow(e.InnerException);
            }
        }

        private static void DisposeData(object data)
        {
            if (data != null && data is IDisposable)
            {
                ((IDisposable)data).Dispose();
            }
        }

        private static void DisposeDatas(List<object> datas)
        {
            if (datas != null)
            {
                foreach (object data in datas)
                {
                    DisposeData(data);
                }
            }
        }

        private static void LoadBasicAsset(RegisteredType type, string path, AssetSource source, object data, bool isFallback = false)
        {
            App.Assert(!type.IsCompound);

            // Load or reload the asset
            var existingBasic = LookupAsset(path);
            if (existingBasic != null)
            {
                // Reload an existing asset
                //App.Log("Reloading basic asset {0}", path);
                ((IBasicAsset)existingBasic.Asset).Reload(data);
                existingBasic.Sources[0] = source;
                existingBasic.IsFallback = isFallback;
                s_assetsReloadedList.Add(path);
            }
            else
            {
                // Create a new asset
                //App.Log("Loading basic asset {0}", path);
                IBasicAsset asset = ConstructBasicAsset(type, path, data);
                var loadedAsset = new LoadedAsset(type, path, asset);
                loadedAsset.Sources = new List<AssetSource>(1);
                loadedAsset.Sources.Add(source);
                loadedAsset.IsFallback = isFallback;
                s_loadedAssets.Add(path, loadedAsset);
                s_assetsLoadedList.Add(path);
            }

            // Store or dispose the data
            if (!isFallback)
            {
				string extension = AssetPath.GetExtension(path);
				if (path == type.FallbackPath)
                {
					if (type.FallbackData != null)
					{
						DisposeData(type.FallbackData);
					}
					type.FallbackData = data;
                }
                else
                {
                    DisposeData(data);
                }
            }
        }

        private static List<AssetSource> CalculateCompoundSources(string path)
        {
            var results = new List<AssetSource>(s_assetSources.Count);
            for (int i = 0; i < s_assetSources.Count; ++i)
            {
                var source = s_assetSources[i];
                if (source.FileStore.FileExists(path))
                {
                    results.Add(source);
                }
            }
            return results;
        }

        private static bool CompareSources(List<AssetSource> a, List<AssetSource> b)
        {
            if (a.Count == b.Count)
            {
                for (int i = 0; i < a.Count; ++i)
                {
                    if (a[i] != b[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        private static ICompoundAsset ConstructCompoundAsset(RegisteredType type, string path)
        {
            App.Assert(type.IsCompound);
            try
            {
                return (ICompoundAsset)type.Constructor.Invoke(new object[] {
                    path
                });
            }
            catch (TargetInvocationException e)
            {
                throw App.Rethrow(e.InnerException);
            }
        }

        private static void LoadCompoundAsset(RegisteredType type, string path, List<AssetSource> sources, List<object> datas, bool isFallback = false)
        {
            App.Assert(type.IsCompound);
            App.Assert(sources.Count == datas.Count);

            // Load or reload the data
            var existingCompound = LookupAsset(path);
            if (existingCompound != null)
            {
                // Reload an existing asset
                //App.Log("Reloading compound asset {0}", path);
                var asset = (ICompoundAsset)existingCompound.Asset;
                asset.Reset();
                foreach (var data in datas)
                {
                    asset.AddLayer(data);
                }
                existingCompound.Sources = sources;
                existingCompound.IsFallback = isFallback;
                s_assetsReloadedList.Add(path);
            }
            else
            {
                // Create a new asset
                //App.Log("Loading compound asset {0}", path);
                var asset = ConstructCompoundAsset(type, path);
                foreach (var data in datas)
                {
                    asset.AddLayer(data);
                }
                var loadedAsset = new LoadedAsset(type, path, asset);
                loadedAsset.Sources = sources;
                loadedAsset.IsFallback = isFallback;
                s_loadedAssets.Add(path, loadedAsset);
                s_assetsLoadedList.Add(path);
            }

            // Store or dispose the data
            if (!isFallback)
            {
				string extension = AssetPath.GetExtension(path);
				if (path == type.FallbackPath)
                {
					if (type.FallbackData != null)
					{
						DisposeDatas((List<object>)type.FallbackData);
					}
                    type.FallbackData = datas;
                }
                else
                {
                    DisposeDatas(datas);
                }
            }
        }

        // UNLOAD

        public static void Unload(string path)
        {
            UnloadAsset(path);
            FireAssetLoadEvents();
        }

        public static void UnloadAll()
        {
            foreach (var path in s_loadedAssets.Keys.ToList())
            {
                UnloadAsset(path);
            }
            FireAssetLoadEvents();
        }

        public static void UnloadUnsourced()
        {
            foreach (var path in s_loadedAssets.Keys.ToList())
            {
                var existing = LookupAsset(path);
                if (!existing.IsFallback)
                {
                    bool sourced = false;
                    foreach (AssetSource source in existing.Sources)
                    {
                        if (s_assetSources.Contains(source) &&
                            source.FileStore.FileExists(path))
                        {
                            sourced = true;
                            break;
                        }
                    }
                    if (!sourced)
                    {
                        UnloadAsset(path);
                    }
                }
            }
            FireAssetLoadEvents();
        }

        private static void UnloadAsset(string path)
        {
            // Find the asset
            var loadedAsset = LookupAsset(path);
            if (loadedAsset != null)
            {
                // Unload the asset
                loadedAsset.Asset.Dispose();
                s_loadedAssets.Remove(path);
                s_assetsUnloadedList.Add(path);

                // Dispose the fallback data if we're unloaded a fallback asset
                if (!loadedAsset.IsFallback)
                {
					if (path == loadedAsset.Type.FallbackPath)
                    {
						var type = loadedAsset.Type;
                        if (type.IsCompound)
                        {
							DisposeDatas((List<object>)type.FallbackData);
                        }
                        else
                        {
							DisposeData(type.FallbackData);
                        }
						type.FallbackData = null;
                    }
                }
            }
        }

        // QUERY

        public static bool Exists<TAsset>(string path) where TAsset : class, IAsset
        {
            var loadedAsset = LookupAsset(path);
            if (loadedAsset != null &&
                loadedAsset.Asset is TAsset &&
                !loadedAsset.IsFallback)
            {
                return true;
            }
            return false;
        }

        public static TAsset Get<TAsset>(string path) where TAsset : class, IAsset
        {
            // Try to get the asset
            var loadedAsset = LookupAsset(path);
            if (loadedAsset != null)
            {
                if (!(loadedAsset.Asset is TAsset))
                {
					App.LogError("Asset {0} is loaded but is not of type {1}", path, typeof(TAsset).Name);
                    throw new AssetLoadException(path, "Asset type mismatch");
                }
                return (TAsset)loadedAsset.Asset;
            }

            // Try to load the asset from fallback data
            var type = LookupType(typeof(TAsset));
			if (type != null && type.FallbackData != null)
            {
				App.LogError("Could not find asset {0}. Loading {1} in it's place.", path, type.FallbackPath);
                if (type.IsCompound)
                {
					// Compound
					var fallbackDatas = (List<object>)type.FallbackData;
					var fallbackSources = LookupAsset(type.FallbackPath).Sources;
                    LoadCompoundAsset(type, path, fallbackSources, fallbackDatas, true);
                    return (TAsset)(LookupAsset(path).Asset);
                }
                else
                {
                    // Basic
                    var fallbackSource = LookupAsset(type.FallbackPath).Sources[0];
                    LoadBasicAsset(type, path, fallbackSource, type.FallbackData, true);
                    return (TAsset)(LookupAsset(path).Asset);
                }
            }

            // Error
            App.LogError("Could not find asset {0}. No fallback available.", path);
            throw new AssetLoadException(path, "No such asset");
        }

        public static IEnumerable<TAsset> List<TAsset>(string path) where TAsset : class, IAsset
        {
            // Find the assets
            foreach (var asset in s_loadedAssets.Values)
            {
                if (asset.Asset is TAsset &&
                    AssetPath.GetDirectoryName(asset.Path) == path &&
                    !asset.IsFallback)
                {
                    yield return (TAsset)asset.Asset;
                }
            }
        }

        public static IEnumerable<TAsset> Find<TAsset>(string path = "") where TAsset : class, IAsset
        {
            // Find the assets
            var pathWithSlash = (path != "") ? path + "/" : "";
            foreach (var asset in s_loadedAssets.Values)
            {
                if (asset.Asset is TAsset &&
                    !asset.IsFallback &&
                    asset.Path.StartsWith(pathWithSlash, StringComparison.Ordinal))
                {
                    yield return (TAsset)asset.Asset;
                }
            }
        }

        private static RegisteredType LookupType(string extension)
        {
            RegisteredType type;
            if (s_extensionToRegisteredType.TryGetValue(extension, out type))
            {
                return type;
            }
            return null;
        }

        private static RegisteredType LookupType(Type assetType)
        {
            RegisteredType type;
            if (s_typeToRegisteredType.TryGetValue(assetType, out type))
            {
                return type;
            }
            return null;
        }

        private static LoadedAsset LookupAsset(string path)
        {
            LoadedAsset asset;
            if (s_loadedAssets.TryGetValue(path, out asset))
            {
                return asset;
            }
            return null;
        }

        // STREAMING

        public static Stream OpenStreamingAsset(string path)
        {
            var loadedAsset = LookupAsset(path);
            if (loadedAsset != null)
            {
                // Open the asset
                var type = loadedAsset.Type;
                if (type.IsCompound)
                {
                    throw new IOException("Compound assets have multiple sources, so cannot be streamed");
                }
                else
                {
					var loadPath = loadedAsset.IsFallback ? loadedAsset.Type.FallbackPath : loadedAsset.Path;
                    var source = loadedAsset.Sources[0];
                    return source.FileStore.OpenFile(loadPath);
                }
            }
            else
            {
                // Try to open the fallback asset for the type
                var extension = AssetPath.GetExtension(path);
                var type = LookupType(extension);
				if (type != null && type.FallbackData != null)
                {
                    if (type.IsCompound)
                    {
                        throw new IOException("Compound assets have multiple sources, so cannot be streamed");
                    }
                    else
                    {
						var loadPath = type.FallbackPath;
                        var source = LookupAsset(loadPath).Sources[0];
                        return source.FileStore.OpenFile(loadPath);
                    }
                }
            }

            App.LogError("Could not open asset {0}. No fallback available.", path);
            throw new AssetLoadException(path, "No such asset");
        }

        // EVENTS

        private static HashSet<string> s_assetsLoadedList = new HashSet<string>();
        private static HashSet<string> s_assetsReloadedList = new HashSet<string>();
        private static HashSet<string> s_assetsUnloadedList = new HashSet<string>();

        private static void FireAssetLoadEvents()
        {
            if (s_assetsLoadedList.Count > 0)
            {
                if (OnAssetsLoaded != null)
                {
                    OnAssetsLoaded.Invoke(new AssetLoadEventArgs(s_assetsLoadedList));
                }
                s_assetsLoadedList.Clear();
            }
            if (s_assetsReloadedList.Count > 0)
            {
                if (OnAssetsReloaded != null)
                {
                    OnAssetsReloaded.Invoke(new AssetLoadEventArgs(s_assetsReloadedList));
                }
                s_assetsReloadedList.Clear();
            }
            if (s_assetsUnloadedList.Count > 0)
            {
                if (OnAssetsUnloaded != null)
                {
                    OnAssetsUnloaded.Invoke(new AssetLoadEventArgs(s_assetsUnloadedList));
                }
                s_assetsUnloadedList.Clear();
            }
        }
    }
}
