using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeroSystemEngine.Character;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HeroSystemEngine.Dice;
using HeroSystemEngine.Manuevers;
using Ploeh.AutoFixture;

//using Moq;


namespace HeroSystemEngine.Manuevers
{







    public class CharacterTestObjectFactory
    {

        Fixture StandardizedFixture;
        

        public CharacterTestObjectFactory()
        {
            StandardizedFixture = new Fixture();
            StandardizedFixture.Behaviors.Add(new OmitOnRecursionBehavior());



        }


        public HeroSystemCharacter BaseCharacter
        {
            get
            {
                var baseCharacter= StandardizedFixture.Build<HeroSystemCharacter>()
                    .Without(p => p.Manuevers)
                    .Without(p => p.State)
                    .Without(p => p.ActivePhase)
                    .Without(p => p.HeldManuever)
                    .Without(p => p.STR)
                    .Without(p => p.DEX)
                    .Without(p => p.CON)
                    .Without(p => p.BOD)
                    .Without(p => p.EGO)
                    .Without(p => p.PRE)
                    .Without(p => p.COM)
                    .Without(p => p.SPD)
                    .Without(p => p.END)
                    .Without(p => p.STUN)
                    .Without(p => p.END)
                    .Without(p => p.REC)
                    .Without(p => p.OCV)
                    .Without(p => p.DCV)
   
                    .Without(p => p.ECV)
                    .Without(p => p.INT)
                    .Without(p => p.TimesHumanSize)
                    .Without(p => p.GrabbedBy)
                    .Without(p => p.PD)
                    .Without(p => p.ED)
                    .Without(p => p.RPD)
                    .Without(p => p.PER)
                    .Without(p => p.RED)
                    .Without(p => p.HeldFoci)
                    .Without(p => p.PerceptionModifiers)
                    .Without(p => p.PerceptionMultipliers)

                    .Without(p=>p.RangedModModifier)
                    .Without(p => p.RangedModifierMultiplier)
                     .Without(p => p.DamageClassModifier)
                      .Without(p => p.GlobalModifier)
                      .Without(p => p.DamageMultiplier)
                    .Without(p => p.IsAborting)
                    .Without(p => p.CombatSequence)
                    .Without(p => p.SegmentNumberThatLastPhaseActivatedOn)
                    .Without(p => p.Characteristics)
                    .Without(p => p.ActiveManuever)

                    .Without(p => p.CoveringManuever)
                    .Without(p => p.ManueverInProgess)
                    .Create();
                baseCharacter.OCV.MaxValue = 3;
                baseCharacter.DCV.MaxValue = 3;
                baseCharacter.DEX.MaxValue = 10;
                baseCharacter.STUN.MaxValue = 20;
                baseCharacter.BOD.MaxValue = 10;
                baseCharacter.RPD.MaxValue = 0;
                baseCharacter.RED.MaxValue = 0;
                baseCharacter.PD.MaxValue = 2;
                baseCharacter.ED.MaxValue = 2;
                baseCharacter.PD.MaxValue = 2;
                baseCharacter.ED.MaxValue = 2;
                baseCharacter.SPD.MaxValue = 2;
                baseCharacter.INT.MaxValue = 10;


                baseCharacter.Hex.X = 1;
                baseCharacter.Hex.Y = 1;
                baseCharacter.Hex.Z = 1;
                Dice.Dice.RandomnessState = RandomnessState.average;
                return baseCharacter;
            }
        } 

        public HeroSystemCharacter BaseCharacterWithOneCombatManuever
        {
            get
            {
                var baseCharacter = BaseCharacter;
                var combatManuever = AddPhysicalCombatManeuverToCharacterWithDamage(baseCharacter, 3);
                return baseCharacter;

            }
        }

        public HeroSystemCharacter BaseCharacterWithStrike
        {
            get
            {
                var baseCharacter = BaseCharacter;
                var combatManuever = new Strike(baseCharacter);
                return baseCharacter;

            }
        }

        public HeroSystemCharacter BaseCharacterWithOneRangedCombatManuever
        {
            get
            {
                var baseCharacter = BaseCharacter;
                var ranged = new Attack("Basic Ranged", baseCharacter,
                    DamageType.Normal, 5, DefenseType.PD,  true);
                return baseCharacter;
            }
        }

        public HeroSystemCharacter BaseCharacterWithOneKillingAttackCombatManuever
        {
            get
            {
                var baseCharacter = BaseCharacter;
                
                var ranged = new Attack("Basic Killing", baseCharacter,
                    DamageType.Killing, 3, DefenseType.RPD,  true);
                return baseCharacter;
            }
        }

        public HeroSystemCharacter BaseCharacterWithOneHandKillingAttackCombatManuever {
            get
            {
                var baseCharacter = BaseCharacter;
                baseCharacter.STR.MaxValue = 60;
                var ranged = new HandKillingAttack(baseCharacter, 3,DefenseType.RPD);
                
                return baseCharacter;
            }
        }

        public List<HeroSystemCharacter> FastMediumAndSlowCharacters {
            get
            {
                List<HeroSystemCharacter> characters = new List<HeroSystemCharacter>();

                HeroSystemCharacter character = BaseCharacter;
                character.SPD.MaxValue = 2;
                character.Name = "Slow Character";
                characters.Add(character);
                character.DEX.MaxValue = 10;

                character = BaseCharacter;
                character.SPD.MaxValue = 4;
                character.Name = "Medium Character";
                characters.Add(character);
                character.DEX.MaxValue = 20;

                character = BaseCharacter;
                character.SPD.MaxValue = 6;
                character.Name = "Fast Character";
                characters.Add(character);
                character.DEX.MaxValue = 30;

                return characters;

            }
        }


        private Attack AddPhysicalCombatManeuverToCharacterWithDamage(HeroSystemCharacter character,
            int damageDice)
        {
            return new Attack("Basic", character, DamageType.Normal, damageDice,
                DefenseType.PD,  false);

        }


        public Attack AddRangedAttackToCharacter(HeroSystemCharacter character)
        {
            return new Attack("Ranged", character, DamageType.Normal, 10, DefenseType.PD, true);
        }
    }
}
