using UnityEngine;

namespace BadGuy
{
    [CreateAssetMenu(menuName = "BAD GUY/Harness tuning")]
    public sealed class HarnessTuning : ScriptableObject
    {
        [Header("PROPOSED starting values - not approved balance")]
        public float BaseDamage = 20;
        public int MagazineSize = 12, StartingReserve = 72, ReserveCap = 120;
        public float ReloadSeconds = 1.2f, FireInterval = .16f, OverdriveSeconds = 8;
        [Range(0, 1)] public float AIDropChance = .05f, AmmoChance = .2f, BiryaniChance = .3f;
        public float PickupSeconds = 8, TransitionSeconds = 12;
        public float CopHP = 40, RockHP = 30, KnifeHP = 40, RunnerHP = 10, BossHP = 1200;
        public float CopDamage = 10, RockDamage = 8, KnifeDamage = 20, BossDamage = 15;
        public float HealthFor(ActorKind kind) => kind == ActorKind.Cop ? CopHP : kind == ActorKind.Rock ? RockHP : kind == ActorKind.Knife ? KnifeHP : kind == ActorKind.Runner ? RunnerHP : kind == ActorKind.Boss ? BossHP : 1;
    }
}

