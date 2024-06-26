
using Server.Spells.First;
using Server.Targeting;
using System;
using System.Collections.Generic;

namespace Server.Spells.Fourth
{
    public class CurseSpell : MagerySpell
    {
        private static readonly SpellInfo _Info = new SpellInfo(
            "Curse", "Des Sanct",
            227,
            9031,
            Reagent.Nightshade,
            Reagent.Garlic,
            Reagent.SulfurousAsh);

        private static readonly Dictionary<Mobile, Timer> _UnderEffect = new Dictionary<Mobile, Timer>();

        public CurseSpell(Mobile caster, Item scroll)
            : base(caster, scroll, _Info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Fourth;

        public static void AddEffect(Mobile m, TimeSpan duration, int strOffset, int dexOffset, int intOffset)
        {
            if (m == null)
            {
                return;
            }

            if (_UnderEffect.TryGetValue(m, out Timer value))
            {
                value.Stop();

                _UnderEffect[m] = null;
            }

            // my spell is stronger, so lets remove the lesser spell
            if (WeakenSpell.IsUnderEffects(m) && SpellHelper.GetCurseOffset(m, StatType.Str) <= strOffset)
            {
                WeakenSpell.RemoveEffects(m, false);
            }

            if (ClumsySpell.IsUnderEffects(m) && SpellHelper.GetCurseOffset(m, StatType.Dex) <= dexOffset)
            {
                ClumsySpell.RemoveEffects(m, false);
            }

            if (FeeblemindSpell.IsUnderEffects(m) && SpellHelper.GetCurseOffset(m, StatType.Int) <= intOffset)
            {
                FeeblemindSpell.RemoveEffects(m, false);
            }

            _UnderEffect[m] = Timer.DelayCall(duration, RemoveEffect, m);

            m.UpdateResistances();
        }

        public static void RemoveEffect(Mobile m)
        {
            if (!WeakenSpell.IsUnderEffects(m))
            {
                m.RemoveStatMod("[Magic] Str Curse");
            }

            if (!ClumsySpell.IsUnderEffects(m))
            {
                m.RemoveStatMod("[Magic] Dex Curse");
            }

            if (!FeeblemindSpell.IsUnderEffects(m))
            {
                m.RemoveStatMod("[Magic] Int Curse");
            }

            BuffInfo.RemoveBuff(m, BuffIcon.Curse);

            if (_UnderEffect.TryGetValue(m, out Timer value))
            {
                value?.Stop();

                _UnderEffect.Remove(m);
            }

            m.UpdateResistances();
        }

        public static bool UnderEffect(Mobile m)
        {
            return _UnderEffect.ContainsKey(m);
        }

        public override void OnCast()
        {
            Caster.Target = new InternalTarget(this);
        }

        public override bool OnInstantCast(IEntity target)
        {
            Target t = new InternalTarget(this);
            if (Caster.InRange(target, t.Range) && Caster.InLOS(target))
            {
                t.Invoke(Caster, target);
                return true;
            }
            else
                return false;
        }

        public static bool DoCurse(Mobile caster, Mobile m, bool masscurse)
        {
            if (Mysticism.StoneFormSpell.CheckImmunity(m))
            {
                caster.SendLocalizedMessage(1080192); // Your target resists your ability reduction magic.
                return true;
            }

            int oldStr = SpellHelper.GetCurseOffset(m, StatType.Str);
            int oldDex = SpellHelper.GetCurseOffset(m, StatType.Dex);
            int oldInt = SpellHelper.GetCurseOffset(m, StatType.Int);

            int newStr = SpellHelper.GetOffset(caster, m, StatType.Str, true, true);
            int newDex = SpellHelper.GetOffset(caster, m, StatType.Dex, true, true);
            int newInt = SpellHelper.GetOffset(caster, m, StatType.Int, true, true);

            if ((-newStr > oldStr && -newDex > oldDex && -newInt > oldInt) ||
                (newStr == 0 && newDex == 0 && newInt == 0))
            {
                return false;
            }

            SpellHelper.AddStatCurse(caster, m, StatType.Str, false);
            SpellHelper.AddStatCurse(caster, m, StatType.Dex, true);
            SpellHelper.AddStatCurse(caster, m, StatType.Int, true);

            int percentage = (int)(SpellHelper.GetOffsetScalar(caster, m, true) * 100);
            TimeSpan length = SpellHelper.GetDuration(caster, m);
            string args;

            if (masscurse)
            {
                args = string.Format("{0}\t{0}\t{0}", percentage);
                BuffInfo.AddBuff(m, new BuffInfo(BuffIcon.MassCurse, 1075839, length, m, args));
            }
            else
            {
                args = $"{percentage}\t{percentage}\t{percentage}\t{10}\t{10}\t{10}\t{10}";
                BuffInfo.AddBuff(m, new BuffInfo(BuffIcon.Curse, 1075835, 1075836, length, m, args));
            }

            AddEffect(m, SpellHelper.GetDuration(caster, m), oldStr, oldDex, oldInt);

            m.Spell?.OnCasterHurt();

            m.Paralyzed = false;

            m.FixedParticles(0x374A, 10, 15, 5028, EffectLayer.Waist);
            m.PlaySound(0x1E1);

            return true;
        }

        public void Target(Mobile m)
        {
            if (!Caster.CanSee(m))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
            }
            else if (CheckHSequence(m))
            {
                SpellHelper.Turn(Caster, m);

                SpellHelper.CheckReflect(this, Caster, ref m);

                if (DoCurse(Caster, m, false))
                {
                    HarmfulSpell(m);
                }
                else
                {
                    DoHurtFizzle();
                }
            }

            FinishSequence();
        }

        private class InternalTarget : Target
        {
            private readonly CurseSpell _Owner;
            public InternalTarget(CurseSpell owner)
                : base(10, false, TargetFlags.Harmful)
            {
                _Owner = owner;
            }

            protected override void OnTarget(Mobile from, object o)
            {
                if (o is Mobile mobile)
                {
                    _Owner.Target(mobile);
                }
            }

            protected override void OnTargetFinish(Mobile from)
            {
                _Owner.FinishSequence();
            }
        }
    }
}
