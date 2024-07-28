using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddressablesTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            AddressableUtility.InstantiateAllWithLabelAsync("FX").Forget();
            //AddressableUtility.Instantiate("Assets/Prefabs/FX_Prefabs/Smoke_FX.prefab");
        }
    }
}
