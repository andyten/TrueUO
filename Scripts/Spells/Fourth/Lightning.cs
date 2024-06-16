using Server.Items;
using Server.Mobiles;

using Server.Targeting;
using System;

namespace Server.Spells.Fourth
{
    public class LightningSpell : MagerySpell
    {
        private static readonly SpellInfo m_Info = new SpellInfo(
            "Lightning", "Por Ort Grav",
            239,
            9021,
            Reagent.MandrakeRoot,
            Reagent.SulfurousAsh);
        public LightningSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Fourth;
        public override bool DelayedDamage => false;
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

        public void Target(IEntity entity)
        {
            if (entity is IDamageable m)
            {
                if (!Caster.CanSee(m))
                {
                    Caster.SendLocalizedMessage(500237); // Target can not be seen.
                }
                else if (CheckHSequence(m))
                {
                    Mobile source = Caster;
                    SpellHelper.Turn(Caster, m.Location);

                    SpellHelper.CheckReflect(this, ref source, ref m);

                    double damage = GetNewAosDamage(23, 1, 4, m);

                    if (m is Mobile)
                    {
                        Effects.SendBoltEffect(m, true, 0, false);
                    }
                    else
                    {
                        Effects.SendBoltEffect(EffectMobile.Create(m.Location, m.Map, EffectMobile.DefaultDuration), true, 0, false);
                    }

                    if (damage > 0)
                    {
                        SpellHelper.Damage(this, m, damage, 0, 0, 0, 0, 100);
                    }
                }

                FinishSequence();
            }
            else
            {
                Effects.SendBoltEffect(EffectMobile.Create(entity.Location, entity.Map, EffectMobile.DefaultDuration), true, Utility.Random(128), false);
            }
        }

        private class InternalTarget : Target
        {
            private readonly LightningSpell m_Owner;
            public InternalTarget(LightningSpell owner)
                : base(10, false, TargetFlags.Harmful)
            {
                m_Owner = owner;
            }

            protected override void OnTarget(Mobile from, object o)
            {
                if (o is IEntity entity)
                    m_Owner.Target(entity);
            }

            protected override void OnTargetFinish(Mobile from)
            {
                m_Owner.FinishSequence();
            }
        }
    }
}
