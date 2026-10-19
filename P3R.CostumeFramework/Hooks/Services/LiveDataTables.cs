using IUnrealObjects = UE.Toolkit.Interfaces.IUnrealObjects;
using Unreal.ObjectsEmitter.Interfaces;
using Unreal.ObjectsEmitter.Interfaces.Types;

namespace P3R.CostumeFramework.Hooks.Services;

/// <summary>
/// Live instances of a named data table, each with its rows as first loaded.
/// Row pointers die with their table, so an instance is replaced when it reloads and dropped on
/// <c>UObject::BeginDestroy</c>; <see cref="ForEach"/> holds destruction off while it writes rows.
/// </summary>
internal sealed unsafe class LiveDataTables<TRow> where TRow : unmanaged
{
    private readonly Dictionary<nint, (DataTable<TRow> Table, Dictionary<string, TRow> Vanilla)> tables = [];

    public LiveDataTables(IDataTables dt, IUnrealObjects objects, string name, Action<DataTable<TRow>> loaded)
    {
        dt.FindDataTable<TRow>(name, table =>
        {
            var vanilla = table.Rows.ToDictionary(x => x.Name, x => *x.Self, StringComparer.OrdinalIgnoreCase);
            lock (this.tables) this.tables[(nint)table.Self] = (table, vanilla);
            loaded(table);
        });

        objects.OnObjectBeginDestroy += obj =>
        {
            lock (this.tables) this.tables.Remove((nint)obj.Self);
        };
    }

    public void ForEach(Action<DataTable<TRow>, IReadOnlyDictionary<string, TRow>> action)
    {
        lock (this.tables)
        {
            foreach (var (table, vanilla) in this.tables.Values)
            {
                action(table, vanilla);
            }
        }
    }
}
