using Server.Items;
using Server.Misc;
using Server.Mobiles;

using Server.Targeting;
using System;

namespace Server.Spells.Seventh
{
    public class EnergyFieldSpell : MagerySpell
    {
        private static readonly SpellInfo m_Info = new SpellInfo(
            "Energy Field", "In Sanct Grav",
            221,
            9022,
            false,
            Reagent.BlackPearl,
            Reagent.MandrakeRoot,
            Reagent.SpidersSilk,
            Reagent.SulfurousAsh);
        public EnergyFieldSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Seventh;
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

        public void Target(IPoint3D p)
        {
            if (!Caster.CanSee(p))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
            }
            else if (SpellHelper.CheckTown(p, Caster) && SpellHelper.CheckWater(new Point3D(p), Caster.Map) && CheckSequence())
            {
                SpellHelper.Turn(Caster, p);

                SpellHelper.GetSurfaceTop(ref p);

                int dx = Caster.Location.X - p.X;
                int dy = Caster.Location.Y - p.Y;
                int rx = (dx - dy) * 44;
                int ry = (dx + dy) * 44;

                bool eastToWest;

                if (rx >= 0 && ry >= 0)
                {
                    eastToWest = false;
                }
                else if (rx >= 0)
                {
                    eastToWest = true;
                }
                else if (ry >= 0)
                {
                    eastToWest = true;
                }
                else
                {
                    eastToWest = false;
                }

                Effects.PlaySound(p, Caster.Map, 0x20B);

                TimeSpan duration;

                duration = TimeSpan.FromSeconds((15 + (Caster.Skills.Magery.Fixed / 5)) / 7);

                Point3D pnt = new Point3D(p);
                int itemID = eastToWest ? 0x3946 : 0x3956;

                if (SpellHelper.CheckField(pnt, Caster.Map))
                    new InternalItem(itemID, pnt, Caster, Caster.Map, duration);

                for (int i = 1; i <= 2; ++i)
                {
                    Timer.DelayCall(TimeSpan.FromMilliseconds(i * 300), index =>
                    {
                        Point3D point = new Point3D(eastToWest ? pnt.X + index : pnt.X, eastToWest ? pnt.Y : pnt.Y + index, pnt.Z);
                        SpellHelper.AdjustField(ref point, Caster.Map, 16, false);

                        if (SpellHelper.CheckField(point, Caster.Map))
                            new InternalItem(itemID, point, Caster, Caster.Map, duration);

                        point = new Point3D(eastToWest ? pnt.X + -index : pnt.X, eastToWest ? pnt.Y : pnt.Y + -index, pnt.Z);
                        SpellHelper.AdjustField(ref point, Caster.Map, 16, false);

                        if (SpellHelper.CheckField(point, Caster.Map))
                            new InternalItem(itemID, point, Caster, Caster.Map, duration);
                    }, i);
                }
            }

            FinishSequence();
        }

        [DispellableField]
        private class InternalItem : Item
        {
            private readonly Timer m_Timer;
            private readonly Mobile m_Caster;

            public InternalItem(int itemID, Point3D loc, Mobile caster, Map map, TimeSpan duration)
                : base(itemID)
            {
                Movable = false;
                Light = LightType.Circle300;

                MoveToWorld(loc, map);
                Effects.SendLocationParticles(EffectItem.Create(loc, map, EffectItem.DefaultDuration), 0x376A, 9, 10, 5029);

                m_Caster = caster;

                if (Deleted)
                    return;

                m_Timer = new InternalTimer(this, duration);
                m_Timer.Start();
            }

            public InternalItem(Serial serial)
                : base(serial)
            {
                m_Timer = new InternalTimer(this, TimeSpan.FromSeconds(5.0));
                m_Timer.Start();
            }

            public override bool BlocksFit => true;

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);
                writer.Write(0); // version
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);
                reader.ReadInt();
            }

            public override bool OnMoveOver(Mobile m)
            {
                if (m is PlayerMobile)
                {
                    int noto;

                    noto = Notoriety.Compute(m_Caster, m);

                    if (noto == Notoriety.Enemy || noto == Notoriety.Ally)
                    {
                        return false;
                    }

                    if (m.Map != null && (m.Map.Rules & MapRules.FreeMovement) == 0)
                    {
                        return false;
                    }
                }
                return base.OnMoveOver(m);
            }

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                m_Timer?.Stop();
            }

            private class InternalTimer : Timer
            {
                private readonly InternalItem m_Item;
                public InternalTimer(InternalItem item, TimeSpan duration)
                    : base(duration)
                {
                    m_Item = item;
                }

                protected override void OnTick()
                {
                    m_Item.Delete();
                }
            }
        }

        public class InternalTarget : Target
        {
            private readonly EnergyFieldSpell m_Owner;

            public InternalTarget(EnergyFieldSpell owner)
                : base(15, true, TargetFlags.None)
            {
                m_Owner = owner;
            }

            protected override void OnTarget(Mobile from, object o)
            {
                if (o is IPoint3D point3D)
                    m_Owner.Target(point3D);
            }

            protected override void OnTargetFinish(Mobile from)
            {
                m_Owner.FinishSequence();
            }
        }
    }
}
