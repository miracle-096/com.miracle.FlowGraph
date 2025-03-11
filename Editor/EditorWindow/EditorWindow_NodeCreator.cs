using System;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

public class EditorWindow_NodeCreator : OdinMenuEditorWindow
{
    [MenuItem("FlowChart/FlowChart节点配置")]
    private static void OpenWindow()
    {
        var window = GetWindow<EditorWindow_NodeCreator>();
        window.position = GUIHelper.GetEditorWindowRect().AlignCenter(720, 720);
        window.titleContent = new GUIContent("FlowChart设置");
    }

    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree();
        // 获取所有的资产
        var assets = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Packages/com.miracle.FlowGraph/Editor/EditorWindow" });
        var assetList = new List<ScriptableObject>();
        foreach (var assetGUID in assets)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            assetList.Add(asset);
        }

        assetList.Sort((a, b) =>
        {
            var orderA = GetOrderFromName(a.name);
            var orderB = GetOrderFromName(b.name);
            return string.Compare(orderA, orderB, StringComparison.Ordinal);
        });

        // 添加到菜单树
        foreach (var asset in assetList)
        {
            tree.Add($"FlowChart设置/{asset.name}", asset);
        }

        return tree;
    }
    
    private string GetOrderFromName(string name)
    {
        var parts = name.Split('_');
        return parts.Length > 1 ? parts[0] : "zzzz"; // 用于表示较低的排序优先级
    }
}
