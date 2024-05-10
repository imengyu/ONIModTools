using ONIModTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using static OxygenNotIncluded.Mods.ONIModTools.RuntimeConsole;
using static STRINGS.BUILDINGS.PREFABS.DOOR.CONTROL_STATE;

namespace OxygenNotIncluded.Mods.ONIModTools
{
  public class RuntimeConsole : MonoBehaviour
  {
    private bool windowShow = false;
    private Rect windowRect = new Rect(620, 120, 650, 500);
    private bool isResizing = false;
    private Rect windowResizeStart = new Rect();
    private Vector2 windowMinSize = new Vector2(620, 350);

    private Vector2 scrollPositionText = new Vector2();

    private string filter = "";
    private bool showWindowWhenStart = true;
    private bool showWindowWhenError = true;
    private bool filterError = true;
    private bool filterWarning = true;
    private bool filterInfo = true;
    private bool autoScroll = true;

    private ConsoleCopy consoleCopy;

    private void Start()
    {
      DontDestroyOnLoad(gameObject);

      windowShow = PlayerPrefs.GetInt("RuntimeConsoleShow", showWindowWhenStart ? 1 : 0) == 1;
      windowRect.width = PlayerPrefs.GetFloat("RuntimeConsoleWidth", windowRect.width);
      windowRect.height = PlayerPrefs.GetFloat("RuntimeConsoleHeight", windowRect.height);

      typeof(Debug).GetField("s_loggingDisabled", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, false);

      consoleCopy = new ConsoleCopy((message) =>
      {
        LogType type = LogType.Log;
        if (message.Contains("[WARNING]"))
          type = LogType.Warning;
        HandleLog(message, "", type);
      });
      SceneInitializerLoader.ReportDeferredError = delegate (SceneInitializerLoader.DeferredError deferred_error)
      {
        HandleLog(deferred_error.msg, deferred_error.stack_trace, LogType.Error);
      };
      Application.logMessageReceived += HandleLog;
      Application.logMessageReceivedThreaded += HandleLog;
      Debug.Log("Console show");
    }
    private void OnDestroy()
    {
      if (consoleCopy != null)
      {
        consoleCopy.Dispose();
        consoleCopy = null;
      }
      PlayerPrefs.SetInt("RuntimeConsoleShow", showWindowWhenStart ? 1 : 0);
      PlayerPrefs.SetFloat("RuntimeConsoleWidth", windowRect.width);
      PlayerPrefs.SetFloat("RuntimeConsoleHeight", windowRect.height);
    }
    public List<LogItem> logItems { get; } = new List<LogItem>();
    public class LogItem
    {
      public bool expand = false;
      public string time;
      public string condition;
      public string stackTrace;
      public LogType type;
    }

    private void HandleLog(string condition, string stackTrace, LogType type)
    {
      logItems.Add(new LogItem()
      {
        time = System.DateTime.Now.ToString("T"),
        expand = false,
        condition = condition,
        stackTrace = stackTrace,
        type = type,
      });

      if (logItems.Count > 512)
        logItems.RemoveAt(0);

      if (type == LogType.Exception || type == LogType.Error)
      {
        if (showWindowWhenError)
          Show();
      }

      if (autoScroll)
      {
        scrollPositionText.y = logItems.Count * 30;
      }
    }

    public class ConsoleCopy : IDisposable
    {
      private readonly TextWriter m_DoubleWriter;
      private readonly TextWriter m_OldOut;

      public delegate void HandleLog(string message);

      private class DoubleWriter : TextWriter
      {
        private TextWriter source;
        private StringBuilder buffer = new StringBuilder();
        private HandleLog outCallback;
        public DoubleWriter(TextWriter source, HandleLog outCallback)
        {
          this.source = source;
          this.outCallback = outCallback;
        }

        public override Encoding Encoding => source.Encoding;

        public override void Flush()
        {
          if (buffer.Length > 1)
          {
            outCallback.Invoke(buffer.ToString());
            buffer.Clear();
          }
        }
        public override void Write(char value)
        {
          buffer.Append(value);
          if (value == '\n')
          {
            outCallback.Invoke(buffer.ToString());
            buffer.Clear();
          }
        }
      }

      public ConsoleCopy(HandleLog outCallback)
      {
        m_OldOut = Console.Out;
        m_DoubleWriter = new DoubleWriter(m_OldOut, outCallback);
        Console.SetOut(m_DoubleWriter);
      }
      public void Dispose()
      {
        Console.SetOut(m_OldOut);
      }
    }

    public void Show()
    {
      windowShow = true;
    }

    private void OnGUI()
    {
      if (windowShow)
      {
        var horizRatio = Screen.width / 1280;
        var vertRatio = Screen.height / 768;
        if (horizRatio < 1) horizRatio = 1;
        if (vertRatio < 1) vertRatio = 1;

        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(horizRatio, vertRatio, 1));
        windowRect = GUI.Window(1, windowRect, DoMyWindow, "Runtime Console");
      }
    }

    private void DoMyWindow(int windowID)
    {
      GUI.DragWindow(new Rect(0, 0, 2000, 20));

      GUILayout.BeginVertical();
      GUILayout.BeginHorizontal();

      if (GUILayout.Button("X - Close Window"))
        windowShow = false;
      if (GUILayout.Button("Clear"))
        logItems.Clear();
      GUILayout.Label($"Count: {logItems.Count}");

      filter = GUILayout.TextField(filter, GUILayout.MinWidth(220));
      showWindowWhenStart = GUILayout.Toggle(showWindowWhenStart, "Show when Start");
      showWindowWhenError = GUILayout.Toggle(showWindowWhenError, "Show Window when Error");
      GUILayout.Space(10);

      autoScroll = GUILayout.Toggle(autoScroll, "Auto scroll");

      GUILayout.Space(10);

      filterError = GUILayout.Toggle(filterError, "Error");
      filterWarning = GUILayout.Toggle(filterWarning, "Warning");
      filterInfo = GUILayout.Toggle(filterInfo, "Log");

      GUILayout.EndHorizontal();

      scrollPositionText = GUILayout.BeginScrollView(scrollPositionText, GUILayout.Width(windowRect.width - 30));

      Color oldColor = GUI.contentColor;
      for (var i = 0; i < logItems.Count; i++)
      {
        var item = logItems[i];
        switch (item.type)
        {
          case LogType.Error:
          case LogType.Assert:
          case LogType.Exception:
            GUI.contentColor = Color.red;
            break;
          case LogType.Warning:
            GUI.contentColor = Color.yellow;
            break;
          case LogType.Log:
            GUI.contentColor = Color.white;
            break;
        }
        if (item.type == LogType.Log && !filterInfo)
          continue;
        if (item.type == LogType.Warning && !filterWarning)
          continue;
        if ((item.type == LogType.Error || item.type == LogType.Assert || item.type == LogType.Exception) && !filterError)
          continue;
        if (!string.IsNullOrEmpty(filter) && !item.condition.Contains(filter))
          continue;

        GUILayout.BeginHorizontal();

        if (!string.IsNullOrEmpty(item.stackTrace) && GUILayout.Button(item.expand ? "▼" : "▶", GUILayout.Width(22)))
          item.expand = !item.expand;
        if (GUILayout.Button("Copy", GUILayout.Width(44), GUILayout.Height(24)))
          GUIUtility.systemCopyBuffer = item.condition + " " + item.stackTrace;

        GUILayout.Label(item.condition, GUILayout.Height(24));
        GUILayout.EndHorizontal();

        if (item.expand)
          GUILayout.Box(item.stackTrace);
      }

      GUI.contentColor = oldColor;

      GUILayout.EndScrollView();
      GUILayout.EndVertical();

      windowRect = Utils.ResizeWindow(windowRect, ref isResizing, ref windowResizeStart, windowMinSize);
    }
  }
}
