using RebuildSharedData.Data;
using RebuildSharedData.Enum;
using RoRebuildServer.EntityComponents;
using System.Diagnostics;
using RebuildSharedData.Enum.EntityStats;
using RoRebuildServer.EntityComponents.Util;
using RoRebuildServer.Networking;
using RoRebuildServer.Simulation.Util;
using RoRebuildServer.Simulation.StatusEffects.Setup;

namespace RoRebuildServer.Simulation.Skills.SkillHandlers.Swordsman;

[SkillHandler(CharacterSkill.MagnumBreak, SkillClass.Physical, SkillTarget.Self)]
public class MagnumBreakHandler : SkillHandlerBase
{
    public override void Process(CombatEntity source, CombatEntity? target, Position position, int lvl, bool isIndirect,
        bool isItemSource)
    {
        var map = source.Character.Map;
        Debug.Assert(map != null);

        map.AddVisiblePlayersAsPacketRecipients(source.Character);

        var blastDistance = lvl >= 3 ? 3 : 2;
        var hitBonus = 100 + lvl * 10;
        if (source.Character.Type == CharacterType.Monster && lvl > 10)
        {
            blastDistance = 3;
            hitBonus += 50;
        }

        var attackMultiplier = lvl switch
        {
            1 => 1.5f,
            2 => 2.25f,
            _ => 3.0f
        };
        var element = AttackElement.None;

        using var targetList = EntityListPool.Get();
        map.GatherEnemiesInArea(source.Character, source.Character.Position, blastDistance, targetList, true, true);

        var attack = new AttackRequest(CharacterSkill.MagnumBreak, attackMultiplier, 1, AttackFlags.Physical, element);
        attack.AccuracyRatio = hitBonus;

        foreach (var e in targetList)
        {
            if (!e.TryGet<WorldObject>(out var blastTarget))
                continue;

            var distanceFromBlast = source.Character.WorldPosition.DistanceTo(blastTarget.WorldPosition);
            var res = source.CalculateCombatResult(e.Get<CombatEntity>(), attack);
            res.KnockBack = 2;
            res.AttackPosition = source.Character.Position;
            res.Time += distanceFromBlast * 0.03f;

            source.ExecuteCombatResult(res, false);


            CommandBuilder.AttackMulti(source.Character, blastTarget, res, false);
        }

        if (lvl >= 3 && source.Character.Type == CharacterType.Player)
        {
            var status = StatusEffectState.NewStatusEffect(CharacterStatusEffect.MagnumBreakAtkBuff, 15f, 50);
            source.AddStatusEffect(status);
        }

        source.ApplyCooldownForAttackAction(position);

        if (source.Character.Type == CharacterType.Player)
            source.Player.SetSkillSpecificCooldown(CharacterSkill.MagnumBreak, 3f);

        CommandBuilder.SkillExecuteSelfTargetedSkillAutoVis(source.Character, CharacterSkill.MagnumBreak, lvl, isIndirect);
    }
}
