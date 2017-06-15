using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HeroSystemEngine.Character;
using HeroSystemEngine.Dice;
using HeroSystemEngine.Manuevers;
using HeroSystemsEngine.CombatSequence;
using HeroSystemsEngine.GameMap;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Moq;
using HeroSystemsEngine.Focus;
namespace HeroSystemsEngine.Manuevers
{

    [TestClass]
    public class ManueverTest
    {
        private HeroSystemCharacter Character;
        private Manuever Manuever;
        
        private CombatSequence.CombatSequence Sequence;

        [TestInitialize]
        public void BaseCharacterWithTestManuever()
        {
            CharacterTestObjectFactory factory = new CharacterTestObjectFactory();
            Character = factory.BaseCharacter;
            Manuever = new TestManuever(Character);
            Sequence = new CombatSequence.CombatSequence();
            Sequence.AddCharacter(Character);
            Sequence.StartCombat();
        }

        [TestMethod]
        public void ActivateManuverWithBonuses_AddsBonusesToCharacter()
        {
            int OCV = Character.OCV.CurrentValue;

            Character.Manuevers["Test"].Perform();
            Assert.AreEqual(OCV + Manuever.Modifier.OCV.ModiferAmount, Character.OCV.CurrentValue);

        }

        [TestMethod]
        public void WhenNextPhaseActivates_BonusFromCombatManueverIsRemoved()
        {
            int OCV = Character.OCV.CurrentValue;
            Character.Manuevers["Test"].Perform();
            Sequence.CompleteActivePhase();
  
            //assert
            Assert.AreEqual(OCV, Character.OCV.CurrentValue);
        }

    }

    [TestClass]
    public class ManueverBonusTest
    {
        CombatSequence.CombatSequence Sequence;
        HeroSystemCharacter SpeedFiveCharacter;
        HeroSystemCharacter SpeedSixCharacter;
        Manuever ManueverWithPhaseDurationBonus;

        CharacterTestObjectFactory Factory = new CharacterTestObjectFactory();

        [TestInitialize]
        public void TwoCharactersWithManueversWithDifferentBonusesAndDurationsAndLengths()
        {
            Sequence = new CombatSequence.CombatSequence();
            SpeedFiveCharacter = setupCharacterWithSpdDexStr(5,10,20);
            SpeedSixCharacter = setupCharacterWithSpdDexStr(6, 20, 20);

            setupManueverwithCharacterStrBonusDurationTypeAndLength
                (SpeedFiveCharacter, "manuever", 5, DurationUnit.Continuous, 
                PhaseLength.Half);

            setupManueverwithCharacterStrBonusDurationTypeAndLength
                (SpeedSixCharacter, "manuever", 6, DurationUnit.Continuous,
                PhaseLength.Full);

            setupManueverwithCharacterStrBonusDurationTypeAndLength
                (SpeedSixCharacter, "ManueverWithSegmentDurationBonus", 12, DurationUnit.Segment,
                PhaseLength.Full);

            setupManueverwithCharacterStrBonusDurationTypeAndLength
                (SpeedFiveCharacter, "ManueverWithPhaseDurationBonus", 15, DurationUnit.Segment,
                PhaseLength.Full);

            setupManueverwithCharacterStrBonusDurationTypeAndLength
               (SpeedSixCharacter, "ManueverWithPhaseDurationBonus", 18, DurationUnit.Segment,
               PhaseLength.Full);

            Sequence.StartCombat();

        }
        private Manuever setupManueverwithCharacterStrBonusDurationTypeAndLength(HeroSystemCharacter character, string manuevername,int bonus, DurationUnit durationUnit, PhaseLength length)
        {
            Manuever manuever = new TestCombatManuever(character);
            manuever.DurationUnit = durationUnit;
            manuever.Modifier.STR.ModiferAmount = bonus;
            manuever.PhaseActionTakes = length;
  
            character.Manuevers[manuevername] = manuever;
            return manuever;
        }
        private HeroSystemCharacter setupCharacterWithSpdDexStr(int spd, int dex,int str)
        {
            HeroSystemCharacter character = Factory.BaseCharacter;
            character = Factory.BaseCharacter;
            character.SPD.CurrentValue = spd;
            character.DEX.CurrentValue = dex;
            character.STR.MaxValue = str;
            Sequence.AddCharacter(character);
            return character;
        }

        [TestMethod]
        public void CharacterPerformsManueverWithSegmentDuration_DeactivatesOnNextSegment()
        {
            SpeedSixCharacter.Manuevers["ManueverWithSegmentDurationBonus"].Perform();
            //at next character in same segment, manuver still active
            Assert.AreEqual(12, SpeedSixCharacter.STR.Modifier);

            //go to next segment, manuever no longer active
            Sequence.CompleteActivePhase();
            Assert.AreEqual(SpeedSixCharacter.STR.MaxValue, SpeedSixCharacter.STR.CurrentValue);
            Assert.AreEqual(0, SpeedSixCharacter.STR.Modifier);

        }

        [TestMethod]
        public void CharacterPerformsManueverWithPhaseDuration_DeactivatesOnNextPhase()
        {
            var x = Sequence.ActivateNextPhaseInSequence;
            SpeedFiveCharacter.Manuevers["ManueverWithPhaseDurationBonus"].Perform();

            //at phase 2, bonus still active   
            Assert.AreEqual(15, SpeedFiveCharacter.STR.Modifier);

            //goes to phase 3, bonus still active
            Sequence.CompleteActivePhase();
            Assert.AreEqual(SpeedFiveCharacter.STR.MaxValue, SpeedFiveCharacter.STR.CurrentValue);
            Assert.AreEqual(0, SpeedFiveCharacter.STR.Modifier);

        }

        [TestMethod]
        public void CharacterPerformsManueverWithContinuousDuration_DoesNotDeactivatesOnNextPhaseOrSegment()
        {
            var x = Sequence.ActivateNextPhaseInSequence;
            SpeedFiveCharacter.Manuevers["manuever"].Perform();

            //goes to phase 2, bonus still active
            Sequence.CompleteActivePhase();
            Assert.AreEqual(5, SpeedFiveCharacter.STR.Modifier);

            //goes forward 6 phases bonus still active
            Sequence.CompleteActivePhase();
            Sequence.CompleteActivePhase();
            Sequence.CompleteActivePhase();
            Sequence.CompleteActivePhase();
            Assert.AreEqual(5, SpeedFiveCharacter.STR.Modifier);
        }

        [TestMethod]
        public void TwoCharactersPerformsManuever_CorrectBonusGoesToCorrectCharacter()
        {
            SpeedSixCharacter.Manuevers["manuever"].Perform();
            SpeedFiveCharacter.Manuevers["manuever"].Perform();

            Assert.AreEqual(5, SpeedFiveCharacter.STR.Modifier);
            Assert.AreEqual(SpeedFiveCharacter.STR.MaxValue + SpeedFiveCharacter.STR.Modifier,
                SpeedFiveCharacter.STR.CurrentValue);


            Assert.AreEqual(SpeedSixCharacter.STR.MaxValue + SpeedSixCharacter.STR.Modifier,
                SpeedSixCharacter.STR.CurrentValue);
            Assert.AreEqual(6, SpeedSixCharacter.STR.Modifier);

        }

        [TestMethod]
        public void TwoCharactersPerformsManueverandNextPhaseStarts_BonusIsDeactivatedOnCorrectCharacter()
        {
            SpeedSixCharacter.Manuevers["ManueverWithPhaseDurationBonus"].Perform();
            SpeedFiveCharacter.Manuevers["ManueverWithPhaseDurationBonus"].Perform();

            Assert.AreEqual(SpeedFiveCharacter.STR.MaxValue + SpeedFiveCharacter.STR.Modifier,
                SpeedFiveCharacter.STR.CurrentValue);
            Assert.AreEqual(15, SpeedFiveCharacter.STR.Modifier);

            Assert.AreEqual(SpeedSixCharacter.STR.MaxValue, SpeedSixCharacter.STR.CurrentValue);
            Assert.AreEqual(0, SpeedSixCharacter.STR.Modifier);


        }

        [TestMethod]
        public void CharacterPerformsManueverWithIncrementalBonus_TotalIsDeactivatedWhenManueverOver()
        {
            SpeedSixCharacter.Manuevers["ManueverWithPhaseDurationBonus"].Perform();
            var bonus = SpeedSixCharacter.STR.Modifier;

            SpeedSixCharacter.Manuevers["ManueverWithPhaseDurationBonus"].Modifier.STR.ModiferAmount = bonus * 2;
            SpeedSixCharacter.Manuevers["ManueverWithPhaseDurationBonus"].ActivateModifier();

            SpeedSixCharacter.Manuevers["ManueverWithPhaseDurationBonus"].Modifier.STR.ModiferAmount = bonus * 3;
            SpeedSixCharacter.Manuevers["ManueverWithPhaseDurationBonus"].ActivateModifier();

            SpeedSixCharacter.Manuevers["ManueverWithPhaseDurationBonus"].Modifier.STR.ModiferAmount = bonus * 4;
            SpeedSixCharacter.Manuevers["ManueverWithPhaseDurationBonus"].ActivateModifier();

            Assert.AreEqual(SpeedSixCharacter.STR.MaxValue + bonus * 4, SpeedSixCharacter.STR.CurrentValue);

            SpeedSixCharacter.Manuevers["ManueverWithPhaseDurationBonus"].Deactivate();
            Assert.AreEqual(0, SpeedSixCharacter.STR.Modifier);
            Assert.AreEqual(SpeedSixCharacter.STR.MaxValue, SpeedSixCharacter.STR.CurrentValue);


        }


        [TestMethod]
        public void CharacterPerformsManueverWithMathAndMultiplierBonus_TotalIsCalculatedCorrectly()
        {
            CharasteristicModifier charModifier = SpeedSixCharacter.Manuevers["ManueverWithPhaseDurationBonus"].Modifier.STR;
            charModifier.Multiplier = .5;
            SpeedSixCharacter.Manuevers["ManueverWithPhaseDurationBonus"].Perform();

            Assert.AreEqual((SpeedSixCharacter.STR.MaxValue + charModifier.ModiferAmount) * charModifier.Multiplier, SpeedSixCharacter.STR.CurrentValue);

        }

        [TestMethod]
        public void CharacterPerformsTwoManueversWithBonusToSameAttribute_TotalIsCalculatedCorrectly()
        {
            Manuever manuever = (Manuever) SpeedSixCharacter.Manuevers["manuever"];
            manuever.PhaseActionTakes = PhaseLength.Zero;
            manuever.Perform();

            ManueverWithPhaseDurationBonus = (Manuever) SpeedSixCharacter.Manuevers["ManueverWithPhaseDurationBonus"];
            ManueverWithPhaseDurationBonus.PhaseActionTakes = PhaseLength.Zero;
            SpeedSixCharacter.Manuevers["ManueverWithPhaseDurationBonus"].Perform();

            Assert.AreEqual(ManueverWithPhaseDurationBonus.Modifier.OCV.ModiferAmount
                + manuever.Modifier.OCV.ModiferAmount+ SpeedSixCharacter.OCV.MaxValue, 
                SpeedSixCharacter.OCV.CurrentValue);

            manuever.Modifier.Deactivate(SpeedSixCharacter);
            Assert.AreEqual(ManueverWithPhaseDurationBonus.Modifier.OCV.ModiferAmount
                + SpeedSixCharacter.OCV.MaxValue,
                SpeedSixCharacter.OCV.CurrentValue);


        }
    }

    public class BaseCombatManueverTest
    {
        public HeroSystemCharacter Attacker;
        public HeroSystemCharacter Defender;
        public CombatSequence.CombatSequence Sequence;
        public CharacterTestObjectFactory Factory = new CharacterTestObjectFactory();

        [TestInitialize]
        public void SetupAttackerAndDefender()
        {
            
            Sequence = new CombatSequence.CombatSequence();

            Attacker = Factory.BaseCharacter;
            Attacker.Name = "attacker";
            Attacker.DEX.CurrentValue = 20;
            Defender = Factory.BaseCharacter;
            Defender.Name = "DOdger";



            Defender.DEX.CurrentValue = 10;
            Sequence.AddCharacter(Attacker);
            Sequence.AddCharacter(Defender);
            
            Sequence.StartCombat();

            //UnderlyingAttack attack = Attacker.Manuevers["Strike"] as UnderlyingAttack;
            //attack?.Target(Defender);

        }
    }
    [TestClass]

    public class DodgeTest : BaseCombatManueverTest
    {
        [TestMethod]
        public void CharacterActivatesDodge_HeHasPlusThreeDCV()
        {

            int DCV = Defender.DCV.CurrentValue;

            //act
            Defender.Manuevers["Dodge"].Perform();

            //assert
            Assert.AreEqual(DCV + 3, Defender.DCV.CurrentValue);
        }

        [TestMethod]
        public void NextPhaseActivates_DCVBonusIsRemoved()
        {
            int DCV = Defender.DCV.CurrentValue;
            Sequence.CompleteActivePhase();       
            //act

            Defender.Manuevers["Dodge"].Perform();
            Sequence.CompleteActivePhase();
            Sequence.CompleteActivePhase();

            //assert
            Assert.AreEqual(DCV, Defender.DCV.CurrentValue);
        }


        [TestMethod]
        public void DefenderCanAbortToDodgeOnlyBeforeBeingHitByAttack()
        {
            Attack attack = Attacker.Manuevers["Strike"] as Attack;
            attack?.Target(Defender);
            Assert.AreEqual(true, Defender.Manuevers["Dodge"].CanPerform);
            (Attacker.Manuevers["Strike"] as Attack).HitStatus = HitStatus.Hit;
            Assert.AreEqual(false, Defender.Manuevers["Dodge"].CanPerform);
        }


    }

    [TestClass]
     public class BlockTest : BaseCombatManueverTest
    {
        private Block Block;
        private Strike Strike;

        [TestInitialize]
        public void Given()
        {
            Block = Defender.Manuevers["Block"] as Block;
            Defender.OCV.CurrentValue = 60;      
            Attacker.OCV.CurrentValue = 30;
            Strike = Attacker.Manuevers["Strike"] as Strike;
            Strike.Target(Defender);
            Dice.RandomnessState = RandomnessState.max;
        }

        [TestMethod]
        public void BlockAttackRollIsSucesssful_ThenAttackIsBlocked()
        {
            Defender.OCV.CurrentValue = 60;      
            Attacker.OCV.CurrentValue = 30;
            Defender.Manuevers["Abort"].Perform();     
            Block.Perform();

            Assert.AreEqual(BlockStatus.BlockSuccessful, Block?.BlockStatus);
            Assert.AreEqual(HitResult.Blocked, Strike?.Result.HitResult);
            Assert.AreEqual(Sequence.ActivePhase, Attacker.ActivePhase);  


        }

        [TestMethod]
        public void DefenderAttackedByRangeAttack_DefenderCannotBlockTheAttack()
        {
            var ranged = new Attack("Basic Ranged", Attacker,
                DamageType.Normal, 5, DefenseType.PD,true);
            ranged.Target(Defender);
            Defender.Manuevers["Abort"].Perform();
            Assert.AreEqual(false, Block.CanPerform);

        }



        /*
        [TestMethod]
        public void DefenderSucessfullyBlocks_COntinueToBlockAtMinusTwoOCV()
        {
            AddAnotherNastyToTheCombat(11);       
            AddAnotherNastyToTheCombat(10);      
            AddAnotherNastyToTheCombat(9);        
            AddAnotherNastyToTheCombat(7);
            Defender.OCV.MaxValue = 60;
            Defender.OCV.CurrentValue = 60;  
            Attacker.OCV.MaxValue = 30;
            Attacker.OCV.CurrentValue = 30;
            Strike strike = Attacker.Manuevers["Strike"] as Strike;
            Defender.Manuevers["Abort"].Perform();
            Defender.Name = "Blocker";
            
            Block?.Perform();        
            Assert.AreEqual(BlockStatus.BlockSuccessful, Block?.BlockStatus);
            Assert.AreEqual(HitResult.Blocked, strike?.Result.HitResult);
            Assert.AreEqual(Defender.IsBlocking, true);

            ValidateSuccessfulBlockFromNextNasty(1);
            ValidateSuccessfulBlockFromNextNasty(2);
            //ValidateSuccessfulBlockFromNextNasty(3);


            Defender.OCV.CurrentValue = 2;
            strike = Sequence.ActivePhase.Character.Manuevers["Strike"] as Strike;
            strike?.PerformAttack(Defender);
            Assert.AreEqual(BlockStatus.BlockFailed, Block?.BlockStatus);
            Assert.AreEqual(HitResult.Hit, strike?.Result.HitResult);
            Assert.AreEqual(Defender.IsBlocking, false);


        }
        */

        private void AddAnotherNastyToTheCombat(int dex)
        {
            HeroSystemCharacter anotherNasty = Factory.BaseCharacter;
            anotherNasty.DEX.CurrentValue = dex;
            Sequence.AddCharacter(anotherNasty);
        }

        private void ValidateSuccessfulBlockFromNextNasty(int times)
        {
            Strike strike;
            int OCV = Defender.OCV.MaxValue;
            int expectedPenalty =  (2 * times);

            strike = Sequence.ActivePhase.Character.Manuevers["Strike"] as Strike;
            strike?.PerformAttack(Defender);

            Assert.AreEqual(BlockStatus.BlockSuccessful, Block?.BlockStatus);
            Assert.AreEqual(HitResult.Blocked, strike?.Result.HitResult);
            Assert.AreEqual(OCV - expectedPenalty, Defender.OCV.CurrentValue);
            Assert.AreEqual(Defender.IsBlocking, true);
        }
    }
    [TestClass]
    public class DisarmTest : BaseCombatManueverTest
    {
        [TestInitialize]
        public void setupDefenderAndAttackerWithDisarm()
        {
            
            Attacker.OCV.MaxValue = 50;
            Defender.HoldFocus(new Focus.Focus(FocusType.OAF));
            Attacker.STR.CurrentValue = 90;
            Defender.STR.CurrentValue = 30;
            Defender.Name = "Disarm";
        }

        [TestMethod]
        public void AttackerMakesSuccessfulDisarm_DisarmHits()
        {
            Disarm disarm = Attacker.Manuevers["Disarm"] as Disarm;
            disarm.Target(Defender);
            disarm.TargetedFocus = Defender.HeldFoci.First();

            disarm.Perform();
            Assert.AreEqual(HitResult.Hit, disarm.Result.HitResult);

        }

        [TestMethod]
        public void DisarmHitsAndStrengthRollFails_DefenderHoldsOntoTargetedFocus()
        {
            Disarm disarm = Attacker.Manuevers["Disarm"] as Disarm;
            Attacker.STR.CurrentValue = 1;
            disarm.Target(Defender);
            disarm.TargetedFocus = Defender.HeldFoci.First();

            disarm.Perform();

            Assert.AreEqual(disarm.Result.HitResult, HitResult.FocusHeld);
            Assert.AreEqual(disarm.TargetedFocus, Defender.HeldFoci.First());
            Assert.AreEqual(true,Defender.HeldFoci.Contains(disarm.TargetedFocus));


        }

        [TestMethod]
        public void DisarmHitsAndStrengthRollWins_DefenderDropsTargetedFocus()
        {
            Disarm disarm = Attacker.Manuevers["Disarm"] as Disarm;
            Attacker.STR.CurrentValue = 100;
            disarm.Target(Defender);
            disarm.TargetedFocus = Defender.HeldFoci.First();

            disarm.Perform();

            Assert.AreEqual(disarm.Result.HitResult, HitResult.Hit);
            Assert.AreEqual(false, Defender.HeldFoci.Contains(disarm.TargetedFocus));
            


        }

        [TestMethod]
        public void DisarmSucessful_TargetedFocusGoesFlyingUpToSixGameHexes()
        {
           

        }

        [TestMethod]
        public void DisarmTwoHandedWeapons_SufferAMinusTwoOCVPenalty()
        {
            Defender.HeldFoci.First().Drop();

            Focus.Focus twoHandedfocus = new Focus.Focus(FocusType.OAF);
            twoHandedfocus.HandsRequired = HandsRequired.TwoHanded;
            Defender.HoldFocus(twoHandedfocus);

            Disarm disarm = Attacker.Manuevers["Disarm"] as Disarm;
            disarm.Target(Defender);
            disarm.TargetedFocus = twoHandedfocus;

            Assert.AreEqual( - 2, disarm.Modifier.OCV.ModiferAmount);
            disarm.TargetedFocus = Defender.HeldFoci.First();
        }

        public void DefenderInteruptsAttackerWithDisarmUsingHeldAction_DefenderMustWinDexRollToBeAbleToDisarmBeforeBeingAttacked()
        {
        }

        [TestMethod]
        public void DefenderSuccessfullyInterruptsAttackerWithDisarmUsingHeldAction_AttackerLosesHisAction()
        {
            
            Sequence.CompleteActivePhase();


            HoldActionManuever hold = Defender.Manuevers["Hold Action"] as HoldActionManuever;
            hold.Perform();

            
            Strike strike = Attacker.Manuevers["Strike"] as Strike;
            strike.Target(Attacker);

            hold.Interrupt(InterruptionWith.Defensive);

            Disarm disarm = Attacker.Manuevers["Disarm"] as Disarm;
            Phase expectedActivePhase = Attacker.ActivePhase;
            disarm.Target(Defender);
            disarm.TargetedFocus = Defender.HeldFoci.First();

            disarm.Perform();

            Assert.AreEqual(null, Sequence.InterruptedPhase);
            Assert.AreNotEqual(expectedActivePhase, Sequence.ActivePhase);

        }
    }

    [TestClass]
    public class BraceTest : BaseCombatManueverTest
    {


        [TestMethod]
        public void CharacterIsBraced_CharacterHasPlusTwoThatAffectsRangeOffsetOnly()
        {
            Defender.Hex = new GameHex(1, 1, 1);
            Attacker.Hex = new GameHex(1, 9, 1);

            Attack ranged = new Attack("Basic Ranged", Attacker,
                DamageType.Normal, 5, DefenseType.PD, true);
            Attack rangedAttack = Attacker.Manuevers["Basic Ranged"] as Attack;
            rangedAttack?.Target(Defender);

            int changeToHit = rangedAttack.RollRequiredToHitDefender;
            int DCV = Attacker.DCV.CurrentValue;
            Attacker.Manuevers["Brace"].Perform();


            Assert.AreEqual(11, rangedAttack.RollRequiredToHitDefender);
            Assert.AreEqual(2, Attacker.DCV.CurrentValue);


        }


       
    }

    [TestClass]
    public class BasicGrabTest : BaseCombatManueverTest
    {
        public Grab Grab;
        [TestInitialize]
        public void setupDefenderAndAttackerWithGrab()
        {
            
            Attacker.Hex = new GameHex(1, 0, 0);         
            Defender.Hex = new GameHex(1, 0, 0);
            

            Attacker.OCV.MaxValue = 10;
            Attacker.STR.CurrentValue = 90;
            Defender.STR.CurrentValue = 30;

            Grab = Attacker.Manuevers["Grab"] as Grab;
        }
        [TestMethod]
        public void DefenderIsGrabbedSucessfullyAndFailsCasualStrRoll_HeIsGrabbedByAttacker()
        {
            Grab.Target(Defender);
            Grab.Perform();
            Assert.AreEqual(Defender.IsGrabbed, true);
            Assert.AreEqual(Defender.GrabbedBy, Attacker);
        }

        [TestMethod]
        public void AttackerNotInSameHex_CannotGrabDefender()
        {
            Grab.Target(Defender);
            Assert.AreEqual(Grab.CanPerform, true);

            Defender.Hex = new GameHex(2, 0, 0);
            Assert.AreEqual(Grab.CanPerform, false);
        }

        [TestMethod]
        public void AttackerMuchSmallerThanDefender_CannotGrabDefender()
        {
            Defender.TimesHumanSize = 3;
            Attacker.TimesHumanSize = 1;

            Grab.Target(Defender);
            Assert.AreEqual(Grab.CanPerform, false);
        }

        [TestMethod]
        public void NextPhaseActivatesForGrabbedDefender_MayMakeAnImmediateCasualStrengthRollToEscape()
        {

            Grab.Target(Defender);
            Grab.Perform();        

            Defender.Manuevers["Hold Action"].Perform();
            Attacker.STR.CurrentValue = 1;

            Attacker.Manuevers["Hold Action"].Perform();

            Assert.AreEqual(Defender.IsGrabbed, false);


        }

        [TestMethod]
        public void AttackerGrabsWithOneHand_AttackerSTRIsMinusFive()
        {
            Grab.Target(Defender);
            Grab.UsingOneHand = true;

          //  Assert.AreEqual(Grab.EffectiveSTR, Attacker.STR.CurrentValue - 5);

        }

    }
    [TestClass]
    public class GrabFocusTest : BasicGrabTest
    {
        private Weapon Gun;
        private int AttackerOCV;
        [TestInitialize] 
        public void setupDefenderWithFocus()
        {
            Attacker.Name = "Grabber";
            Defender.Name = "Gunner";
            Gun = new Weapon(FocusType.OAF);
            ((WeaponManuever)Defender.Manuevers["Weapon Manuever"]).Weapon = Gun;
            Defender.HeldFoci.Add(Gun);
        }
        [TestMethod]
        public void GrabbingDefenderArms_DefenderCannotUseAccesibleFocus()
        {
            Grab.Target(Defender);
            Grab.TargetedFocus = Gun;

            Grab.Perform();

            Assert.AreEqual(Defender.Manuevers["Weapon Manuever"].CanPerform, false);
        }

        [TestMethod]
        public void AttackerGrabbingDefenderFocus_PerformsGrabAtMinusTwoOCV()
        {      
            Grab.Target(Defender);
            Grab.TargetedFocus = Gun;
            Assert.AreEqual(Grab.Modifier.OCV.ModiferAmount, -3);
        }

        [TestMethod]
        public void AttackerSuccessfullyGrabsDefenderFocus_AttackerIsHoldingFocusIfHeWinsSTRRoll()
        {
            Grab.Target(Defender);
            Grab.TargetedFocus = Gun;
            Grab.Perform();

            Assert.AreEqual(true, Attacker.HeldFoci.Contains(Gun));
            Assert.AreEqual(false, Defender.HeldFoci.Contains(Gun));
        }

        [TestMethod]
        public void AttackerSuccessfullyGrabsDefenderFocus_NeitherDefenderNorAttackerCanUseFocusUntilOneWinsAStrRoll()
        {
           
            Defender.STR.CurrentValue = Attacker.STR.CurrentValue * 3;
            Grab.Target(Defender);
            Grab.TargetedFocus = Gun;
            Grab.Perform();

            Assert.AreEqual(HitResult.NotInControl, Grab.Result.HitResult);
            Assert.AreEqual(Defender.Manuevers["Weapon Manuever"].CanPerform, false);

            PullFocusAwayFromEnemy pull = (PullFocusAwayFromEnemy)Defender.Manuevers["Pull Focus Away"];


            pull.Perform();
            Assert.AreEqual(Defender.Manuevers["Weapon Manuever"].CanPerform, true);

        }
        [TestMethod]
        public void AttackerSuccessfullyGrabsDefenderFocus_OnlySuffersDCVHalvingForThatSegment()
        {
            int DCV = Attacker.DCV.CurrentValue;
            Defender.STR.CurrentValue = 0;
            Grab.Target(Defender);
            Grab.TargetedFocus = Gun;
            Grab.Perform();
            Sequence.CompleteActivePhase();  

            Assert.AreEqual(DCV, Attacker.DCV.CurrentValue);

        }




    }
    [TestClass]
    public class GrabModifiersToCVTest : BasicGrabTest
    {
        private int DefenderOriginalOCV;
        private int DefenderOriginalDCV;
        private int AttackerOriginalOCV;
        private double AttackerOriginalDCV;
        private HeroSystemCharacter OtherCombatant;


        [TestInitialize]
        public void Given()
        {
            setupDefenderAndAttackerWithGrab();
           
            DefenderOriginalOCV = Defender.OCV.CurrentValue;
            DefenderOriginalDCV = Defender.DCV.CurrentValue;
            AttackerOriginalDCV = Attacker.DCV.CurrentValue;
            AttackerOriginalOCV = Attacker.OCV.CurrentValue;
            OtherCombatant = Factory.BaseCharacter;


        }
        [TestMethod]
        public void AttackerSuccesfullyGrabsDefender_MinusTwoDCVBonusIsReplacedWithHalfMultiplier()
        {
            Grab.Target(Defender);
            Assert.AreEqual(-2, Grab.Modifier.DCV.ModiferAmount);

            Grab.Perform();
            Assert.AreEqual(Math.Ceiling((double)(Attacker.DCV.MaxValue *.5)), Attacker.DCV.CurrentValue);
        }
        [TestMethod]
        public void AttackerSuccesfullyGrabsDefender_DefenderHasMinusThreeOCVAgainstAttackerAndHalfAgainstEveryoneElseAndHalfDCV()
        {
            Grab.Target(Defender);         
            Grab.Perform();

            Strike strike = Defender.Manuevers["Strike"] as Strike;
            strike?.Target(Attacker);
            Assert.AreEqual(DefenderOriginalOCV, Defender.OCV.CurrentValue + 3);

            strike.Target(OtherCombatant);
            Assert.AreEqual(Math.Ceiling((double)DefenderOriginalOCV * .5), Defender.OCV.CurrentValue);
            Assert.AreEqual(Math.Ceiling((double)DefenderOriginalDCV * .5), Defender.DCV.CurrentValue);
        }

        [TestMethod]
        public void AttackerSuccesfullyGrabsDefender_AttackerHasFullOCVAgainstDefenderAndHalfAgainstEveryoneElseAndHalfDCV()
        {
            Grab.Target(Defender);
            Grab.Perform();
            Defender.ActivePhase.Complete();

            Squeeze squeeze = Attacker.Manuevers["Squeeze"] as Squeeze;
            Assert.AreEqual(AttackerOriginalOCV, Attacker.OCV.CurrentValue);

            Strike strike = Attacker.Manuevers["Strike"] as Strike;
            strike?.Target(OtherCombatant);
            Assert.AreEqual(Math.Ceiling((double)AttackerOriginalOCV * .5), Attacker.OCV.CurrentValue);
        }
        [TestMethod]
        public void GrabbedDefenderIsStrongerByTwentySTR_MayAttackeGrabbingAttackerAtFullOCVNonGrabbingAttackerAtMinusThreeOCVAndHasMinusTwoDCV()
        {;
            Defender.STR.CurrentValue = Attacker.STR.CurrentValue + 20;
            Grab.Target(Defender);
            Grab.Perform();

            Strike strike = Defender.Manuevers["Strike"] as Strike;
            strike?.Target(Attacker);
            Assert.AreEqual(DefenderOriginalOCV, Defender.OCV.CurrentValue);

            strike?.Target(OtherCombatant);
            Assert.AreEqual(DefenderOriginalOCV - 3, Defender.OCV.CurrentValue);

        }
    }

    [TestClass]
    public class GrabAndSqueezeTest : BaseCombatManueverTest
    {
        public Grab Grab;
        public Squeeze Squeeze;
        [TestInitialize]
        public void Given()
        {
            Attacker.Hex = new GameHex(1, 0, 0);
            Defender.Hex = new GameHex(1, 0, 0);


            Attacker.OCV.MaxValue = 10;
            Attacker.STR.CurrentValue = 90;
            Defender.STR.CurrentValue = 30;

            Grab = Attacker.Manuevers["Grab"] as Grab;

            //setupDefenderAndAttackerWithGrab();

            Grab.Target(Defender);
            Grab.FollowUp = GrabFollowUps.Squeeze;

            Dice.RandomnessState = RandomnessState.max;
            Grab.Perform();

            Squeeze = Attacker.Manuevers["Squeeze"] as Squeeze;


        }

        [TestMethod]
        public void GrabberSqueezesOrThrowsGrabbedDefenderLaterPhase_AttackRollIsrequired()
        {

            

            Defender.DCV.CurrentValue = 60;
            Defender.Manuevers["Hold Action"].Perform();

            Squeeze.Perform();

            AttackResult result = Squeeze.Result;
            Assert.AreEqual(HitResult.Miss, result.HitResult);


        }

        [TestMethod]
        public void GrabberSqueezesGrabbedDefenderLaterPhase_GrabIsStillActive()
        { 
            Defender.Manuevers["Hold Action"].Perform();
            Squeeze.Perform();
            Assert.AreEqual(Attacker.ActiveManuever, Grab);
        }

        [TestMethod]
        public void AttackerHasNotGrabbedDefender_AttackerCannotSqueezeOrThrowDefender()
        {
            Assert.AreEqual(false, Defender.Manuevers["Grab"].CanPerform);
        }

        [TestMethod]
        public void AttackerSuccessfullyGrabsDefender_ThenHeCanAutomaticallySqueezeOrThrowDefender()
        {       
            int squeezeDamage = new NormalDamageDicePool(Attacker.STR.STRDamage).Roll();
            int expected = Defender.STUN.MaxValue - (squeezeDamage - Defender.PD.CurrentValue);
            Assert.AreEqual(expected, Defender.STUN.CurrentValue);
        }



    }

    [TestClass]
    public class GrabAndThrowTest : BasicGrabTest
    {
        public void Given()
        {

            setupDefenderAndAttackerWithGrab();

        }


        public void AttackerSuccessfullyGrabsDefenderAndThrow_DoesAttackerSTRDamageToTargetAndThrowsHim()
        {
        }

        public void AttackerThrowsGrabeeAtAnotherDefender_MustRollSeparateAttackRollToHitDefender()
        {
        }

        public void AttackerThrowsGrabeeAtAnotherDefender_DoesFullStrDamageToBothGrabeeAndDefender()
        {
        }

        public void AttackerThrowsGrabeeOnNextphaseAndFailsAttackRoll_GrabeeIsFree()
        {
        }
    }

    [TestClass]
    public class GrabbeOptionsTest : BasicGrabTest
        {

        public Grab Grab;

        [TestInitialize]
        public void Given()
        {
              

        }


        public void DefenderHasJustBeenGrabbed_DefenderMayUseMartialEscapeAsPartOfFreeCasualStrenghthRollToEscape()
        { }
        public void DefenderHasJustBeenGrabbed_DefenderMayNotUseContortionistAsPartOfFreeCasualStrenghthRollToEscape()
        { }
        public void GrabbedDefender_MayOnlyAttackUsingFreeLimbs() { }
        public void NextPhaseActivatesForGrabbedDefender_UsingContortionistOrMovementBonusesCauseCasualEscapeToBeAHalfPhaseAction(){ }


    }

    [TestClass]
    public class HaymakerTest  
    {
        private Haymaker Haymaker;
        private Attack BaseAttack;
        public HeroSystemCharacter Attacker;
        public HeroSystemCharacter Defender;
        public CombatSequence.CombatSequence Sequence;
        public CharacterTestObjectFactory Factory = new CharacterTestObjectFactory();
        [TestInitialize]
        public void Given()
        {
            
            Dice.RandomnessState = RandomnessState.average;
            Sequence = new CombatSequence.CombatSequence();

            Attacker = Factory.BaseCharacter;
            Attacker.Name = "haymakerer";
            Attacker.DEX.CurrentValue = 20;
            Defender = Factory.BaseCharacter;
            Defender.Name = "defender";
            Defender.SPD.MaxValue = 5;
            Attacker.SPD.MaxValue = 6;
            Defender.DEX.CurrentValue = 10;
            Sequence.AddCharacter(Attacker);
            Sequence.AddCharacter(Defender);
            Haymaker = Attacker.Manuevers["Haymaker"] as Haymaker;
            BaseAttack = new Attack("BaseAttack",Attacker,DamageType.Normal, 10,DefenseType.PD);
            Sequence.StartCombat();

            Sequence.CompleteActivePhase();
            Sequence.CompleteActivePhase();
        }


        [TestMethod]
        public void AttackerHaymakersDefender_HaymakerLandsAtEndOfSegmentFollowingPhase()
        {
            Haymaker.UnderlyingAttack = BaseAttack;
            Haymaker.PerformAttack(Defender);

            //haymaker goes to phase 3
            Assert.AreEqual(3,Sequence.ActiveSegment.Number);
            Assert.AreEqual(Haymaker,Attacker.ActiveManuever);
            Assert.AreEqual(Defender.STUN.MaxValue, Defender.STUN.MaxValue);
            Assert.AreEqual(Haymaker, Attacker.ManueverInProgess);
            
            //end phase 3
            Sequence.CompleteActivePhase();
            Assert.AreEqual(null,Attacker.ManueverInProgess);

            Assert.IsTrue(Defender.STUN.MaxValue > Defender.STUN.CurrentValue);

        }

        [TestMethod]
        public void HaymakerDamage_Does4ExtraDamageClassToUnderlyingAttack()
        {

            Haymaker.UnderlyingAttack = BaseAttack;
            Haymaker.PerformAttack(Defender);

            Assert.AreEqual(14, BaseAttack.Damage.DamageDiceNumber);


        }
        [TestMethod]
        public void CharacterAbortsInSegmentHaymakerLaunches_HaymakerIsCancelled()
        {
            Phase haymakeringPhase = Attacker.ActivePhase;
            Haymaker.UnderlyingAttack = BaseAttack;
            Haymaker.PerformAttack(Defender);

            Attacker.Manuevers["Abort"].Perform();

            Assert.AreEqual(0,Attacker.DamageClassModifier);

            Assert.AreNotEqual(Attacker.ActiveManuever,Haymaker);
            Assert.AreNotEqual(haymakeringPhase,Attacker.ActivePhase);
        }

        public void CharacterMovesMoreThanOneInchBeforeHaymakerLaunched_HaymakerIsCancelled()
        {
            


        }

        public void CharacterSuffersKnockbackBeforeHaymakerLaunched_HaymakerIsCancelled()
        {
        }

        [TestMethod]
        public void CharacterStunnedOrUnconsiousBforekHaymakerLaunched_HaymakerIsCancelled()
        {
            Phase haymakeringPhase = Attacker.ActivePhase;
            Haymaker.UnderlyingAttack = BaseAttack;
            Haymaker.PerformAttack(Defender);

            Defender.DamageClassModifier = 100;
            Defender.OCV.MaxValue = 99;

            Strike s = Defender.Manuevers["Strike"] as Strike;
            s.PerformAttack(Attacker);

            Assert.AreEqual(0, Attacker.DamageClassModifier);
            Assert.AreNotEqual(Attacker.ManueverInProgess, Haymaker);
            Assert.AreNotEqual(haymakeringPhase, Attacker.ActivePhase);
        }

    }

    [TestClass]
    public class SetTest : BaseCombatManueverTest
    {
        private Set Set;
        private Attack Ranged;
        private Attack Strike;
        private int OCVwithSet;
        [TestInitialize] 
        public void AttackerWithSetAndRangedAndStrike()
        {
            Set = Attacker.Manuevers["Set"] as Set;
            Strike = Attacker.Manuevers["Strike"] as Strike;
            Set.Target = Defender;
            Set.Perform();
            Ranged = new Attack( "Ranged", Attacker, DamageType.Normal, 12,DefenseType.PD,true);
            OCVwithSet = Attacker.OCV.CurrentValue;
        }

        [TestMethod]
        public void SetDoesNotWotkWithHTHManuevers()
        {          
            Strike.Target(Defender);
            Assert.AreEqual(OCVwithSet, Attacker.OCV.CurrentValue + 1);
        }

        [TestMethod]
        public void SetOnlyWorksWithRangeManuevers()
        {
            Ranged.Target(Defender);
            Assert.AreEqual(OCVwithSet, Attacker.OCV.CurrentValue);
        }

        [TestMethod]
        public void AttackerAimsAtSomeoneElse_SetManueverIsInteruptted()
        {
            Ranged.Target(Factory.BaseCharacter);
            Assert.AreEqual(OCVwithSet, Attacker.OCV.CurrentValue+1);
        }

        [TestMethod]
        public void AttackerWithSetIsKnockedBackOrOutOrStunned_SetManueverIsInteruptted()
        {
            Damage damage = new Damage(20,DamageType.Normal, DefenseType.PD);
            Attacker.TakeDamage(damage.RollDamage());

            Assert.AreEqual(OCVwithSet, Attacker.OCV.CurrentValue + 1);
        }

        [TestMethod]
        public void AttackerWithSetLosesBeadOnDefender_SetManueverIsInteruptted()
        {
            
            Ranged.Target(null);
            Assert.AreEqual(OCVwithSet, Attacker.OCV.CurrentValue + 1);
        }

        [TestMethod]
        public void AttackerWithSetDoesNotEitherAimsOrAttacksWithRangeInLaterPhase_ContinuesToKeepHisSetBonus()
        {
            Ranged.Target(Defender);
            var x = Sequence.ActivateNextPhaseInSequence;
            x = Sequence.ActivateNextCombatPhaseInActiveSegment;
            //do nothing but maintian target
            Assert.AreEqual(OCVwithSet, Attacker.OCV.CurrentValue);

            //attack target
            Ranged.PerformManuever();
            Assert.AreEqual(OCVwithSet, Attacker.OCV.CurrentValue);

            //do something else lose target
            x = Sequence.ActivateNextPhaseInSequence;
            Attacker.Manuevers["Dodge"].Perform();
            Assert.AreEqual(OCVwithSet, Attacker.OCV.CurrentValue + 1);



        }


        [TestMethod]
        public void AttackerWhoPerformsSetAndBrace_AttackerHasCombinedBonuses()
        {
            Defender.ActivePhase.Complete();
 
            Set.IsBracing = true;
            Set.Perform();

            Assert.AreEqual(OCVwithSet, Attacker.OCV.CurrentValue);
            Assert.AreEqual(2, Attacker.RangedModModifier);
            Assert.AreEqual(.5, Attacker.DCV.Multiplier);
        }


    }

    [TestClass]
    public class BlazingAwayTest : BaseCombatManueverTest
    {
        private BlazingAway BlazingAway;

        [TestInitialize]
        public void AttackerWithBlazingAwayThatHasTargetedThreeCharacters()
        {
            BlazingAway = Attacker.Manuevers["Blazing Away"] as BlazingAway;

            BlazingAway.UnderlyingAttack = new Attack( "Ranged", Attacker, DamageType.Normal, 12, DefenseType.PD, true);
            BlazingAway.AddTarget(Factory.BaseCharacter);
            BlazingAway.AddTarget(Factory.BaseCharacter);
            BlazingAway.AddTarget(Factory.BaseCharacter);
        }

        [TestMethod]
        public void AttackerPerformsBlazingAwayAtMultipleTargets_EachTargetIsAttacked()
        {
            BlazingAway.Perform();
            List<AttackResult> results = BlazingAway.Results;

            Assert.AreEqual(3, results.Count);


            foreach (var result in results)
            {
                Assert.AreEqual(HitResult.Miss, result.HitResult);
                Assert.IsNotNull(result.HitResult);
                Assert.IsNotNull(result.Target);
                Assert.IsNotNull(result.DamageResult);

            }



        }

        public void AttackerPerformsBlazingAway_PerformsIndpendantAttackRollForEachTarget()
        {
             BlazingAway.Perform();
            
        }

        [TestMethod]
        public void AttackerPerformsBlazingAway_OnlyHitsOnAThreeOrless()
        {
            Assert.AreEqual(3, BlazingAway.RollRequiredToHitDefender);
        }

        public void AttackerPerformsBlazingAwayWithAreaEffect_OnlyDoesDamageOnThreeOrLess() { }

        public void AttackercannotPerformsBlazingAwayWithAttackThatHasExtraTimelimitation()
        {
            
        }


    }

    [TestClass]
    public class ClubTest: BaseCombatManueverTest
    {
        private Club Club;
        [TestInitialize]
        public void AttackerWithClubManueverForHandKillingAttack()
        {
            Club = Attacker.Manuevers["Club"] as Club;
            Club.UnderlyingAttack = new Attack("Killing UnderlyingAttack", Attacker, DamageType.Killing,6, DefenseType.PD, false);



        }

        [TestMethod]
        public void AttackerClubsWithHandKillingAttack_DoesNormalDamageBasedOnKilingAttackDamage()
        {
            Assert.AreEqual(DamageType.Normal,Club.Damage.DamageType);
            Assert.AreEqual(Club.UnderlyingAttack.DamageDiceNumber * 3, Club.DamageDiceNumber);

        }

    }

    [TestClass]
    public class CoverTest : BaseCombatManueverTest
    {
        private Cover Cover;

        [TestInitialize]
        public void AttackerWithCoverForRangedAttack()
        {
            Cover = Attacker.Manuevers["Cover"] as Cover;
            Cover.UnderlyingAttack = new Attack("Ranged UnderlyingAttack", Attacker, DamageType.Normal, 7, DefenseType.PD, true);
            Cover.Target(Defender);
        }

        [TestMethod]
        public void AttackerSucessfullyCoversDefender_NoDamageIsDone()
        {
            Cover.HitStatus = HitStatus.Hit;
            Cover.Perform();
            Assert.AreEqual(Defender.STUN.MaxValue, Defender.STUN.CurrentValue);
        }

        [TestMethod]
        public void AttackerSucessfullyCoversDefender_CanInflictDamageFromCoveredAttackWheneverAttackerWants()
        {
            Cover.HitStatus = HitStatus.Hit;
            Cover.Perform();

            Strike strike = Defender.Manuevers["Strike"] as Strike;
            strike.Target(Attacker);

            Cover.Interrupt();

            Assert.AreEqual
                (Defender.STUN.MaxValue - Cover.UnderlyingAttack.Result.DamageResult.STUN + Defender.PD.CurrentValue, Defender.STUN.CurrentValue);

        }

        [TestMethod]
        public void DefenderSucessfullyDistractsAttackerAndWinsDexRoll_DefenderIsNoLongerCovered()
        {

        }
    }

    [TestClass]
    public class HipShotTest : BaseCombatManueverTest
    {
        private HipShot HipShot;
        private Attack Ranged;

        [TestInitialize]
        public void AttackerWithSlowerDexThanDefenderAndHipShotForRangedAttack()
        {
            
            HipShot  = Defender.Manuevers["Hip Shot"] as HipShot;

            Defender.DEX.MaxValue = 20;
            Ranged = new Attack("Ranged UnderlyingAttack", Defender, DamageType.Normal, 7, DefenseType.PD, true);

        }

        [TestMethod]
        public void AttackertBeginningOfSegmentHeHasPhas_AttackerCanPerformHipshotAe()
        {
            Assert.AreNotEqual(Sequence.ActivePhase, Defender.ActivePhase);
            Assert.AreEqual(true, HipShot.CanPerform);
        }

        [TestMethod]
        public void AttackerPerformsHipshot_GoesInOrderOfPlusOneDexOfCurrentDexvalue()
        {
            HipShot.Perform();
            Assert.AreEqual(Sequence.ActivePhase, Defender.ActivePhase);
            Phase phase = Sequence.ActivePhase;
        }

        [TestMethod]
        public void AttackerPerformsHipshot_DexIsBackToNormalForUnderlyingAttack()
        {
            HipShot.Perform();
            Ranged.PerformAttack(Attacker);
            Sequence.ActivePhase.Complete();
            Assert.AreEqual(Defender.DEX.MaxValue, Defender.DEX.CurrentValue);
        }

        [TestMethod]

        public void AttackerPerformsHipshot_AttackerhasMinusOneOCVWhenAttacking()
        {
            HipShot.Perform();
            Assert.AreEqual(Defender.OCV.MaxValue, Defender.OCV.CurrentValue + 1);
            
        }
    }




    [TestClass]
    public class HurryTest : BaseCombatManueverTest
    {
        private Hurry Hurry;
        private Attack Ranged;

        [TestInitialize]
        public void AttackerWithSlowerDexThanDefenderAndHurryForRangedAttack()
        {
            Hurry = Defender.Manuevers["Hurry"] as Hurry;

            Defender.DEX.MaxValue = 20;
            Ranged = new Attack("Ranged UnderlyingAttack", Defender, DamageType.Normal, 7, DefenseType.PD, true);
        }

        [TestMethod]
        public void AttackertBeginningOfSegmentHeHasPhas_AttackerCanPerformHurryAe()
        {
            Assert.AreNotEqual(Sequence.ActivePhase, Defender.ActivePhase);
            Assert.AreEqual(true, Hurry.CanPerform);
        }

        [TestMethod]
        public void AttackerPerformsHurry_GoesInOrderOfPlusOneDexOfCurrentDexvalue()
        {
            Hurry.Perform();
            Assert.AreEqual(Sequence.ActivePhase, Defender.ActivePhase);
            Phase phase = Sequence.ActivePhase;
        }

        [TestMethod]
        public void AttackerPerformsHurry_DexIsBackToNormalForUnderlyingAttack()
        {
            Hurry.Perform();
            Ranged.PerformAttack(Attacker);
            Sequence.ActivePhase.Complete();
            Assert.AreEqual(Defender.DEX.MaxValue, Defender.DEX.CurrentValue);
        }

        [TestMethod]

        public void AttackerPerformsHurry_AttackerhasMinusTwoGlobalWhenAttacking()
        {
            Hurry.Perform();
            Assert.AreEqual(Defender.GlobalModifier, -2);
        }
    }

    [TestClass]
    public class PullingAPunchTest: BaseCombatManueverTest
    {
        private PullingAPunch Pull;

        [TestInitialize]
        public void AttackerWithMassiveSTRAndPulledPunchForStrike()
        {
            Attacker.STR.MaxValue = 100;
            Pull = Attacker.Manuevers["Pulling A Punch"] as PullingAPunch;
            Pull.UnderlyingAttack = Attacker.Manuevers["Strike"] as Strike;
            Dice.RandomnessState = RandomnessState.average;
            Pull.Target(Defender);
            
        }
        [TestMethod]
        public void AttackerPullsPunch_LosesOneOCvPer5DCsOfAttack()
        {
            Assert.AreEqual(-4, Pull.Modifier.OCV.ModiferAmount);
        }



        [TestMethod]
        public void AttackerPullsPunchAndMakesExactAttackRoll_DoesFullDamage()
        {
            Attacker.OCV.MaxValue = 6;
            
            int fullBod = Pull.Damage.RollDamage().BOD;

            
            Pull.Perform();
            AttackResult result = Pull.Result;

            Assert.AreEqual(result.DamageResult.BOD, fullBod);
        }

        [TestMethod]
        public void AttackerPullsPunch_DoesHalfBody()
        {
            Attacker.OCV.MaxValue = 9;
            int fullBod = Pull.Damage.RollDamage().BOD;

            Pull.Perform();

            Assert.AreEqual(fullBod /2 - Defender.PD.CurrentValue, Defender.BOD.MaxValue - Defender.BOD.CurrentValue );

        }
    }

    [TestClass]
    public class RapidFireManuever : BaseCombatManueverTest
    {
        private RapidFire RapidFire;
        private Attack Ranged;
        private HeroSystemCharacter NextDefender;
        private HeroSystemCharacter LastDefender;

        [TestInitialize]
        public void AttackerWithRapidFireForRangedManueverWithOCVAndDCVBonusesAndDCVMultiplierPenalityAndThreeDefenders()
        {
            RapidFire = Attacker.Manuevers["Rapid Fire"] as RapidFire;
            Ranged = new Attack("Ranged UnderlyingAttack", Attacker, DamageType.Normal,20, DefenseType.PD, true);
            Ranged.Modifier.OCV.ModiferAmount = 0;
            Ranged.Modifier.DCV.ModiferAmount = -1;
            Ranged.Modifier.DCV.Multiplier = .5;
            RapidFire.UnderlyingAttack = Ranged;
            Dice.RandomnessState = RandomnessState.average;

            NextDefender = Factory.BaseCharacter;
            LastDefender = Factory.BaseCharacter;

            



        }

        [TestMethod]
        public void AttackerRapidFires_AttackerHasHalfDCV()
        {
            RapidFire.ActivateModifier();
            Assert.AreEqual(Math.Ceiling((double) (Attacker.DCV.MaxValue * 1/2)), Attacker.DCV.CurrentValue);
        }

        [TestMethod]
        public void AttackerRapidFires_TakesFullPhaseAction()
        {
            Phase phase = Attacker.ActivePhase;
            RapidFire.AddTarget(Defender);
            RapidFire.Perform();
            Assert.AreEqual(true, phase.Finished);
        }



   
            
        [TestMethod]
        public void AttackerRapidFireMultipleTimes_AttackerSuffersMinus2OCVForEachShot()
        {
            Phase phase = Attacker.ActivePhase;
            targetThreeDefenders();
            Assert.AreEqual(5, RapidFire.RollRequiredToHit(Defender));

            RapidFire.RemoveTarget(Defender);
            Assert.AreEqual(7, RapidFire.RollRequiredToHit(Defender));


        }

        private void targetThreeDefenders()
        {
            RapidFire.AddTarget(Defender);
            RapidFire.AddTarget(NextDefender);
            RapidFire.AddTarget(LastDefender);
        }

        [TestMethod]
        public void AttackerRapidFiresMultipleTimesAndMisses_AllSubsequentAttacksMiss()
        {
           
            targetThreeDefenders();

            //should hit both first and last target but not middle
            Defender.DCV.CurrentValue = -6;
            LastDefender.DCV.CurrentValue = -6;
            RapidFire.Perform();

            Assert.AreEqual(HitResult.Hit,RapidFire.Results[0].HitResult);
            Assert.AreEqual(HitResult.Miss, RapidFire.Results[1].HitResult);
            Assert.AreEqual(HitResult.Miss, RapidFire.Results[2].HitResult);




        }

        [TestMethod]
        public void AttackerTargetsRapidFireAgainstMultipleTargets_AttackerCAnnotTargetCharactersOutside180VisionField(){ }

        [TestMethod]
        public void AttackerRapidFireMultipleTimeAtSingleTargetAndDoesKnockback_OnlyMostSeverknockbackapplied()
        {
            Defender.DCV.CurrentValue = -6;
            Defender.PD.CurrentValue = 0;

            targetFirstDefenderThreeTimes();

            RapidFire.Perform();

            //3 20d6 attacks do 210 damage + 21 knockback on an average roll 
            Assert.AreEqual(231, Defender.STUN.MaxValue - Defender.STUN.CurrentValue);



        }
        private void targetFirstDefenderThreeTimes()
        {
            RapidFire.AddTarget(Defender);
            RapidFire.AddTarget(Defender);
            RapidFire.AddTarget(Defender);

        }

        [TestMethod]
        public void
            AttackerRapidFireMultipleTimeAtMultipleTargetAndDoesKnockback_OnlyMostSevereKnockbackIsAppliedToEachTarget()
        {
            targetAllDefenderThreeTimes();
            RapidFire.Perform();

            Assert.AreEqual(231, Defender.STUN.MaxValue - Defender.STUN.CurrentValue);
            Assert.AreEqual(231, Defender.STUN.MaxValue - NextDefender.STUN.CurrentValue);
            Assert.AreEqual(231, Defender.STUN.MaxValue - LastDefender.STUN.CurrentValue);
        }
        private void targetAllDefenderThreeTimes()
        {
            Attacker.OCV.CurrentValue=99;
            RapidFire.AddTarget(Defender);
            RapidFire.AddTarget(Defender);
            RapidFire.AddTarget(Defender);
            Defender.PD.CurrentValue = 0;

            RapidFire.AddTarget(NextDefender);
            RapidFire.AddTarget(NextDefender);
            RapidFire.AddTarget(NextDefender);
            NextDefender.PD.CurrentValue = 0;

            RapidFire.AddTarget(LastDefender);
            RapidFire.AddTarget(LastDefender);
            RapidFire.AddTarget(LastDefender);
            LastDefender.PD.CurrentValue = 0;
        }

        [TestMethod]
        public void AttackerRapidFireMultipleTimes_UnderlyingCVPenaltiesAreCumulativeForEachAttack()
        {
            
            int originalToHitRoll = RapidFire.RollRequiredToHit(Defender);
            Ranged.Modifier.OCV.ModiferAmount = -1;
            RapidFire.UnderlyingAttack = Ranged;
            targetFirstDefenderThreeTimes();

            Assert.AreEqual(originalToHitRoll-3-6, RapidFire.RollRequiredToHit(Defender));
            Assert.AreEqual(- 3, RapidFire.UnderlyingAttack.Modifier.DCV.ModiferAmount);

        }

        [TestMethod]
        public void AttackerRapidFireMultipleTimes_UnderlyingCVBonusesAreAppliedOnceOnly()
        {
            int originalToHitRoll = RapidFire.RollRequiredToHit(Defender);
            Ranged.Modifier.OCV.ModiferAmount = +1;
            RapidFire.UnderlyingAttack = Ranged;
            targetFirstDefenderThreeTimes();

            Assert.AreEqual(originalToHitRoll +1 - 6, RapidFire.RollRequiredToHit(Defender));
        }

        [TestMethod]
        public void AttackerRapidFireMultipleTimes_ExistingDCVPenaltiesAppliedBeforeHalvingDCV()
        {
            Attacker.OCV.MaxValue = 20;
            Attacker.DCV.MaxValue = 10;
            targetFirstDefenderThreeTimes();
            RapidFire.Perform();
            Assert.AreEqual(Math.Ceiling((decimal) (Attacker.DCV.MaxValue -3)/4) , Attacker.DCV.CurrentValue);
        }

        [TestMethod]
        public void AttackerRapidFire_DCVMultiplierPenaltyNotAddToRapidFireMultiplierPenalty()
        {
            RapidFire.UnderlyingAttack.Modifier.DCV.Multiplier = .25;
            targetFirstDefenderThreeTimes();
            RapidFire.Perform();

            Assert.AreEqual(.25, Attacker.DCV.Multiplier);
        }

    }

    [TestClass]
    public class RollWithAPunchTest : BaseCombatManueverTest
    {
        Strike Strike;
        RollWithAPunch RollWithAPunch;
        
        [TestInitialize]
        public void DefenderWithRollAndAttackerWithStrikeAndAttackerTargetsDefender()
        {
            Strike = Attacker.Manuevers["Strike"] as Strike;
            RollWithAPunch = Defender.Manuevers["Roll With A Punch"] as RollWithAPunch;
            Strike.Target(Defender);
            Attacker.STR.MaxValue = 25;
            Defender.OCV.MaxValue = 4;
            Dice.RandomnessState = RandomnessState.average;
        }

        [TestMethod]
        public void DefenderHitByPhysicalAttack_DefenderCanRollWithPunch()
        {

            var status = Strike.AttemptToHitDefender();

            Defender.Manuevers["Abort"].Perform();
            Assert.AreEqual(true, RollWithAPunch.CanPerform);
        }

        [TestMethod]
        public void DefenderHitByEnergyAttack_DefenderCannotRollWithPunch()
        {
            Strike.Damage.WorksAgainstDefense = DefenseType.ED;
            var status = Strike.AttemptToHitDefender();

            Defender.Manuevers["Abort"].Perform();
            Assert.AreEqual(false,RollWithAPunch.CanPerform);
        }


        [TestMethod]
        public void DefenderHitByPysicalAttackAndDefenderRollsWithPunch_DefenderTakesHalfStunandBodAfterPD()
        {
            var status = Strike.AttemptToHitDefender();

            DamageAmount damageBeforeRoll = Strike.RollDamage();
            damageBeforeRoll = Defender.DeductDefenseFromDamage(damageBeforeRoll);

            Defender.Manuevers["Abort"].Perform();
            RollWithAPunch.Perform();

            Assert.AreEqual(damageBeforeRoll.STUN / 2, Defender.STUN.MaxValue - Defender.STUN.CurrentValue);


        }
    }

    [TestClass]
    public class SnapshotTest : BaseCombatManueverTest
    {
        private IFixture MockFixture;
        private SnapShot SnapShot;
        private Attack RangedAttackerAttack;
        private ProtectingCover Cover; 


        private List<HeroSystemCharacter> Enemies;
        private Attack RangedDefenderAttack;

        [TestInitialize]
        public void PerceptiveAttackerWithSnapShotForRangedAttackAndAttackerIsBehindCoverFromDefenderAndDefenderHasRangedAttack()
        {
            Attacker.INT.MaxValue = 40;
            SnapShot = Attacker.Manuevers["Snap Shot"] as SnapShot;
            RangedAttackerAttack = new Attack("RangedAttackerAttack", Attacker, DamageType.Normal, 3, DefenseType.ED, true);
            SnapShot.UnderlyingAttack = RangedAttackerAttack;

            
            MapFactory.ActiveGameMap.BarrierBetweenHexes = true;
            MapFactory.ActiveGameMap.HexBesideOtherHex = true;
            MapFactory.ActiveGameMap.ProtectingCover=new ProtectingCoverStub();
            Cover = MapFactory.ActiveGameMap.GetConcealmentForCharacterBetweenOtherCharacter(Attacker, Defender);

            RangedDefenderAttack = new Attack("RangedAttackerAttack", Defender, DamageType.Normal, 3, DefenseType.ED, true);

            Dice.RandomnessState = RandomnessState.average;

        }

        public void configureMockFixture()
        {
            MockFixture = new Fixture();
            MockFixture.Customize(new AutoConfiguredMoqCustomization());
            MockFixture.Customizations.Add(new NumericSequenceGenerator());
            //handle recursion
            MockFixture.Behaviors.Add(new OmitOnRecursionBehavior());
        }

        [TestMethod]
        public void AttackerIsFullyConcealedFromDefenderBehindBarrierHeCanDuckOutFrom_CanPerformSnapshotAgainstDefender()
        {
            SnapShot.Target(Defender, true);
            Assert.AreEqual(true, SnapShot.CanPerform);
        }

        [TestMethod]
        public void BarrierIsNotBetweenAttackerAndDefender_CannotPerformSnapshotAgainstDefender()
        {
            MapFactory.ActiveGameMap.ProtectingCover = null;
            SnapShot.Target(Defender, true);
            Assert.AreEqual(false, SnapShot.CanPerform);


        }

        [TestMethod]
        public void AttackerPerformSnapshotAgainstDefender_AttackerAttacksAtMinusOneOCV()
        {
            SnapShot.PerformAttack(Defender);

            Assert.AreEqual(-1, Attacker.OCV.Modifier);

        }

        [TestMethod]
        public void AttackerAwareOfDefender_CanPerformSnapShotAgainstDefender() 
        {
            SnapShot.Target(Defender, true);
            Assert.AreEqual(true, SnapShot.CanPerform);

        }

        [TestMethod]
        public void AttackerNotAwareOfDefenderAndMakesPerception_CanPerformSnapShotAgainstDefender() 
        {
            SnapShot.Target(Defender, false);
            Assert.AreEqual(true, SnapShot.CanPerform);
        }

        [TestMethod]
        public void AttackerNotAwareOfDefenderAndDoesNotMakesPerception_CanPerformSnapShotAgainstDefender()
        {
            Attacker.PER.Modifier = -12;
            SnapShot.Target(Defender, false);
            Assert.AreEqual(false, SnapShot.CanPerform);
        }



        [TestMethod]
        public void AttackerPerforsSnapshot_AttackerIsOnlyPartlyCoveredAndCanBeAttackedByDefenderForSegmentSnapshotWasMade()
        {
            SnapShot.PerformAttack(Defender);

            Assert.AreEqual(ConcealmentAmount.Partial, Cover.BlockingCoverProvidedAgainstOtherCharacter(Defender, Attacker));

            Assert.AreEqual(true, RangedDefenderAttack.CanPerform);

        }

        [TestMethod]
        public void AttackerPerforsSnapshotAndSegmentIsOver_AttackerIsFullyCoveredAndCannotBeAttackedByDefenderForSegmentSnapshotWasMade()
        {
            SnapShot.PerformAttack(Defender);
            Sequence.ActivePhase.Complete();
            Sequence.ActivePhase.Complete();

            RangedDefenderAttack.Target(Attacker);
            Assert.AreEqual(false, RangedDefenderAttack.CanPerform);
            Assert.AreEqual(ConcealmentAmount.Full, Cover.BlockingCoverProvidedAgainstOtherCharacter(Defender, Attacker));



        }


    }

    [TestClass]
    public class SweepTest : BaseCombatManueverTest
    {
        private Sweep Sweep;
        private Strike Strike;
        private HeroSystemCharacter NextDefender;
        private HeroSystemCharacter LastDefender;

        [TestInitialize]
        public void AttackerWithSweep()
        {
            Sweep = Attacker.Manuevers["Sweep"] as Sweep;
            
            Sweep.UnderlyingAttack = Attacker.Manuevers["Strike"] as Strike;

        }

        [TestMethod]
        public void CannotSweepWithHaymakerOtMoveBy()
        {
            Sweep.UnderlyingAttack = Attacker.Manuevers["Move By"] as Attack;
            Assert.AreEqual(false, Sweep.CanPerform);

            Sweep.UnderlyingAttack = Attacker.Manuevers["Haymaker"] as Attack;
            Assert.AreEqual(false, Sweep.CanPerform);

        }

        [TestMethod]
        public void CanOnlySweepWithMoveThroughIfAllTargetsAreAdjacent()
        {
            Sweep.UnderlyingAttack = Attacker.Manuevers["Move Through"] as Attack;
            HeroSystemCharacter defender = Factory.BaseCharacter;
            Sweep.AddTarget(defender);

            defender = Factory.BaseCharacter;
            defender.Hex.BesideOtherHex = true;
            Sweep.AddTarget(defender);


            defender = Factory.BaseCharacter;
            defender.Hex.BesideOtherHex = false;
            Sweep.AddTarget(defender);

            Assert.AreEqual(2, Sweep.Targets.Count);



        }



        [TestMethod]
        public void AttackerSwipesWithGrab_AttackerCanGrabNumberOfTargetsBasedOnNumberOfLimbs()
        {
            Sweep.UnderlyingAttack = Attacker.Manuevers["Grab"] as Attack;
            Attacker.Limbs = 3;
            HeroSystemCharacter defender = Factory.BaseCharacter;
            Sweep.AddTarget(defender);

            defender = Factory.BaseCharacter;
            Sweep.AddTarget(defender);

            defender = Factory.BaseCharacter;
            Sweep.AddTarget(defender);

            defender = Factory.BaseCharacter;
            Sweep.AddTarget(defender);

            Assert.AreEqual(3, Sweep.Targets.Count);



        }



    }
    public class TestManuever : Manuever
    {

        public TestManuever(HeroSystemCharacter character) : base(ManueverType.MovementManuever, "Test", character, true)
        {

            Modifier.OCV.ModiferAmount = 12;


        }

        public override bool canPerform()
        {
            return true;

        }

        public override void PerformManuever()
        {

        }

        public override bool CanAbortDuringCombatManuever(Manuever manuever)
        {
            return false;
        }
    }
    public class TestCombatManuever : Manuever
    {

        public TestCombatManuever(HeroSystemCharacter character) : base(ManueverType.MovementManuever, "Test Combat", character, true)
        {

            Modifier.OCV.ModiferAmount = 12;


        }

        public override bool canPerform()
        {
            return true;

        }

        public override bool CanAbortDuringCombatManuever(Manuever manuever)
        {
            return false;
        }

        public override void PerformManuever()
        {

        }
    }

    [TestClass]
    public class MultiPowerAttackTest :BaseCombatManueverTest
    {
        private MultiPowerAttack MultiPowerAttack;
        private Attack RangedEnergy;
        private Attack RangedKillingAttack;
        private Attack Strike;


        [TestInitialize]
        public void AttackerAddsPowerOfDifferentAttackRollTypes_PowerIsNotAddedToTheMultipower()
        {
            MultiPowerAttack = new MultiPowerAttack(Attacker);
            RangedKillingAttack = new Attack("RangedKillingAttack",Attacker,DamageType.Normal, 10,DefenseType.PD, true);
            RangedKillingAttack.Modifier.OCV.ModiferAmount = 4;
            RangedKillingAttack.Modifier.DCV.ModiferAmount = 2;

            RangedEnergy = new Attack("RangedEnergy", Attacker, DamageType.Normal, 3, DefenseType.PD, true);
            Strike = Attacker.Manuevers["Strike"] as Strike;
            RangedEnergy.Modifier.OCV.ModiferAmount = 2;
            RangedEnergy.Modifier.DCV.ModiferAmount = 4;
        }

        [TestMethod]
        public void AllAttacksMustBeRangedOrHTH()
        {
            MultiPowerAttack.AddAttack(RangedEnergy);
            MultiPowerAttack.AddAttack(Strike);

            Assert.AreEqual(1,MultiPowerAttack.UnderlyingAttacks.Count);
        }


        public void AllAttacksMustBeFromDifferentPowerFrameowrks() { }

        [TestMethod]
        public void AttackerHitsWithMultiPower_DefenderDefenseAppliedForeachAttackSeparately()
        {
            MultiPowerAttack.AddAttack(RangedEnergy);
            MultiPowerAttack.AddAttack(RangedKillingAttack);

            MultiPowerAttack.Target(Defender);

            MultiPowerAttack.Perform();

            int actualDamagetaken = Defender.STUN.MaxValue - Defender.STUN.CurrentValue;
            int expectedDamageTaken = RangedEnergy.Result.DamageResult.STUN - Defender.PD.CurrentValue +
                                      RangedKillingAttack.Result.DamageResult.STUN - Defender.PD.CurrentValue + MultiPowerAttack.Result.KnockbackResults.Damage.STUN- Defender.PD.CurrentValue;

            Assert.AreEqual(actualDamagetaken, expectedDamageTaken);



        }

        [TestMethod]
        public void AttackeerHitsWithMultiPower_OnlyMostsevereKnockbackAppliedToDefendr()
        {
            MultiPowerAttack.AddAttack(RangedEnergy);
            MultiPowerAttack.AddAttack(RangedKillingAttack);

            MultiPowerAttack.Target(Defender);

            MultiPowerAttack.Perform();

            KnockbackResult severe =
                MultiPowerAttack.UnderlyingAttacks.OrderBy(x => x.Result.KnockbackResults.Result)
                    .Select(y => y.Result.KnockbackResults)
                    .FirstOrDefault();
            Assert.AreEqual(severe,MultiPowerAttack.Result.KnockbackResults);
        }

        public void AttacksCannotBeBothBasedOnStrIfTheyHaveTheSameEffect() { }

        [TestMethod]
        public void
            AttackerHitsWithMultiPowerAndEachUnderlyingAttackHasDifferentCVBonuses_AttackUsesLowestCVBonusOfUnderlyingAttack
            ()
        {
            MultiPowerAttack.AddAttack(RangedEnergy);
            MultiPowerAttack.AddAttack(RangedKillingAttack);

            MultiPowerAttack.Target(Defender);
            MultiPowerAttack.Perform();

            Assert.AreEqual(Attacker.OCV.Modifier, 2 );
        }

        public void
            AttackerHitsWithMultiPowerAndPowerIsMissleDeflected_MultiPowerTreatedAsOneAttack()
        {
            
        }            
       
            
    }

    [TestClass]
    public class MoveThroughTest
    {
        [TestInitialize]
        public void AttackerMovesthroughTarget_DamageIsDeterminedByRelativeVelocityOfAttackerAndDefender()
        { }
    }


    [TestClass]
    public class MoveByTest
    {
        [TestInitialize]
        public void AttackerMovesthroughTarget_DamageIsDeterminedByRelativeVelocityOfAttackerAndDefender()
        { }
    }
}



