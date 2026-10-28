using System;

namespace BadGuy
{
    public enum HitZone { Body, Head, Crotch, Limb }
    public enum ActorKind { Cop, Rock, Knife, Runner, Civilian, Bhabi, Boss }
    public enum PickupKind { Ammo, Biryani, AI }

    // Engine-independent rules are shared by the range, encounters and batch checks.
    public static class CombatRules
    {
        public static float Damage(float baseDamage, HitZone zone, bool overdrive)
        {
            float multiplier = zone == HitZone.Crotch ? 3f : zone == HitZone.Head ? 2f : zone == HitZone.Limb ? .7f : 1f;
            return baseDamage * multiplier * (overdrive ? 1.15f : 1f);
        }
        public static float AreaFactor(bool bhabiPresent, bool casualty) => casualty ? .7f : bhabiPresent ? 1.2f : 1f;
        public static float LinearFactor(bool bhabiPresent, bool casualty) => (float)Math.Sqrt(AreaFactor(bhabiPresent, casualty));
        public static bool RollAI(HitZone finishingZone, float chance, double roll) => finishingZone == HitZone.Crotch && roll < chance;
    }

    [Serializable]
    public class RunState
    {
        public float Health = 100;
        public int Magazine = 12, Reserve = 72, Score, Kills, CiviliansHit;
        public bool AIReady, BhabiCasualty;
        public float OverdriveRemaining;
        public bool Overdrive => OverdriveRemaining > 0;
        public bool TryFire()
        {
            if (Magazine <= 0) return false;
            Magazine--; return true;
        }
        public void Reload(int capacity)
        {
            int moved = Math.Min(Math.Max(0, capacity - Magazine), Reserve);
            Magazine += moved; Reserve -= moved;
        }
        public void Collect(PickupKind kind, int amount, int reserveCap)
        {
            if (kind == PickupKind.Ammo) Reserve = Math.Min(reserveCap, Reserve + amount);
            if (kind == PickupKind.Biryani) Health = Math.Min(100, Health + amount);
            if (kind == PickupKind.AI && !Overdrive) AIReady = true;
        }
        public bool Activate(float duration)
        {
            if (!AIReady || Overdrive) return false;
            AIReady = false; OverdriveRemaining = duration; return true;
        }
        public bool HurtCivilian(bool isBhabi)
        {
            if (Overdrive) return false;
            CiviliansHit++; Score -= 1000;
            if (isBhabi) BhabiCasualty = true;
            return true;
        }
    }
}

