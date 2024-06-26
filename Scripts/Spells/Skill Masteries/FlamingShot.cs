using Server.Items;
using Server.Network;

using Server.Targeting;
using System;
using System.Collections.Generic;

namespace Server.Spells.SkillMasteries
{
    public class FlamingShotSpell : SkillMasterySpell
    {
        private static readonly SpellInfo m_Info = new SpellInfo(
                "Flameing Shot", "",
                -1,
                9002
            );

        public override int RequiredMana => 30;

        public override DamageType SpellDamageType => DamageType.SpellAOE;
        public override SkillName CastSkill => SkillName.Archery;
        public override SkillName DamageSkill => SkillName.Tactics;
        public override bool DelayedDamage => true;

        public FlamingShotSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override bool CheckCast()
        {
            if (!CheckWeapon())
            {
                Caster.SendLocalizedMessage(1156000); // You must have an Archery weapon to use this ability!
                return false;
            }

            return base.CheckCast();
        }

        public override void SendCastEffect()
        {
            Caster.PrivateOverheadMessage(MessageType.Regular, 1150, 1155999, Caster.NetState); // You ready a volley of flaming arrows!
            Effects.SendTargetParticles(Caster, 0x3709, 10, 30, 2724, 0, 9907, EffectLayer.LeftFoot, 0);
            Caster.PlaySound(0x5CF);
        }

        public override void OnCast()
        {
            Caster.Target = new MasteryTarget(this, allowGround: true);
        }

        public override bool OnInstantCast(IEntity target)
        {
            Target t = new MasteryTarget(this);
            if (Caster.InRange(target, t.Range) && Caster.InLOS(target))
            {
                t.Invoke(Caster, target);
                return true;
            }
            else
                return false;
        }

        protected override void OnTarget(object o)
        {
            BaseWeapon weapon = GetWeapon();

            if (weapon is BaseRanged ranged && !(ranged is BaseThrown) && o is IPoint3D p && SpellHelper.CheckTown(p, Caster) && CheckSequence())
            {
                List<Mobile> targets = new List<Mobile>();

                foreach (IDamageable target in AcquireIndirectTargets(p, 5))
                {
                    if (target is Mobile mobile)
                    {
                        targets.Add(mobile);
                    }
                }

                int count = targets.Count;

                for (var index = 0; index < targets.Count; index++)
                {
                    Mobile mob = targets[index];

                    Caster.MovingEffect(mob, ranged.EffectID, 18, 1, false, false);

                    if (ranged.CheckHit(Caster, mob))
                    {
                        double damage = GetNewAosDamage(40, 1, 5, mob);

                        if (count > 2)
                        {
                            damage = damage / count;
                        }

                        damage *= GetDamageScalar(mob);
                        Caster.DoHarmful(mob);

                        SpellHelper.Damage(this, mob, damage, 0, 100, 0, 0, 0);

                        Server.Timer.DelayCall(TimeSpan.FromMilliseconds(800), obj =>
                        {
                            Mobile mobile = obj;

                            mobile?.FixedParticles(0x36BD, 20, 10, 5044, EffectLayer.Head);
                        }, mob);

                        mob.PlaySound(0x1DD);
                    }
                }

                ColUtility.Free(targets);

                ranged.PlaySwingAnimation(Caster);
                Caster.PlaySound(0x101);
            }
        }
    }
}
