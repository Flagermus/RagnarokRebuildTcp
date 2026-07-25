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
        var attack = new AttackRequest(CharacterSkill.BowlingBash, 2.5f, 1, AttackFlags.Physical, AttackElement.None);

        map.AddVisiblePlayersAsPacketRecipients(source.Character, target.Character);

        var lastHitTime = 0f;

        for (var t = 0; t < aoeTargets.Count; t++)
        {
            var hitTarget = aoeTargets[t].Get<CombatEntity>();
            var isPrimary = t == 0;
            var (_, hitLastTime) = HitTarget(source, hitTarget, attack, hits, knockDir, isPrimary);
            if (hitLastTime > lastHitTime)
                lastHitTime = hitLastTime;
        }

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

                //ThermalMark is StatusClientVisibility.Everyone, so removing it above sends a status-remove packet
                //and clears the packet recipient list as part of that send. Repopulate it here so the detonation's
                //AttackMulti call below actually has recipients to send to.
                map.AddVisiblePlayersAsPacketRecipients(source.Character, hit.Character);

                using var blast = EntityListPool.Get();
                map.GatherEnemiesInArea(source.Character, hit.Character.Position, 2, blast, true, true);

                var detAttack = new AttackRequest(CharacterSkill.BowlingBash, 1.0f, 1, AttackFlags.Physical, AttackElement.None);
                foreach (var e in blast)
                {
                    if (!e.TryGet<WorldObject>(out var blastTarget))
                        continue;
                    var detRes = source.CalculateCombatResult(blastTarget.CombatEntity, detAttack);
                    detRes.IsIndirect = true;
                    detRes.Time = lastHitTime + 0.15f;
                    source.ExecuteCombatResult(detRes, false);
                    CommandBuilder.AttackMulti(source.Character, blastTarget, detRes, false);
                }
            }
        }

        source.ApplyCooldownForAttackAction(target.Character.Position);

        if (source.Character.Type == CharacterType.Player)
            source.Player.SetSkillSpecificCooldown(CharacterSkill.BowlingBash, 3f);

        CommandBuilder.ClearRecipients();
    }

    private (DamageInfo firstResult, float lastHitTime) HitTarget(CombatEntity src, CombatEntity target, AttackRequest attack, int hits, Direction knockDir, bool isPrimaryTarget)
    {
        DamageInfo? firstResult = null;
        DamageInfo lastRes = default;
        for (var i = 0; i < hits; i++)
        {
            var res = src.CalculateCombatResult(target, attack);
            if (i > 0)
                res.Time += 0.15f * i;

            if (isPrimaryTarget && i == 0)
            {
                CommandBuilder.SkillExecuteTargetedSkill(src.Character, target.Character, CharacterSkill.BowlingBash, 1, res);
            }
            else
            {
                CommandBuilder.AttackMulti(src.Character, target.Character, res, false);
            }

            if (i == 0)
                firstResult = res;

            src.ExecuteCombatResult(res, false);
            lastRes = res;
        }

        if (lastRes.IsDamageResult)
            ApplyKnockback(target, knockDir, 3);

        return (firstResult ?? new DamageInfo(), lastRes.Time);
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
