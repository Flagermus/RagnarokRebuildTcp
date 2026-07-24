using RebuildSharedData.Enum;
using RoRebuildServer.EntityComponents;
using RoRebuildServer.Simulation.StatusEffects.Setup;

namespace RoRebuildServer.Simulation.StatusEffects._1stJob;

[StatusEffectHandler(CharacterStatusEffect.ThermalMark, StatusClientVisibility.Everyone, StatusEffectFlags.NoSave)]
public class StatusThermalMark : StatusEffectBase
{
}
