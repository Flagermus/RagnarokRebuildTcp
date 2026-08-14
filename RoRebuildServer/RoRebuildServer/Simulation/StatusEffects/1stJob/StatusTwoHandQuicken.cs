using RebuildSharedData.Data;
using RebuildSharedData.Enum;
using RebuildSharedData.Enum.EntityStats;
using RoRebuildServer.Data;
using RoRebuildServer.EntityComponents;
using RoRebuildServer.EntityComponents.Character;
using RoRebuildServer.Simulation.Skills;
using RoRebuildServer.Simulation.StatusEffects.Setup;

namespace RoRebuildServer.Simulation.StatusEffects._1stJob;

[StatusEffectHandler(CharacterStatusEffect.TwoHandQuicken,
    StatusClientVisibility.Everyone,
    StatusEffectFlags.None,
    "AspdBoost")]
public class StatusTwoHandQuicken : StatusEffectBase
{
    // State Value 1 : ASPD bonus (always 20)
    // State Value 2 : hit-charge counter
    // State Value 3 : Two-Hand Quicken skill level (short)

    public override float Duration => int.MaxValue / 1000f; //effectively permanent; ended by SP drain, cancel, or weapon change

    public override StatusUpdateMode UpdateMode =>
        StatusUpdateMode.OnUpdate
        | StatusUpdateMode.OnDealDamage
        | StatusUpdateMode.OnChangeEquipment;

    public override StatusUpdateResult OnChangeEquipment(CombatEntity ch, ref StatusEffectState state)
    {
        if (ch.Character.Type != CharacterType.Player || state.Value2 > 0)
            return StatusUpdateResult.Continue;

        var weapon = ch.Player.GetItemIdForEquipSlot(EquipSlot.Weapon);
        if (weapon < 0)
            return StatusUpdateResult.EndStatus;

        if (!DataManager.WeaponInfo.TryGetValue(weapon, out var weaponInfo) ||
            weaponInfo.WeaponClass != (int)WeaponClass.TwoHandSword)
            return StatusUpdateResult.EndStatus;

        return StatusUpdateResult.Continue;
    }

    public override void OnApply(CombatEntity ch, ref StatusEffectState state)
    {
        ch.AddStat(CharacterStat.AspdBonus, state.Value1);
    }

    public override void OnExpiration(CombatEntity ch, ref StatusEffectState state)
    {
        ch.SubStat(CharacterStat.AspdBonus, state.Value1);
    }

    public override StatusUpdateResult OnUpdateTick(CombatEntity ch, ref StatusEffectState state)
    {
        // Drain 1 SP per second. End status if SP runs out.
        if (ch.Character.Type != CharacterType.Player)
            return StatusUpdateResult.Continue;

        if (!ch.Player.TryTakeSpValue(1))
            return StatusUpdateResult.EndStatus;

        return StatusUpdateResult.Continue;
    }

    public override StatusUpdateResult OnAttack(CombatEntity ch, ref StatusEffectState state, ref DamageInfo info)
    {
        // Only basic attacks count. Skip skill damage and non-damage results.
        if (info.AttackSkill != CharacterSkill.None)
            return StatusUpdateResult.Continue;
        if (!info.IsDamageResult)
            return StatusUpdateResult.Continue;
        if (ch.Character.Type != CharacterType.Player)
            return StatusUpdateResult.Continue;

        // Critical attacks grant 2 charges; normal attacks grant 1.
        var gain = info.Result == AttackResult.CriticalDamage ? 2 : 1;
        state.Value2 += gain;

        if (state.Value2 < 10)
            return StatusUpdateResult.Continue;

        // Proc: fire Bowling Bash at no SP cost, no cooldown.
        var bbLevel = ch.Player.MaxAvailableLevelOfSkill(CharacterSkill.BowlingBash);
        if (bbLevel <= 0)
        {
            // Player has no Bowling Bash learned/granted - proc is silent.
            state.Value2 = 0;
            return StatusUpdateResult.Continue;
        }

        var targetEntity = ch.Player.Target;
        if (!targetEntity.TryGet<CombatEntity>(out _))
        {
            // No valid target. Reset the counter and bail.
            state.Value2 = 0;
            return StatusUpdateResult.Continue;
        }

        var castInfo = new SkillCastInfo
        {
            Skill = CharacterSkill.BowlingBash,
            Level = bbLevel,
            TargetEntity = targetEntity,
            TargetedPosition = Position.Invalid,
            IsIndirect = true,
        };

        if (SkillHandler.ValidateTarget(castInfo, ch, isIndirect: true) == SkillValidationResult.Success)
        {
            SkillHandler.ExecuteSkill(castInfo, ch);
            // Auto-cast is "truly free" - clear the cooldown that
            // BowlingBashHandler.Process just applied.
            ch.Player.ResetSkillSpecificCooldown(CharacterSkill.BowlingBash);
        }

        // Reset charge counter regardless of whether the cast succeeded.
        state.Value2 = 0;
        return StatusUpdateResult.Continue;
    }
}