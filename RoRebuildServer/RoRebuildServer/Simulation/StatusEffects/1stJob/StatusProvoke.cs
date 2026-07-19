using RebuildSharedData.Enum;
using RoRebuildServer.EntityComponents.Character;
using RoRebuildServer.EntityComponents;
using RoRebuildServer.Simulation.StatusEffects.Setup;
using RebuildSharedData.Enum.EntityStats;
using RoRebuildServer.EntityComponents.Util;

namespace RoRebuildServer.Simulation.StatusEffects._1stJob
{
    [StatusEffectHandler(CharacterStatusEffect.Provoke, StatusClientVisibility.Owner, StatusEffectFlags.NoSave)]
    public class StatusProvoke : StatusEffectBase
    {
        public override void OnApply(CombatEntity ch, ref StatusEffectState state)
        {
            ch.AddStat(CharacterStat.AddDefPercent, -10);
            ch.AddStat(CharacterStat.AddAttackPercent, 10);
        }

        public override void OnExpiration(CombatEntity ch, ref StatusEffectState state)
        {
            ch.SubStat(CharacterStat.AddDefPercent, -10);
            ch.SubStat(CharacterStat.AddAttackPercent, 10);
        }
    }
}
