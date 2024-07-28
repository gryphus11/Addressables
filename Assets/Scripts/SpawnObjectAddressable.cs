using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;

[System.Serializable]
public class AssetReferenceAudioClip : AssetReferenceT<AudioClip>
{
    public AssetReferenceAudioClip(string guid) : base(guid)
    {
    }
}

public class SpawnObjectAddressable : MonoBehaviour
{
    [SerializeField]
    private AssetReference assetReference;

    /// <summary>
    /// Label�� ���� ����
    /// </summary>
    [SerializeField]
    private AssetLabelReference labelReference;

    /// <summary>
    /// Label�� ���� ����
    /// </summary>
    [SerializeField]
    private AssetLabelReference labelSpriteReference;

    /// <summary>
    /// ���ӿ�����Ʈ��
    /// </summary>
    [SerializeField]
    private AssetReferenceGameObject assetReferenceGameObject;

    /// <summary>
    /// ��������Ʈ ��Ʋ�󽺸�
    /// </summary>
    [SerializeField]
    private AssetReferenceAtlasedSprite assetReferenceAtlasedSprite;

    /// <summary>
    /// Texture 2D��
    /// </summary>
    [SerializeField]
    private AssetReferenceTexture2D assetReferenceTexture2D;

    /// <summary>
    /// Ŀ����. AudioClip�� ��巹���� ����.
    /// </summary>
    [SerializeField]
    private AssetReferenceAudioClip assetReferenceAudioClip;

    List<GameObject> instantiatedList = new List<GameObject>();

    List<AudioClip> clips = new List<AudioClip>();

    List<Texture2D> texture2dList = new List<Texture2D>();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            InstantiateGameObjectAsync(assetReferenceGameObject).Forget();
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            //LoadAudioClips().Forget();
            LoadTextures().Forget();
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            AddressableUtility.Clear();
        }
    }


    private async UniTaskVoid InstantiateGameObjectAsync(AssetReferenceGameObject assetReference)
    {
        // �Ʒ� ���� ����
        //var instance = await assetReference.InstantiateAsync();
        //var instance = await Addressables.InstantiateAsync("Assets/Prefabs/Environments/ForestMap.prefab");
        //var instance = await Addressables.InstantiateAsync("Environment");
        //instantiatedList.Add(instance);

        //�ѹ��� �ε�
        var envs = await AddressableUtility.InstantiateAllWithLabelAsync("Environments");
        var skys = await AddressableUtility.InstantiateAllWithLabelAsync("Skybox");
        var fxs = await AddressableUtility.InstantiateAllWithLabelAsync("FX");

        if(envs != null)
            instantiatedList.AddRange(envs);

        if(skys != null)
            instantiatedList.AddRange(skys);

        if(fxs != null)
            instantiatedList.AddRange(fxs);
    }

    private async UniTaskVoid LoadTextures()
    {
        List<GameObject> aaa = new List<GameObject>();
        await AddressableUtility.LoadAssetsAsync<GameObject>("FX", (list) => { Debug.Log(list); });
    }

    private async UniTaskVoid LoadAudioClips()
    {
        //await AddressableUtility.LoadAssetsAsync(labelReference, clips);
    }

    private void OnDestroy()
    {
        AddressableUtility.Clear();
    }
}
