using P3R.CostumeFramework.Costumes.Models;
using IUnrealObjects = UE.Toolkit.Interfaces.IUnrealObjects;
using Unreal.ObjectsEmitter.Interfaces;

namespace P3R.CostumeFramework.Hooks.Services;

internal sealed class CostumeMontageService : PoseTableServiceBase
{
    public CostumeMontageService(IDataTables dt, IUnrealObjects objects, IUnreal unreal, CostumeManager manager, CostumeHooks hooks)
        : base(dt, objects, unreal, manager, hooks, "Anim_Montage", "montage") { }

    protected override string GetPoseValue(Costume costume, string rowName)
    {
        var charId = AssetUtils.GetCharIdStringShort(costume.Character);
        var configured = costume.Config.Animation.AnimMontage;

        if (configured != null)
        {
            var expected = $"AM_BtlPc{charId}";
            var actual = System.IO.Path.GetFileNameWithoutExtension(configured);
            if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
            {
                Log.Error(
                    $"[CostumeMontageService] Custom montage name '{actual}' does not match expected '{expected}'. " +
                    "This can sometimes crash.");
            }

            return AssetUtils.GetUnrealAssetPath(configured);
        }

        return $"/Game/Xrd777/Battle/Players/Pc{charId}/AM_BtlPc{charId}.AM_BtlPc{charId}";
    }
}

internal sealed class CostumeSceneMontageService : PoseTableServiceBase
{
    public CostumeSceneMontageService(IDataTables dt, IUnrealObjects objects, IUnreal unreal, CostumeManager manager, CostumeHooks hooks)
        : base(dt, objects, unreal, manager, hooks, "scenemontage", "scene montage") { }

    protected override string GetPoseValue(Costume costume, string rowName)
    {
        if (costume.Config.Animation.SceneMontage != null)
        {
            return AssetUtils.GetUnrealAssetPath(costume.Config.Animation.SceneMontage);
        }

        var charId = AssetUtils.GetCharIdStringShort(costume.Character);
        return $"/Game/Xrd777/Battle/Players/Pc{charId}/AM_BtlPc{charId}_Scene.AM_BtlPc{charId}_Scene";
    }
}

internal sealed class CostumeCritCameraService : PoseTableServiceBase
{
    public CostumeCritCameraService(IDataTables dt, IUnrealObjects objects, IUnreal unreal, CostumeManager manager, CostumeHooks hooks)
        : base(dt, objects, unreal, manager, hooks, "crit", "crit camera") { }

    protected override string GetPoseValue(Costume costume, string rowName)
    {
        if (costume.Config.Animation.CritCamera != null)
        {
            return AssetUtils.GetUnrealAssetPath(costume.Config.Animation.CritCamera);
        }

        var charId = AssetUtils.GetCharIdStringShort(costume.Character);
        return $"/Game/Xrd777/Battle/Critical/LS_Btl_Critical_Pc{charId}.LS_Btl_Critical_Pc{charId}";
    }
}

internal sealed class CostumeCylinderService : PoseTableServiceBase
{
    public CostumeCylinderService(IDataTables dt, IUnrealObjects objects, IUnreal unreal, CostumeManager manager, CostumeHooks hooks)
        : base(dt, objects, unreal, manager, hooks, "CylinderTable", "cylinder table") { }

    protected override string GetPoseValue(Costume costume, string rowName)
    {
        if (costume.Config.Animation.CylinderTable != null)
        {
            return AssetUtils.GetUnrealAssetPath(costume.Config.Animation.CylinderTable);
        }

        var charId = AssetUtils.GetCharIdStringShort(costume.Character);
        return $"/Game/Xrd777/Battle/Players/Pc{charId}/DT_BtlPc{charId}Cylinder.DT_BtlPc{charId}Cylinder";
    }
}

internal sealed class CostumeVisualTableService : PoseTableServiceBase
{
    public CostumeVisualTableService(IDataTables dt, IUnrealObjects objects, IUnreal unreal, CostumeManager manager, CostumeHooks hooks)
        : base(dt, objects, unreal, manager, hooks, "VisualTable", "visual table") { }

    protected override string GetPoseValue(Costume costume, string rowName)
    {
        if (costume.Config.Animation.VisualTable != null)
        {
            return AssetUtils.GetUnrealAssetPath(costume.Config.Animation.VisualTable);
        }

        var charId = AssetUtils.GetCharIdStringShort(costume.Character);
        return $"/Game/Xrd777/Battle/Players/Pc{charId}/DT_BtlPc{charId}CharacterVisual.DT_BtlPc{charId}CharacterVisual";
    }
}
