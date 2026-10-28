using UnityEngine;

namespace BadGuy
{
    public sealed class HitRegion : MonoBehaviour
    {
        public HarnessTarget Owner;
        public HitZone Zone;
    }
    public sealed class HarnessTarget : MonoBehaviour
    {
        public HarnessController Owner;
        public ActorKind Kind;
        public float Health;
        public bool Dead;
        public Transform PrecisionRegion;
        public TextMesh Label;
        public Renderer Body;
        public Color BaseColor;
        public float NextAttack, ReleaseAt = -1;
        public bool Civilian => Kind == ActorKind.Civilian || Kind == ActorKind.Bhabi;

        public void TakeHit(HitZone zone)
        {
            if (Dead) return;
            if (Civilian)
            {
                if (!Owner.State.HurtCivilian(Kind == ActorKind.Bhabi)) { Owner.Message("AI protected civilian"); return; }
                Dead = true; Owner.Message("Civilian casualty: -1000"); Destroy(gameObject); return;
            }
            float damage = CombatRules.Damage(Owner.Tuning.BaseDamage, zone, Owner.State.Overdrive);
            Health -= damage;
            if (zone == HitZone.Crotch)
            {
                Owner.State.Score += 300;
                Owner.Message("NUTSHOT! " + damage.ToString("0.#") + " damage");
                ReleaseAt = -1; NextAttack = Time.time + 2;
            }
            if (Health > 0) return;
            Dead = true; Owner.OnDefeat(this, zone); Destroy(gameObject);
        }
        void Update()
        {
            if (Owner == null || Dead) return;
            if (PrecisionRegion != null)
            {
                float s = CombatRules.LinearFactor(Owner.BhabiVisible(), Owner.State.BhabiCasualty);
                PrecisionRegion.localScale = new Vector3(.20f * s, .16f * s, .12f);
            }
            string display=Kind==ActorKind.Cop?"Dirty Cop":Kind==ActorKind.Knife?"Cadre / BLADE":Kind==ActorKind.Rock?"Cadre / ROCK":Kind==ActorKind.Runner?"Ballsy Idiot":Kind==ActorKind.Bhabi?"Gulshan Bhabi":Kind==ActorKind.Boss?"The Patron":Kind.ToString();
            Label.text = display + (Civilian ? " / DO NOT SHOOT" : " / " + Mathf.CeilToInt(Health));
            if (!Owner.IsCombat || !Owner.AttacksEnabled || Owner.Paused || Civilian || Kind == ActorKind.Runner) return;
            if (ReleaseAt > 0)
            {
                Body.material.color = Color.Lerp(BaseColor, Color.white, Mathf.PingPong(Time.time * 5, 1));
                if (Time.time >= ReleaseAt && (!Owner.LevelMode || !TryGetComponent<LevelActorMotion>(out var motion) || motion.ReadyToFire))
                {
                    ReleaseAt = -1; Body.material.color = BaseColor; Owner.Attack(this); NextAttack = Time.time + 3;
                }
            }
            else if (Time.time >= NextAttack && Owner.CanTelegraph(this))
            {
                ReleaseAt = Time.time + (Kind == ActorKind.Knife ? 1.6f : 1.2f);
                Owner.Message(Kind == ActorKind.Knife ? "Kopa Shamsu! Intercept the orange blade." : Kind + " winding up!");
            }
        }
    }
    public sealed class HarnessProjectile : MonoBehaviour
    {
        public HarnessController Owner;
        public float Damage, Speed = 5;
        void Update()
        {
            if (!Owner.IsCombat || !Owner.AttacksEnabled || Owner.Paused) return;
            transform.position = Vector3.MoveTowards(transform.position, Owner.View.transform.position, Speed * Time.deltaTime);
            transform.Rotate(0, 0, 180 * Time.deltaTime);
            if (Vector3.Distance(transform.position, Owner.View.transform.position) < 1.5f)
            {
                Owner.State.Health = Mathf.Max(0, Owner.State.Health - Damage); Owner.Message("Projectile hit: -" + Damage + " HP"); Destroy(gameObject);
            }
        }
    }
    public sealed class HarnessPickup : MonoBehaviour
    {
        public HarnessController Owner;
        public PickupKind Kind;
        public int Amount;
        public float Remaining;
        public bool Collected;
        public bool Emergency;
        public void Collect()
        {
            if (Collected) return;
            Collected = true; Owner.State.Collect(Kind, Amount, Owner.Tuning.ReserveCap); Owner.Message("Collected " + Kind); Destroy(gameObject);
        }
        void Update()
        {
            if (!Owner.CanShoot) return;
            Remaining -= Time.deltaTime;
            if (Remaining <= 0) Destroy(gameObject);
        }
    }
}

