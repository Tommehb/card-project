// Stick this on the NetworkManager GameObject
using UnityEngine;
using Unity.Netcode;

public class PersistNetworkManager : MonoBehaviour
{
    void Awake()
    {
        var nm = GetComponent<NetworkManager>();
        if (nm == null)
        {
            Debug.LogWarning("PersistNetworkManager requires a NetworkManager component.");
            return;
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton != nm)
        {
            Destroy(gameObject);
            return;
        }

        if (GetComponent<LanSessionManager>() == null)
        {
            gameObject.AddComponent<LanSessionManager>();
        }

        DontDestroyOnLoad(nm.gameObject);
    }
}
