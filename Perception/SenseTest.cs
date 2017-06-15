using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using HeroSystemEngine.Character;
using HeroSystemEngine.Dice;
using HeroSystemEngine.Manuevers;
using HeroSystemsEngine.GameMap;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroSystemsEngine.Perception
{
    [TestClass]
    public class SenseTargetingTest
    {
        private HeroSystemCharacter Character;
        private ITargetable Target;
        private Sense Sight;
        private Sense Hearing;
        private Sense Touch;
        private CharacterSenses CharacterSenses;
        private CharacterTestObjectFactory Factory= new CharacterTestObjectFactory();
        private Strike Strike;
        private Attack Ranged;

        [TestInitialize]
        public void CharacterWithSightAndOtherCharacterTarget()
        {
            Character = Factory.BaseCharacter;
            Character.OCV.MaxValue = 4;
            Character.DCV.MaxValue = 4;
            Target = Factory.BaseCharacter;
            Sight = Character.CharacterSenses.Sight;
            Hearing = Character.CharacterSenses.Hearing;
            Touch =  Character.CharacterSenses.Touch;
            CharacterSenses = Character.CharacterSenses;
            Strike = Character.Manuevers["Strike"] as Strike;
            Ranged = Factory.AddRangedAttackToCharacter(Character);
            CharacterSenses.Target = Target;


        }

        [TestMethod]
        public void CharacterUsesTargetingSense_CharacterCanDetermineExactLocationOfOfTarget()
        {
            LocationPrecision location = Sight.DetermineLocationOfTarget();
            Assert.AreEqual(LocationPrecision.ExactLocation, location);
        }

        [TestMethod]
        public void CharacterUsesNonTargetingSense_CharacterCanDetermineGeneralLocationOfOfTarget()
        {
            Hearing.PerceptionModifer = 7;
            LocationPrecision location = Hearing.DetermineLocationOfTarget();
            Assert.AreEqual(LocationPrecision.GeneralLocation, location);
        }

        [TestMethod]
        public void CharacterUsesSensewithNoRange_CharacterCannotDetermineLocationOfTarget()
        {
            LocationPrecision location = Touch.DetermineLocationOfTarget();
            Assert.AreEqual(LocationPrecision.CantPercieveLocation, location);
        }

        [TestMethod]
        public void CharacterEntersCombatWithOneTargetingSenseActive_CharacterSuffersNoPenaltyToOCVandDCV()
        {
            int OCV = Character.OCV.CurrentValue;
            Strike.Target(Target as HeroSystemCharacter);

            Assert.AreEqual(Character.OCV.CurrentValue, OCV);
            

        }




        private void blindCharacter()
        {
            Character.CharacterSenses.Sight.IsDisabled = true;

        }

        [TestMethod]
        public void CharacterIsInHTHCombatAndCharacterCantPercieveTargetWithTargetingSense_CharacterSufferHalfCV()
        {
            Character.INT.CurrentValue = -5;
            int DCV = Character.DCV.CurrentValue;
            int OCV = Character.OCV.CurrentValue;
            blindCharacter();
            Strike.Target(Target as HeroSystemCharacter);

            Assert.AreEqual(OCV/2,Character.OCV.CurrentValue);
            Assert.AreEqual(DCV / 2, Character.DCV.CurrentValue);
        }

        [TestMethod]
        public void
            CharacterIsInRangeCombatAndCharacterCantPercieveTargetWithTargetingSenseA_haracterSufferHalfDCVAnd0OCV()
        {
            Character.INT.CurrentValue = -5;
            int OCV = Character.OCV.CurrentValue;
            blindCharacter();
            Ranged.Target(Target as HeroSystemCharacter);

            Assert.AreEqual(0, Character.OCV.CurrentValue);
        }

        [TestMethod]
        public void
            CharacterIsInHTHCombatAndCharacterCantPercieveTargetWithTargetingSenseAndMakesAPerceptionRollWithNonTargetingSense_CharacterHasMinusOneDCVAndHalfOCV
            ()
        {
            int OCV = Character.OCV.CurrentValue;
            int DCV = Character.OCV.CurrentValue;
            blindCharacter();
            Character.CharacterSenses.Hearing.PerceptionModifer = 10;
            Strike.Target(Target as HeroSystemCharacter);

            Assert.AreEqual(OCV /2, Character.OCV.CurrentValue);
            Assert.AreEqual(DCV -1, Character.DCV.CurrentValue);
        }

        [TestMethod]
        public void
            CharacterIsInRangeCombatAndCharacterCantPercieveTargetWithTargetingSenseAndMakesAPerceptionRoll_CharacterHasFullDCVAndHalfOCV
            ()
        {
            int OCV = Character.OCV.CurrentValue;
            int DCV = Character.OCV.CurrentValue;
            blindCharacter();
            Character.CharacterSenses.Hearing.PerceptionModifer = 10;
            Ranged.Target(Target as HeroSystemCharacter);

            Assert.AreEqual(OCV / 2, Character.OCV.CurrentValue);
            Assert.AreEqual(DCV, Character.DCV.CurrentValue);
        }


        [TestMethod]
        public void
            CharacterFailsToPercieveWithMostEffectiveNonTargetingSense_WillAttemptToPercieveWithNextNonTargetingSense
            ()
        {
            CharacterSenses.IsDisabled = true;
            Sense hearing = CharacterSenses.Hearing;
            CharacterSenses.Hearing.IsDisabled = false;
            hearing.PerceptionModifer = -6;
             
            Sense superHearing = CharacterSenses.HearingGroup.CreateSenseWithNameForGroup("Super H");
            superHearing.PerceptionModifer = - 3;

            Dice.RandomnessState = RandomnessState.max;
            

            Character.CharacterSenses.Hearing.PerceptionModifer = 10;
            Ranged.Target(Target as HeroSystemCharacter);

        }



        [TestMethod]
        public void
            CharacterFailsToDetermineLocationWithMostEffectiveNonTargetingSense_WillAttemptTodetermineWithNextNonTargetingSense
            ()
        {
            CharacterSenses.IsDisabled = true;

            Sense hearing = CharacterSenses.Hearing;
            hearing.IsDisabled = false;
            hearing.PerceptionModifer = 0;

            Sense smell = CharacterSenses.SmellTaste;
            hearing.PerceptionModifer = 2;
            smell.IsDisabled = false;
            Target.PerceptionModifiers[SenseGroupType.Hearing] = -5;


            Assert.AreEqual(CharacterSenses.SenseThatSuccessfullyDeterminesLocationOfTarget, smell);

        }



        public void CharacterTriesToPercieveTargetWithNonTargetingSense_CharacterUsesHalfPhaseAction() { }
    }

    [TestClass]
    public class SenseGroupTest
    {
        CharacterTestObjectFactory Factory = new CharacterTestObjectFactory();
        private HeroSystemCharacter Target;
        private CharacterSenses CharacterSenses;
        private HeroSystemCharacter Attacker;
        private SenseAffectingPower BlindingPower;
        private Sense SuperSight;
        private SenseGroup SightGroup;

        [TestInitialize]
        public void CharacterWithStandardSensesAndAttackerWithSenseAffectingPower()
        {
            Target = Factory.BaseCharacter;
            CharacterSenses = Target.CharacterSenses;
            Attacker = Factory.BaseCharacter;
            BlindingPower = new SenseAffectingPower(Attacker, 10, true);
            SuperSight = CharacterSenses.SightGroup.CreateSenseWithNameForGroup("Super Sight");
            SightGroup = CharacterSenses.SenseGroups["Sight"];

        }

        [TestMethod]
        public void CharacterInitialized_CharacterHasInherientSensesInSenseGroups()
        {
            Assert.AreEqual("Hearing", CharacterSenses.SenseGroups["Hearing"].Senses["Hearing"].Name);
            Assert.AreEqual("Sight", CharacterSenses.SenseGroups["Sight"].Senses["Sight"].Name);
            Assert.AreEqual("Touch", CharacterSenses.SenseGroups["Touch"].Senses["Touch"].Name);
            Assert.AreEqual("SmellTaste", CharacterSenses.SenseGroups["SmellTaste"].Senses["SmellTaste"].Name);
            Assert.AreEqual(SenseGroupType.Mental, CharacterSenses.SenseGroups["Mental"].Type);



        }

        [TestMethod]
        public void SensePowerAddedToSenseGroup_SensePowerInheritsAttributesOfThatSense()
        {
           

            Assert.AreEqual(SightGroup.Type, SuperSight.Type);
            Assert.AreEqual(SightGroup.Discriminate, SuperSight.Discriminate);
            Assert.AreEqual(SightGroup.IsTargetingSense, SuperSight.IsTargetingSense);
            Assert.AreEqual(SightGroup.IsRanged, SuperSight.IsRanged);


        }

        [TestMethod]
        public void CharacterWithSensePowerIsAffectedByPowerThatTargetsSenseGroupItIsSimulating_SenseIsAffected()
        {
            SuperSight.Power = SensingPower.NRayPerception;

            BlindingPower.AffectsGroup = SenseGroupType.Sight;
            BlindingPower.PerformAttack(Target);
            Assert.AreEqual(true, SuperSight.IsDisabled);
        }

        [TestMethod]
        public void CharacterWithSensePowerIsAffectedByPowerThatTargetsSensePower_SenseIsAffected()
        {
            SuperSight.Power = SensingPower.NRayPerception;

            BlindingPower.PerformAttack(Target);
            Assert.AreEqual(true, SuperSight.IsDisabled);
        }

       

        
    }

    [TestClass]
    public class CharacterSenseTest
    {
        private CharacterTestObjectFactory Factory = new CharacterTestObjectFactory();
        private HeroSystemCharacter Target;
        private CharacterSenses CharacterSenses;
        private Sense SuperSight;
        private Sense Sight;
        private Sense Hearing;
        private SenseGroup SightGroup;

        [TestInitialize]
        public void CharacterWithSuperSightAndGoodEyeSightAndAmazingHearingAndCharacterHasTargetedAnotherCharacter()
        {
            Target = Factory.BaseCharacter;

            CharacterSenses = Factory.BaseCharacter.CharacterSenses;
            CharacterSenses.Target = Target;

            SightGroup = CharacterSenses.SenseGroups["Sight"];
            SuperSight = SightGroup.CreateSenseWithNameForGroup("Super Sight");
            SuperSight.PerceptionModifer = 10;

            Sight = CharacterSenses.Sight;
            Sight.PerceptionModifer = 5;

            Hearing = CharacterSenses.Hearing;
            Hearing.PerceptionModifer = +14;

        }
        [TestMethod]

        public void CharacterAttemptsToPercieveATarget_SenseWithBestChanceToPerceiveIsUsed()
        {
            Assert.AreEqual(SuperSight, CharacterSenses.SenseThatSucessfullyPercievesTarget);

            SuperSight.IsDisabled = true;
            Assert.AreEqual(Sight, CharacterSenses.SenseThatSucessfullyPercievesTarget);
        }

        [TestMethod]
        public void CharacterAttemptsToPercieveATarget_SenseWithTargetingSenseChosenBeforeNonTargeting()
        {
            CharacterSenses.IsDisabled = true;

            
            Hearing.IsDisabled = false;

            Sight.IsDisabled = false;


            //act assert
            CharacterSenses.Target = Target;
            Assert.AreEqual(Sight, CharacterSenses.SenseThatSucessfullyPercievesTarget);

            //act-assert
            Sight.IsDisabled = true;
            Assert.AreEqual(Hearing, CharacterSenses.SenseThatSucessfullyPercievesTarget);
        }

        [TestMethod]
        public void
            CharacterAttemptsToPercieveATargetFailsToPercieveWithMosteffectiveSense_WillAttemptToPercieveTargetWithNextMostEffectiveSense
            ()
        {
            CharacterSenses.IsDisabled = true;
            SuperSight.IsDisabled = false;
            SuperSight.PerceptionModifer = -10;
            Sight.IsDisabled = false;
            Sight.PerceptionModifer = -5;
            Hearing.IsDisabled = false;

            CharacterSenses.Target = Target;

            Assert.AreEqual(Hearing, CharacterSenses.SenseThatSucessfullyPercievesTarget);

        }

        public void CharacterAttemptsToDetermineLocationOftargetWithNonTargetingSense_TakesHalfPhaseAction()
        { }

        public void CharacterAttemptsToDetermineLocationOftargetWithTargetingSense_TakesZeroPhaseAction()
        { }
    }




    [TestClass]
    public class PerceptionTest
    {
        private HeroSystemCharacter Viewer;
        private HeroSystemCharacter Target;
        CharacterTestObjectFactory Factory = new CharacterTestObjectFactory();
        private IGameMap Map; 
        [TestInitialize]
        public void PerceptiveCharacterAndTargetCharacterThatsIsFarAway()
        {
             Map= MapFactory.ActiveGameMap;
            Viewer = Factory.BaseCharacter;
            Target = Factory.BaseCharacter;
            Target.Hex = new GameHex(1,1,22);
        }
        [TestMethod]
        public void CharacterTriesToPercieveTargetAtADistance_PerceptionRollIsSubjectToRangeModifer()
        {
            Viewer.CharacterSenses.Target = Target;
            
            Assert.AreEqual(0,Viewer.CharacterSenses.Sight.PerceptionModifer);
            Assert.AreEqual(5, Viewer.CharacterSenses.Sight.RollRequiredToPercieveTarget);

            Viewer.CharacterSenses.Sight.Perceive();



        }

        [TestMethod]
        public void CharacterTriesToSeeTarget_PerceptionModifiersAreNegative_PERRollIsRequredToSeeTarget()
        {

            Map.SightConditions.Add(SightPerceptionModifiers.ExtremelyHighContrast);
            Map.SightConditions.Add(SightPerceptionModifiers.LowContrast);

            Viewer.CharacterSenses.Target = Target;

            Target.Hex = new GameHex(1, 1, 1);

            Assert.AreEqual(false, Viewer.CharacterSenses.Sight.IsRollRequiredToPercieveTarget);
            Map.SightConditions.Remove(SightPerceptionModifiers.ExtremelyHighContrast);


            Map.SightConditions.Add(SightPerceptionModifiers.DarkNight);
            Assert.AreEqual(true, Viewer.CharacterSenses.Sight.IsRollRequiredToPercieveTarget);

            


        }
        public void CharacterTriesToHearTarget_PerceptionModifiersAreNegative_PERRollIsRequredToSeeTarget() { }

        public void CharacterTriesToSmellTarget_PerceptionModifiersAreNegative_SuffersAdditionalMinus5ToRoll()
        {
        }




    }

    [TestClass]
    public class SenseAdjustmentPowerTest
    {

    }





}
