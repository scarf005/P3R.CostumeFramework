using P3R.CostumeFramework.Costumes.Models;
using P3R.CostumeFramework.Hooks.Models;
using IUnrealObjects = UE.Toolkit.Interfaces.IUnrealObjects;
using Unreal.ObjectsEmitter.Interfaces;
using Unreal.ObjectsEmitter.Interfaces.Types;

namespace P3R.CostumeFramework.Hooks.Services;

internal sealed unsafe class CostumeResultService
{
    private const string TableName = "DT_BtlResultLS";

    private readonly IUnreal unreal;
    private readonly CostumeManager manager;

    private readonly LiveDataTables<FBtlResultSequence> tables;

    private int loadCount;

    public CostumeResultService(IDataTables dt, IUnrealObjects objects, IUnreal unreal, CostumeManager manager, CostumeHooks hooks)
    {
        this.unreal = unreal;
        this.manager = manager;

        this.tables = new(dt, objects, TableName, this.OnTableLoaded);
        hooks.OnCostumeChanged += _ => this.ApplyAll();
    }

    private void OnTableLoaded(DataTable<FBtlResultSequence> loaded)
    {
        Log.Information(
            $"Result LS table loaded #{++this.loadCount}: {loaded.Rows.Length} rows " +
            $"[{string.Join(", ", loaded.Rows.Select(r => r.Name))}]");

        this.ApplyAll();
    }

    private void ApplyAll()
    {
        this.tables.ForEach((table, vanilla) =>
        {
            RestoreVanilla(table, vanilla);

            foreach (var costume in this.manager.GetCurrentCostumes())
            {
                this.Apply(table, costume);
            }
        });
    }

    private static void RestoreVanilla(DataTable<FBtlResultSequence> table, IReadOnlyDictionary<string, FBtlResultSequence> vanilla)
    {
        foreach (var row in table.Rows)
        {
            if (vanilla.TryGetValue(row.Name, out var original))
            {
                *row.Self = original;
                row.Self->Sequencer.baseObj.baseObj.WeakPtr = new();
            }
        }
    }

    private void Apply(DataTable<FBtlResultSequence> table, Costume costume)
    {
        var lsPath = costume.Config.Result.LsPath;
        if (string.IsNullOrEmpty(lsPath))
        {
            return;
        }

        var rowName = $"pc_{(int)costume.Character}";
        var row = table.Rows.FirstOrDefault(x => x.Name.Equals(rowName, StringComparison.OrdinalIgnoreCase));
        if (row == null)
        {
            return;
        }

        var unrealPath = AssetUtils.GetUnrealAssetPath(lsPath);
        var ptr = row.Self;
        ptr->Sequencer.baseObj.baseObj.ObjectId.AssetPathName = *this.unreal.FName(unrealPath);
        ptr->Sequencer.baseObj.baseObj.WeakPtr = new();

        Log.Debug($"Result LS [{rowName}] set to {unrealPath} ({costume.Character} || {costume.Name}).");
    }
}