#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Agora.Rtc;

public static class AgoraStatsTime
{
    private static readonly TimeSource _timeSource = new();

    static AgoraStatsTime()
    {
        _timeSource.Stop();
    }

    public static long Milliseconds => _timeSource.Milliseconds;

    public static void Reset()
    {
        _timeSource.Reset();
    }

    public static void Start()
    {
        _timeSource.Start();
    }

    public static void Stop()
    {
        _timeSource.Stop();
    }
}

public enum AgoraCallEvent
{
}

public enum AgoraCallEventArg
{
}

public enum AgoraChannelType
{
    HostSpeech,
    AudienceSpeech,
    HostGroup,
    AudienceGroup
}

public struct AgoraCallEventStats
{
    public int playerID;
    public int sessionId;
    public bool isTelevision;
    public AgoraCallEvent callEvent;
    public AgoraCallEventArg callEventArg;
}

public struct AgoraCallStats
{
    public AgoraChannelType ChennelType;
    public int UserCount;
    public int AgoraUserCount;
    public int TvCount;
    public int SumOfAllResolutions;
    public RtcStats RtcStats;
}

public static class AgoraStats
{
    // channelID(string), list
    private static Dictionary<string, VideoCallable> _videoCallables;

    private static long _lastUpdatePerSecondStatsTime;

    private static bool _isDirtyStats;

    private static CallService _callService;

    static AgoraStats()
    {
        MasterStats = new List<KeyValuePair<long, RtcStats>>();
        StatsMaps = new Dictionary<string, List<KeyValuePair<long, AgoraCallStats>>>();
        CallEventMaps = new Dictionary<string, List<KeyValuePair<long, AgoraCallEventStats>>>();

        _lastUpdatePerSecondStatsTime = 0;
    }

    public static List<KeyValuePair<long, RtcStats>> MasterStats { get; }

    public static Dictionary<string, List<KeyValuePair<long, AgoraCallStats>>> StatsMaps { get; }

    public static Dictionary<string, List<KeyValuePair<long, AgoraCallEventStats>>> CallEventMaps { get; }

    public static bool IsInitialized => _callService != default;

    public static void InitOrReset(CallService callService)
    {
        _callService = callService;
        AgoraStatsTime.Reset();

        MasterStats.Clear();

        foreach (var stat in StatsMaps)
        {
            stat.Value.Clear();
        }

        StatsMaps.Clear();


        foreach (var stat in CallEventMaps)
        {
            stat.Value.Clear();
        }

        CallEventMaps.Clear();

        _lastUpdatePerSecondStatsTime = 0;
    }

    public static void Release()
    {
        _callService = default;

        MasterStats.Clear();

        foreach (var stat in StatsMaps)
        {
            stat.Value.Clear();
        }

        StatsMaps.Clear();


        foreach (var stat in CallEventMaps)
        {
            stat.Value.Clear();
        }

        CallEventMaps.Clear();

        _lastUpdatePerSecondStatsTime = 0;
    }

    public static bool GetLastMasterStats(out RtcStats stat)
    {
        if (MasterStats == default)
        {
            stat = default;
            return false;
        }

        if (MasterStats.Count == 0)
        {
            stat = default;
            return false;
        }

        stat = MasterStats[MasterStats.Count - 1].Value;

        return true;
    }

    public static void AddStats(string callChannel, RtcStats stats, int allResolution)
    {
        if (!StatsMaps.ContainsKey(callChannel))
        {
            StatsMaps.Add(callChannel, new List<KeyValuePair<long, AgoraCallStats>>());
        }

        var callchannel = _callService.GetVideoCallChannel(callChannel);
        var callStats = new AgoraCallStats();
        callStats.RtcStats = stats;
        if (callchannel.VideoCallable.GroupType == GroupType.NEARBY)
        {
            //group
            if (callchannel.IsAudience)
            {
                callStats.ChennelType = AgoraChannelType.AudienceGroup;
            }
            else
            {
                callStats.ChennelType = AgoraChannelType.HostGroup;
            }
        }
        else
        {
            // speech
            if (callchannel.IsAudience)
            {
                callStats.ChennelType = AgoraChannelType.AudienceSpeech;
            }
            else
            {
                callStats.ChennelType = AgoraChannelType.AudienceGroup;
            }
        }

        callStats.UserCount = callchannel.UserCount;
        callStats.AgoraUserCount = callchannel.VideoCallable.Players.Count;
        callStats.TvCount = callchannel.VideoCallable.SharableScreens.Count;
        callStats.SumOfAllResolutions = allResolution;

        StatsMaps[callChannel].Add(new KeyValuePair<long, AgoraCallStats>(AgoraStatsTime.Milliseconds, callStats));

        _isDirtyStats = true;
    }

    public static void AddEvent(string callChannel, int playerID, int sessionId, bool isTelevision,
        AgoraCallEvent callEvent, AgoraCallEventArg callEventArg)
    {
        if (!CallEventMaps.ContainsKey(callChannel))
        {
            CallEventMaps.Add(callChannel, new List<KeyValuePair<long, AgoraCallEventStats>>());
        }

        var eventStats = new AgoraCallEventStats();

        CallEventMaps[callChannel]
            .Add(new KeyValuePair<long, AgoraCallEventStats>(AgoraStatsTime.Milliseconds, eventStats));
    }

    public static void Update()
    {
        if (_isDirtyStats == false)
        {
            return;
        }

        var now = AgoraStatsTime.Milliseconds;
        var deltaTime = now - _lastUpdatePerSecondStatsTime;

        if (deltaTime >= 1000)
        {
            var masterStat = new RtcStats();

            foreach (var stat in StatsMaps)
            {
                var currentPair = stat.Value[stat.Value.Count - 1];
                if (AgoraStatsTime.Milliseconds - currentPair.Key > 3000)
                {
                    continue;
                }

                var currentStat = currentPair.Value;

                masterStat.duration = Math.Max(masterStat.duration, currentStat.RtcStats.duration);
                masterStat.txBytes = Math.Max(masterStat.txBytes, currentStat.RtcStats.txBytes);
                masterStat.rxBytes = Math.Max(masterStat.rxBytes, currentStat.RtcStats.rxBytes);
                masterStat.txAudioBytes = Math.Max(masterStat.txAudioBytes, currentStat.RtcStats.txAudioBytes);
                masterStat.txVideoBytes = Math.Max(masterStat.txVideoBytes, currentStat.RtcStats.txVideoBytes);
                masterStat.rxAudioBytes = Math.Max(masterStat.rxAudioBytes, currentStat.RtcStats.rxAudioBytes);
                masterStat.rxVideoBytes = Math.Max(masterStat.rxVideoBytes, currentStat.RtcStats.rxVideoBytes);
                masterStat.txKBitRate = Math.Max(masterStat.txKBitRate, currentStat.RtcStats.txKBitRate);
                masterStat.rxKBitRate = Math.Max(masterStat.rxKBitRate, currentStat.RtcStats.rxKBitRate);
                masterStat.rxAudioKBitRate =
                    Math.Max(masterStat.rxAudioKBitRate, currentStat.RtcStats.rxAudioKBitRate);
                masterStat.txAudioKBitRate =
                    Math.Max(masterStat.txAudioKBitRate, currentStat.RtcStats.txAudioKBitRate);
                masterStat.rxVideoKBitRate =
                    Math.Max(masterStat.rxVideoKBitRate, currentStat.RtcStats.rxVideoKBitRate);
                masterStat.txVideoKBitRate =
                    Math.Max(masterStat.txVideoKBitRate, currentStat.RtcStats.txVideoKBitRate);
                masterStat.lastmileDelay = Math.Max(masterStat.lastmileDelay, currentStat.RtcStats.lastmileDelay);
                masterStat.txPacketLossRate =
                    Math.Max(masterStat.txPacketLossRate, currentStat.RtcStats.txPacketLossRate);
                masterStat.rxPacketLossRate =
                    Math.Max(masterStat.rxPacketLossRate, currentStat.RtcStats.rxPacketLossRate);
                masterStat.userCount += currentStat.RtcStats.userCount;
                masterStat.cpuAppUsage = Math.Max(masterStat.cpuAppUsage, currentStat.RtcStats.cpuAppUsage);
                masterStat.cpuTotalUsage = Math.Max(masterStat.cpuTotalUsage, currentStat.RtcStats.cpuTotalUsage);
                masterStat.gatewayRtt += currentStat.RtcStats.gatewayRtt;
                masterStat.memoryAppUsageRatio = Math.Max(masterStat.memoryAppUsageRatio,
                    currentStat.RtcStats.memoryAppUsageRatio);
                masterStat.memoryTotalUsageRatio = Math.Max(masterStat.memoryTotalUsageRatio,
                    currentStat.RtcStats.memoryTotalUsageRatio);
                masterStat.memoryAppUsageInKbytes = Math.Max(masterStat.memoryAppUsageInKbytes,
                    currentStat.RtcStats.memoryAppUsageInKbytes);
            }

            MasterStats.Add(new KeyValuePair<long, RtcStats>(now, masterStat));

            _lastUpdatePerSecondStatsTime = now;
        }

        _isDirtyStats = true;
    }
}
#endif
