using Assets.Scripts.Effects.EffectHandlers;
using Assets.Scripts.Effects.EffectHandlers.Skills.Knight;
using Assets.Scripts.Network;
using RebuildSharedData.Enum;
using RebuildSharedData.Enum.EntityStats;

namespace Assets.Scripts.SkillHandlers.Handlers.Knight
{
    [SkillHandler(CharacterSkill.BowlingBash)]
    public class BowlingBashHandler : SkillHandlerBase
    {
        public override void OnHitEffect(ServerControllable target, ref AttackResultData attack)
        {
            target.Messages.SendHitEffect(attack.Src, attack.MotionTime, (int)HitEffectType.Normal, attack.HitCount);
        }

        public override void ExecuteSkillTargeted(ServerControllable src, ref AttackResultData attack)
        {
            src.PerformBasicAttackMotion();
            
            var id = CameraFollower.Instance.EffectIdLookup["BowlingBash"];
            CameraFollower.Instance.AttachEffectToEntity(id, src.gameObject, src.Id);

            if(attack.Target != null)
                BowlingBashImpactEffect.Create(attack.Target, attack.DamageTiming);
        }
    }
}