using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Agora.Rtc;
using UnityEditor;
using UnityEngine;

public class AgoraMonitor
{
    public enum SortDirection
    {
        Ascending,
        Descending
    }

    public enum SortKey
    {
        Name,
        StartTime,
        LastTime,
        Type, // HostGroup, AudienceGroup, Speech
        UserCount,
        AgoraUserCount,
        TvCount,
        SumOfAllResolutions
    }


    private static readonly string[] SortKeyNames = Enum.GetNames(typeof(SortKey));
    private static readonly string[] SortDirectionNames = Enum.GetNames(typeof(SortDirection));
    private static string[] AgoraChannelTypeNames = Enum.GetNames(typeof(AgoraChannelType));
    private readonly GUIStyle[] _cellStyles;
    private readonly GUIStyle _columnHeaderButtonStyle;
    private readonly GUIStyle _lowerLeftLabelStyle;

    private readonly GUIStyle _miniButtonStyle;
    private readonly GUIStyle _upperCenterLabelStyle;

    private readonly GUIStyle _upperLeftLabelStyle;
    private string _channelsStatsFilter = "";

    private bool _channelsStatsFoldout = true;
    private SortDirection _channelsStatsSortDirection;
    private SortKey _channelsStatsSortKey;
    private long _lastUpdatedAt;

    private bool _masterStatsFoldout = true;

    public AgoraMonitor()
    {
        _miniButtonStyle = new GUIStyle(EditorStyles.miniButtonLeft);
        _miniButtonStyle.alignment = TextAnchor.MiddleLeft;

        _columnHeaderButtonStyle = new GUIStyle(EditorStyles.miniButtonLeft);
        _columnHeaderButtonStyle.alignment = TextAnchor.MiddleLeft;
        _columnHeaderButtonStyle.margin = new RectOffset(0, 0, 0, 0);

        var tex1 = MakeTex(1, 1, new Color(0.5f, 0.5f, 0.5f, 0.25f));
        var tex2 = MakeTex(1, 1, new Color(0.5f, 0.5f, 0.5f, 0.5f));

        _cellStyles = new GUIStyle[2];

        _cellStyles[0] = new GUIStyle(_columnHeaderButtonStyle);
        _cellStyles[0].alignment = TextAnchor.MiddleLeft;

        _cellStyles[1] = new GUIStyle(_columnHeaderButtonStyle);
        _cellStyles[1].alignment = TextAnchor.MiddleLeft;

        _upperLeftLabelStyle = new GUIStyle(GUI.skin.label);
        _upperLeftLabelStyle.alignment = TextAnchor.UpperLeft;
        _lowerLeftLabelStyle = new GUIStyle(GUI.skin.label);
        _lowerLeftLabelStyle.alignment = TextAnchor.LowerLeft;
        _upperCenterLabelStyle = new GUIStyle(EditorStyles.miniLabel);
        _upperCenterLabelStyle.alignment = TextAnchor.UpperCenter;
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        var pix = new Color[width * height];

        for (var i = 0; i < pix.Length; i++)
        {
            pix[i] = col;
        }

        var result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();

        return result;
    }

    public bool TryUpdate()
    {
        if (AgoraStatsTime.Milliseconds - _lastUpdatedAt >= 1000)
        {
            _lastUpdatedAt = AgoraStatsTime.Milliseconds;
            return true;
        }

        return false;
    }

    public void DrawMaster()
    {
        RtcStats master = default;
        if (AgoraStats.GetLastMasterStats(out master) == false)
        {
            return;
        }

        if (_masterStatsFoldout = EditorGUILayout.Foldout(_masterStatsFoldout, "Master Stats"))
        {
            using (new EditorGUILayout.VerticalScope(new GUIStyle { padding = { left = 20, top = 10, right = 10, bottom = 10 } }))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Playing Time", TimeString(_lastUpdatedAt));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Total Sent Bytes", master.txBytes.ToString("#,0") + " bytes");
                    EditorGUILayout.LabelField("Total Audio Sent Bytes",
                        master.txAudioBytes.ToString("#,0") + " bytes");
                    EditorGUILayout.LabelField("Total Video Sent Bytes",
                        master.txVideoBytes.ToString("#,0") + " bytes");
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Total Recv Bytes", master.rxBytes.ToString("#,0") + " bytes");
                    EditorGUILayout.LabelField("Total Audio Recv Bytes",
                        master.rxAudioBytes.ToString("#,0") + " bytes");
                    EditorGUILayout.LabelField("Total Video Recv Bytes",
                        master.rxVideoBytes.ToString("#,0") + " bytes");
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Total Sent KBitrate",
                        master.txKBitRate.ToString("#,0") + " Kbit/s");
                    EditorGUILayout.LabelField("Total Audio Sent KBitrate",
                        master.txAudioKBitRate.ToString("#,0") + " Kbit/s");
                    EditorGUILayout.LabelField("Total Video Sent KBitrate",
                        master.txVideoKBitRate.ToString("#,0") + " Kbit/s");
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Total Recv KBitrate",
                        master.rxKBitRate.ToString("#,0") + " Kbit/s");
                    EditorGUILayout.LabelField("Total Audio Recv KBitrate",
                        master.rxAudioKBitRate.ToString("#,0") + " Kbit/s");
                    EditorGUILayout.LabelField("Total Video Recv KBitrate",
                        master.rxVideoKBitRate.ToString("#,0") + " Kbit/s");
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("CpuAppUsage", master.cpuAppUsage.ToString());
                    EditorGUILayout.LabelField("CpuTotalUsage", master.cpuTotalUsage.ToString());
                    EditorGUILayout.LabelField("GatewayRtt", master.gatewayRtt.ToString());
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("MemoryAppUsageRatio", master.memoryAppUsageRatio.ToString());
                    EditorGUILayout.LabelField("MemoryTotalUsageRatio", master.memoryTotalUsageRatio.ToString());
                    EditorGUILayout.LabelField("MemoryAppUsageInKbytes", master.memoryAppUsageInKbytes.ToString());
                }

                GUILayout.Space(10);

                DrawChart(100);

                GUILayout.Space(10);
                EditorGUILayout.TextArea("", GUI.skin.horizontalSlider);
                GUILayout.Space(20);
            }
        }
    }


    public void DrawChannels()
    {
        var title = "Call Channel";

        if (_channelsStatsFoldout = EditorGUILayout.Foldout(_channelsStatsFoldout, title))
        {
            using (new EditorGUILayout.VerticalScope(new GUIStyle { padding = { left = 20, top = 10, right = 10, bottom = 10 } }))
            {
                var stats = AgoraStats.StatsMaps;

                IEnumerable<KeyValuePair<string, List<KeyValuePair<long, AgoraCallStats>>>> sortedStats;
                switch (_channelsStatsSortKey)
                {
                    default:
                    case SortKey.Name:
                        sortedStats = _channelsStatsSortDirection == SortDirection.Ascending
                            ? stats.OrderBy(x => x.Key)
                            : stats.OrderByDescending(x => x.Key);
                        break;
                    case SortKey.StartTime:
                        sortedStats = _channelsStatsSortDirection == SortDirection.Ascending
                            ? stats.OrderBy(x => x.Value[0].Key)
                            : stats.OrderByDescending(x => x.Value[0].Key);
                        break;
                    case SortKey.LastTime:
                        sortedStats = _channelsStatsSortDirection == SortDirection.Ascending
                            ? stats.OrderBy(x => x.Value[x.Value.Count - 1].Key)
                            : stats.OrderByDescending(x => x.Value[x.Value.Count - 1].Key);
                        break;
                    case SortKey.Type:
                        sortedStats = _channelsStatsSortDirection == SortDirection.Ascending
                            ? stats.OrderBy(x => x.Value[x.Value.Count - 1].Value.ChennelType)
                            : stats.OrderByDescending(x => x.Value[x.Value.Count - 1].Value.ChennelType);
                        break;
                    case SortKey.UserCount:
                        sortedStats = _channelsStatsSortDirection == SortDirection.Ascending
                            ? stats.OrderBy(x => x.Value[x.Value.Count - 1].Value.UserCount)
                            : stats.OrderByDescending(x => x.Value[x.Value.Count - 1].Value.UserCount);
                        break;
                    case SortKey.TvCount:
                        sortedStats = _channelsStatsSortDirection == SortDirection.Ascending
                            ? stats.OrderBy(x => x.Value[x.Value.Count - 1].Value.TvCount)
                            : stats.OrderByDescending(x => x.Value[x.Value.Count - 1].Value.TvCount);
                        break;
                    case SortKey.SumOfAllResolutions:
                        sortedStats = _channelsStatsSortDirection == SortDirection.Ascending
                            ? stats.OrderBy(x => x.Value[x.Value.Count - 1].Value.SumOfAllResolutions)
                            : stats.OrderByDescending(x => x.Value[x.Value.Count - 1].Value.SumOfAllResolutions);
                        break;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    GUILayout.Label("Sort:");
                    _channelsStatsSortKey = (SortKey)EditorGUILayout.Popup((int)_channelsStatsSortKey, SortKeyNames,
                        GUILayout.Width(120));
                    _channelsStatsSortDirection =
                        (SortDirection)EditorGUILayout.Popup((int)_channelsStatsSortDirection, SortDirectionNames,
                            GUILayout.Width(90));

                    GUILayout.Label("Filter:");
                    _channelsStatsFilter = EditorGUILayout.TextField(_channelsStatsFilter);
                }

                DrawMessagesHeader();

                var rowCount = 0;
                foreach (var stat in sortedStats)
                {
                    if (_channelsStatsFilter.Length > 0 && !stat.Key.Contains(_channelsStatsFilter))
                    {
                        continue;
                    }

                    rowCount++;

                    var cellStyle = _cellStyles[rowCount % 2];

                    var lastStat = stat.Value[stat.Value.Count - 1];

                    var oldColor = GUI.color;

                    if (AgoraStatsTime.Milliseconds - lastStat.Key < 3000)
                    {
                        GUI.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                    }
                    else
                    {
                        GUI.color = new Color(0.4f, 0.4f, 0.4f, 1f);
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var resolutionStr = "";
                        if (lastStat.Value.SumOfAllResolutions == 0)
                        {
                            resolutionStr = "None";
                        }
                        else if (lastStat.Value.SumOfAllResolutions < 921600)
                        {
                            resolutionStr = "HD";
                        }
                        else if (lastStat.Value.SumOfAllResolutions < 2073600)
                        {
                            resolutionStr = "FHD";
                        }
                        else if (lastStat.Value.SumOfAllResolutions < 3686400)
                        {
                            resolutionStr = "2K";
                        }
                        else if (lastStat.Value.SumOfAllResolutions < 8847360)
                        {
                            resolutionStr = "4K";
                        }

                        GUILayout.Label(stat.Key, cellStyle);
                        GUILayout.Label(TimeString(stat.Value[0].Key), cellStyle, GUILayout.Width(80));
                        GUILayout.Label(TimeString(lastStat.Key), cellStyle, GUILayout.Width(80));
                        GUILayout.Label($"{lastStat.Value.ChennelType}", cellStyle, GUILayout.Width(110));
                        GUILayout.Label($"{lastStat.Value.UserCount}", cellStyle, GUILayout.Width(90));
                        GUILayout.Label($"{lastStat.Value.AgoraUserCount}", cellStyle, GUILayout.Width(130));
                        GUILayout.Label($"{lastStat.Value.TvCount}", cellStyle, GUILayout.Width(70));
                        GUILayout.Label($"{lastStat.Value.SumOfAllResolutions} [{resolutionStr}]", cellStyle,
                            GUILayout.Width(150));
                    }
                }

                GUILayout.Space(10);
                EditorGUILayout.TextArea("", GUI.skin.horizontalSlider);
                GUILayout.Space(20);
            }
        }
    }

    private void DrawMessagesHeader()
    {
        var oldColor = GUI.color;
        GUI.color = new Color(1f, 0.7f, 0.7f);

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawColumnHeaderButton("Name", SortKey.Name, -1);
            DrawColumnHeaderButton("StartTime", SortKey.StartTime, 80);
            DrawColumnHeaderButton("LastTime", SortKey.LastTime, 80);
            DrawColumnHeaderButton("Type", SortKey.Type, 110);
            DrawColumnHeaderButton("UserCount", SortKey.UserCount, 90);
            DrawColumnHeaderButton("AgoraUserCount", SortKey.AgoraUserCount, 130);
            DrawColumnHeaderButton("TvCount", SortKey.TvCount, 70);
            DrawColumnHeaderButton("SumOfAllResolutions", SortKey.SumOfAllResolutions, 150);
        }

        GUI.color = oldColor;
    }

    private void DrawColumnHeaderButton(string name, SortKey desiredSortKey, int width)
    {
        var buttonCaption = _channelsStatsSortKey == desiredSortKey
            ? _channelsStatsSortDirection == SortDirection.Ascending ? $"{name}▼" : $"{name}▲"
            : name;

        bool result;
        if (width <= 0)
        {
            result = GUILayout.Button(buttonCaption, _columnHeaderButtonStyle);
        }
        else
        {
            result = GUILayout.Button(buttonCaption, _columnHeaderButtonStyle, GUILayout.Width(width));
        }

        if (result)
        {
            if (_channelsStatsSortKey == desiredSortKey)
            {
                // Toggle direction
                _channelsStatsSortDirection = _channelsStatsSortDirection == SortDirection.Ascending
                    ? SortDirection.Descending
                    : SortDirection.Ascending;
                GUI.changed = true;
            }
            else
            {
                // Change sort key
                _channelsStatsSortKey = desiredSortKey;
                GUI.changed = true;
            }
        }
    }

    private string TimeString(long milliseconds)
    {
        var isminus = milliseconds < 0 ? true : false;
        var absMillisec = Math.Abs(milliseconds);

        var minutes = absMillisec / 1000 / 60;
        var seconds = absMillisec / 1000 % 60;
        var frac = absMillisec % 1000;

        var sb = new StringBuilder();

        if (isminus)
        {
            sb.Append('-');
        }

        if (minutes != 0)
        {
            sb.Append($"{minutes:D2}:");
        }

        sb.Append($"{seconds:D2}.{frac:D3}");

        return sb.ToString();
    }

    private void DrawChart(int height)
    {
        // Draw Chart
        var prevHandlesColor = Handles.color;
        {
            var rect = EditorGUI.IndentedRect(GUILayoutUtility.GetRect(GUILayoutUtility.GetLastRect().width, height));
            Handles.DrawSolidRectangleWithOutline(rect, Color.black, Color.gray);

            var drawableRect = new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4);

            // Grid (9 lines)
            Handles.color = new Color(1f, 1f, 1f, 0.15f); // new Color(0.05f, 0.25f, 0.05f);
            for (var i = 1; i < 10; i++)
            {
                var yTop = drawableRect.y + drawableRect.height / 10 * i;
                Handles.DrawLine(new Vector3(drawableRect.x, yTop), new Vector3(drawableRect.xMax, yTop));
            }

            var maxCount = 60;

            // masterFilter
            var currentTime = AgoraStatsTime.Milliseconds;
            var startTime = currentTime - 60 * 1000;
            //startTime = startTime > 0 ? startTime : 0;
            var duration = currentTime - startTime;

            var masterStats = AgoraStats.MasterStats;
            var statsCount = masterStats.Count;
            var lastIndex = statsCount - 1;
            var chartStartIndex = 0;

            var maxValue = uint.MinValue;
            var minValue = uint.MaxValue;

            for (var i = 0; i < statsCount; ++i)
            {
                var currentStat = masterStats[lastIndex - i];

                if (currentStat.Key < startTime)
                {
                    break;
                }

                maxValue = Math.Max(maxValue, currentStat.Value.txKBitRate);
                maxValue = Math.Max(maxValue, currentStat.Value.rxKBitRate);

                minValue = Math.Min(minValue, currentStat.Value.txKBitRate);
                minValue = Math.Min(minValue, currentStat.Value.rxKBitRate);

                chartStartIndex = lastIndex - i;
            }

            var gridSizeX = drawableRect.width / 12;
            var gridOffsetX = gridSizeX * (currentTime % 5000f / 5000f);
            // Grid (12 line) - width : 1min, line = 5sec
            for (var i = 1; i < 13; ++i)
            {
                var posX = drawableRect.x - gridOffsetX + gridSizeX * i;
                Handles.DrawLine(new Vector3(posX, drawableRect.y), new Vector3(posX, drawableRect.yMax));

                if (i != 1)
                {
                    var lineTime = currentTime + (5000 - currentTime % 5000) - 5000 * (13 - i);
                    var temBound = new Rect(posX - 50, drawableRect.y, 100, 30);
                    GUI.Label(temBound, TimeString(lineTime), _upperCenterLabelStyle);
                }
            }

            var bound = new Rect(drawableRect.x, drawableRect.y, 100, 30);
            GUI.Label(bound, $"{maxValue}\nKBitrate", _upperLeftLabelStyle);
            bound = new Rect(drawableRect.x, drawableRect.yMax - 30, 100, 30);
            GUI.Label(bound, $"{minValue}\nKBitrate", _lowerLeftLabelStyle);

            var sendPrevPos = Vector3.zero;
            var recvPrevPos = Vector3.zero;
            var isFirst = true;
            for (var i = chartStartIndex; i < statsCount; ++i)
            {
                if (i == chartStartIndex)
                {
                    continue;
                }

                var prevStat = masterStats[i - 1];
                var currentStat = masterStats[i];
                var posX = 0f;
                var posY = 0f;
                if (isFirst)
                {
                    isFirst = false;

                    posX = (prevStat.Key - startTime) / 60000f; //60sec
                    posX = drawableRect.x + drawableRect.width * posX;

                    posY = (prevStat.Value.txKBitRate - minValue) / (float)(maxValue - minValue);
                    posY = drawableRect.yMax - drawableRect.height * posY;
                    sendPrevPos = new Vector3(posX, posY);

                    posY = (prevStat.Value.rxKBitRate - minValue) / (float)(maxValue - minValue);
                    posY = drawableRect.yMax - drawableRect.height * posY;
                    recvPrevPos = new Vector3(posX, posY);
                }

                posX = (currentStat.Key - startTime) / 60000f; //60sec
                posX = drawableRect.x + drawableRect.width * posX;

                posY = (currentStat.Value.txKBitRate - minValue) / (float)(maxValue - minValue);
                posY = drawableRect.yMax - drawableRect.height * posY;
                var sendCurrentPos = new Vector3(posX, posY);

                posY = (currentStat.Value.rxKBitRate - minValue) / (float)(maxValue - minValue);
                posY = drawableRect.yMax - drawableRect.height * posY;
                var recvCurrentPos = new Vector3(posX, posY);

                // send - red
                Handles.color = Color.red;
                Handles.DrawLine(sendPrevPos, sendCurrentPos);

                // recv - blue
                Handles.color = Color.blue;
                Handles.DrawLine(recvPrevPos, recvCurrentPos);

                sendPrevPos = sendCurrentPos;
                recvPrevPos = recvCurrentPos;
            }
        }
        Handles.color = prevHandlesColor;
    }

    private class DataUnit
    {
        private static readonly long[] _scales =
        {
            1_000, // 1 K
            10_000, // 10 K
            100_000, // 100 K
            500_000, // 500 K
            1_000_000, // 1 M
            2_000_000, // 2 M
            10_000_000, // 10 M
            20_000_000, // 20 M
            100_000_000, // 100 M
            200_000_000, // 200 M
            500_000_000, // 500 M
            1_000_000_000, // 1 G
            10_000_000_000, // 10 G
            20_000_000_000, // 20 G
            50_000_000_000, // 10 G
            100_000_000_000 // 100 G
        };

        public static long GetScaleMaxValue(long value)
        {
            foreach (var scale in _scales)
            {
                if (value < scale)
                {
                    return scale;
                }
            }

            return value;
        }

        public static string Humanize(long value)
        {
            if (value <= 1_000)
            {
                return value.ToString("#,0");
            }

            if (value <= 1_000_000)
            {
                return (value / 1_000).ToString("#,0") + " K"; // K
            }

            if (value <= 1_000_000_000)
            {
                return (value / 1_000_000).ToString("#,0") + " M"; // M
            }

            if (value <= 1_000_000_000_000)
            {
                return (value / 1_000_000_000).ToString("#,0") + " G"; // G
            }

            return (value / 1_000_000_000_000).ToString("#,0") + " T"; // T
        }
    }
}
