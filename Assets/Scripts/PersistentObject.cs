using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

public class PersistentObject : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

}
