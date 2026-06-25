using UnityEngine;

/*
    SafeZoneTrigger — completes the patent's "reach the yellow safe area to escape"
    mechanic. Attach to the SafeZone GameObject (which already holds the BoxCollider
    that GameHandler enables once every key is collected). When the player enters the
    unlocked safe zone, this ends the run as a WIN via GameHandler.PlayerEscaped().

    Note: GameHandler keeps the safe-zone collider disabled until all keys are found,
    so this trigger only fires after the zone unlocks. The keysFound check below is a
    second guard in case the collider is left enabled.
*/
[RequireComponent(typeof(Collider))]
public class SafeZoneTrigger : MonoBehaviour
{
    public GameHandler gameHandler;

    void Start()
    {
        if (gameHandler == null)
            gameHandler = FindAnyObjectByType<GameHandler>();

        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (gameHandler == null) return;

        if (gameHandler.IsCoop)
        {
            // Only the owning client requests; the server validates and ends the run for everyone.
            Player player = other.GetComponent<Player>();
            if (player == null || !player.IsLocalControlled) return;
            if (!gameHandler.CoopAllKeysFound) return;
            gameHandler.CoopRequestEscape();
            return;
        }

        if (gameHandler.keysFound < gameHandler.totalKeys) return; // not unlocked yet

        float timeSurvived = Time.time - gameHandler.startTime;
        gameHandler.PlayerEscaped(timeSurvived);
    }
}
