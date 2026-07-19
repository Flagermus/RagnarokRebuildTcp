using RebuildSharedData.Enum;
using RoRebuildServer.EntityComponents;
using RoRebuildServer.EntityComponents.Character;
using RoRebuildServer.Simulation.StatusEffects.Setup;

namespace RoRebuildServer.Simulation.StatusEffects._1stJob;

[StatusEffectHandler(CharacterStatusEffect.Endure, StatusClientVisibility.Everyone, StatusEffectFlags.NoSave)]
public class StatusEndure : StatusEffectBase
{
    public override StatusUpdateMode UpdateMode => StatusUpdateMode.OnTakeDamage;

    public override StatusUpdateResult OnTakeDamage(CombatEntity ch, ref StatusEffectState state, ref DamageInfo info)
    {
        if (info.Result != AttackResult.NormalDamage && info.Result != AttackResult.CriticalDamage)
            return StatusUpdateResult.Continue;
        info.Flags |= DamageApplicationFlags.NoHitLock;
        return StatusUpdateResult.Continue;
    }
}
