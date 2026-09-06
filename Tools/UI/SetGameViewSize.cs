// Run with Unity Pipeline eval_file. Set Temp/ui-capture-size.txt to WIDTH HEIGHT first.
// Uses Unity Editor's internal GameView API; never included in the player build.
var dimensions = System.IO.File.ReadAllText("Temp/ui-capture-size.txt").Trim().Split(' ');
int width = int.Parse(dimensions[0]), height = int.Parse(dimensions[1]);
var assembly = typeof(UnityEditor.Editor).Assembly;
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
var singletonType = typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(sizesType);
var sizes = singletonType.GetProperty("instance", flags).GetValue(null);
var group = sizesType.GetProperty("currentGroup", flags).GetValue(sizes);
var groupType = group.GetType();
var sizeType = assembly.GetType("UnityEditor.GameViewSize");
var kind = System.Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"), "FixedResolution");
var size = System.Activator.CreateInstance(sizeType, flags, null, new object[] { kind, width, height, "BetoBeto UI verification" }, null);
groupType.GetMethod("AddCustomSize", flags).Invoke(group, new[] { size });
int index = (int)groupType.GetMethod("GetTotalCount", flags).Invoke(group, null) - 1;
var viewType = assembly.GetType("UnityEditor.GameView");
var view = UnityEditor.EditorWindow.GetWindow(viewType);
viewType.GetProperty("selectedSizeIndex", flags).SetValue(view, index);
view.Repaint();
return new { width, height, index };
