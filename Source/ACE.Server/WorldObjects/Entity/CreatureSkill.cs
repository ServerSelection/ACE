using System;

using ACE.Common.Extensions;
using ACE.Database.Models.Shard;
using ACE.DatLoader;
using ACE.Entity.Enum;
using ACE.Server.Entity;

namespace ACE.Server.WorldObjects.Entity
{
    public class CreatureSkill
    {
        private readonly Creature creature;
        // This is the underlying database record
        public readonly BiotaPropertiesSkill BiotaPropertiesSkill;

        public readonly Skill Skill;

        public CreatureSkill(Creature creature, BiotaPropertiesSkill biotaPropertiesSkill)
        {
            this.creature = creature;
            this.BiotaPropertiesSkill = biotaPropertiesSkill;

            Skill = (Skill)biotaPropertiesSkill.Type;
        }

        public SkillAdvancementClass AdvancementClass
        {
            get => (SkillAdvancementClass)BiotaPropertiesSkill.SAC;
            set
            {
                if (BiotaPropertiesSkill.SAC != (uint)value)
                    creature.ChangesDetected = true;

                BiotaPropertiesSkill.SAC = (uint)value;
            }
        }

        public bool IsUsable
        {
            get
            {
                if (AdvancementClass == SkillAdvancementClass.Trained || AdvancementClass == SkillAdvancementClass.Specialized)
                    return true;

                if (AdvancementClass == SkillAdvancementClass.Untrained)
                {
                    DatManager.PortalDat.SkillTable.SkillBaseHash.TryGetValue((uint)Skill, out var skillTableRecord);

                    if (skillTableRecord?.MinLevel == 1)
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Total experience for this skill,
        /// both spent and earned
        /// </summary>
        public uint ExperienceSpent
        {
            get => BiotaPropertiesSkill.PP;
            set
            {
                if (BiotaPropertiesSkill.PP != value)
                    creature.ChangesDetected = true;

                BiotaPropertiesSkill.PP = value;
            }
        }

        /// <summary>
        /// Total skill level due to
        /// directly raising the skill
        /// </summary>
        public ushort Ranks
        {
            get => BiotaPropertiesSkill.LevelFromPP;
            set
            {
                if (BiotaPropertiesSkill.LevelFromPP != value)
                    creature.ChangesDetected = true;

                BiotaPropertiesSkill.LevelFromPP = value;
            }
        }

        public uint Base
        {
            get
            {
                uint total = 0;

                if (IsUsable)
                    total = AttributeFormula.GetFormula(creature, Skill, false);

                total += InitLevel + Ranks;

                if (creature is Player player)
                    total += GetAugBonus(player);

                return total;
            }
        }

        public uint Current
        {
            get
            {
                uint total = 0;

                if (IsUsable)
                    total = AttributeFormula.GetFormula(creature, Skill);

                total += InitLevel + Ranks;

                var skillMod = creature.EnchantmentManager.GetSkillMod(Skill);
                total += (uint)skillMod;    // can be negative?

                if (creature is Player player)
                {
                    var vitae = player.Vitae;

                    if (vitae != 1.0f)
                        total = (uint)(total * vitae).Round();

                    // it seems this gets applied after vitae?
                    total += GetAugBonus(player);
                }

                return total;
            }
        }

        public uint GetAugBonus(Player player)
        {
            uint total = 0;

            if (player.AugmentationJackOfAllTrades != 0)
                total += (uint)(player.AugmentationJackOfAllTrades * 5);

            if (player.LumAugAllSkills != 0)
                total += (uint)player.LumAugAllSkills;

            if (player.AugmentationSkilledMelee > 0 && Player.MeleeSkills.Contains(Skill))
                total += (uint)(player.AugmentationSkilledMelee * 10);
            else if (player.AugmentationSkilledMissile > 0 && Player.MissileSkills.Contains(Skill))
                total += (uint)(player.AugmentationSkilledMissile * 10);
            else if (player.AugmentationSkilledMagic > 0 && Player.MagicSkills.Contains(Skill))
                total += (uint)(player.AugmentationSkilledMagic * 10);

            switch (Skill)
            {
                case Skill.ArmorTinkering:
                case Skill.ItemTinkering:
                case Skill.MagicItemTinkering:
                case Skill.WeaponTinkering:
                case Skill.Salvaging:

                    if (player.LumAugSkilledCraft != 0)
                        total += (uint)player.LumAugSkilledCraft;
                    break;
            }

            if (AdvancementClass == SkillAdvancementClass.Specialized && player.LumAugSkilledSpec != 0)
                total += (uint)player.LumAugSkilledSpec * 2;

            if (player.Enlightenment != 0)
                total += (uint)player.Enlightenment;

            return total;
        }

        /// <summary>
        /// A bonus from character creation: +5 for trained, +10 for specialized
        /// </summary>
        public uint InitLevel
        {
            get => BiotaPropertiesSkill.InitLevel;
            set => BiotaPropertiesSkill.InitLevel = value;
        }
    }
}
