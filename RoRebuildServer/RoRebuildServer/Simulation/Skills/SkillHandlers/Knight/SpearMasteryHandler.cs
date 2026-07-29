using RebuildSharedData.Data;
using RebuildSharedData.Enum;
using RoRebuildServer.EntityComponents;

namespace RoRebuildServer.Simulation.Skills.SkillHandlers.Knight;

[SkillHandler(CharacterSkill.SpearMastery, SkillClass.None, SkillTarget.Passive)]
public class SpearMasteryHandler : SkillHandlerBase
{
    //spear-specific range and Hard DEF ignore bonuses are handled directly in Player.cs (range)
    //and CombatEntity.DamageHandling.cs (Hard DEF ignore), not here.

    public override void Process(CombatEntity source, CombatEntity? target, Position position, int lvl, bool isIndirect,
        bool isItemSource)
    {
        throw new NotImplementedException();
    }
}
