
using Server.Targeting;
using System;
using System.Collections.Generic;

namespace Server.Spells.Third
{
    public class BlessSpell : MagerySpell
    {
        private static readonly SpellInfo _Info = new SpellInfo(
            "Bless", "Rel Sanct",
            203,
            9061,
            Reagent.Garlic,
            Reagent.MandrakeRoot);

        private static Dictionary<Mobile, InternalTimer> _Table;

        public static bool IsBlessed(Mobile m)
        {
            return _Table != null && _Table.ContainsKey(m);
        }

        public static void AddBless(Mobile m, TimeSpan duration)
        {
            if (_Table == null)
            {
                _Table = new Dictionary<Mobile, InternalTimer>();
            }

            if (_Table.TryGetValue(m, out InternalTimer value))
            {
                value.Stop();
            }

            _Table[m] = new InternalTimer(m, duration);
        }

        public static void RemoveBless(Mobile m)
        {
            if (_Table != null && _Table.TryGetValue(m, out InternalTimer value))
            {
                value.Stop();
                m.Delta(MobileDelta.Stat);

                _Table.Remove(m);
            }
        }

        public BlessSpell(Mobile caster, Item scroll)
            : base(caster, scroll, _Info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Third;

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

        public void Target(Mobile m)
        {
            if (!Caster.CanSee(m))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
            }
            else if (CheckBSequence(m))
            {
                SpellHelper.Turn(Caster, m);

                int oldStr = SpellHelper.GetBuffOffset(m, StatType.Str);
                int oldDex = SpellHelper.GetBuffOffset(m, StatType.Dex);
                int oldInt = SpellHelper.GetBuffOffset(m, StatType.Int);

                int newStr = SpellHelper.GetOffset(Caster, m, StatType.Str, false, true);
                int newDex = SpellHelper.GetOffset(Caster, m, StatType.Dex, false, true);
                int newInt = SpellHelper.GetOffset(Caster, m, StatType.Int, false, true);

                if ((newStr < oldStr && newDex < oldDex && newInt < oldInt) ||
                    (newStr == 0 && newDex == 0 && newInt == 0))
                {
                    DoHurtFizzle();
                }
                else
                {
                    SpellHelper.AddStatBonus(Caster, m, false, StatType.Str);
                    SpellHelper.AddStatBonus(Caster, m, true, StatType.Dex);
                    SpellHelper.AddStatBonus(Caster, m, true, StatType.Int);

                    int percentage = (int)(SpellHelper.GetOffsetScalar(Caster, m, false) * 100);
                    TimeSpan length = SpellHelper.GetDuration(Caster, m);
                    string args = $"{percentage}\t{percentage}\t{percentage}";
                    BuffInfo.AddBuff(m, new BuffInfo(BuffIcon.Bless, 1075847, 1075848, length, m, args));

                    m.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);
                    m.PlaySound(0x1EA);

                    AddBless(Caster, length + TimeSpan.FromMilliseconds(50));
                }
            }

            FinishSequence();
        }

        private class InternalTarget : Target
        {
            private readonly BlessSpell _Owner;

            public InternalTarget(BlessSpell owner)
                : base(10, false, TargetFlags.Beneficial)
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

        private class InternalTimer : Timer
        {
            public Mobile Mobile { get; }

            public InternalTimer(Mobile m, TimeSpan duration)
                : base(duration)
            {
                Mobile = m;
                Start();
            }

            protected override void OnTick()
            {
                RemoveBless(Mobile);
            }
        }
    }
}
