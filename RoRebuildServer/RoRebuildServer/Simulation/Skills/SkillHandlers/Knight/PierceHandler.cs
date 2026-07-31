using RebuildSharedData.Data;
using RebuildSharedData.Enum;
using RebuildSharedData.Enum.EntityStats;
using RoRebuildServer.EntityComponents;
using RoRebuildServer.EntityComponents.Util;
using RoRebuildServer.Networking;
using RoRebuildServer.Simulation.Util;

namespace RoRebuildServer.Simulation.Skills.SkillHandlers.Knight;

[SkillHandler(CharacterSkill.Pierce, SkillClass.Physical)]
public class PierceHandler : SkillHandlerBase
{
    public override SkillValidationResult ValidateTarget(CombatEntity source, CombatEntity? target, Position position, int lvl,
        bool isIndirect, bool isItemSource)
    {
        if (source.Character.Type == CharacterType.Player && (source.Player.MainWeaponClass < (int)WeaponClass.Spear || source.Player.MainWeaponClass > (int)WeaponClass.TwoHandSpear))
            return SkillValidationResult.IncorrectWeapon; //spear only

        return base.ValidateTarget(source, target, position, lvl, isIndirect, isItemSource);
    }

    public override void Process(CombatEntity source, CombatEntity? target, Position position, int lvl, bool isIndirect, bool isItemSource)
    {
        lvl = lvl.Clamp(1, 1);

        if (target == null || !target.IsValidTarget(source))
            return;

        var hasMomentum = source.HasStatusEffectOfType(CharacterStatusEffect.Momentum);
        var targetStunned = target.HasStatusEffectOfType(CharacterStatusEffect.Stun);

        // Base = 3 hits * 1.5 = 4.5. Bonuses are additive percentages of that base:
        // Momentum +50% of base, Stun +30% of base. Final strike multiplier:
        // 450% / 675% (Momentum) / 585% (Stun) / 810% (both).
        var totalMultiplier = 4.5f;
        if (hasMomentum) totalMultiplier += 4.5f * 0.5f;   // +50% of base 450%
        if (targetStunned) totalMultiplier += 4.5f * 0.3f; // +30% of base 450%

        if (hasMomentum)
            source.StatusContainer!.RemoveStatusEffectOfType(CharacterStatusEffect.Momentum);

        var perHit = totalMultiplier / 3f;
        var req = new AttackRequest(CharacterSkill.Pierce, perHit, 3, AttackFlags.Physical, AttackElement.None);
        var res = source.CalculateCombatResult(target, req);

        source.ApplyCooldownForAttackAction(target);
        if (source.Character.Type == CharacterType.Player)
            source.Player.SetSkillSpecificCooldown(CharacterSkill.Pierce, 3f);

        source.ExecuteCombatResult(res, false);

        CommandBuilder.SkillExecuteTargetedSkillAutoVis(source.Character, target.Character, CharacterSkill.Pierce, 1, res);
    }
}
