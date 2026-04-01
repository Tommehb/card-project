using UnityEngine;

public enum LanSpawnPointType
{
    None = 0,
    Lobby = 1,
    Gameplay = 2,
}

public class LanSpawnPoint : MonoBehaviour
{
    [SerializeField] private LanSpawnPointType spawnType = LanSpawnPointType.Lobby;
    [SerializeField] [Min(0)] private int slotIndex;

    public LanSpawnPointType SpawnType => spawnType;
    public int SlotIndex => slotIndex;

    private void OnDrawGizmos()
    {
        Gizmos.color = spawnType == LanSpawnPointType.Gameplay ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.35f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward);
    }
}
