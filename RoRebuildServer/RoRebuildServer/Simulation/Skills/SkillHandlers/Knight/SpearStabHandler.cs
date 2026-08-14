using RebuildSharedData.Data;
using RebuildSharedData.Enum;
using RebuildSharedData.Enum.EntityStats;
using RoRebuildServer.EntityComponents;
using RoRebuildServer.EntityComponents.Character;
using RoRebuildServer.EntityComponents.Util;
using RoRebuildServer.Networking;
using RoRebuildServer.Simulation.Pathfinding;
using RoRebuildServer.Simulation.StatusEffects.Setup;
using RoRebuildServer.Simulation.Util;

namespace RoRebuildServer.Simulation.Skills.SkillHandlers.Knight;

[SkillHandler(CharacterSkill.SpearStab, SkillClass.Physical)]
public class SpearStabHandler : SkillHandlerBase
{
    public override int GetSkillRange(CombatEntity source, int lvl) => 4;

    public override SkillValidationResult ValidateTarget(CombatEntity source, CombatEntity? target, Position position, int lvl,
        bool isIndirect, bool isItemSource)
    {
        if (!isIndirect && source.Character.Type == CharacterType.Player && (source.Player.MainWeaponClass < (int)WeaponClass.Spear || source.Player.MainWeaponClass > (int)WeaponClass.TwoHandSpear))
            return SkillValidationResult.IncorrectWeapon; //spear only

        return base.ValidateTarget(source, target, position, lvl, isIndirect, isItemSource);
    }

    public override void Process(CombatEntity source, CombatEntity? target, Position position, int lvl, bool isIndirect, bool isItemSource)
    {
        lvl = lvl.Clamp(1, 3);
        var map = source.Character.Map;

        if (target == null || !target.IsValidTarget(source) || map == null)
            return;

        var srcPoint = source.Character.Position;
        var endPoint = target.Character.Position;

        source.Character.FaceTargetWithoutClientUpdate(target.Character);

        using var potentialTargets = EntityListPool.Get();
        map.GatherEnemiesInArea(source.Character, target.Character.Position, 2, potentialTargets, true, true);
        map.AddVisiblePlayersAsPacketRecipients(source.Character, target.Character);

        DamageInfo primaryResult = DamageInfo.EmptyResult(source.Entity, target.Entity);
        var hitCount = 0;
        var hasHitResult = false;

        using var hitTargets = EntityListPool.Get();

        foreach (var potentialTarget in potentialTargets)
        {
            if (!potentialTarget.TryGet<CombatEntity>(out var splashTarget))
                continue;

            hitTargets.Add(potentialTarget);

            //flat 300% ATK at every level, per spec
            var flags = AttackFlags.Physical;
            if (lvl >= 3)
                flags |= AttackFlags.FullSpearMasteryTip; //Lv3: full tip-zone effect ignores 10% hard DEF at any range
            var req = new AttackRequest(CharacterSkill.SpearStab, 3.0f, 1, flags, AttackElement.None);
            var res = source.CalculateCombatResult(splashTarget, req);
            res.IsIndirect = true;
            if (splashTarget.Character.Position == source.Character.Position)
                res.AttackPosition = source.Character.Position - Directions.GetVectorForDirection(source.Character.FacingDirection); //if we're stacked attack in view direction
            source.ExecuteCombatResult(res, false);

            CommandBuilder.AttackMulti(source.Character, splashTarget.Character, res, false);

            if (splashTarget == target || !hasHitResult)
                primaryResult = res;
            hasHitResult = true;

            hitCount++;
        }

        //Momentum is granted at level 3 when the stab connects with 2 or more targets
        if (lvl >= 3 && hitCount >= 2)
        {
            var momentum = StatusEffectState.NewStatusEffect(CharacterStatusEffect.Momentum, 5f);
            source.AddStatusEffect(momentum);
        }

        var knockDir = srcPoint == endPoint
            ? source.Character.FacingDirection
            : DistanceCache.Direction(srcPoint, endPoint);
        foreach (var hitTarget in hitTargets)
        {
            if (hitTarget.TryGet<CombatEntity>(out var splashTarget))
                ApplyKnockback(splashTarget, knockDir, 5);
        }

        //Level 2: root all hit targets for 3 seconds, applied on the strike landing (delayed statuses don't send packets here)
        if (lvl >= 2)
        {
            var motionTime = source.GetAttackMotionTime();
            foreach (var hitTarget in hitTargets)
            {
                if (hitTarget.TryGet<CombatEntity>(out var splashTarget))
                {
                    var root = StatusEffectState.NewStatusEffect(CharacterStatusEffect.Root, 3f);
                    splashTarget.AddStatusEffect(root, false, motionTime);
                }
            }
        }

        source.ApplyCooldownForAttackAction(target);

        if (source.Character.Type == CharacterType.Player)
            source.Player.SetSkillSpecificCooldown(CharacterSkill.SpearStab, 3f);

        //the pushes above cleared the packet recipient list, so repopulate it for the final skill packet.
        //the skill packet must report a direct (non-indirect) hit or the client skips the thrust animation.
        var skillPacketResult = primaryResult;
        skillPacketResult.IsIndirect = false;
        map.AddVisiblePlayersAsPacketRecipients(source.Character, target.Character);
        CommandBuilder.SkillExecuteTargetedSkill(source.Character, target.Character, CharacterSkill.SpearStab, lvl, skillPacketResult);
        CommandBuilder.ClearRecipients();
    }

    private void ApplyKnockback(CombatEntity target, Direction dir, int distance)
    {
        if (target.GetSpecialType() == CharacterSpecialType.Boss || dir == Direction.None)
            return;

        var map = target.Character.Map;
        if (map == null) return;

        var walkData = map.WalkData;
        bool IsWalkable(Position p) =>
            p.X >= 0 && p.Y >= 0 && p.X < walkData.Width && p.Y < walkData.Height && walkData.IsCellWalkable(p);

        var pos = target.Character.Position;
        var leftDir = (Direction)(((int)dir + 7) % 8);
        var rightDir = (Direction)(((int)dir + 1) % 8);
        for (var i = 0; i < distance; i++)
        {
            var next = pos.AddDirectionToPosition(dir);
            if (IsWalkable(next))
            {
                pos = next;
                continue;
            }

            //primary direction is blocked; slide around the obstacle while keeping the thrust axis
            var nextLeft = pos.AddDirectionToPosition(leftDir);
            var nextRight = pos.AddDirectionToPosition(rightDir);
            if (IsWalkable(nextLeft))
                pos = nextLeft;
            else if (IsWalkable(nextRight))
                pos = nextRight;
            else
                break;
        }

        if (pos != target.Character.Position)
        {
            map.ChangeEntityPosition3(target.Character, target.Character.WorldPosition, pos, false);

            //The teleport does not cancel an in-progress walk, so a monster that was advancing on the
            //caster would keep walking back toward them right after the knockback. Stop it like the
            //built-in knockback does (see CombatEntity.DamageHandling).
            target.Character.StopMovingImmediately();

            if (target.Character.Type == CharacterType.Monster)
                target.Character.QueuedAction = QueuedAction.None;
        }
    }
}
