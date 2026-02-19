// Assets/Scripts/AnimationSystem/Editor/AnimationDatabaseCsvImporter.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class AnimationDatabaseCsvImporter
{
    private const char Sep = ',';

    [MenuItem("Tools/Animation/Import CSV → AnimationDatabase (Asset/Addressables)")]
    public static void ImportCsvToSelectedDatabase()
    {
        var db = Selection.activeObject as AnimationDatabase;
        if (db == null)
        {
            EditorUtility.DisplayDialog("Import CSV", "Select an AnimationDatabase asset first.", "OK");
            return;
        }

        string csvPath = EditorUtility.OpenFilePanel("Select CSV file", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(csvPath)) return;

        var lines = File.ReadAllLines(csvPath);
        if (lines.Length < 2)
        {
            EditorUtility.DisplayDialog("Import CSV", "CSV must contain header + at least 1 row.", "OK");
            return;
        }

        var header = lines[0].Split(Sep).Select(x => x.Trim()).ToArray();

        int idxChar = FindCol(header, "CharacterId");
        int idxIntent = FindCol(header, "Intent");
        int idxVariant = FindCol(header, "Variant");
        int idxSource = FindCol(header, "Source");
        int idxClipPath = FindCol(header, "ClipPath");
        int idxAddressKey = FindCol(header, "AddressKey");

        if (idxChar < 0 || idxIntent < 0 || idxVariant < 0 || idxSource < 0 || idxClipPath < 0 || idxAddressKey < 0)
        {
            EditorUtility.DisplayDialog("Import CSV",
                "Missing columns. Required:\nCharacterId,Intent,Variant,Source,ClipPath,AddressKey",
                "OK");
            return;
        }

        var clipNameLookup = BuildClipNameLookup();

        var entries = new List<AnimationEntry>();
        var errors = new List<string>();
        var warnings = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (int line = 1; line < lines.Length; line++)
        {
            var raw = lines[line];
            if (string.IsNullOrWhiteSpace(raw)) continue;

            var cols = raw.Split(Sep).Select(x => x.Trim()).ToArray();

            string charId = Get(cols, idxChar);
            string intent = Get(cols, idxIntent);
            string variant = Get(cols, idxVariant);
            string source = Get(cols, idxSource);
            string clipPath = Get(cols, idxClipPath);
            string addressKey = Get(cols, idxAddressKey);

            if (string.IsNullOrWhiteSpace(charId) || string.IsNullOrWhiteSpace(intent))
            {
                errors.Add($"Line {line + 1}: CharacterId/Intent empty.");
                continue;
            }

            var key = new AnimationKey(charId, intent, variant);
            if (!key.IsValid)
            {
                errors.Add($"Line {line + 1}: invalid key.");
                continue;
            }

            string id = key.ToId();
            if (!seen.Add(id))
                warnings.Add($"Line {line + 1}: duplicate key {id} (later overrides earlier).");

            var clipRef = new AnimationClipRef();

            if (string.Equals(source, "Asset", StringComparison.OrdinalIgnoreCase))
            {
                clipRef.source = AnimationClipSource.Asset;

                AnimationClip clip = null;

                if (!string.IsNullOrWhiteSpace(clipPath))
                {
                    clipPath = NormalizeToAssetsPath(clipPath);
                    clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                    if (clip == null)
                    {
                        errors.Add($"Line {line + 1}: ClipPath not found: {clipPath}");
                        continue;
                    }
                }
                else
                {
                    string tryName = string.IsNullOrEmpty(variant) ? $"{charId}_{intent}" : $"{charId}_{intent}_{variant}";
                    if (clipNameLookup.TryGetValue(tryName, out var resolved))
                    {
                        clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(resolved);
                    }
                    else
                    {
                        errors.Add($"Line {line + 1}: ClipPath empty and cannot resolve by name: {tryName}");
                        continue;
                    }
                }

                clipRef.clip = clip;
                clipRef.addressKey = null;
            }
            else if (string.Equals(source, "Addressable", StringComparison.OrdinalIgnoreCase))
            {
                clipRef.source = AnimationClipSource.Addressable;

                if (string.IsNullOrWhiteSpace(addressKey))
                {
                    errors.Add($"Line {line + 1}: Source=Addressable but AddressKey is empty.");
                    continue;
                }

                clipRef.clip = null;
                clipRef.addressKey = addressKey.Trim();
            }
            else
            {
                errors.Add($"Line {line + 1}: Unknown Source='{source}'. Must be Asset or Addressable.");
                continue;
            }

            entries.Add(new AnimationEntry(key, clipRef));
        }

        if (errors.Count > 0)
        {
            string msg = $"Import aborted: {errors.Count} errors.\n\n" + string.Join("\n", errors.Take(30));
            Debug.LogError(msg);
            EditorUtility.DisplayDialog("Import CSV - Errors", msg, "OK");
            return;
        }

        db.SetEntries(entries);
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        string summary = $"Imported {entries.Count} entries.\nWarnings: {warnings.Count}\n";
        if (warnings.Count > 0) summary += "\n" + string.Join("\n", warnings.Take(50));
        Debug.Log(summary);
        EditorUtility.DisplayDialog("Import CSV - Done", summary, "OK");
    }

    private static int FindCol(string[] header, string name)
    {
        for (int i = 0; i < header.Length; i++)
            if (string.Equals(header[i], name, StringComparison.OrdinalIgnoreCase))
                return i;
        return -1;
    }

    private static string Get(string[] cols, int idx) => idx < 0 || idx >= cols.Length ? "" : cols[idx] ?? "";

    private static string NormalizeToAssetsPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return path;
        path = path.Trim();
        int assetsIdx = path.IndexOf("Assets", System.StringComparison.Ordinal);
        if (assetsIdx >= 0) return path.Substring(assetsIdx);
        return path;
    }

    private static Dictionary<string, string> BuildClipNameLookup()
    {
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        var guids = AssetDatabase.FindAssets("t:AnimationClip");
        foreach (var g in guids)
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(p);
            if (clip == null) continue;
            if (!dict.ContainsKey(clip.name))
                dict.Add(clip.name, p);
        }
        return dict;
    }
}
#endif