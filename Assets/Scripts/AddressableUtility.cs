using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = UnityEngine.Object;
using Unity.VisualScripting;
using System;
using UnityEditor.VersionControl;

public static class AddressableUtility
{
    private static Dictionary<string, UnityEngine.Object> resources = new Dictionary<string, Object>();

    private static Dictionary<string, AsyncOperationHandle> handles = new Dictionary<string, AsyncOperationHandle>();

    public static int HandleCount { get; private set; }

    public static void LoadAsync<T>(string key, System.Action<T> callback = null) where T : Object
    {
        InternalLoadAssetAsync(key, () => Addressables.LoadAssetAsync<T>(key), callback).Forget();
    }

    public static void LoadAsync<T>(AssetReference key, System.Action<T> callback = null) where T : Object
    {
        InternalLoadAssetAsync(key.Asset.name, () => key.LoadAssetAsync<T>(), null).Forget();
    }

    public static async UniTask<T> LoadAssetAsync<T>(string key) where T : Object
    {
        return await InternalLoadAssetAsync(key, () => Addressables.LoadAssetAsync<T>(key), null);
    }

    public static async UniTask<T> LoadAssetAsync<T>(AssetReference key) where T : Object
    {
        return await InternalLoadAssetAsync(key.Asset.name, () => key.LoadAssetAsync<T>(), null);
    }

    public static void Instantiate(string key, Transform parent = null, Action<GameObject> callback = null)
    {
        LoadAsync<GameObject>(key, (prefab) => 
        {
            GameObject instance = GameObject.Instantiate(prefab, parent);
            instance.AddComponent<SelfReleaseInstance>();
            instance.name = prefab.name;
            
            if(parent != null)
                instance.transform.localPosition = parent.transform.position;

            callback?.Invoke(instance);
        });
    }

    public static void LoadAllAsync<T>(string key, System.Action<List<T>> callback = null) where T : Object
    {
        LoadAssetsAsync(key, callback).Forget();
    }

    public static async UniTask<List<T>> LoadAssetsAsync<T>(object key, System.Action<List<T>> callback = null) where T : Object
    {
        var locations = await LoadResourceLocationsAsync(key);

        if (locations == null)
            return null;

        var results = new List<T>();

        await LoadAndUpdateCollection(locations, results);

        return results;
    }


    static async UniTask LoadAndUpdateCollection<T>(IList<IResourceLocation> locations, List<T> loadedAssetList) where T : Object
    {
        foreach (var location in locations)
        {
            string key = location.PrimaryKey;
            var asset = await InternalLoadAssetAsync<T>(key, () => Addressables.LoadAssetAsync<T>(key));
            
            if (asset != null && !loadedAssetList.Contains(asset))
                loadedAssetList.Add(asset);
        }
    }
    
    private static async UniTask<T> InternalLoadAssetAsync<T>(string key, System.Func<AsyncOperationHandle<T>> getHandle, System.Action<T> callback = null) where T : Object
    {
        Debug.Log($"Load Async Asset : {key}");

        // 이미 로드되어 있는 경우
        if (resources.TryGetValue(key, out Object resource))
        {
            callback?.Invoke(resource as T);
            return resource as T;
        }

        // 로딩이 진행중인 경우 콜백만 추가
        if (handles.ContainsKey(key))
        {
            handles[key].Completed += (op) => { callback?.Invoke(op.Result as T); };
            return null;
        }

        var handle = getHandle();
        handles.Add(key, handle);
        HandleCount++;

        handle.Completed += (op) =>
        {
            resources.Add(key, handle.Result);
            callback?.Invoke(handle.Result);
            HandleCount--;
        };

        await handle.ToUniTask();

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log($"Succeeded To Load Asset Async : {key}");
            return handle.Result;
        }
        else
        {
            Debug.LogError($"Failed To Load Asset Async : {key} / {handle.OperationException}");
            return null;
        }
    }

    public static void Release(string key)
    {
        if (resources.TryGetValue(key, out Object resource) == false)
            return;

        resources.Remove(key);

        if (handles.TryGetValue(key, out AsyncOperationHandle handle))
            Addressables.Release(handle);

        handles.Remove(key);
    }

    public static void Clear()
    {
        resources.Clear();

        foreach (var handle in handles.Values)
            Addressables.Release(handle);

        handles.Clear();
    }

    public static void Destroy(GameObject go, float seconds = 0.0f)
    {
        Object.Destroy(go, seconds);
    }

    // 라벨에 해당하는 모든 게임 오브젝트를 인스턴스화하는 함수
    public static async UniTask<List<GameObject>> InstantiateAllWithLabelAsync(string label)
    {
        var locations = await LoadResourceLocationsAsync(label);

        if (locations == null)
        {
            Debug.LogError($"Failed to load resource locations for label: {label}");
            return null;
        }

        var instances = new List<GameObject>();

        foreach (var location in locations)
        {
            var instance = await InstantiateAsync(location);
            if (instance != null)
            {
                Debug.Log($"#### Instantiated : {location.PrimaryKey} ");
                instances.Add(instance);
            }
            else
            {
                Debug.LogError($"Failed to instantiate object at {location}");
            }
        }

        return instances;
    }


    // 리소스 경로를 불러 온다
    private static async UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync(object key)
    {
        var locationsHandle = Addressables.LoadResourceLocationsAsync(key);
        await locationsHandle.ToUniTask();

        if (locationsHandle.Status == AsyncOperationStatus.Succeeded && locationsHandle.Result.Count > 0)
        {
            return locationsHandle.Result;
        }
        else
        {
            return null;
        }
    }

    private static async UniTask<GameObject> InstantiateAsync(IResourceLocation location)
    {
        var handle = Addressables.InstantiateAsync(location);
        await handle.ToUniTask();

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            handle.Result.AddComponent<SelfReleaseInstance>();
            return handle.Result;
        }
        else
        {
            return null;
        }
    }

    public static void ReleaseAssets<T>(IList<T> objects) where T : Object
    {
        try
        {
            if (objects == null)
                return;

            foreach (var obj in objects)
            {
                if (obj == null)
                    continue;

                if (obj is GameObject)
                {
                    Addressables.ReleaseInstance(obj as GameObject);
                }
                else
                {
                    Addressables.Release(obj);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.Log($"{e}");
        }
    }
}
