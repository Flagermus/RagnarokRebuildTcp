using RebuildSharedData.Enum;
using RoRebuildServer.Simulation.StatusEffects.Setup;

namespace RoRebuildServer.Simulation.StatusEffects._1stJob
{
    [StatusEffectHandler(CharacterStatusEffect.Momentum, StatusClientVisibility.Owner, StatusEffectFlags.NoSave)]
    public class StatusMomentum : StatusEffectBase
    {
    }
}
