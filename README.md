ModTools 模组开发工具
---

[Mod in Steam](https://steamcommunity.com/sharedfiles/filedetails/?id=3241644477)

● Introduction

ModTools is an in-game scene hierarchy viewer and a toolkit for mod authors, providing the following functions:
1. Ingame log view
2. Scene Hierarchy Viewer
3. Locate UI and selected GameObject

● Usage
In the game pause ui, select "ModTools" to enable the function.

● Function

Log window

After enable the mod, a log window will be displayed after the game starts.
You can also pause the game and select ModTools>RuntimeConsole to open it.

Scene Data Viewer Window

On the left is the scene hierarchy tree (Unity) 
* You can click on "Refresh Scene tree" to refresh the list;
* You can search for filtering objects in the input box;
* Each data can be selected by clicking on it, or expanded to its child level by clicking on the "+" button.

On the middle is a list of components of selected GameObject

* You can click on "Refresh Component List" to refresh the list;
* Each data can be clicked to select and display its attributes

On the right is the field list of the selected Component
* The purple text is the field name
* C button: Copy to internal clipboard
* P button: Pastes values from the internal clipboard, which is a good way to set up an internal object value
* Get: Get the field value, which will overwrite the field value you edited in the editor
* Set: Apply the field value you edited in editor to the object. If not shown, it indicates that the field is read-only
* Copy: Copy the current field information and value (only String) to the clipboard
* Show Hidden: Whether to display fields with [HideInInspector] attribute
* Show Fields: Whether to display fields (if not is property)

Upper button
* Raycast UI Objects: locate the UI elements in the game. Clicking on the button will display "Raycasting..." At this point, click with mouse that will be locate UI elements in mouse position.
* Location Game Selected: locate the selected objects (buildings, objects) in the game
* Expand Window/Small Window: Expand and collapse window

● New update features

5.8
- Fixed small text size in high resolution
- Added Array and Dictionary nested field display
- Added field jump editing for object types
- Edit data interface layout optimization

---

● 简介

模组开发工具是一个为模组开发者提供的工具，方便模组开发，提供了以下几种功能：

1. 游戏内日志实时打印
2. 场景数据查看器
3. 定位游戏对象

● 使用

在游戏暂停界面，选中“ModTools”可以开启功能。

● 功能

▷ 日志窗口

启用模组后在游戏启动后，会显示日志窗口，其中包含了日志等级分类开关。
也可以在游戏中暂停界面选择 ModTools > RuntimeConsole 来打开。

▷ 场景数据查看器窗口

左侧是Unity场景层级树
* 可以点击“Refresh Scense tree”刷新列表；
* 可以在输入框中搜索筛选对象；
* 每条数据可以点击选中，或者点击“+”展开子级。

中测是游戏对象上绑定组件的列表
* 可以点击“Refresh Component list”刷新列表；
* 每条数据可以点击选中，显示其中的属性

右侧是选中的组件的字段列表
* 紫色文字是属性名称
* C按钮：复制到内部剪贴板
* P按钮：从内部剪贴板粘贴值，这在设置一个对象时还拥有
* Get：获取字段值，会覆盖编辑器内您编辑的字段值
* Set：将您编辑的字段值应用至对象, 如果没有，则表示此字段只读
* Copy：复制当前字段信息至剪贴板

* ShowHidden：是否显示有HideInInspector属性的字段
* ShowFields：是否显示

上方按钮
* Raycast UI Objects：定位游戏中的UI对象，点击后按钮会显示“Raycasting...”，此时再点击需要定位的UI即可定位
* Locate Game Selected：定位游戏中选中的对象（建筑，掉落物）
* Expand Window/Small Window：展开折叠按钮


