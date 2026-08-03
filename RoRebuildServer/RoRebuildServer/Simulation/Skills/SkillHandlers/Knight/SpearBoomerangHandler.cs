using RebuildSharedData.Data;
using RebuildSharedData.Enum;
using RoRebuildServer.Data;
using RoRebuildServer.EntityComponents;
using RoRebuildServer.Networking;
using RoRebuildServer.Simulation.Pathfinding;
using RoRebuildServer.Simulation.Util;

namespace RoRebuildServer.Simulation.Skills.SkillHandlers.Knight;

[SkillHandler(CharacterSkill.SpearBoomerang, SkillClass.Physical)]
public class SpearBoomerangHandler : SkillHandlerBase
{
    public override int GetSkillRange(CombatEntity source, int lvl)
    {
        return 13;
    }

    public override SkillValidationResult ValidateTarget(CombatEntity source, CombatEntity? target, Position position, int lvl,
        bool isIndirect, bool isItemSource)
    {
        if (source.Character.Type == CharacterType.Player && (source.Player.MainWeaponClass < (int)WeaponClass.Spear || source.Player.MainWeaponClass > (int)WeaponClass.TwoHandSpear))
            return SkillValidationResult.IncorrectWeapon; //spear only

        return base.ValidateTarget(source, target, position, lvl, isIndirect, isItemSource);
    }

    public override void Process(CombatEntity source, CombatEntity? target, Position position, int lvl,
        bool isIndirect, bool isItemSource)
    {
        lvl = lvl.Clamp(1, 1);

        if (target == null || !target.IsValidTarget(source))
            return;

        var res = source.CalculateCombatResult(target, 3.0f, 1, AttackFlags.Physical | AttackFlags.Ranged, CharacterSkill.SpearBoomerang);
        res.AttackMotionTime = 0.2f; //throw motion is half as long as the attack one
        res.Time = Time.ElapsedTimeFloat + 0.3f + source.Character.Position.DistanceTo(target.Character.Position) / ServerConfig.ArrowTravelTime;
        source.ExecuteCombatResult(res, false);

        if (source.Character.Type == CharacterType.Player)
            source.Player.SetSkillSpecificCooldown(CharacterSkill.SpearBoomerang, 3f);

        var ch = source.Character;

        CommandBuilder.SkillExecuteTargetedSkillAutoVis(source.Character, target.Character, CharacterSkill.SpearBoomerang, lvl, res);
    }
}
