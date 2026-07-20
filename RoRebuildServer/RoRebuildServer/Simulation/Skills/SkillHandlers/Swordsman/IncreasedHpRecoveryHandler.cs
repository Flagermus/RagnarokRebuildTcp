using RebuildSharedData.Data;
using RebuildSharedData.Enum;
using RebuildSharedData.Enum.EntityStats;
using RoRebuildServer.EntityComponents;

namespace RoRebuildServer.Simulation.Skills.SkillHandlers.Swordsman;

[SkillHandler(CharacterSkill.IncreasedHPRecovery, SkillClass.Physical, SkillTarget.Passive)]
public class IncreasedHpRecoveryHandler : SkillHandlerBase
{
    public override void ApplyPassiveEffects(CombatEntity owner, int lvl)
    {
        //don't add AddHpRecoveryPercent here, it's calculated in a special way in Player.HpRegenTick() and has a unique visual
        //item heal bonus is +20% at Lv2 and Lv3 (flat, not stacking within this skill)
        var itemBonus = lvl >= 2 ? 20 : 0;
        owner.AddStat(CharacterStat.AddHpItemEffectivenessPercent, itemBonus);
    }

    public override void RemovePassiveEffects(CombatEntity owner, int lvl)
    {
        var itemBonus = lvl >= 2 ? 20 : 0;
        owner.SubStat(CharacterStat.AddHpItemEffectivenessPercent, itemBonus);
    }

    public override void Process(CombatEntity source, CombatEntity? target, Position position, int lvl, bool isIndirect, bool isItemSource)
    {
        throw new NotImplementedException();
    }
}
