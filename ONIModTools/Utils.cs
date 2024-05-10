using System.IO;
using UnityEngine;

namespace ONIModTools
{
  internal class Utils
  {
    public static Texture2D LoadTexture2dFromFile(string path, int width, int height)
    {
      Texture2D t2d = new Texture2D(width, height);
      t2d.LoadImage(File.ReadAllBytes(path));
      t2d.Apply();
      return t2d;
    }
    private static GUIContent gcDrag = new GUIContent("◢", "drag to resize");
    private static GUIStyle styleWindowResize = null;
    public static Rect ResizeWindow(Rect windowRect, ref bool isResizing, ref Rect resizeStart, Vector2 minWindowSize)
    {
      if (styleWindowResize == null)
      {
        // this is a custom style that looks like a // in the lower corner
        styleWindowResize = GUI.skin.GetStyle("Box");
      }

      Vector2 mouse = GUIUtility.ScreenToGUIPoint(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
      Rect r = GUILayoutUtility.GetRect(gcDrag, styleWindowResize);
      r.x = windowRect.width - 45;
      r.width = 30;

      if (Event.current.type == EventType.MouseDown && r.Contains(mouse))
      {
        isResizing = true;
        resizeStart = new Rect(mouse.x, mouse.y, windowRect.width, windowRect.height);
        //Event.current.Use();  // the GUI.Button below will eat the event, and this way it will show its active state
      }
      else if (Event.current.type == EventType.MouseUp && isResizing)
      {
        isResizing = false;
      }
      else if (!Input.GetMouseButton(0))
      {
        // if the mouse is over some other window we won't get an event, this just kind of circumvents that by checking the button state directly
        isResizing = false;
      }
      else if (isResizing)
      {
        windowRect.width = Mathf.Max(minWindowSize.x, resizeStart.width + (mouse.x - resizeStart.x));
        windowRect.height = Mathf.Max(minWindowSize.y, resizeStart.height + (mouse.y - resizeStart.y));
        windowRect.xMax = Mathf.Min(Screen.width, windowRect.xMax);  // modifying xMax affects width, not x
        windowRect.yMax = Mathf.Min(Screen.height, windowRect.yMax);  // modifying yMax affects height, not y
      }

      GUI.Button(r, gcDrag, styleWindowResize);
      return windowRect;
    }
  }
}
