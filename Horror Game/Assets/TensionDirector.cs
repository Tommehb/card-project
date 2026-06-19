using System.Collections.Generic;
using UnityEngine;

/*
    TensionDirector — realizes the patent's "rising tension" claim and finally plays the
    unused dark-horror-soundscape ambience.

    It loops an ambient soundscape and ramps its volume + pitch with a 0..1 "threat"
    value derived from:
      - proximity to the nearest ACTIVE mannequin (closer = more tension), and
      - how far the run has progressed (later = more tension), scaled by how many keys
        remain to be found.

    Attach to an "Ambience" GameObject that has an AudioSource (assign the soundscape
    clip), or assign the AudioSource below.
*/
public class TensionDirector : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource ambient;
    [Tooltip("Optional: assigned to the AudioSource if its clip is empty.")]
    public AudioClip soundscape;
    public float baseVolume = 0.25f;
    public float maxVolume = 1.0f;
    public float minPitch = 0.92f;
    public float maxPitch = 1.12f;

    [Header("Threat (proximity)")]
    [Tooltip("At or beyond this distance to the nearest mannequin, proximity threat = 0.")]
    public float farDistance = 30f;
    [Tooltip("At or within this distance, proximity threat = 1.")]
    public float nearDistance = 4f;

    [Header("Threat (time)")]
    [Tooltip("Seconds of run time at which the slow time-based dread reaches its max.")]
    public float timeToMaxDread = 240f;
    [Range(0f, 10f)] public float responsiveness = 4f; // lerp speed toward target threat

    private Transform player;
    private GameHandler gameHandler;
    private readonly List<GameObject> mannequins = new List<GameObject>();
    private float threat;

    void Start()
    {
        gameHandler = FindAnyObjectByType<GameHandler>();
        AcquirePlayer();
        CollectMannequins();

        if (ambient == null) ambient = GetComponent<AudioSource>();
        if (ambient != null)
        {
            if (ambient.clip == null && soundscape != null) ambient.clip = soundscape;
            ambient.loop = true;
            ambient.volume = baseVolume;
            if (ambient.clip != null && !ambient.isPlaying) ambient.Play();
        }
    }

    void AcquirePlayer()
    {
        if (player != null) return;
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void CollectMannequins()
    {
        mannequins.Clear();
        // include inactive so the list is stable even though GameHandler enables a
        // subset per level after its own Start() runs; we filter by active each frame.
        foreach (var m in FindObjectsByType<RedMannequin>(FindObjectsInactive.Include))
            mannequins.Add(m.gameObject);
        foreach (var m in FindObjectsByType<YellowManneguin>(FindObjectsInactive.Include))
            mannequins.Add(m.gameObject);
        foreach (var m in FindObjectsByType<GreenMannequin>(FindObjectsInactive.Include))
            mannequins.Add(m.gameObject);
    }

    void Update()
    {
        if (player == null) { AcquirePlayer(); }
        if (ambient == null) return;

        float target = ComputeThreat();
        threat = Mathf.MoveTowards(threat, target, responsiveness * Time.deltaTime);

        ambient.volume = Mathf.Lerp(baseVolume, maxVolume, threat);
        ambient.pitch = Mathf.Lerp(minPitch, maxPitch, threat);
    }

    private float ComputeThreat()
    {
        float proximity = 0f;
        if (player != null)
        {
            float nearest = float.MaxValue;
            for (int i = 0; i < mannequins.Count; i++)
            {
                var go = mannequins[i];
                if (go == null || !go.activeInHierarchy) continue;
                float d = Vector3.Distance(player.position, go.transform.position);
                if (d < nearest) nearest = d;
            }
            if (nearest < float.MaxValue)
                proximity = Mathf.InverseLerp(farDistance, nearDistance, nearest); // 0 far -> 1 near
        }

        // slow background dread that grows with elapsed time
        float elapsed = gameHandler != null ? Time.time - gameHandler.startTime : Time.timeSinceLevelLoad;
        float timeDread = Mathf.Clamp01(elapsed / Mathf.Max(1f, timeToMaxDread)) * 0.5f;

        return Mathf.Clamp01(Mathf.Max(proximity, timeDread));
    }
}
