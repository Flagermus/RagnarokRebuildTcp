using RebuildSharedData.Enum;
using RebuildSharedData.Enum.EntityStats;
using RoRebuildServer.EntityComponents;
using RoRebuildServer.EntityComponents.Character;
using RoRebuildServer.EntityComponents.Util;
using RoRebuildServer.Networking;
using RoRebuildServer.Simulation.StatusEffects.Setup;

namespace RoRebuildServer.Simulation.StatusEffects._1stJob;

[StatusEffectHandler(CharacterStatusEffect.CounterAttack, StatusClientVisibility.Owner, StatusEffectFlags.NoSave)]
public class StatusCounterAttack : StatusEffectBase
{
    public override StatusUpdateMode UpdateMode => StatusUpdateMode.OnCalculateDamageTaken;

    public override StatusUpdateResult OnCalculateDamage(CombatEntity ch, ref StatusEffectState state, ref AttackRequest req, ref DamageInfo info)
    {
        if (!info.IsDamageResult)
            return StatusUpdateResult.Continue;

        //magic damage is reduced but does not consume the stance
        if (req.Flags.HasFlag(AttackFlags.Magical))
        {
            info.Damage = (int)(info.Damage * 0.75f);
            return StatusUpdateResult.Continue;
        }

        if (!req.Flags.HasFlag(AttackFlags.Physical))
            return StatusUpdateResult.Continue;

        if (!info.Source.TryGet<CombatEntity>(out var attacker))
            return StatusUpdateResult.Continue;

        //must be within our own attack range; no facing requirement
        if (ch.Character.Position.SquareDistance(attacker.Character.Position) > ch.GetStat(CharacterStat.Range))
            return StatusUpdateResult.Continue;

        //fully negate the incoming hit (including every hit of a multi-hit skill, since it resolves as one DamageInfo)
        info.Result = AttackResult.Block;
        info.Damage = 0;

        //retaliate for 500% ATK
        ch.Character.LookAtEntity(ref attacker.Entity);
        var counterReq = new AttackRequest(CharacterSkill.CounterAttack, 5f, 1, AttackFlags.Physical, AttackElement.None);
        var res = ch.CalculateCombatResult(attacker, counterReq);
        ch.ApplyCooldownForAttackAction(attacker);
        ch.ExecuteCombatResult(res, false);

        //guaranteed 1.5s stun on the attacker, bypassing normal stun resistance
        var stun = StatusEffectState.NewStatusEffect(CharacterStatusEffect.Stun, 1.5f);
        attacker.AddStatusEffect(stun, false, res.AttackMotionTime);

        ch.Character.StopMovingImmediately();

        CommandBuilder.SkillExecuteTargetedSkillAutoVis(ch.Character, attacker.Character, CharacterSkill.CounterAttack, 1, res);

        return StatusUpdateResult.EndStatus;
    }
}
