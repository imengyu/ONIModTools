using ONIModTools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;
using YamlDotNet.Core.Tokens;
using static STRINGS.DUPLICANTS.TRAITS;

namespace OxygenNotIncluded.Mods.ONIModTools
{
  public class RuntimeScenceDebugger : MonoBehaviour
  {
    private const float WINDOW_MIN_WIDTH = 620;
    private const float WINDOW_MIN_HEIGHT = 500;
    private float WINDOW_MAX_WIDTH;
    private float WINDOW_MAX_HEIGHT;
    private const float WINDOW_WIDTH = 1120;
    private const float WINDOW_WIDTH2 = 1820;
    private const float WINDOW_HEIGHT = 700;
    private const float WINDOW_HEIGHT2 = 900;
    private const float LEFT_WIDTH = 320;
    private const float RIGHT_WIDTH = WINDOW_WIDTH - LEFT_WIDTH;
    private const float RIGHT_LIST_WIDTH = 220;

    private bool windowShow = true;
    private Rect windowRect = new Rect(20, 20, WINDOW_WIDTH, WINDOW_HEIGHT);
    private bool isResizing = false;
    private Rect windowResizeStart = new Rect();
    private Vector2 windowMinSize = new Vector2(WINDOW_MIN_WIDTH, WINDOW_MIN_HEIGHT);
    private Vector2 scrollPositionLeft;
    private Vector2 scrollPositionRightSide;
    private Vector2 scrollPositionRightCenter;

    private GUIStyle activeTextStyle = new GUIStyle();
    private GUIStyle activeTextStyle2 = new GUIStyle();
    private GUIStyle errorTextStyle = new GUIStyle();
    private GUIStyle windowStyle ;


    public void Show()
    {
      windowShow = true;
    }

    private void Start()
    {
      windowRect.width = PlayerPrefs.GetFloat("RuntimeScenceDebuggerWidth", WINDOW_WIDTH);
      windowRect.height = PlayerPrefs.GetFloat("RuntimeScenceDebuggerHeight", WINDOW_HEIGHT);
      WINDOW_MAX_WIDTH = Screen.width;
      WINDOW_MAX_HEIGHT = Screen.height;
      GetScenseGameObjects();
    }
    private void OnDestroy()
    {
      SaveWindowSize();
    }
    private void Update()
    {
      if (recastMode && Input.GetMouseButtonDown(0))
      {
        recastMode = false;
        RaycastObjects();
      }
    }

    private bool recastMode = false;
    private bool initedGuiStyle = false;
    private void InitGuiStyle()
    {
      if (!initedGuiStyle)
      {
        windowStyle = new GUIStyle(GUI.skin.window);
        var assetsPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/assets/";
        var background = Utils.LoadTexture2dFromFile(assetsPath + "background.png", 128, 128); ;
        windowStyle.active.background = background;
        windowStyle.onFocused.background = background;
        windowStyle.onActive.background = background;
        windowStyle.onHover.background = background;
        windowStyle.onHover.textColor = Color.white;
        windowStyle.onNormal.background = background;
        windowStyle.normal.background = background;
        activeTextStyle.fontStyle = FontStyle.Bold;
        activeTextStyle.normal.textColor = new Color(0.7f, 0.4f, 0.9f);
        activeTextStyle.alignment = TextAnchor.MiddleLeft;
        activeTextStyle2.normal.textColor = Color.white;
        activeTextStyle2.alignment = TextAnchor.MiddleLeft;
        activeTextStyle2.clipping = TextClipping.Clip;
        errorTextStyle.normal.textColor = new Color(0.9f, 0.1f, 0.2f);
        initedGuiStyle = true;
      }
    }
    private void SaveWindowSize()
    {
      PlayerPrefs.SetFloat("RuntimeScenceDebuggerWidth", windowRect.width);
      PlayerPrefs.SetFloat("RuntimeScenceDebuggerHeight", windowRect.height);
    }

    private void OnGUI()
    {
      if (windowShow)
      {
        InitGuiStyle();

        var horizRatio = Screen.width / 1280;
        var vertRatio = Screen.height / 768;
        if (horizRatio < 1) horizRatio = 1;
        if (vertRatio < 1) vertRatio = 1;

        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(horizRatio, vertRatio, 1));

        windowRect = GUI.Window(0, windowRect, DoMyWindow, "ModTools Scense Explorer", windowStyle);
      }
    }

    private void DoMyWindow(int windowID)
    {
      GUI.DragWindow(new Rect(0, 0, 2000, 20));

      KInputManager.devToolFocus = windowRect.Contains(Event.current.mousePosition + windowRect.position);

      GUILayout.BeginHorizontal();

      if (GUILayout.Button("X - Close Window"))
      {
        windowShow = false;
        KInputManager.devToolFocus = false;
      }
      if (GUILayout.Button(recastMode ? "Raycasting..." : "Raycast UI Objects"))
        recastMode = true;

      var selected = SelectTool.Instance.selected;

      if (GUILayout.Button($"Locate Game Selected ({(selected != null ? (selected.name + " pos: " + selected.gameObject.transform.position) : "null")})"))
      {
        if (selected != null)
        {
          searchName = "";
          SelectGameObject(selected.gameObject, true);
        }
      }

      GUILayout.Label("|");

      if (GUILayout.Button("Reset Window Size"))
      {
        windowRect.width = WINDOW_WIDTH;
        windowRect.height = WINDOW_HEIGHT;
        SaveWindowSize();
      }
      if (GUILayout.Button(windowRect.height == WINDOW_HEIGHT ? "▼ Expand Window" : "▲ Small Window"))
      {
        windowRect.height = windowRect.height == WINDOW_HEIGHT ? WINDOW_HEIGHT2 : WINDOW_HEIGHT;
        SaveWindowSize();
      }
      if (GUILayout.Button(windowRect.width == WINDOW_WIDTH ? "▶ Expand Window" : "◀ Small Window"))
      {
        windowRect.width = windowRect.width == WINDOW_WIDTH ? WINDOW_WIDTH2 : WINDOW_WIDTH;
        SaveWindowSize();
      }

      GUILayout.EndHorizontal();

      GUILayout.BeginHorizontal();

      //Left
      //========================================
      scrollPositionLeft = GUILayout.BeginScrollView(
        scrollPositionLeft,
        GUILayout.Width(LEFT_WIDTH)
      );
      RenderLeft();
      GUILayout.EndScrollView();
      //========================================

      //Right
      //========================================
      RenderRight();
      //========================================

      GUILayout.EndHorizontal();

      windowRect = Utils.ResizeWindow(windowRect, ref isResizing, ref windowResizeStart, windowMinSize);
    }

    private GameObject[] allGameObjects = null;
    private class TreeItem
    {
      public int page = 1;
      public bool open = false;
      public string fullName = "";
      public GameObject gameObject;
      public List<TreeItem> children = new List<TreeItem>();
    }
    private TreeItem scenseTree = new TreeItem();
    private void GetScenseGameObjects()
    {
      allGameObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
      scenseTree.children.Clear();

      if (searchName == "")
      {
        for (int i = 0; i < allGameObjects.Length; i++)
        {
          var item = allGameObjects[i];
          if (item.scene.isLoaded && item.transform.parent == null)
          {
            var item2 = new TreeItem { gameObject = item };
            scenseTree.children.Add(item2);
          }
        }
      }
      else
      {
        for (int i = 0; i < allGameObjects.Length; i++)
        {
          var item = allGameObjects[i];
          if (item.scene.isLoaded && item.name.Contains(searchName))
          {
            var item2 = new TreeItem { gameObject = item };
            scenseTree.children.Add(item2);
          }
        }
      }

      if (currentGameObject != null)
        SelectGameObject(currentGameObject, true);
    }

    private const int PAGE_SIZE = 32;
    private const int PAGE_SIZE2 = 8;
    private string searchName = "";
    private string searchPropName = "";

    void RenderPager(int allPage, int allCount, int currentPage, Action<int> setPage)
    {
      if (allPage > 0)
      {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("<< First"))
        {
          if (currentPage > 1)
            setPage(1);
        }
        if (GUILayout.Button("← Prev"))
        {
          if (currentPage > 1)
            setPage(currentPage - 1);
        }
        GUILayout.Button($"Page {currentPage} of {allPage} All {allCount}");
        if (GUILayout.Button("Next →"))
        {
          if (currentPage < allPage)
            setPage(currentPage + 1);
        }
        if (GUILayout.Button("Last >>"))
        {
          if (currentPage < allPage)
            setPage(allPage);
        }
        GUILayout.EndHorizontal();
      }
    }
    void RenderTree(TreeItem scenseTree, int space)
    {
      if (scenseTree == null || scenseTree.children.Count == 0)
        return;

      int allPage = scenseTree.children.Count > 64 ? (int)Math.Ceiling(scenseTree.children.Count / (double)PAGE_SIZE) : 0;
      if (allPage > 0)
        RenderPager(allPage, scenseTree.children.Count, scenseTree.page, (page) => scenseTree.page = page);

      if (scenseTree.page > 0 && scenseTree.page - 1 * PAGE_SIZE > scenseTree.children.Count)
        scenseTree.page = 1;
      for (
        int i = (scenseTree.page > 0 ? scenseTree.page - 1 : 1) * PAGE_SIZE;
        (allPage == 0 || i < scenseTree.page * PAGE_SIZE) && i < scenseTree.children.Count;
        i++
      )
      {
        var item = scenseTree.children[i];
        if (item.gameObject == null)
          continue;
        GUILayout.BeginHorizontal();

        GUILayout.Space(space);

        if (GUILayout.Button(item.open ? "-" : "+", GUILayout.Width(20)))
        {
          item.open = !item.open;
          if (item.open)
            LoadScenseTreeChild(item);
        }
        if (GUILayout.Button(item.gameObject.activeSelf ? "A" : "D", GUILayout.Width(22)))
          item.gameObject.SetActive(!item.gameObject.activeSelf);

        if (GUILayout.Button(
          (currentGameObject == item.gameObject ? "▶" : "") + 
          (item.fullName != "" ? item.fullName : item.gameObject.name)
         ))
          SelectGameObject(item.gameObject);

        GUILayout.EndHorizontal();

        if (item.open)
          RenderTree(item, space + 10);
      }
    }
    void RenderLeft()
    {
      if (GUILayout.Button($"({scenseTree.children.Count}) Refresh Scense tree"))
        GetScenseGameObjects();

      GUILayout.BeginHorizontal();

      searchName = GUILayout.TextField(searchName, GUILayout.Width(200));

      if (GUILayout.Button("Search"))
        GetScenseGameObjects();

      GUILayout.EndHorizontal();

      RenderTree(scenseTree, 0);
    }

    private GameObject currentGameObject = null;
    private List<Component> currentComponents = new List<Component>();
    private string currentComponentsError = "";
    private object currentComponent = null;

    private PropItemArrayGetSet ArrayGetSet = new PropItemArrayGetSet();
    private PropItemDictGetSet DictGetSet = new PropItemDictGetSet();

    private class PropItemArrayGetSet : PropItemCustomGetSet
    {
      public IList Array;
      public int Index = 0;

      public PropItemArrayGetSet()
      {
        Set = (value) => Array[Index] = value;
        Get = () => Array[Index];
      }
    }
    private class PropItemDictGetSet : PropItemCustomGetSet
    {
      public IDictionary Dict;
      public object Key;

      public PropItemDictGetSet()
      {
        Set = (value) => Dict[Key] = value;
        Get = () => Dict[Key];
      }
    }
    private class PropItemCustomGetSet
    {
      public Action<object> Set = null;
      public Func<object> Get = null;
    }
    private class PropItem
    {
      public FieldInfo Field = null;
      public PropertyInfo Property = null;
      public PropItemCustomGetSet GetSet = null;

      public Dictionary<int, PropItem> Child = new Dictionary<int, PropItem>();
      public PropItem GetChildItem(int index)
      {
        if (!Child.TryGetValue(index, out var a))
        {
          a = new PropItem();
          Child.Add(index, a);
        }
        return a;
      }

      public Type ValueType = null;
      public string Name = null;
      public object Value = null;
      public object[] TempValues = new object[8];
      public bool CanSet = true;
      public bool Error = false;
      public bool HideInInspector = false;
      public int Page = 1;
    }
    private Stack<object> currentComponentSelectHistory = new Stack<object>();
    private List<PropItem> currentComponentProps = new List<PropItem>();
    private string currentComponentPropsError = "";
    private object propCopyValue = null;
    private bool showHidden = false;
    private bool showFields = false;
    private bool showTypeName = true;
    private bool sortDown = true;

    void LoadScenseTreeChild(TreeItem item)
    {
      item.children.Clear();
      var childCount = item.gameObject.transform.childCount;
      for (int j = 0; j < childCount; j++)
      {
        var item2 = item.gameObject.transform.GetChild(j);
        var treeItem = new TreeItem { 
          gameObject = item2.gameObject
        };
        item.children.Add(treeItem);
      }
    }
    void LoadComponentList()
    {
      currentComponents.Clear();
      if (currentGameObject)
        currentComponents.AddRange(currentGameObject.GetComponents<Component>());
    }
    void LoadPropList()
    {
      currentComponentProps.Clear();
      var type = currentComponent.GetType();
      var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
      for (var i = 0; i < fields.Length && i < 512; i++)
      {
        var field = fields[i];
        if (field.FieldType.IsSubclassOf(typeof(Delegate)) 
          || field.FieldType.IsSubclassOf(typeof(UnityEvent))
         )
          continue;
        try
        {
          var item = new PropItem();
          item.Field = field;
          item.Name = field.Name;
          item.ValueType = field.FieldType;
          item.CanSet = true;
          item.Value = field.GetValue(currentComponent);
          item.HideInInspector = field.GetCustomAttribute(typeof(HideInInspector), true) != null;
          currentComponentProps.Add(item);
          RenderPropertyEditorItemPreApplyData(item);
        } 
        catch
        {
          Debug.Log($"Failed to get value of field {field.Name}");
        }
      }
      var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
      for (var i = 0; i < properties.Length && i < 512; i++)
      {
        var property = properties[i];
        if (property.GetCustomAttribute(typeof(ObsoleteAttribute), true) != null)
          continue;
        if (property.PropertyType.IsSubclassOf(typeof(Delegate)) 
          || property.PropertyType.IsSubclassOf(typeof(UnityEvent))
          || !property.CanRead
         )
          continue;
        try
        {
          var item = new PropItem();
          item.Property = property;
          item.Name = property.Name;
          item.ValueType = property.PropertyType;
          item.CanSet = property.CanWrite;
          item.Value = property.GetValue(currentComponent);
          item.HideInInspector = property.GetCustomAttribute(typeof(HideInInspector), true) != null;
          currentComponentProps.Add(item);
          RenderPropertyEditorItemPreApplyData(item);
        }
        catch
        {
          Debug.Log($"Failed to get value of property {property.Name}");
        }
      }
      currentComponentProps.Sort((a, b) => a.Name.CompareTo(b.Name) * (sortDown ? 1 : -1));
    }
    void RaycastObjects()
    {
      searchName = "";
      currentGameObject = null;
      currentComponent = null;
      var canvases = FindObjectsOfType<Canvas>();
      var result = new List<GameObject>();
      foreach (Canvas canvas in canvases)
      {
        PointerEventData pointerEventData = new PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        pointerEventData.position = Input.mousePosition;
        GraphicRaycaster gr = canvas.GetComponent<GraphicRaycaster>();
        if (gr == null)
          gr = canvas.gameObject.AddComponent<GraphicRaycaster>();
        List<RaycastResult> results = new List<RaycastResult>();
        gr.Raycast(pointerEventData, results);
        if (results.Count != 0)
        {
          foreach(var r in results)
            result.Add(r.gameObject);
        }
      }
      scenseTree.children.Clear();
      scenseTree.page = 1;
      foreach (var item in result)
        scenseTree.children.Add(new TreeItem { 
          gameObject = item,
          fullName = GetGameObjectFullName(item)
        });
    }

    string GetGameObjectFullName(GameObject go)
    {
      StringBuilder stringBuilder = new StringBuilder();
      var obj = go.transform;
      while (obj != null)
      {
        stringBuilder.Insert(0, $"{obj.name} / ");
        obj = obj.parent;
      }
      return stringBuilder.ToString();
    }
    void SelectComponent(object component, bool history = true)
    {
      if (history)
        currentComponentSelectHistory.Push(currentComponent);
      currentComponent = component;
      LoadPropList();
    }
    void SelectComponentBack()
    {
      SelectComponent(currentComponentSelectHistory.Pop(), false);
    }
    void SelectGameObject(GameObject go, bool relocateTree = false)
    {
      currentGameObject = go;

      if (relocateTree)
      {
        var parentTree = new List<GameObject>();
        var obj = currentGameObject.transform;
        while (obj != null)
        {
          parentTree.Insert(0, obj.gameObject);
          obj = obj.parent;
        }

        var currentTree = scenseTree;
        for (int i = 0; i < parentTree.Count; i++)
        {
          if (currentTree != scenseTree)
            LoadScenseTreeChild(currentTree);
          var index = currentTree.children.FindIndex((item) => item.gameObject == parentTree[i].gameObject);
          if (index < 0)
            break;
          currentTree.open = true;
          currentTree.page = (index / PAGE_SIZE) + 1;
          currentTree = currentTree.children[index];
        }
      }

      LoadComponentList();

      if (currentComponent != null)
      {
        currentComponent = currentGameObject.GetComponent(currentComponent.GetType());
        if (currentComponent != null)
          LoadPropList();
        else
          currentComponentProps.Clear();
      }
    }
    void RenderRight()
    {
      if (currentGameObject == null)
      {
        GUILayout.Label("Select a GameObject in left list");
        return;
      }

      GUILayout.BeginHorizontal();

      //Left
      //========================================
      scrollPositionRightSide = GUILayout.BeginScrollView(scrollPositionRightSide, GUILayout.Width(RIGHT_LIST_WIDTH));
      RenderComponentList();
      GUILayout.EndScrollView();
      //========================================

      //Right
      //========================================
      scrollPositionRightCenter = GUILayout.BeginScrollView(scrollPositionRightCenter);
      RenderPropertyList();
      GUILayout.EndScrollView();
      //========================================

      GUILayout.EndHorizontal();
    }
    void RenderComponentList()
    {
      try
      {
        if (!string.IsNullOrEmpty(currentComponentsError))
          GUILayout.Label("Load list failed: " + currentComponentsError, errorTextStyle);

        if (GUILayout.Button($"Refresh Component list"))
          LoadComponentList();

        GUILayout.Label($"Select {(currentGameObject != null ? currentGameObject.name : null)} (Count:{currentComponents.Count})", activeTextStyle2);
        for (var i = 0; i < currentComponents.Count; i++)
        {
          var comp = currentComponents[i];
          if (GUILayout.Button((currentComponent == (object)comp ? "▶" : "") + comp.GetType().FullName))
            SelectComponent(comp);
        }
        currentComponentsError = "";
      }
      catch(Exception e)
      {
        currentComponents.Clear();
        currentComponentsError = e.Message;
      }
    }
    void RenderPropertyList()
    {
      try
      {
        if (!string.IsNullOrEmpty(currentComponentPropsError))
          GUILayout.Label("Load list failed: " + currentComponentPropsError, errorTextStyle);

        GUILayout.BeginHorizontal();

        if (currentComponentSelectHistory.Count > 1 && GUILayout.Button("◀ GoBack", GUILayout.Width(100)))
          SelectComponentBack();
        if (GUILayout.Button("Sort: " + (sortDown ? "A-Z" : "Z-A"), GUILayout.Width(100)))
        {
          sortDown = !sortDown;
          currentComponentProps.Sort((a, b) => a.Name.CompareTo(b.Name) * (sortDown ? 1 : -1));
        }
        GUILayout.Button("Filter: ", GUILayout.Width(70));
        searchPropName = GUILayout.TextField(searchPropName, GUILayout.MinWidth(200));

        GUILayout.Button($"Select {(currentComponent != null ? currentComponent.GetType().Name : "null")} (Count:{currentComponentProps.Count})", activeTextStyle2);

        showHidden = GUILayout.Toggle(showHidden, "Show Hidden", GUILayout.Width(100));
        showFields = GUILayout.Toggle(showFields, "Show Fields", GUILayout.Width(100));
        showTypeName = GUILayout.Toggle(showTypeName, "Type Name", GUILayout.Width(100));

        if (GUILayout.Button($"Refresh Property list", GUILayout.Width(200)))
          LoadPropList();

        GUILayout.EndHorizontal();

        if (currentComponent == null)
          return;
        if (currentComponentProps.Count == 0)
        {
          GUILayout.Label("This component has no properties");
          return;
        }

        for (var i = 0; i < currentComponentProps.Count; i++)
        {
          if (currentComponentProps.Count == 0)
            break;
          var item = currentComponentProps[i];
          if (item.Error)
            continue;
          var isField = item.Field != null;
          if (isField && !showFields)
            continue;
          if (item.HideInInspector && !showHidden)
            continue;
          if (!string.IsNullOrEmpty(searchPropName) && !item.Name.Contains(searchPropName))
            continue;
          try
          {
            RenderPropertyItem(item);
          } 
          catch
          {
            item.Error = true;
          }
        }

        currentComponentPropsError = "";
      } 
      catch (Exception e) 
      {
        currentComponentProps.Clear();
        currentComponentPropsError = e.ToString();
      }
    }

    private class ObjectFastStringData
    {
      public FieldInfo Field;
      public PropertyInfo Property;
      public bool HasField;
      public StringBuilder Temp = new StringBuilder();
    }
    private class EnumValueData
    {
      public string[] Names;
      public List<object> Value;
    }
    private Dictionary<string, ObjectFastStringData> objectFastStringData = new Dictionary<string, ObjectFastStringData>();
    private Dictionary<string, EnumValueData> enumTempData = new Dictionary<string, EnumValueData>();

    string RenderPropertyObjectFastString(object value)
    {
      if (value == null) 
        return "null";
      var type = value.GetType();
      if (objectFastStringData.TryGetValue(type.FullName, out var data)) {
        if (!data.HasField) {
          var str = value.ToString();
          if (str.Length > 30)
            str = str.Substring(0, 30) + "...";
          return str;
        }

        try
        {
          data.Temp.Clear();
          data.Temp.Append("{ ");
          if (data.Field != null)
          {
            data.Temp.Append(data.Field.Name);
            data.Temp.Append(" = \"");
            data.Temp.Append(data.Field.GetValue(value).ToString());
            data.Temp.Append("\", ");
          }
          if (data.Property != null)
          {
            data.Temp.Append(data.Property.Name);
            data.Temp.Append(" = \"");
            data.Temp.Append(data.Property.GetValue(value).ToString());
            data.Temp.Append("\", ");
          }
          data.Temp.Append("... }");
          return data.Temp.ToString();
        }
        catch
        {
          data.HasField = false;
          return "";
        }
      }

      data = new ObjectFastStringData();

      var fields = type.GetFields();
      foreach (var field in fields) {
        if (field.FieldType == typeof(string))
        {
          data.Field = field;
          break;
        }
      }
      var properties = type.GetProperties();
      foreach (var property in properties)
      {
        if (property.PropertyType == typeof(string))
        {
          data.Property = property;
          break;
        }
      }
      data.HasField = data.Field != null || data.Property != null;

      objectFastStringData.Add(type.FullName, data);
      return "";
    }
    void RenderPropertyItem(PropItem item)
    {
      GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));

      GUILayout.Label(item.Name, activeTextStyle, GUILayout.Width(170), GUILayout.Height(33));
      if (GUILayout.Button("C", GUILayout.Width(22)))
        propCopyValue = item.Value;
      if (GUILayout.Button("P", GUILayout.Width(22)))
        item.Value = propCopyValue;
      if (GUILayout.Button("Get", GUILayout.Width(36)))
      {
        if (item.GetSet != null) item.Value = item.GetSet.Get();
        if (item.Field != null) item.Value = item.Field.GetValue(currentComponent);
        if (item.Property != null) item.Value = item.Property.GetValue(currentComponent);
      }
      RenderPropertyEditorItem(item.ValueType, item, out var hasEdtior);
      if (item.CanSet && hasEdtior)
      {
        if (GUILayout.Button("Set", GUILayout.Width(36)))
        {
          if (item.GetSet != null) item.GetSet.Set(RenderPropertyEditorItemBeforeSaveData(item));
          if (item.Field != null) item.Field.SetValue(currentComponent, RenderPropertyEditorItemBeforeSaveData(item));
          if (item.Property != null) item.Property.SetValue(currentComponent, RenderPropertyEditorItemBeforeSaveData(item));
          RenderPropertyEditorItemPreApplyData(item);
        }
      }
      else
        GUILayout.Button(new GUIContent("-", "This prop is Readonly"), GUILayout.Width(36));
      if (GUILayout.Button("Copy", GUILayout.Width(44)))
        GUIUtility.systemCopyBuffer = item.Name + " " + (item.Value == null ? "null" : item.Value.ToString());
      GUILayout.EndHorizontal();
    }

    object RenderPropertyEditorItemBeforeSaveData(PropItem item)
    {
      if (item.ValueType == typeof(Vector2))
        return new Vector2((float)item.TempValues[0], (float)item.TempValues[1]);
      if (item.ValueType == typeof(Vector3))
        return new Vector3((float)item.TempValues[0], (float)item.TempValues[1], (float)item.TempValues[2]);
      if (item.ValueType == typeof(Quaternion))
        return new Quaternion((float)item.TempValues[0], (float)item.TempValues[1], (float)item.TempValues[2], (float)item.TempValues[3]);
      if (item.ValueType == typeof(Color))
        return new Color((float)item.TempValues[0], (float)item.TempValues[1], (float)item.TempValues[2], (float)item.TempValues[3]);
      if (item.ValueType == typeof(Rect))
        return new Rect((float)item.TempValues[0], (float)item.TempValues[1], (float)item.TempValues[2], (float)item.TempValues[3]);
      return item.Value;
    }
    void RenderPropertyEditorItemPreApplyData(PropItem item)
    {
      if (item.ValueType == typeof(Vector2))
      {
        item.TempValues[0] = ((Vector2)item.Value).x;
        item.TempValues[1] = ((Vector2)item.Value).y;
      }
      if (item.ValueType == typeof(Vector3))
      {
        item.TempValues[0] = ((Vector3)item.Value).x;
        item.TempValues[1] = ((Vector3)item.Value).y;
        item.TempValues[2] = ((Vector3)item.Value).z;
      }
      if (item.ValueType == typeof(Quaternion))
      {
        item.TempValues[0] = ((Quaternion)item.Value).x;
        item.TempValues[1] = ((Quaternion)item.Value).y;
        item.TempValues[2] = ((Quaternion)item.Value).z;
        item.TempValues[3] = ((Quaternion)item.Value).w;
      }
      if (item.ValueType == typeof(Color))
      {
        item.TempValues[0] = ((Color)item.Value).r;
        item.TempValues[1] = ((Color)item.Value).g;
        item.TempValues[2] = ((Color)item.Value).b;
        item.TempValues[3] = ((Color)item.Value).a;
      }
      if (item.ValueType == typeof(Rect))
      {
        item.TempValues[0] = ((Rect)item.Value).x;
        item.TempValues[1] = ((Rect)item.Value).y;
        item.TempValues[2] = ((Rect)item.Value).width;
        item.TempValues[3] = ((Rect)item.Value).height;
      }
    }
    void RenderPropertyEditorItem(Type type, PropItem item, out bool hasEdtior)
    {
      try
      {
        GUILayout.Space(10);

        if (showTypeName)
        {
          GUILayout.Label(type.Name, activeTextStyle, GUILayout.Height(33), GUILayout.Width(140));
          GUILayout.Space(10);
        }

        hasEdtior = false;

        if (type == null || item == null || item.Value == null)
        {
          GUILayout.Label("null", activeTextStyle2, GUILayout.MinHeight(33));
          return;
        }
        if (type.IsSubclassOf(typeof(Delegate)) || type.IsSubclassOf(typeof(UnityEvent)))
          return;

        hasEdtior = true;

        if (type == typeof(string))
        {
          item.Value = GUILayout.TextField((string)item.Value);
          return;
        }
        if (type == typeof(bool))
        {
          item.Value = GUILayout.Toggle((bool)item.Value, "true");
          return;
        }
        if (type == typeof(int))
        {
          var text = GUILayout.TextField(item.Value.ToString());
          if (int.TryParse(text, out var v))
            item.Value = v;
          else
            GUILayout.Label("Invalid int !", errorTextStyle);
          return;
        }
        if (type == typeof(float))
        {
          var text = GUILayout.TextField(item.Value.ToString());
          if (float.TryParse(text, out var v))
            item.Value = v;
          else
            GUILayout.Label("Invalid float !", errorTextStyle);
          return;
        }
        if (type == typeof(short))
        {
          var text = GUILayout.TextField(item.Value.ToString());
          if (short.TryParse(text, out var v))
            item.Value = v;
          else
            GUILayout.Label("Invalid short !", errorTextStyle);
          return;
        }
        if (type == typeof(string))
        {
          item.Value = GUILayout.TextField(item.Value.ToString());
          return;
        }
        if (type == typeof(Vector2))
        {
          var text = GUILayout.TextField(item.TempValues[0].ToString());
          if (float.TryParse(text, out var v))
            item.TempValues[0] = v;
          text = GUILayout.TextField(item.TempValues[1].ToString());
          if (float.TryParse(text, out v))
            item.TempValues[1] = v;
          return;
        }
        if (type == typeof(Vector3))
        {
          var text = GUILayout.TextField(item.TempValues[0].ToString());
          if (float.TryParse(text, out var v))
            item.TempValues[0] = v;
          text = GUILayout.TextField(item.TempValues[1].ToString());
          if (float.TryParse(text, out v))
            item.TempValues[1] = v;
          text = GUILayout.TextField(item.TempValues[2].ToString());
          if (float.TryParse(text, out v))
            item.TempValues[2] = v;
          return;
        }
        if (type == typeof(Quaternion))
        {
          var text = GUILayout.TextField(item.TempValues[0].ToString());
          if (float.TryParse(text, out var v))
            item.TempValues[0] = v;
          text = GUILayout.TextField(item.TempValues[1].ToString());
          if (float.TryParse(text, out v))
            item.TempValues[1] = v;
          text = GUILayout.TextField(item.TempValues[2].ToString());
          if (float.TryParse(text, out v))
            item.TempValues[2] = v;
          text = GUILayout.TextField(item.TempValues[3].ToString());
          if (float.TryParse(text, out v))
            item.TempValues[3] = v;
          return;
        }
        if (type == typeof(Matrix4x4)) 
          return; //TODO
        if (type == typeof(Matrix2x3)) 
          return; //TODO
        if (type == typeof(Color))
        {
          var text = GUILayout.TextField(item.TempValues[0]?.ToString() ?? "");
          if (float.TryParse(text, out var v))
            item.TempValues[0] = v;
          text = GUILayout.TextField(item.TempValues[1].ToString());
          if (float.TryParse(text, out v))
            item.TempValues[1] = v;
          text = GUILayout.TextField(item.TempValues[2].ToString());
          if (float.TryParse(text, out v))
            item.TempValues[2] = v;
          text = GUILayout.TextField(item.TempValues[3].ToString());
          if (float.TryParse(text, out v))
            item.TempValues[3] = v;
          return;
        }
        if (type == typeof(Rect))
        {
          var text = GUILayout.TextField(item.TempValues[0].ToString());
          if (float.TryParse(text, out var v))
            item.TempValues[0] = v;
          text = GUILayout.TextField(item.TempValues[1].ToString());
          if (float.TryParse(text, out v))
            item.TempValues[1] = v;
          text = GUILayout.TextField(item.TempValues[2].ToString());
          if (float.TryParse(text, out v))
            item.TempValues[2] = v;
          text = GUILayout.TextField(item.TempValues[3].ToString());
          if (float.TryParse(text, out v))
            item.TempValues[3] = v;
          return;
        }
        if (type == typeof(Sprite))
        {
          var sprite = (Sprite)item.Value;

          Vector2 fullSize = new Vector2(sprite.texture.width, sprite.texture.height);
          Vector2 size = new Vector2(sprite.textureRect.width, sprite.textureRect.height);

          Rect coords = sprite.textureRect;
          coords.x /= fullSize.x;
          coords.width /= fullSize.x;
          coords.y /= fullSize.y;
          coords.height /= fullSize.y;

          Rect position = GUILayoutUtility.GetRect(256, 256);
          GUI.Box(position, "");

          Vector2 ratio;
          ratio.x = position.width / size.x;
          ratio.y = position.height / size.y;
          float minRatio = Mathf.Min(ratio.x, ratio.y);

          Vector2 center = position.center;
          position.width = size.x * minRatio;
          position.height = size.y * minRatio;
          position.center = center;

          GUI.DrawTextureWithTexCoords(position, sprite.texture, coords);
          return;
        }
        if (type == typeof(Texture))
        {
          var valueV = (Texture)item.Value;
          GUILayout.Box(valueV, GUILayout.MinWidth(256), GUILayout.MinHeight(256));
          return;
        }
        if (type == typeof(Texture2D))
        {
          var valueV = (Texture2D)item.Value;
          GUILayout.Box(valueV, GUILayout.MinWidth(256), GUILayout.MinHeight(256));
          return;
        }
        if (type == typeof(GameObject))
        {
          var valueV = (GameObject)item.Value;
          if (GUILayout.Button(valueV == null ? "null" : $"▶ {valueV}"))
            SelectGameObject(valueV, true);
          return;
        }
        if (type.IsEnum)
        {
          if (!enumTempData.TryGetValue(type.FullName, out var enumValue))
          {
            enumValue = new EnumValueData();
            enumValue.Names = Enum.GetNames(type);
            enumValue.Value = new List<object>();
            var values1 = Enum.GetValues(type);
            if (values1 != null)
            foreach (var name in values1)
              enumValue.Value.Add(name);
            enumTempData.Add(type.FullName, enumValue);
          }

          var names = enumValue.Names;
          var values = enumValue.Value;
          if (names == null || names.Length == 0 || values.Count == 0 || names.Length != values.Count)
          {
            hasEdtior = false;
            return;
          }

          int newValue;
          if (values.Count > 4 && values.Count < 16)
            newValue = GUILayout.SelectionGrid((int)item.Value, names, 3);
          else if(values.Count <= 4)
            newValue = GUILayout.Toolbar((int)item.Value, names);
          else
          {
            var selectIndex = values.IndexOf(item.Value);
            if (selectIndex == -1)
              selectIndex = 0;
            if (GUILayout.Button("<"))
              if (selectIndex > 0)
                selectIndex--;
            GUILayout.Button((string)names.GetValue(selectIndex));
            if (GUILayout.Button(">"))
              if (selectIndex < values.Count - 1) 
                selectIndex++;
            newValue = selectIndex;
          }
          if (newValue >= 0 && newValue < values.Count)
            item.Value = values[newValue];
          return;
        }
        if (type.IsArray)
        {
          GUILayout.BeginVertical();

          Array array = (Array)item.Value;
          if (array == null)
            GUILayout.Label("null Array");

          int allPage = array.Length > PAGE_SIZE2 ? (int)Math.Ceiling(array.Length / (double)PAGE_SIZE2) : 0;
          if (allPage > 0)
            RenderPager(allPage, array.Length, item.Page, (page1) => item.Page = page1);

          if (array.Length == 0)
            GUILayout.Label("Empty Array");

          ArrayGetSet.Array = array;
          for (int i = (item.Page > 0 ? item.Page - 1 : 1) * PAGE_SIZE2; (allPage == 0 || i < item.Page * PAGE_SIZE2) && i < array.Length; i++)
          {
            var arrItem = array.GetValue(i);
            var childItem = item.GetChildItem(i);
            ArrayGetSet.Index = i;
            if (childItem.Value == null)
            {
              childItem.GetSet = ArrayGetSet;
              childItem.Name = $"Array[{i}] = ";
              childItem.Value = arrItem;
              childItem.ValueType = arrItem == null ? typeof(object) : arrItem.GetType();
            }
            RenderPropertyItem(childItem);
          }
          GUILayout.EndVertical();
          return;
        }
        if (typeof(IList).IsAssignableFrom(type) || (type.IsGenericType && typeof(List<>) == type.GetGenericTypeDefinition()))
        {
          try
          {
            GUILayout.BeginVertical();

            var array = (IList)item.Value;
            if (array == null)
              GUILayout.Label("null Array");

            int allPage = array.Count > PAGE_SIZE2 ? (int)Math.Ceiling(array.Count / (double)PAGE_SIZE2) : 0;
            if (allPage > 0)
              RenderPager(allPage, array.Count, item.Page, (page1) => item.Page = page1);

            if (array.Count == 0)
              GUILayout.Label("Empty List");

            ArrayGetSet.Array = array;
            for (int i = (item.Page > 0 ? item.Page - 1 : 1) * PAGE_SIZE2; (allPage == 0 || i < item.Page * PAGE_SIZE2) && i < array.Count; i++)
            {
              var item1 = array[i];
              var arrItem = array[i];
              var childItem = item.GetChildItem(i);
              ArrayGetSet.Index = i;
              if (childItem.Value == null)
              {
                childItem.GetSet = ArrayGetSet;
                childItem.Name = $"Array[{i}] = ";
                childItem.Value = arrItem;
                childItem.ValueType = arrItem == null ? typeof(object) : arrItem.GetType();
              }
              RenderPropertyItem(childItem);
            }
            GUILayout.EndVertical();

          }
          catch (Exception e)
          {
            Debug.Log("Array convert type failed: " + e + " " + type);
          }
          return;
        }
        if (typeof(IDictionary).IsAssignableFrom(type) || (type.IsGenericType && typeof(Dictionary<,>) == type.GetGenericTypeDefinition()))
        {
          try
          {
            GUILayout.BeginVertical();

            var dict = (IDictionary)item.Value;
            if (dict == null)
              GUILayout.Label("null Dictionary");

            DictGetSet.Dict = dict;

            int allPage = dict.Keys.Count > PAGE_SIZE2 ? (int)Math.Ceiling(dict.Keys.Count / (double)PAGE_SIZE2) : 0;
            if (allPage > 0)
              RenderPager(allPage, dict.Keys.Count, item.Page, (page1) => item.Page = page1);

            if (dict.Count == 0)
              GUILayout.Label("Empty Dictionary");

            int startIndex = (item.Page > 0 ? item.Page - 1 : 1) * PAGE_SIZE2;
            int index = 0;
            foreach (var key in dict.Keys)
            {
              if (index > startIndex)
              {
                var dictValue = dict[key];
                var childItem = item.GetChildItem(index);
                DictGetSet.Key = key;
                if (childItem.Value == null)
                {
                  childItem.GetSet = DictGetSet;
                  childItem.Name = $"Dictionary[{key}] ({index}) = ";
                  childItem.Value = dictValue;
                  childItem.ValueType = dictValue == null ? typeof(object) : dictValue.GetType();
                }
                RenderPropertyItem(childItem);
              }
              index++;
              if (allPage > 0 && index > item.Page * PAGE_SIZE2)
                break;
            }

            GUILayout.EndVertical();
          }
          catch (Exception e)
          {
            Debug.LogError("Dictionary convert type failed: type: " + type + " e: " + e.Message + " s: " + e.StackTrace);
          }
          return;
        }
        if (!type.IsPrimitive && !type.IsEnum && type.IsValueType)
        {
          if (GUILayout.Button($"▶ Struct {RenderPropertyObjectFastString(item.Value)}"))
            SelectComponent(item.Value);
          return;
        }
        if (type.IsSubclassOf(typeof(object)))
        {
          if (GUILayout.Button($"▶ Object {RenderPropertyObjectFastString(item.Value)}"))
            SelectComponent(item.Value);
          return;
        }
        GUILayout.Label(item.Value.ToString(), activeTextStyle2, GUILayout.MinHeight(33));
        GUILayout.Label(type.FullName, activeTextStyle, GUILayout.Height(33), GUILayout.Width(200));
        GUILayout.Label(" ");
        GUILayout.Label(" ");
        hasEdtior = false;
      } 
      catch (Exception e)
      {
        Debug.LogError("RenderPropertyEditorItem Faild: " + item.Name + " type: " + item.ValueType + " e: " + e.Message + " s: " + e.StackTrace);
        throw e;
      }
    }
  }
}
