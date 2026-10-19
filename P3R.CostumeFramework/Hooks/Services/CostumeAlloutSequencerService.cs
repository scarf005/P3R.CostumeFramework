using P3R.CostumeFramework.Costumes.Models;
using P3R.CostumeFramework.Hooks.Models;
using IUnrealObjects = UE.Toolkit.Interfaces.IUnrealObjects;
using Unreal.ObjectsEmitter.Interfaces;
using Unreal.ObjectsEmitter.Interfaces.Types;

namespace P3R.CostumeFramework.Hooks.Services;

internal sealed unsafe class CostumeAlloutSequencerService
{
    private const string TableName = "DT_BtlAlloutSequencer";

    private readonly IUnreal unreal;
    private readonly CostumeManager manager;

    private readonly LiveDataTables<FBtlAlloutSequencer> tables;

    private int loadCount;

    public CostumeAlloutSequencerService(IDataTables dt, IUnrealObjects objects, IUnreal unreal, CostumeManager manager, CostumeHooks hooks)
    {
        this.unreal = unreal;
        this.manager = manager;

        this.tables = new(dt, objects, TableName, this.OnTableLoaded);
        hooks.OnCostumeChanged += _ => this.ApplyAll();
    }

    private void OnTableLoaded(DataTable<FBtlAlloutSequencer> loaded)
    {
        Log.Information(
            $"Allout sequencer table loaded #{++this.loadCount}: {loaded.Rows.Length} rows " +
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

    private static void RestoreVanilla(DataTable<FBtlAlloutSequencer> table, IReadOnlyDictionary<string, FBtlAlloutSequencer> vanilla)
    {
        foreach (var row in table.Rows)
        {
            if (vanilla.TryGetValue(row.Name, out var original))
            {
                *row.Self = original;
                row.Self->AlloutA.baseObj.baseObj.WeakPtr = new();
                row.Self->AlloutB.baseObj.baseObj.WeakPtr = new();
            }
        }
    }

    private void Apply(DataTable<FBtlAlloutSequencer> table, Costume costume)
    {
        var aPath = costume.Config.Allout.AoalsA;
        var bPath = costume.Config.Allout.AoalsB;
        if (string.IsNullOrEmpty(aPath) && string.IsNullOrEmpty(bPath))
        {
            return;
        }

        var rowName = $"pc_{(int)costume.Character}";
        var row = table.Rows.FirstOrDefault(x => x.Name.Equals(rowName, StringComparison.OrdinalIgnoreCase));
        if (row == null)
        {
            return;
        }

        var ptr = row.Self;

        if (!string.IsNullOrEmpty(aPath))
        {
            this.SetSequence(&ptr->AlloutA, AssetUtils.GetUnrealAssetPath(aPath));
            Log.Debug($"Allout sequencer [{rowName}] A set to {aPath} ({costume.Character} || {costume.Name}).");
        }

        if (!string.IsNullOrEmpty(bPath))
        {
            this.SetSequence(&ptr->AlloutB, AssetUtils.GetUnrealAssetPath(bPath));
            Log.Debug($"Allout sequencer [{rowName}] B set to {bPath} ({costume.Character} || {costume.Name}).");
        }
    }

    private void SetSequence(TSoftObjectPtr<ULevelSequence>* seq, string unrealPath)
    {
        seq->baseObj.baseObj.ObjectId.AssetPathName = *this.unreal.FName(unrealPath);
        seq->baseObj.baseObj.WeakPtr = new();
    }
}
