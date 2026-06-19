using UnityEngine;

/*
    MapZone — a named, trigger-collider region of the map (e.g. "Hallway",
    "Classroom", "Outdoor", "Office", "SafeZone"). Drop this on a GameObject with a
    BoxCollider (set as a trigger) and give it a zoneName. When the player enters or
    leaves, it reports to the PlayerBehaviorLogger, which accumulates time-spent-per-zone
    for the post-run behavior data the patent describes.

    Add a handful of these over the existing (currently empty) Interior / Exterior
    groupings to make player dwell-time measurable.
*/
[RequireComponent(typeof(Collider))]
public class MapZone : MonoBehaviour
{
    public string zoneName = "Zone";

    void Reset()
    {
        // Make the collider a trigger by default when first added in the editor.
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (PlayerBehaviorLogger.Instance != null)
            PlayerBehaviorLogger.Instance.EnterZone(zoneName);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (PlayerBehaviorLogger.Instance != null)
            PlayerBehaviorLogger.Instance.ExitZone(zoneName);
    }
}
