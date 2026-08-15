using RebuildSharedData.Data;
using RebuildSharedData.Enum;
using RebuildSharedData.Enum.EntityStats;
using RoRebuildServer.EntityComponents;
using RoRebuildServer.EntityComponents.Character;
using RoRebuildServer.EntityComponents.Util;
using RoRebuildServer.Networking;
using RoRebuildServer.Simulation.Util;

namespace RoRebuildServer.Simulation.Skills.SkillHandlers.Knight
{
    [SkillHandler(CharacterSkill.BrandishSpear, SkillClass.Physical)]
    public class BrandishSpearHandler : SkillHandlerBase
    {
        public override float GetCastTime(CombatEntity source, CombatEntity? target, Position position, int lvl) => 0f;
        public override int GetSkillRange(CombatEntity source, int lvl) => 5;

        public override SkillValidationResult ValidateTarget(CombatEntity source, CombatEntity? target, Position position, int lvl,
            bool isIndirect, bool isItemSource)
        {
            if (!isIndirect && source.Character.Type == CharacterType.Player && (source.Player.MainWeaponClass < (int)WeaponClass.Spear || source.Player.MainWeaponClass > (int)WeaponClass.TwoHandSpear))
                return SkillValidationResult.IncorrectWeapon; //spear only

            if (!isIndirect && source.Character.Type == CharacterType.Player && !source.Player.HasPeco)
                return SkillValidationResult.Failure; //must be mounted on a Peco Peco

            return base.ValidateTarget(source, target, position, lvl, isIndirect, isItemSource);
        }

        public override void Process(CombatEntity source, CombatEntity? target, Position position, int lvl, bool isIndirect, bool isItemSource)
        {
            lvl = lvl.Clamp(1, 1);
            var map = source.Character.Map;

            if (target == null || !target.IsValidTarget(source) || map == null)
                return;

            var hasMomentum = source.HasStatusEffectOfType(CharacterStatusEffect.Momentum);
            var aoeDistance = hasMomentum ? 4 : 2; //5x5 around target, or 9x9 with Momentum

            source.Character.FaceTargetWithoutClientUpdate(target.Character);

            using var potentialTargets = EntityListPool.Get();
            map.GatherEnemiesInArea(source.Character, target.Character.Position, aoeDistance, potentialTargets, true, true);

            if (hasMomentum)
                source.StatusContainer!.RemoveStatusEffectOfType(CharacterStatusEffect.Momentum);

            map.AddVisiblePlayersAsPacketRecipients(source.Character, target.Character);

            var primaryResult = DamageInfo.EmptyResult(source.Entity, target.Entity);

            foreach (var potentialTarget in potentialTargets)
            {
                if (!potentialTarget.TryGet<CombatEntity>(out var splashTarget))
                    continue;

                var req = new AttackRequest(CharacterSkill.BrandishSpear, 7.0f, 1, AttackFlags.Physical, AttackElement.None);
                var res = source.CalculateCombatResult(splashTarget, req);
                if (splashTarget == target)
                    primaryResult = res;
                source.ExecuteCombatResult(res, false);

                CommandBuilder.AttackMulti(source.Character, splashTarget.Character, res, false);
            }

            CommandBuilder.SkillExecuteTargetedSkill(source.Character, target.Character, CharacterSkill.BrandishSpear, 1, primaryResult);

            if (source.Character.Type == CharacterType.Player)
                source.Player.SetSkillSpecificCooldown(CharacterSkill.BrandishSpear, 3f);

            CommandBuilder.ClearRecipients();
        }
    }
}