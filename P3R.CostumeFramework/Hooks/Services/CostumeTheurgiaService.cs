using System.Globalization;
using System.Xml.Linq;
using P3R.CostumeFramework.Costumes.Models;
using P3R.CostumeFramework.Hooks.Models;
using IUnrealObjects = UE.Toolkit.Interfaces.IUnrealObjects;
using Unreal.ObjectsEmitter.Interfaces;
using Unreal.ObjectsEmitter.Interfaces.Types;

namespace P3R.CostumeFramework.Hooks.Services;
internal sealed unsafe class CostumeTheurgiaService
{
    private const string TableName = "DT_BtlTheurgiaSeqList";

    private readonly IUnreal unreal;
    private readonly CostumeManager manager;

    private readonly LiveDataTables<FBtlTheurgiaSeq> tables;

    // Cache :glool:
    private readonly Dictionary<string, List<TheurgiaItem>> fileCache = new(StringComparer.OrdinalIgnoreCase);

    public CostumeTheurgiaService(IDataTables dt, IUnrealObjects objects, IUnreal unreal, CostumeManager manager, CostumeHooks hooks)
    {
        this.unreal = unreal;
        this.manager = manager;

        this.tables = new(dt, objects, TableName, this.OnTableLoaded);
        hooks.OnCostumeChanged += _ => this.ApplyAll();
    }

    private void OnTableLoaded(DataTable<FBtlTheurgiaSeq> loaded)
    {
        Log.Information($"Theurgia table loaded: snapshotted {loaded.Rows.Length} vanilla rows.");
        this.ApplyAll();
    }

    private void ApplyAll()
    {
        this.tables.ForEach((table, vanilla) =>
        {
            RestoreVanilla(table, vanilla);

            foreach (var costume in this.manager.GetCurrentCostumes())
            {
                foreach (var file in costume.TheurgiaFiles)
                {
                    foreach (var item in this.GetItems(file))
                    {
                        this.ApplyItem(table, costume, item);
                    }
                }
            }
        });
    }

    private static void RestoreVanilla(DataTable<FBtlTheurgiaSeq> table, IReadOnlyDictionary<string, FBtlTheurgiaSeq> vanilla)
    {
        foreach (var row in table.Rows)
        {
            if (vanilla.TryGetValue(row.Name, out var original))
            {
                *row.Self = original;
                // It crashes on soft object references if I don't do this so
                ResetWeakPtrs(row.Self);
            }
        }
    }

    private void ApplyItem(DataTable<FBtlTheurgiaSeq> table, Costume costume, TheurgiaItem item)
    {
        var row = table.Rows.FirstOrDefault(x => x.Name == item.RowId);
        if (row == null)
        {
            Log.Warning($"Theurgia row not found: {item.RowId} (from {costume.Character} || {costume.Name}).");
            return;
        }

        var ptr = row.Self;

        if (item.Sequence != null) this.SetSequence(&ptr->Sequence, item.Sequence);
        if (item.SequenceEvolved != null) this.SetSequence(&ptr->SequenceEvolved, item.SequenceEvolved);
        if (item.SequenceSound != null) this.SetSequence(&ptr->SequenceSound, item.SequenceSound);

        if (item.PersonaScaleA is { } a)
        {
            ptr->PersonaScaleA.X = a.X;
            ptr->PersonaScaleA.Y = a.Y;
            ptr->PersonaScaleA.Z = a.Z;
        }

        if (item.PersonaScaleB is { } b)
        {
            ptr->PersonaScaleB.X = b.X;
            ptr->PersonaScaleB.Y = b.Y;
            ptr->PersonaScaleB.Z = b.Z;
        }

        if (item.PersonaLoopAnimType.HasValue) ptr->PersonaLoopAnimType = item.PersonaLoopAnimType.Value;
        if (item.NeedCommonSkillSceneFromTheurgia.HasValue) ptr->NeedCommonSkillSceneFromTheurgia = item.NeedCommonSkillSceneFromTheurgia.Value;
        if (item.TheurgiaVoiceIndex.HasValue) ptr->TheurgiaVoiceIndex = item.TheurgiaVoiceIndex.Value;
        if (item.DisableLOD.HasValue) ptr->DisableLOD = item.DisableLOD.Value;

        Log.Debug($"Theurgia row {item.RowId} overridden by {costume.Character} || {costume.Name}.");
    }

    private void SetSequence(TSoftObjectPtr<ULevelSequence>* seq, string value)
    {
        seq->baseObj.baseObj.ObjectId.AssetPathName = *this.unreal.FName(value);
        seq->baseObj.baseObj.WeakPtr = new();
    }

    private static void ResetWeakPtrs(FBtlTheurgiaSeq* row)
    {
        row->Sequence.baseObj.baseObj.WeakPtr = new();
        row->SequenceEvolved.baseObj.baseObj.WeakPtr = new();
        row->SequenceSound.baseObj.baseObj.WeakPtr = new();
    }

    private List<TheurgiaItem> GetItems(string file)
    {
        if (this.fileCache.TryGetValue(file, out var cached))
        {
            return cached;
        }

        var items = ParseFile(file);
        this.fileCache[file] = items;
        return items;
    }

    private static List<TheurgiaItem> ParseFile(string file)
    {
        var items = new List<TheurgiaItem>();
        try
        {
            var root = XDocument.Load(file).Root;
            if (root == null)
            {
                return items;
            }

            foreach (var itemEl in root.Elements("Item"))
            {
                var id = ((string?)itemEl.Attribute("id"))?.Trim();
                if (string.IsNullOrEmpty(id))
                {
                    Log.Warning($"Theurgia item missing 'id' attribute in {file}; skipped.");
                    continue;
                }

                var item = new TheurgiaItem
                {
                    RowId = id,
                    Sequence = ReadValue(itemEl, "Sequence"),
                    SequenceEvolved = ReadValue(itemEl, "SequenceEvolved"),
                    SequenceSound = ReadValue(itemEl, "SequenceSound"),
                    PersonaScaleA = ReadVector(itemEl, "PersonaScaleA"),
                    PersonaScaleB = ReadVector(itemEl, "PersonaScaleB"),
                };

                var loop = ReadValue(itemEl, "PersonaLoopAnimType");
                if (loop != null && TryParseAnim(loop, out var anim)) item.PersonaLoopAnimType = anim;

                var need = ReadValue(itemEl, "NeedCommonSkillSceneFromTheurgia");
                if (need != null && bool.TryParse(need, out var nb)) item.NeedCommonSkillSceneFromTheurgia = nb;

                var voice = ReadValue(itemEl, "TheurgiaVoiceIndex");
                if (voice != null && int.TryParse(voice, NumberStyles.Integer, CultureInfo.InvariantCulture, out var vi)) item.TheurgiaVoiceIndex = vi;

                var lod = ReadValue(itemEl, "DisableLOD");
                if (lod != null && bool.TryParse(lod, out var lb)) item.DisableLOD = lb;

                items.Add(item);
            }

            Log.Information($"Parsed {items.Count} theurgia override(s) from {Path.GetFileName(file)}.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to parse theurgia file: {file}");
        }

        return items;
    }

    private static string? ReadValue(XElement parent, string name)
    {
        var el = parent.Element(name);
        return el == null ? null : ((string?)el.Attribute("value"))?.Trim();
    }

    private static (float X, float Y, float Z)? ReadVector(XElement parent, string name)
    {
        var el = parent.Element(name);
        if (el == null)
        {
            return null;
        }

        float Comp(string axis)
        {
            var raw = (string?)el.Element(axis)?.Attribute("value");
            return raw != null && float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : 1f;
        }

        return (Comp("X"), Comp("Y"), Comp("Z"));
    }

    private static bool TryParseAnim(string value, out EBtlPersonaSceneAnimationType anim)
    {
        var name = value;
        var sep = name.LastIndexOf("::", StringComparison.Ordinal);
        if (sep >= 0)
        {
            name = name[(sep + 2)..];
        }

        if (Enum.TryParse(name, true, out anim))
        {
            return true;
        }

        Log.Warning($"Unknown PersonaLoopAnimType '{value}'; leaving vanilla value.");
        return false;
    }

    private sealed class TheurgiaItem
    {
        public string RowId = string.Empty;
        public string? Sequence;
        public string? SequenceEvolved;
        public string? SequenceSound;
        public (float X, float Y, float Z)? PersonaScaleA;
        public (float X, float Y, float Z)? PersonaScaleB;
        public EBtlPersonaSceneAnimationType? PersonaLoopAnimType;
        public bool? NeedCommonSkillSceneFromTheurgia;
        public int? TheurgiaVoiceIndex;
        public bool? DisableLOD;
    }
}
