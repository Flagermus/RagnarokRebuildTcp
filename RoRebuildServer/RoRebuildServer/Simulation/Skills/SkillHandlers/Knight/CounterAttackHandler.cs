using RebuildSharedData.Data;
using RebuildSharedData.Enum;
using RoRebuildServer.EntityComponents;
using RoRebuildServer.Networking;
using RoRebuildServer.Simulation.StatusEffects.Setup;

namespace RoRebuildServer.Simulation.Skills.SkillHandlers.Knight;

[SkillHandler(CharacterSkill.CounterAttack, SkillClass.None, SkillTarget.Self)]
public class CounterAttackHandler : SkillHandlerBase
{
    public override void Process(CombatEntity source, CombatEntity? target, Position position, int lvl, bool isIndirect, bool isItemSource)
    {
        var status = StatusEffectState.NewStatusEffect(CharacterStatusEffect.CounterAttack, 0.5f);
        source.AddStatusEffect(status);

        if (!isIndirect)
        {
            source.ApplyCooldownForSupportSkillAction();
            if (source.Character.Type == CharacterType.Player)
                source.Player.SetSkillSpecificCooldown(CharacterSkill.CounterAttack, 5f);
        }

        CommandBuilder.SkillExecuteSelfTargetedSkillAutoVis(source.Character, CharacterSkill.CounterAttack, lvl, isIndirect);
    }
}
