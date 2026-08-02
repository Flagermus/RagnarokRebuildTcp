using RebuildSharedData.Data;
using RebuildSharedData.Enum;
using RebuildSharedData.Enum.EntityStats;
using RoRebuildServer.EntityComponents;
using RoRebuildServer.EntityComponents.Character;
using RoRebuildServer.EntityComponents.Util;
using RoRebuildServer.EntitySystem;
using RoRebuildServer.Networking;
using RoRebuildServer.Simulation.Util;
using System.Diagnostics;

namespace RoRebuildServer.Simulation.Skills.SkillHandlers.Knight;

[SkillHandler(CharacterSkill.BowlingBash, SkillClass.Physical)]
public class BowlingBashHandler : SkillHandlerBase
{
    public override float GetCastTime(CombatEntity source, CombatEntity? target, Position position, int lvl) => 0.7f;

    public override void Process(CombatEntity source, CombatEntity? target, Position position, int lvl, bool isIndirect, bool isItemSource)
    {
        if (target == null || !target.IsTargetable || source.Character.Map == null)
            return;

        var map = source.Character.Map;
        Debug.Assert(map != null);

        using var aoeTargets = EntityListPool.Get();
        map.GatherEnemiesInArea(source.Character, target.Character.Position, 2, aoeTargets, true, true);

        var hits = 2;
        if (lvl >= 2 && source.Character.Type == CharacterType.Player
            && source.Player.MainWeaponClass == (int)WeaponClass.TwoHandSword)
        {
            hits = aoeTargets.Count switch
            {
                <= 1 => 2,
                <= 3 => 3,
                _ => 4
            };
        }

        var knockDir = source.Character.FacingDirection;
        //One bundled roll per (skill, target): the engine multiplies Damage by HitCount server-side,
        //and the packet's HitCount field makes the client fan out the staggered multi-hit visuals.
        //Multiplier stays 2.5 per strike (each strike is 250% ATK), HitCount carries the hit count,
        //so total damage per target = 2.5 * hits, matching the pre-bundle behavior.
        var attack = new AttackRequest(CharacterSkill.BowlingBash, 2.5f, hits, AttackFlags.Physical, AttackElement.None);

        map.AddVisiblePlayersAsPacketRecipients(source.Character, target.Character);

        var lastHitTime = 0f;

        var bbAoeIds = new HashSet<int>(aoeTargets.Count);
        for (var i = 0; i < aoeTargets.Count; i++)
            bbAoeIds.Add(aoeTargets[i].Get<WorldObject>().Id);

        //Pass 1: detect Thermal Mark blasts. Removing a mark sends a status-remove packet that clears
        //the packet recipient list, so recipients are repopulated once afterwards.
        //overlap tracks how many marked blasts each enemy is inside of, for both BB AoE and non-AoE targets.
        var overlap = new Dictionary<int, int>(aoeTargets.Count * 4);
        var outOfAoeTargets = new Dictionary<int, CombatEntity>();
        var removedMark = false;

        if (lvl >= 3)
        {
            foreach (var t in aoeTargets)
            {
                var hit = t.Get<CombatEntity>();

                var hasMark = hit.HasStatusEffectOfType(CharacterStatusEffect.ThermalMark);
                if (hasMark)
                    hit.StatusContainer!.RemoveStatusEffectOfType(CharacterStatusEffect.ThermalMark);
                else
                {
                    hasMark = hit.StatusContainer?.RemovePendingStatusEffectOfType(CharacterStatusEffect.ThermalMark) ?? false;
                }

                if (!hasMark)
                    continue;

                removedMark = true;

                using var blast = EntityListPool.Get();
                map.GatherEnemiesInArea(source.Character, hit.Character.Position, 2, blast, true, true);

                foreach (var e in blast)
                {
                    if (!e.TryGet<WorldObject>(out var wo)) continue;
                    if (wo == source.Character) continue; //skip self

                    overlap.TryGetValue(wo.Id, out var c);
                    overlap[wo.Id] = c + 1;

                    if (!bbAoeIds.Contains(wo.Id))
                        outOfAoeTargets[wo.Id] = wo.CombatEntity;
                }
            }
        }

        //ThermalMark is StatusClientVisibility.Everyone, so removing it above sends a status-remove packet
        //and clears the packet recipient list as part of that send. Repopulate it here so the attack
        //packets below actually have recipients to send to.
        if (removedMark)
            map.AddVisiblePlayersAsPacketRecipients(source.Character, target.Character);

        //Main hit loop: one bundled roll per target. Targets inside both the BB AoE and a marked blast
        //are handled in Pass 2 below so their detonation damage can be merged into a single packet.
        for (var t = 0; t < aoeTargets.Count; t++)
        {
            var hitTarget = aoeTargets[t].Get<CombatEntity>();
            if (overlap.ContainsKey(hitTarget.Character.Id))
                continue;
            var isPrimary = t == 0;
            var (_, hitLastTime) = HitTarget(source, hitTarget, attack, knockDir, isPrimary);
            if (hitLastTime > lastHitTime)
                lastHitTime = hitLastTime;
        }

        if (lvl >= 3)
        {
            //Pass 2: targets inside both the BB AoE and a marked blast get one BB roll plus one
            //detonation roll, sent as separate packets so both damage pops are visible to the player.
            //Each roll is bundled (HitCount) exactly like Bowling Bash/Pierce to keep server rolls low.
            //The detonation roll sets NoTriggerOnAttackEffects so OnAttack/WhenAttacked/AutoSpell
            //triggers fire only once (from the BB roll).
            for (var t = 0; t < aoeTargets.Count; t++)
            {
                var hitTarget = aoeTargets[t].Get<CombatEntity>();
                if (!overlap.TryGetValue(hitTarget.Character.Id, out var hitCount)) continue;

                var bbAttack = new AttackRequest(CharacterSkill.BowlingBash, 2.5f, hits, AttackFlags.Physical, AttackElement.None);
                var bbRes = source.CalculateCombatResult(hitTarget, bbAttack);

                var detAttack = new AttackRequest(CharacterSkill.BowlingBash, 1.0f, hitCount, AttackFlags.Physical | AttackFlags.NoTriggerOnAttackEffects, AttackElement.None);
                var detRes = source.CalculateCombatResult(hitTarget, detAttack);

                if (bbRes.Time > lastHitTime)
                    lastHitTime = bbRes.Time;

                //Bowling Bash packet: bundled hits, shown as staggered BB damage numbers.
                source.ExecuteCombatResult(bbRes, false);

                if (bbRes.IsDamageResult)
                    ApplyKnockback(hitTarget, knockDir, 3);

                var isPrimary = t == 0;
                source.Character.Map?.AddVisiblePlayersAsPacketRecipients(source.Character, hitTarget.Character);
                if (isPrimary)
                    CommandBuilder.SkillExecuteTargetedSkill(source.Character, hitTarget.Character, CharacterSkill.BowlingBash, 1, bbRes);
                else
                    CommandBuilder.AttackMulti(source.Character, hitTarget.Character, bbRes, false);

                //Thermal Mark detonation packet: separate damage pop, bundled by HitCount = overlap count.
                //Delayed until after the Bowling Bash bundle finishes so the two damage sets display in sequence.
                //0.2 * hits + 0.4 places the first detonation number 0.6s after the last Bowling Bash hit.
                detRes.Time = lastHitTime + 0.2f * hits + 0.4f;
                source.ExecuteCombatResult(detRes, false);
                source.Character.Map?.AddVisiblePlayersAsPacketRecipients(source.Character, hitTarget.Character);
                CommandBuilder.AttackMulti(source.Character, hitTarget.Character, detRes, false);
            }

            //Pass 3: blast targets outside the BB AoE get their own detonation packet, with
            //HitCount = number of overlapping marked blasts that hit them.
            foreach (var (id, ce) in outOfAoeTargets)
            {
                overlap.TryGetValue(id, out var c);
                var detAttack = new AttackRequest(CharacterSkill.BowlingBash, 1.0f, c, AttackFlags.Physical, AttackElement.None);
                var detRes = source.CalculateCombatResult(ce, detAttack);
                detRes.IsIndirect = true;
                detRes.Time = lastHitTime + 0.2f * hits + 0.3f;
                source.ExecuteCombatResult(detRes, false);
                source.Character.Map?.AddVisiblePlayersAsPacketRecipients(source.Character, ce.Character);
                CommandBuilder.AttackMulti(source.Character, ce.Character, detRes, false);
            }
        }

        source.ApplyCooldownForAttackAction(target.Character.Position);

        if (source.Character.Type == CharacterType.Player)
            source.Player.SetSkillSpecificCooldown(CharacterSkill.BowlingBash, 3f);

        CommandBuilder.ClearRecipients();
    }

    private (DamageInfo firstResult, float lastHitTime) HitTarget(CombatEntity src, CombatEntity target, AttackRequest attack, Direction knockDir, bool isPrimaryTarget)
    {
        var res = src.CalculateCombatResult(target, attack); //HitCount already in attack, applied by engine

        //ChangeEntityPosition3 (via ApplyKnockback below) clears the packet recipient list, so recipients
        //are repopulated before each send to prevent multi-target packets from being silently dropped.
        src.Character.Map?.AddVisiblePlayersAsPacketRecipients(src.Character, target.Character);

        if (isPrimaryTarget)
            CommandBuilder.SkillExecuteTargetedSkill(src.Character, target.Character, CharacterSkill.BowlingBash, 1, res);
        else
            CommandBuilder.AttackMulti(src.Character, target.Character, res, false);

        src.ExecuteCombatResult(res, false);

        if (res.IsDamageResult)
            ApplyKnockback(target, knockDir, 3);

        return (res, res.Time);
    }

    private void ApplyKnockback(CombatEntity target, Direction dir, int distance)
    {
        var map = target.Character.Map;
        if (map == null) return;

        var pos = target.Character.Position;
        for (var i = 0; i < distance; i++)
        {
            var next = pos.AddDirectionToPosition(dir);
            if (!map.WalkData.IsCellWalkable(next))
                break;
            pos = next;
        }
        if (pos != target.Character.Position && target.GetSpecialType() != CharacterSpecialType.Boss)
            map.ChangeEntityPosition3(target.Character, target.Character.WorldPosition, pos, false);
    }
}
