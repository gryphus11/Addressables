using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using UnityEngine.ResourceManagement.ResourceLocations;

public static class AddressableUtility
{

    public static async UniTask<T> LoadAssetAsync<T>(string key) where T : Object
    {
        return await LoadAssetInternalAsync(() => Addressables.LoadAssetAsync<T>(key));
    }

    public static async UniTask<T> LoadAssetAsync<T>(AssetReference key) where T : Object
    {
        return await LoadAssetInternalAsync(() => key.LoadAssetAsync<T>());
    }

    public static async UniTask<List<T>> LoadAssetsAsync<T>(object key) where T : Object
    {
        var locations = await LoadResourceLocationsAsync(key);

        if (locations == null)
            return null;

        var results = new List<T>();

        await LoadAndUpdateCollection(locations, results);

        return results;
    }

    public static async UniTask LoadAssetsAsync<T>(object assetNameOrLabel, List<T> updateCollection) where T : Object
    {
        var locations = await Addressables.LoadResourceLocationsAsync(assetNameOrLabel).Task;
        await LoadAndUpdateCollection(locations, updateCollection);
    }



    static async UniTask LoadAndUpdateCollection<T>(IList<IResourceLocation> locations, List<T> loadedAssetList) where T : Object
    {
        foreach (var location in locations)
        {
            var temp = await Addressables.LoadAssetAsync<T>(location);
            
            if (loadedAssetList.Contains(temp))
                continue; // 이미 포함된 상태라면

            loadedAssetList.Add(temp);

        }
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


    // 내부적으로 공용 처리 되는 함수
    internal static async UniTask<T> LoadAssetInternalAsync<T>(System.Func<AsyncOperationHandle<T>> loadFunc) where T : Object
    {
        var handle = loadFunc();
        await handle.ToUniTask();

        return handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : null;
    }

    // 리소스 경로를 불러 온다
    private static async UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync(object key)
    {
        var locationsHandle = Addressables.LoadResourceLocationsAsync(key);
        await locationsHandle.ToUniTask();

        return locationsHandle.Status == AsyncOperationStatus.Succeeded && locationsHandle.Result.Count > 0
            ? locationsHandle.Result
            : null;
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
