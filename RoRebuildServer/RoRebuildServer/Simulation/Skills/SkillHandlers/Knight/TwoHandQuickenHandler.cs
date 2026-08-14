using RebuildSharedData.Data;
using RebuildSharedData.Enum;
using RoRebuildServer.EntityComponents;
using RoRebuildServer.Networking;
using RoRebuildServer.Simulation.StatusEffects.Setup;

namespace RoRebuildServer.Simulation.Skills.SkillHandlers.Knight;

[SkillHandler(CharacterSkill.TwoHandQuicken, SkillClass.Unique, SkillTarget.Self)]
public class TwoHandQuickenHandler : SkillHandlerBase
{
    public override SkillValidationResult ValidateTarget(CombatEntity source, CombatEntity? target, Position position,
        int lvl, bool isIndirect, bool isItemSource)
    {
        if (source.Character.Type == CharacterType.Player && source.Player.MainWeaponClass != (int)WeaponClass.TwoHandSword)
            return SkillValidationResult.IncorrectWeapon;
        return base.ValidateTarget(source, target, position, lvl, false, false);
    }

    public override bool ShouldSkillCostSp(CombatEntity source)
    {
        return !source.HasStatusEffectOfType(CharacterStatusEffect.TwoHandQuicken);
    }

    public override void Process(CombatEntity source, CombatEntity? target, Position position, int lvl, bool isIndirect,
        bool isItemSource)
    {
        source.ApplyCooldownForSupportSkillAction();

        CommandBuilder.SkillExecuteSelfTargetedSkillAutoVis(source.Character, CharacterSkill.TwoHandQuicken, lvl, isIndirect);

        if (source.HasStatusEffectOfType(CharacterStatusEffect.TwoHandQuicken))
        {
            source.RemoveStatusOfTypeIfExists(CharacterStatusEffect.TwoHandQuicken);
            source.UpdateStats(); //RemoveStatusEffectOfType doesn't re-run UpdateStats; recompute ASPD so the buff actually reverts
            return;
        }

        // Value1: ASPD bonus (always 20). Value2: hit-charge counter (managed in OnAttack).
        // Value3: skill level (short).
        var status = StatusEffectState.NewStatusEffect(
            CharacterStatusEffect.TwoHandQuicken,
            int.MaxValue / 1000f, //effectively permanent; client hides timers > 24h (same convention as PecoRiding)
            val1: 20,
            val2: 0,
            val3: (short)lvl
        );
        source.AddStatusEffect(status);
    }
}