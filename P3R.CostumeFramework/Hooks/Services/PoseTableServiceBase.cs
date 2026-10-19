using P3R.CostumeFramework.Costumes.Models;
using P3R.CostumeFramework.Hooks.Models;
using IUnrealObjects = UE.Toolkit.Interfaces.IUnrealObjects;
using Unreal.ObjectsEmitter.Interfaces;
using Unreal.ObjectsEmitter.Interfaces.Types;

namespace P3R.CostumeFramework.Hooks.Services;
internal abstract unsafe class PoseTableServiceBase
{
    protected readonly IUnreal Unreal;
    protected readonly CostumeManager Manager;

    private readonly string label;
    private readonly List<Costume> pendingCostumes = [];
    private readonly LiveDataTables<FBustupParamTable> tables;
    private bool loaded;

    protected PoseTableServiceBase(
        IDataTables dt,
        IUnrealObjects objects,
        IUnreal unreal,
        CostumeManager manager,
        CostumeHooks hooks,
        string tableName,
        string label)
    {
        this.Unreal = unreal;
        this.Manager = manager;
        this.label = label;

        this.tables = new(dt, objects, tableName, this.OnTableLoaded);
        hooks.OnCostumeChanged += this.RefreshForCostume;
    }
    protected abstract string? GetPoseValue(Costume costume, string rowName);
    protected virtual bool ShouldApply(Costume costume) => true;
    protected virtual void OnApplied(Costume costume, FBustupParamTable* row, string rowName) { }

    private void OnTableLoaded(DataTable<FBustupParamTable> loadedTable)
    {
        this.loaded = true;

        this.pendingCostumes.AddRange(this.Manager.GetCurrentCostumes());
        this.ApplyPendingCostumes();
    }

    private void RefreshForCostume(Costume costume)
    {
        if (!this.loaded)
        {
            Log.Debug($"{this.label} table not yet loaded; queueing refresh.");
            if (!this.pendingCostumes.Contains(costume))
            {
                this.pendingCostumes.Add(costume);
            }
            return;
        }

        this.Apply(costume);
    }

    private void ApplyPendingCostumes()
    {
        if (!this.loaded || this.pendingCostumes.Count == 0)
        {
            return;
        }

        foreach (var costume in this.pendingCostumes.ToArray())
        {
            this.Apply(costume);
        }

        this.pendingCostumes.Clear();
    }

    private void Apply(Costume costume)
    {
        if (!this.ShouldApply(costume))
        {
            return;
        }

        this.tables.ForEach((table, _) => this.Apply(table, costume));
    }

    private void Apply(DataTable<FBustupParamTable> table, Costume costume)
    {
        var rowName = $"PC{(int)costume.Character}";
        var row = table.Rows.FirstOrDefault(x => x.Name == rowName);
        if (row == null)
        {
            Log.Debug($"{this.label} row not found for {rowName}.");
            return;
        }

        var rowPtr = row.Self;

        var poseValue = this.GetPoseValue(costume, rowName);
        if (poseValue != null)
        {
            rowPtr->Pose = this.Unreal.FString(poseValue);
            Log.Debug($"Set {this.label} for {rowName} to {poseValue}.");
        }

        this.OnApplied(costume, rowPtr, rowName);
    }
}
