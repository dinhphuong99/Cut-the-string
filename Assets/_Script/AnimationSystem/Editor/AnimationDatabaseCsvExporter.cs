// Assets/Scripts/AnimationSystem/Editor/AnimationDatabaseCsvExporter.cs
#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AnimationDatabaseCsvExporter
{
    [MenuItem("Tools/Animation/Export AnimationDatabase to CSV")]
    public static void ExportSelectedDatabase()
    {
        var db = Selection.activeObject as AnimationDatabase;
        if (db == null)
        {
            EditorUtility.DisplayDialog("Export CSV", "Select an AnimationDatabase asset first.", "OK");
            return;
        }

        string path = EditorUtility.SaveFilePanel("Save CSV", Application.dataPath, db.name + ".csv", "csv");
        if (string.IsNullOrEmpty(path)) return;

        var sb = new StringBuilder();
        sb.AppendLine("CharacterId,Intent,Variant,Source,ClipPath,AddressKey");
        foreach (var e in db.entries)
        {
            string clipPath = e.clipRef.clip ? AssetDatabase.GetAssetPath(e.clipRef.clip) : "";
            string source = e.clipRef.source.ToString();
            string address = e.clipRef.addressKey ?? "";
            sb.AppendLine($"{e.key.characterId},{e.key.intent},{e.key.variant},{source},{clipPath},{address}");
        }

        File.WriteAllText(path, sb.ToString());
        EditorUtility.DisplayDialog("Export CSV", $"Exported {db.entries.Count} lines to {path}", "OK");
    }
}
#endif