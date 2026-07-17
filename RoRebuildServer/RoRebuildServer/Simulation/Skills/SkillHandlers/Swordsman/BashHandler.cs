using RebuildSharedData.Data;
using RebuildSharedData.Enum;
using RebuildSharedData.Enum.EntityStats;
using RoRebuildServer.EntityComponents;
using RoRebuildServer.EntityComponents.Util;
using RoRebuildServer.Networking;
using RoRebuildServer.Simulation.StatusEffects.Setup;
using RoRebuildServer.Simulation.Util;

namespace RoRebuildServer.Simulation.Skills.SkillHandlers.Swordsman
{
    [SkillHandler(CharacterSkill.Bash, SkillClass.Physical)]
    public class BashHandler : SkillHandlerBase
    {
        public override void Process(CombatEntity source, CombatEntity? target, Position position, int lvl, bool isIndirect, bool isItemSource)
        {
            lvl = lvl.Clamp(1, 3);

            if (target == null || !target.IsValidTarget(source))
                return;

            var damageMultiplier = lvl switch
            {
                1 => 1.5f,
                2 => 3.0f,
                3 => 4.0f,
                _ => 1.5f
            };

            var req = new AttackRequest(CharacterSkill.Bash, damageMultiplier, 1, AttackFlags.Physical, AttackElement.None);
            var res = source.CalculateCombatResult(target, req);

            source.ApplyCooldownForAttackAction(target);
            source.ExecuteCombatResult(res, false);

            var momentum = StatusEffectState.NewStatusEffect(CharacterStatusEffect.Momentum, 5f);
            source.AddStatusEffect(momentum);

            if (lvl == 3 && res.IsDamageResult && GameRandom.Next(0, 100) < 25)
            {
                var stun = StatusEffectState.NewStatusEffect(CharacterStatusEffect.Stun, 1.5f);
                target.AddStatusEffect(stun, false, res.AttackMotionTime);
            }

            if (source.Character.Type == CharacterType.Player)
                source.Player.SetSkillSpecificCooldown(CharacterSkill.Bash, 3f);

            CommandBuilder.SkillExecuteTargetedSkillAutoVis(source.Character, target.Character, CharacterSkill.Bash, lvl, res);
        }
    }
}
