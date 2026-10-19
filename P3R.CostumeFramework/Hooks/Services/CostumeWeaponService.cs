using P3R.CostumeFramework.Costumes.Models;
using IUnrealObjects = UE.Toolkit.Interfaces.IUnrealObjects;
using Unreal.ObjectsEmitter.Interfaces;

namespace P3R.CostumeFramework.Hooks.Services;

internal sealed class CostumeWeaponService
{
    private readonly BoolTable boolTable;
    private readonly PathTable pathTable;

    public CostumeWeaponService(IDataTables dt, IUnrealObjects objects, IUnreal unreal, CostumeManager manager, CostumeHooks hooks)
    {
        this.boolTable = new BoolTable(dt, objects, unreal, manager, hooks);
        this.pathTable = new PathTable(dt, objects, unreal, manager, hooks);
    }

    private static string? ResolveWeaponMeshPath(Costume costume, string rowName)
    {
        var path = costume.Config.Weapon.MeshPath;
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
        if (!string.Equals(fileName, "weapon-mesh", StringComparison.OrdinalIgnoreCase))
        {
            Log.Error(
                $"[CostumeFramework] Validation Failed: The weapon mesh configuration for " +
                $"'{costume.Config.Name ?? rowName}' points to a file named '{fileName}'. " +
                "The file MUST be named 'weapon-mesh'. Defaulting to base weapon.");
            return null;
        }

        return path;
    }

    private sealed class BoolTable : PoseTableServiceBase
    {
        public BoolTable(IDataTables dt, IUnrealObjects objects, IUnreal unreal, CostumeManager manager, CostumeHooks hooks)
            : base(dt, objects, unreal, manager, hooks, "WeaponBool", "WeaponBool") { }

        protected override string GetPoseValue(Costume costume, string rowName)
            => ResolveWeaponMeshPath(costume, rowName) != null ? "True" : "False";
    }

    private sealed class PathTable : PoseTableServiceBase
    {
        public PathTable(IDataTables dt, IUnrealObjects objects, IUnreal unreal, CostumeManager manager, CostumeHooks hooks)
            : base(dt, objects, unreal, manager, hooks, "WeaponPath", "WeaponPath") { }

        protected override string? GetPoseValue(Costume costume, string rowName)
        {
            var resolved = ResolveWeaponMeshPath(costume, rowName);
            return resolved != null ? AssetUtils.GetUnrealAssetPath(resolved) : null;
        }
    }
}
