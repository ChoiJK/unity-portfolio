using UnityEditor;
using UnityEngine;

public class AgoraStatsWindow : EditorWindow
{
    private AgoraMonitor _monitor;
    private Vector2 _scrollPosition;

    private void Awake()
    {
        titleContent = new GUIContent("Agora Stats");
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (_monitor?.TryUpdate() ?? false)
        {
            Repaint();
        }
    }

    private void OnGUI()
    {
        if (_monitor == null)
        {
            _monitor = new AgoraMonitor();
        }

        using (var scope = new EditorGUILayout.ScrollViewScope(_scrollPosition, false, false))
        using (new EditorGUILayout.VerticalScope(new GUIStyle { padding = { left = 10, top = 10, right = 10, bottom = 10 } }))
        {
            GUI.changed = false;

            _monitor.DrawMaster();
            _monitor.DrawChannels();

            _scrollPosition = scope.scrollPosition;
        }
    }

    [MenuItem("Tools/Diagnostics/Agora Stats")]
    public static void Open()
    {
        GetWindow<AgoraStatsWindow>().Show();
    }
}
