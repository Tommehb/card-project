using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/*
    PlayerBehaviorLogger — the patent's "post-run behavior data" / design-tool layer.

    Records, for a single run:
      - the player's PATH (position samples over time, tagged with the current map zone),
      - TIME SPENT per map zone (fed by MapZone trigger volumes),
      - the DEATH LOCATION + cause, or the ESCAPE outcome,
      - pacing (total distance travelled, average speed).

    On run end (death or escape) it writes a per-run JSON + CSV to
    Application.persistentDataPath/EES_Runs/ so a developer can review where players
    go, where they linger, and where they die, then tune lighting / sound / key
    placement / enemy speed / patrol routes accordingly.

    Attach this to a single scene object (e.g. an empty "Telemetry" GameObject, or the
    GameHandler object). It self-acquires the player by the "Player" tag.
*/

public class PlayerBehaviorLogger : MonoBehaviour
{
    public static PlayerBehaviorLogger Instance { get; private set; }

    [Header("Sampling")]
    [Tooltip("Seconds between player-position samples for the path trace.")]
    public float sampleInterval = 0.5f;

    [Tooltip("Also echo the export path / summary to the Console on run end.")]
    public bool logToConsole = true;

    [Serializable]
    public class PathSample
    {
        public float t;       // seconds since run start
        public float x;       // world X
        public float z;       // world Z
        public string zone;   // map zone the player was in when sampled
    }

    [Serializable]
    public class ZoneTime
    {
        public string zone;
        public float seconds;
    }

    [Serializable]
    public class RunData
    {
        public string timestamp;
        public int level;
        public int totalKeys;
        public int keysFound;
        public string outcome;       // "Died" | "Escaped" | "Incomplete"
        public string deathReason;   // mannequin type, when Died
        public Vector3 deathPosition;
        public float timeSurvived;
        public float totalDistance;
        public float averageSpeed;
        public List<ZoneTime> zoneTimes = new List<ZoneTime>();
        public List<PathSample> path = new List<PathSample>();
    }

    // ---- run state ----
    private Transform player;
    private GameHandler gameHandler;
    private float runStartTime;
    private float sampleTimer;
    private Vector3 lastSampledPosition;
    private bool hasLastPosition;
    private float totalDistance;
    private bool finished;

    private readonly List<PathSample> path = new List<PathSample>();
    private readonly Dictionary<string, float> zoneSeconds = new Dictionary<string, float>();
    private readonly List<string> activeZones = new List<string>();

    // ---- outcome (set by RecordDeath / RecordEscape) ----
    private string outcome = "Incomplete";
    private string deathReason = "";
    private Vector3 deathPosition;
    private float timeSurvived;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        runStartTime = Time.time;
        gameHandler = FindAnyObjectByType<GameHandler>();
        AcquirePlayer();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void AcquirePlayer()
    {
        if (player != null) return;
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            lastSampledPosition = player.position;
            hasLastPosition = true;
        }
    }

    void Update()
    {
        if (finished) return;
        if (player == null)
        {
            AcquirePlayer();
            if (player == null) return;
        }

        float dt = Time.deltaTime;

        // accumulate dwell time in the current zone
        string current = CurrentZone();
        if (zoneSeconds.ContainsKey(current)) zoneSeconds[current] += dt;
        else zoneSeconds[current] = dt;

        // accumulate distance (pacing)
        if (hasLastPosition)
        {
            Vector3 p = player.position;
            totalDistance += Vector3.Distance(
                new Vector3(p.x, 0f, p.z),
                new Vector3(lastSampledPosition.x, 0f, lastSampledPosition.z));
            lastSampledPosition = p;
        }
        else
        {
            lastSampledPosition = player.position;
            hasLastPosition = true;
        }

        // sample the path on a fixed cadence
        sampleTimer += dt;
        if (sampleTimer >= sampleInterval)
        {
            sampleTimer = 0f;
            path.Add(new PathSample
            {
                t = Time.time - runStartTime,
                x = player.position.x,
                z = player.position.z,
                zone = current
            });
        }
    }

    // ---- zone hooks (called by MapZone) ----
    public void EnterZone(string zone)
    {
        if (string.IsNullOrEmpty(zone)) return;
        activeZones.Add(zone);
    }

    public void ExitZone(string zone)
    {
        if (string.IsNullOrEmpty(zone)) return;
        int idx = activeZones.LastIndexOf(zone);
        if (idx >= 0) activeZones.RemoveAt(idx);
    }

    private string CurrentZone()
    {
        return activeZones.Count > 0 ? activeZones[activeZones.Count - 1] : "Unknown";
    }

    // ---- outcome hooks (called by Player / GameHandler) ----
    public void RecordDeath(Vector3 position, string reason, float survived)
    {
        if (finished) return;
        outcome = "Died";
        deathReason = reason;
        deathPosition = position;
        timeSurvived = survived;
    }

    public void RecordEscape(float survived)
    {
        if (finished) return;
        outcome = "Escaped";
        timeSurvived = survived;
    }

    public void FinalizeAndExport()
    {
        if (finished) return;
        finished = true;

        var data = new RunData
        {
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            level = GameManager.instance != null ? GameManager.instance.level : 0,
            totalKeys = gameHandler != null ? gameHandler.totalKeys : 0,
            keysFound = gameHandler != null ? gameHandler.keysFound : 0,
            outcome = outcome,
            deathReason = deathReason,
            deathPosition = deathPosition,
            timeSurvived = timeSurvived,
            totalDistance = totalDistance,
            averageSpeed = timeSurvived > 0.01f ? totalDistance / timeSurvived : 0f,
            path = path
        };
        foreach (var kv in zoneSeconds)
            data.zoneTimes.Add(new ZoneTime { zone = kv.Key, seconds = kv.Value });

        WriteFiles(data);
    }

    private void WriteFiles(RunData data)
    {
        try
        {
            string dir = Path.Combine(Application.persistentDataPath, "EES_Runs");
            Directory.CreateDirectory(dir);

            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string baseName = $"run_L{data.level}_{data.outcome}_{stamp}";

            string jsonPath = Path.Combine(dir, baseName + ".json");
            File.WriteAllText(jsonPath, JsonUtility.ToJson(data, true));

            string csvPath = Path.Combine(dir, baseName + "_path.csv");
            var sb = new StringBuilder();
            sb.AppendLine("t,x,z,zone");
            foreach (var s in data.path)
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0:0.###},{1:0.###},{2:0.###},{3}", s.t, s.x, s.z, s.zone));
            File.WriteAllText(csvPath, sb.ToString());

            if (logToConsole)
            {
                var zoneSummary = new StringBuilder();
                foreach (var z in data.zoneTimes)
                    zoneSummary.Append($"{z.zone}={z.seconds:0.0}s ");
                Debug.Log($"[EES Telemetry] Run logged ({data.outcome}) -> {jsonPath}\n" +
                          $"time={data.timeSurvived:0.0}s dist={data.totalDistance:0.0} " +
                          $"keys={data.keysFound}/{data.totalKeys} zones: {zoneSummary}");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[EES Telemetry] Failed to write run data: " + e.Message);
        }
    }
}
