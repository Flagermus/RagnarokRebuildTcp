using RebuildSharedData.Data;
using RebuildSharedData.Enum;
using RoRebuildServer.EntityComponents;
using RoRebuildServer.Networking;
using RoRebuildServer.Simulation.StatusEffects.Setup;

namespace RoRebuildServer.Simulation.Skills.SkillHandlers.Swordsman;

[SkillHandler(CharacterSkill.Endure, SkillClass.None, SkillTarget.Self)]
public class EndureHandler : SkillHandlerBase
{
    public override void Process(CombatEntity source, CombatEntity? target, Position position, int lvl, bool isIndirect,
        bool isItemSource)
    {
        var status = StatusEffectState.NewStatusEffect(CharacterStatusEffect.Endure, 4f);
        source.AddStatusEffect(status);

        if (!isIndirect)
        {
            source.ApplyCooldownForSupportSkillAction();
            if (source.Character.Type == CharacterType.Player)
                source.Player.SetSkillSpecificCooldown(CharacterSkill.Endure, 10f);
        }

        CommandBuilder.SkillExecuteSelfTargetedSkillAutoVis(source.Character, CharacterSkill.Endure, lvl, isIndirect);
    }
}
