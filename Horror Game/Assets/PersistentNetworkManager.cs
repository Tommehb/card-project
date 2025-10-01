// Stick this on the NetworkManager GameObject
using UnityEngine;
using Unity.Netcode;

public class PersistNetworkManager : MonoBehaviour
{
    void Awake()
    {
        var nm = GetComponent<NetworkManager>();
        if (nm != null) DontDestroyOnLoad(nm.gameObject);
    }
}
