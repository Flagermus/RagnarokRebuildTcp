using RebuildSharedData.Enum;
using RoRebuildServer.EntityComponents;
using RoRebuildServer.EntityComponents.Character;
using RoRebuildServer.Simulation.StatusEffects.Setup;

namespace RoRebuildServer.Simulation.StatusEffects.GenericDebuffs
{
    [StatusEffectHandler(CharacterStatusEffect.Root, StatusClientVisibility.Everyone)]
    public class StatusRoot : StatusEffectBase
    {
        public override void OnApply(CombatEntity ch, ref StatusEffectState state)
        {
            ch.SetBodyState(BodyStateFlags.Snared);
            ch.Character.StopMovingImmediately(); //Snared only blocks starting new moves, so stop any in-progress movement
        }

        public override void OnExpiration(CombatEntity ch, ref StatusEffectState state)
        {
            ch.RemoveBodyState(BodyStateFlags.Snared);
        }
    }
}
