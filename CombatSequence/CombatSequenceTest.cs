using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Security.Cryptography.X509Certificates;
using Castle.Core.Smtp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HeroSystemEngine.Character;
using HeroSystemEngine.Dice;
using HeroSystemEngine.Manuevers;
using HeroSystemsEngine.GameMap;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Moq;

namespace HeroSystemsEngine.CombatSequence
{
    [TestClass]
    class ResolveCombatSequenceTest
    {
        public CombatSequenceTestFactory Factory = new CombatSequenceTestFactory();

        [TestMethod]
        public void StartCombat_TurnSegmentToTwelveAndActivateSequence()
        {
            CombatSequence sequence = new CombatSequence();
            sequence.StartCombat();

            Assert.AreEqual(sequence.IsStarted, true);
            Assert.AreEqual(sequence.ActiveSegment.Number, 12);
            Assert.AreEqual(sequence.ActiveTurn, 1);
        }

        [TestMethod]
        public void AdddingCharacterToCombatSequence_CharacterIsOrderedByPhasesAndDex()
        {
            CombatSequence sequence = Factory.SequenceWithFastMediumAndSlowCharacters;
            sequence.StartCombat();

            //segment 12
            Segment currentSegment = sequence.ActiveSegment;
            List<Phase> phases = currentSegment.CombatPhases;
            HeroSystemCharacter currentCharacter = phases.First().Character;
            Assert.AreEqual("Fast Character", currentCharacter.Name);

            currentCharacter = phases.ElementAt(1).Character;
            Assert.AreEqual("Medium Character", currentCharacter.Name);

            currentCharacter = phases.ElementAt(2).Character;
            Assert.AreEqual("Slow Character", currentCharacter.Name);


            //segment 2
            currentSegment = sequence.SegmentAfter(currentSegment);
            currentSegment = sequence.SegmentAfter(currentSegment);
            phases = currentSegment.CombatPhases;
            currentCharacter = phases.ElementAt(0).Character;
            Assert.AreEqual("Fast Character", currentCharacter.Name);

            //segment 3
            currentSegment = sequence.SegmentAfter(currentSegment);
            phases = currentSegment.CombatPhases;
            currentCharacter = phases.ElementAt(0).Character;
            Assert.AreEqual("Medium Character", currentCharacter.Name);

            //segment 4
            currentSegment = sequence.SegmentAfter(currentSegment);
            phases = currentSegment.CombatPhases;
            currentCharacter = phases.First().Character;
            Assert.AreEqual("Fast Character", currentCharacter.Name);

            //segment 6
            currentSegment = sequence.SegmentAfter(currentSegment);
            currentSegment = sequence.SegmentAfter(currentSegment);
            phases = currentSegment.CombatPhases;
            currentCharacter = phases.First().Character;
            Assert.AreEqual("Fast Character", currentCharacter.Name);

            currentCharacter = phases.ElementAt(1).Character;
            Assert.AreEqual("Medium Character", currentCharacter.Name);

            currentCharacter = phases.ElementAt(2).Character;
            Assert.AreEqual("Slow Character", currentCharacter.Name);

        }

        [TestMethod]
        public void CompletingACharacterPhase_PhaseOfNextCharacterInSegmentIsActivated()
        {
            //arrange
            CombatSequence sequence = Factory.SequenceWithFastMediumAndSlowCharacters;
            sequence.StartCombat();

            //act
            Segment currentSegment = sequence.ActiveSegment;
            Assert.AreEqual(currentSegment.Number, 12);
            Phase phase = currentSegment.ActivePhase;
            phase.Complete();

            //assert
            phase = currentSegment.ActivePhase;
            Assert.AreEqual("Medium Character", phase.Character.Name);
            Assert.AreEqual(currentSegment.Number, 12);

            //act
            phase.Complete();

            //assert
            phase = sequence.ActivePhase;
            Assert.AreEqual("Slow Character", phase.Character.Name);
            Assert.AreEqual(currentSegment.Number, 12);
            phase.Complete();

        }

        [TestMethod]
        public void
            CompletingAllCharacterPhasesesinSegment_JumpsToNextSegmentWithCharacterAndActivatesThatCharactersPhase()
        {
            //arrange
            CombatSequence sequence = Factory.SequenceWithFastMediumAndSlowCharacters;
            sequence.StartCombat();

            //act
            sequence.CompleteActivePhase();
            sequence.CompleteActivePhase();
            sequence.CompleteActivePhase();

            //assert
            Phase phase = sequence.ActivePhase;
            Assert.AreEqual(sequence.ActiveSegment.Number, 2);
            Assert.AreEqual("Fast Character", phase.Character.Name);

            sequence.CompleteActivePhase();
            ;

        }

        [TestMethod]
        public void CharacterPerformsHalfPhaseAction_OnlyHalfPhaseActionsAreAvailableToBePerformed()
        {
            //arrange
            CombatSequence sequence = Factory.SequenceWithCharacterWithMixOfFullAndHalfPhaseManuevers;
            sequence.StartCombat();
            Phase phase = sequence.ActivePhase;
            HeroSystemCharacter character = phase.Character;
            IManuever halfPhaseManuever = character.Manuevers["Sample Half 1"];

            //act
            halfPhaseManuever.Perform();

            //assert
            IManuever fullManuever = character.Manuevers["Sample Full"];
            Assert.AreEqual(fullManuever.CanPerform, false);
            halfPhaseManuever = character.Manuevers["Sample Half 1"];
            Assert.AreEqual(halfPhaseManuever.CanPerform, true);

        }

        [TestMethod]
        public void CharacterPerformsHalfPhaseAction_OnlyHalfPhaseIsleft()
        {
            //arrange
            CombatSequence sequence = Factory.SequenceWithCharacterWithMixOfFullAndHalfPhaseManuevers;
            sequence.StartCombat();
            Phase phase = sequence.ActivePhase;
            HeroSystemCharacter character = phase.Character;
            IManuever halfPhaseManuever = character.Manuevers["Sample Half 1"];

            //act
            halfPhaseManuever.Perform();

            //arrange
            Assert.AreEqual(.5, phase.PhaseLeft);
        }

        [TestMethod]
        public void PhaseIsActivated_SegmentAndSequenceAndCharacterAllReferenceActivePhase()
        {
            //arrange
            CombatSequence sequence = Factory.SequenceWithCharacterWithMixOfFullAndHalfPhaseManuevers;

            //act
            sequence.StartCombat();
            Phase phase = sequence.ActivePhase;
            HeroSystemCharacter character = phase.Character;

            //assert
            Assert.AreEqual(character.ActivePhase, phase);
            Assert.AreEqual(sequence.ActivePhase, phase);
            Assert.AreEqual(phase.Active, true);

            phase.Complete();
            Assert.AreEqual(phase.Active, false);
            Assert.AreNotEqual(character.ActivePhase, phase);
            Assert.AreNotEqual(sequence.ActivePhase, phase);



        }

        [TestMethod]
        public void CharacterPerformsFullPhaseAction_CharacterPhaseIsOver()
        {
            //arrange
            CombatSequence sequence = Factory.SequenceWithCharacterWithMixOfFullAndHalfPhaseManuevers;
            HeroSystemCharacter nextC = Factory.Factory.BaseCharacter;
            nextC.DEX.MaxValue = 5;
            sequence.AddCharacter(Factory.Factory.BaseCharacter);

            sequence.StartCombat();
            Phase phase = sequence.ActivePhase;
            HeroSystemCharacter character = phase.Character;
            Phase nextPhase = sequence.NextCombatPhase;

            IManuever fullManuever = character.Manuevers["Sample Full"];

            //act
            fullManuever.Perform();

            //assert
            Assert.AreEqual(phase.Active, false);
            Assert.AreEqual(sequence.ActivePhase, nextPhase);

        }

        [TestMethod]
        public void CharacterPerformsHalfPhaseCombatManuever_CharacterPhaseIsOver()
        {
            CombatSequence sequence = Factory.SequenceWithFastMediumAndSlowCharacters;
            sequence.StartCombat();

            Phase attacking = sequence.ActivePhase;
            Strike strike = attacking.Character.Manuevers["Strike"] as Strike;
            strike.Target(sequence.NextCombatPhase.Character);

            strike.Perform();

            Assert.AreNotEqual(attacking, sequence.ActivePhase);


        }

        [TestMethod]
        public void CharacterPerformsTwoHalfPhaseAction_CharacterPhaseIsOver()
        {
            //arrange
            CombatSequence sequence = Factory.SequenceWithCharacterWithMixOfFullAndHalfPhaseManuevers;
            //sequence.AddCharacter(Factory.Factory.BaseCharacter);
            sequence.StartCombat();
            Phase phase = sequence.ActivePhase;
            Phase nextPhase = sequence.NextCombatPhase;
            HeroSystemCharacter character = phase.Character;
            IManuever halfManuever2 = character.Manuevers["Sample Half 2"];
            IManuever halfManuever = character.Manuevers["Sample Half 1"];

            //act
            halfManuever.Perform();
            halfManuever2.Perform();
            //assert
            Assert.AreEqual(phase.Active, false);
        }

        [TestMethod]
        public void TwoCharactersWithSameDexAndPhase_MakeDexRollToDetermineWhoGoesFirst()
        {

            //arrange
            CombatSequence sequence = Factory.SequenceWithFastMediumAndSlowCharacters;
            sequence.StartCombat();

            sequence.ActivePhase.Character.DEX.CurrentValue = 20;

            //to do mock rthis


        }

        [TestMethod]
        public void ChangeCharacterDEXAndSPDInCombatSequence_UpdatesPhaseOrderCorrectly()
        {
        }

        [TestMethod]
        public void WhenSegmentTwelveFinished_PostSegmentTwelveRecoveryRunOnAllCharcters()
        {


            CombatSequence sequence = Factory.SequenceWithFastMediumAndSlowCharactersWithMocRecManeuver;
            sequence.StartCombat();
            HeroSystemCharacter character = sequence.Characters.FirstOrDefault();

            sequence.CompleteActivePhase();
            sequence.CompleteActivePhase();
            sequence.CompleteActivePhase();

            Mock.Get(character?.Manuevers["Recover"])
                .Verify(foo => foo.Perform(), Times.Exactly(3));

        }

    }

    [TestClass]
    class CombatSequenceTimerTest
    {
        public CharacterTestObjectFactory Factory = new CharacterTestObjectFactory();
        public CombatSequence Sequence;
        public int Time;
        public HeroSystemCharacter Character;
        [TestInitialize]
        public void Given()
        {
            Sequence  = new CombatSequence(); 
            Sequence.StartCombat();
            Character = Factory.BaseCharacter;
            Character.SPD.MaxValue = 6;
            Sequence.AddCharacter(Character);
            Sequence.StartCombat();

        }

        [TestMethod]
        public void TrackingDurationWithOneOrMoreSegment_TimerActivatesAtBeginningOfAppropriateSegment()
        {
            Time = 1;
            SequenceTimer timer = new SequenceTimer(DurationUnit.Segment, Time, Sequence);

            timer.TimerAction += new SequenceTimerAction(TestTimerCompletedOnCorrectSegment);
            timer.StartTimer();

            var x = Sequence.ActivateNextSegment;


            Time = 4;
            timer.Time = Time;


            timer.TimerAction += new SequenceTimerAction(TestTimerCompletedOnCorrectSegment);
            timer.StartTimer();
            x = Sequence.ActivateNextSegment;
            x = Sequence.ActivateNextSegment;
            x = Sequence.ActivateNextSegment;
            x = Sequence.ActivateNextSegment;


        }
        void TestTimerCompletedOnCorrectSegment(SequenceTimer timer)
        {
           // SequenceTimer timer = (SequenceTimer)sender;
            Segment seg = timer.Start as Segment;

            int dest=0;
            if (seg.Number + timer.Time <= 12)
            {
                dest = seg.Number + timer.Time;
            }
            else
            {
                dest= seg.Number + timer.Time - 12;
            }

            Assert.AreEqual(dest, Sequence.ActiveSegment.Number);
        }

        [TestMethod]
        public void TrackingDurationWithOneOrMoreSegmentsToEndOfSegment_TimerActivatesAtEndOfAppropriateSegment()
        {
            Time = 2;
            SequenceTimer timer = new SequenceTimer(DurationUnit.Segment, Time, Sequence, Timing.End);

            timer.TimerAction += new SequenceTimerAction(TestTimerCompletedOnCorrectSegment);
            timer.StartTimer();

            //complete phase 12
            Sequence.CompleteActivePhase();
            Assert.IsNull(timer.Stop);

            //complete phase 2
            Sequence.CompleteActivePhase();  

            //timer stops after phase 2
            Segment segment = (Segment) timer.Stop;
            Assert.AreEqual( 2, segment.Number);

        }
        [TestMethod]
        public void TrackingDurationWithOneOrMorePhase_TimerActivatesAtBeginningOfAppropriatePhase()
        {
            Time = 3;
            SequenceTimer timer = new SequenceTimer(DurationUnit.Phase, Time, Sequence);

            timer.TimerAction += new SequenceTimerAction(TestTimerCompletedOnCorrectPhase);
            timer.StartTimer();

            Sequence.CompleteActivePhase();
            Sequence.CompleteActivePhase();
            Sequence.CompleteActivePhase();


        }
        void TestTimerCompletedOnCorrectPhase(SequenceTimer timer)
        {
           // SequenceTimer timer = (SequenceTimer)sender;
            Phase phase = timer.Start as Phase;
            Assert.AreEqual(timer.IsTracking, false);

            Assert.AreEqual(6, Sequence.ActiveSegment.Number);
        }

        [TestMethod]
        public void TrackingDurationWithOneOrMorePhasesToEndOfPhase_TimerActivatesAtEndOfAppropriatePhase()
        {
            Time = 3;
            SequenceTimer timer = new SequenceTimer(DurationUnit.Phase, Time, Sequence, Timing.End);

            timer.TimerAction += new SequenceTimerAction(TestTimerCompletedOnCorrectPhase);
            timer.StartTimer();

            //go to phase 6
            Sequence.CompleteActivePhase();
            Sequence.CompleteActivePhase();
            Sequence.CompleteActivePhase();

            Assert.IsNull(timer.Stop);
            Phase phase2 = Character.ActivePhase;

            //complete phase 6 - timer shoukd run
            Sequence.CompleteActivePhase();
            Assert.IsNotNull(timer.Stop);
            Assert.AreEqual(timer.Stop, phase2);


        }

        [TestMethod]
        public void TrackingDurationWithOneOrMoreTurns_TimerActivatesAtppropriateTurn()
        {
            //get past segment 12
            Time = 3;
            SequenceTimer timer = new SequenceTimer(DurationUnit.Turn, Time, Sequence);

            timer.TimerAction += new SequenceTimerAction(TestTimerCompletedAtEndOfTurn);
            timer.StartTimer();
            Sequence.CompleteActivePhase();
            Sequence.CompleteActivePhase();
            Sequence.CompleteActivePhase();
            Sequence.CompleteActivePhase();
            Sequence.CompleteActivePhase();
            Sequence.CompleteActivePhase();

            Assert.IsNotNull(timer.Stop);
            Assert.AreEqual(timer.Stop, Sequence.Segments[12]);


        }

        void TestTimerCompletedAtEndOfTurn(SequenceTimer sender)
        {

        }


    }


    public class CombatSequenceTestFactory
    {
        public CharacterTestObjectFactory Factory = new CharacterTestObjectFactory();
        Fixture MockFixture;

        public CombatSequenceTestFactory()
        {
            MockFixture = new Fixture();
            MockFixture.Customize(new AutoMoqCustomization());
            MockFixture.Behaviors.Add(new OmitOnRecursionBehavior());
        }

        public CombatSequence SequenceWithFastMediumAndSlowCharacters
        {
            get
            {
                List<HeroSystemCharacter> characters = Factory.FastMediumAndSlowCharacters;
                CombatSequence sequence = new CombatSequence();
                sequence.AddCharacters(characters);
                return sequence;
            }
        }

        public CombatSequence SequenceWithCharacterWithMixOfFullAndHalfPhaseManuevers
        {
            get
            {
                HeroSystemCharacter character = Factory.BaseCharacter;
                character.DEX.MaxValue = 30;
                Manuever testManuever = new TestManuever("Sample Full", character);
                testManuever.PhaseActionTakes = PhaseLength.Full;
                
                testManuever = new TestManuever("Sample Half 1", character);
                Manuever testManuever2 = new TestManuever("Sample Half 2", character);

                CombatSequence sequence = new CombatSequence();
                sequence.AddCharacter(character);
                return sequence;

            }


        }

        public CombatSequence SequenceWithFastMediumAndSlowCharactersWithMocRecManeuver
        {
            get
            {
                CombatSequence sequence = SequenceWithFastMediumAndSlowCharacters;
                var mockRec = MockFixture.Create<IManuever>()
                ;
                foreach (var character in sequence.Segments[12].Characters)
                {
                    character.Manuevers["Recover"]=mockRec;
                }
                return sequence;
            }
        }

    }

    [TestClass]
    public class HoldActionTest
    {
        public CombatSequenceTestFactory Factory = new CombatSequenceTestFactory();

        [TestMethod]
        public void CharacterHoldsActionUntilLowerDex_PhaseExecutesAtLowerDexSpecified()
        {
            //arrange
            CombatSequence sequence = Factory.SequenceWithFastMediumAndSlowCharacters;
            sequence.StartCombat();
            HeroSystemCharacter character = sequence.ActivePhase.Character;

            //act
            HoldActionManuever holdManuever = (HoldActionManuever) character.Manuevers["Hold Action"];
            holdManuever.DexWaitingFor = 15;

            character.Manuevers["Hold Action"].Perform();

            //assert
            Assert.AreEqual(character.ActivePhase , null);
            Assert.AreEqual(character.HeldManuever , character.Manuevers["Hold Action"]);
            Assert.AreEqual(character.HeldManuever, sequence.HeldManuevers[0]);
            Assert.AreEqual(sequence.ActivePhase.Character.DEX.CurrentValue, 20);

            sequence.CompleteActivePhase();
            Assert.AreEqual(character, sequence.ActivePhase.Character);


        }

        [TestMethod]
        public void CharacterHoldsActionUntilLowerDexAndTheCompletesHeldAction_InterruptedPhaseActsNext()
        {
            //arrange
            CombatSequence sequence = Factory.SequenceWithFastMediumAndSlowCharacters;
            sequence.StartCombat();
            HeroSystemCharacter character = sequence.ActivePhase.Character;

            
            HoldActionManuever holdManuever = (HoldActionManuever)character.Manuevers["Hold Action"];
            holdManuever.DexWaitingFor = 15;
                
            character.Manuevers["Hold Action"].Perform();

            sequence.CompleteActivePhase();
            //act

            //execute the held action
            sequence.CompleteActivePhase();

            //assert
            Assert.AreEqual(sequence.ActiveSegment.CombatPhases[2], sequence.ActivePhase);
            Assert.AreEqual(character.HeldManuever, null);
            Assert.AreEqual(sequence.HeldManuevers.Count, 0);
        }


        [TestMethod]
        public void CharacterHoldsActionUntilDifferentSegment_PhaseExecutesAtSegmentSpecified()
        {
            //arrange
            CombatSequence sequence = Factory.SequenceWithFastMediumAndSlowCharacters;
            sequence.StartCombat();      

            HeroSystemCharacter character = sequence.ActivePhase.Character;

            //act
            HoldActionManuever holdManuever = (HoldActionManuever)character.Manuevers["Hold Action"];

            holdManuever.DexWaitingFor = 15;
            holdManuever.SegmentWaitingFor = 1;

            character.Manuevers["Hold Action"].Perform();

            sequence.CompleteActivePhase();
            sequence.CompleteActivePhase();

            Assert.AreEqual(sequence.ActiveSegment.Number, holdManuever.SegmentWaitingFor);

        }

        [TestMethod]
        public void CharacterHoldsActionUntilCharactersNextPhase_CharacterLosesHeldAction()
        {
            //arrange
            CombatSequence sequence = Factory.SequenceWithFastMediumAndSlowCharacters;
            sequence.StartCombat();
            HeroSystemCharacter character = sequence.ActivePhase.Character;
            HoldActionManuever holdManuever = (HoldActionManuever)character.Manuevers["Hold Action"];

            //act
            holdManuever.DexWaitingFor = 10;
            holdManuever.SegmentWaitingFor = 2;
            character.Manuevers["Hold Action"].Perform();
            sequence.CompleteActivePhase();
            sequence.CompleteActivePhase();
            
            Assert.AreEqual(character.HeldManuever, null);
            Assert.AreEqual(0, sequence.HeldManuevers.Count);
     

        }

        [TestMethod]
        public void CharacterHoldsActionAfterHalfPhaseAction_OnlyHalfPhaseActionsAreAvailable()
        {
            //arrange
            CombatSequence sequence = Factory.SequenceWithFastMediumAndSlowCharacters;
            sequence.StartCombat();
            HeroSystemCharacter character = sequence.ActivePhase.Character;
            TestManuever testManuever = new TestManuever("Sample Half 1", character);

            //act
            character.Manuevers["Sample Half 1"].Perform();

            HoldActionManuever holdManuever = (HoldActionManuever)character.Manuevers["Hold Action"];
            holdManuever.DexWaitingFor = 15;
            character.Manuevers["Hold Action"].Perform();
        
            sequence.CompleteActivePhase();

            //assert
            Assert.AreEqual(.5, sequence.ActivePhase.PhaseLeft);

        }

        [TestMethod]
        public void CharacterHoldsAction_CanInteruptOtherCharactersPhase()
        {
            //arrange
            CombatSequence sequence = Factory.SequenceWithFastMediumAndSlowCharacters;
            sequence.StartCombat();
            HeroSystemCharacter holder = sequence.ActivePhase.Character;
            HoldActionManuever held = holder.Manuevers["Hold Action"] as HoldActionManuever;

            HeroSystemCharacter attacker = sequence.NextCombatPhase.Character;
            Attack ranged = new Attack( "Basic Ranged", attacker,
                    DamageType.Normal, 5, DefenseType.PD, true);

            HeroSystemCharacter defender = sequence.NextCombatPhase.Character;

            

           //act

            held.Perform();           
            ranged.Target(defender);   
            held.Interrupt(InterruptionWith.Defensive);

           

            //Assert
            Assert.AreEqual(holder, sequence.ActivePhase.Character);
            Assert.AreEqual(sequence.ActivePhase, held.HeldPhase);

            Assert.AreEqual(sequence.InterruptedPhase.Character, attacker);

            Assert.AreEqual(sequence.InterruptedPhase.PhaseLeft, 1);



        }

        [TestMethod]
        public void CharacterHoldsActionGenerically_HeCanInteruptOtherCharactersPhaseOnlyIfHeWinsDexRoll()
        {
            //arrange
            CombatSequence sequence = Factory.SequenceWithFastMediumAndSlowCharacters;
            sequence.StartCombat();
            HeroSystemCharacter holder = sequence.ActivePhase.Character;
            
            HoldActionManuever held = holder.Manuevers["Hold Action"] as HoldActionManuever;

            HeroSystemCharacter attacker = sequence.NextCombatPhase.Character;
            Attack ranged = new Attack("Basic Ranged", attacker, DamageType.Normal, 5, DefenseType.PD,  true);

            HeroSystemCharacter defender = sequence.NextCombatPhase.Character;
            Dice.RandomnessState = RandomnessState.average;


            //act

            held.Perform();           
            ranged.Target(defender);          
            held.Interrupt(InterruptionWith.Generic);

            //assert
            Assert.AreEqual(holder, sequence.ActivePhase.Character);
            Assert.AreEqual(sequence.ActivePhase, held.HeldPhase);
            Assert.AreEqual(sequence.InterruptedPhase.Character, attacker);
            Assert.AreEqual(sequence.InterruptedPhase.PhaseLeft, 1);
        }

        [TestMethod]
        public void CharacterHoldsActionGenerically_HeGoesAfterOtherCharactersPhaseIfHeLosesDexRoll()
        {
            //arrange
            CombatSequence sequence = Factory.SequenceWithFastMediumAndSlowCharacters;
            sequence.StartCombat();
            HeroSystemCharacter holder = sequence.ActivePhase.Character;

            HoldActionManuever held = holder.Manuevers["Hold Action"] as HoldActionManuever;

            HeroSystemCharacter attacker = sequence.NextCombatPhase.Character;
            Attack ranged = new Attack("Basic Ranged", attacker, DamageType.Normal, 5, DefenseType.PD, true);

            HeroSystemCharacter defender = sequence.NextCombatPhase.Character;
            Dice.RandomnessState = RandomnessState.average;


            //act

            held.Perform();
           
            ranged.Target(defender);

            holder.DEX.CurrentValue = 5;
            held.Interrupt(InterruptionWith.Generic);

            Assert.AreNotEqual(holder, sequence.ActivePhase.Character);
            Assert.AreEqual(attacker, sequence.ActivePhase.Character);

            sequence.CompleteActivePhase();
            Assert.AreEqual(holder, sequence.ActivePhase.Character);



        }

        [TestMethod]
        public void MoreThanOneCharacterTriesToUseHeldActionAtSameTime_CharacterWhoWinsDexRollGoesFirst()
        {
            //arrange
            CombatSequence sequence = Factory.SequenceWithFastMediumAndSlowCharacters;
            sequence.StartCombat();
            HeroSystemCharacter holder1 = sequence.ActivePhase.Character;
            

            HeroSystemCharacter holder2 = sequence.ActiveSegment.PhaseNextInDexOrder.Character;

            HoldActionManuever held1 = holder1.Manuevers["Hold Action"] as HoldActionManuever;
            held1.DexWaitingFor = 10;

            HoldActionManuever held2 = holder2.Manuevers["Hold Action"] as HoldActionManuever;
            held2.DexWaitingFor = 10;

            
            held1.Perform();
            holder1.DEX.CurrentValue = 30;

            held2.Perform();

            Dice.RandomnessState = RandomnessState.average;

            Assert.AreEqual(holder1, sequence.ActivePhase.Character);
            sequence.CompleteActivePhase();

            Assert.AreEqual(holder2, sequence.ActivePhase.Character);


        }

        [TestMethod]
        public void CharacterUsingHeldDefensiveAction_AlwaysGoesBeforeOtherHeldOrInterruptedActions()
        {
            //arrange
            CombatSequence sequence = Factory.SequenceWithFastMediumAndSlowCharacters;
            sequence.StartCombat();
            HeroSystemCharacter holder1 = sequence.ActivePhase.Character;


            HeroSystemCharacter attacker = sequence.ActiveSegment.PhaseNextInDexOrder.Character;

            HoldActionManuever held1 = holder1.Manuevers["Hold Action"] as HoldActionManuever;
       
            held1.Perform();

            Attack attack = attacker.Manuevers["Strike"] as Attack;

            attack?.Target(holder1);

            holder1.DEX.CurrentValue = 10;
            held1.Interrupt(InterruptionWith.Defensive );

            Assert.AreEqual(holder1, sequence.ActivePhase.Character);


        }

    }

    [TestClass]
    public class AbortActionTest
    {
        public CombatSequenceTestFactory Factory = new CombatSequenceTestFactory();

        [TestMethod]
        public void AbortingToDefensiveActionWhenAttackInProgess_InteruptsAttack()
        {

            //arrange
            CombatSequence sequence = Factory.SequenceWithFastMediumAndSlowCharacters;
            sequence.StartCombat();
            HeroSystemCharacter attacker = sequence.ActivePhase.Character;


            HeroSystemCharacter defender = sequence.ActiveSegment.PhaseNextInDexOrder.Character;

            
            Attack attack = attacker.Manuevers["Strike"] as Attack;

            attack?.Target(defender);

            defender.Manuevers["Abort"].Perform();

            Assert.AreEqual(sequence.InterruptedPhase.Character, attacker);

        }


        [TestMethod]
        public void AbortingToAction_CharacterCannotAbortAgainUntilNextPhaseStarts()
        {
            //arrange
            CombatSequence sequence = Factory.SequenceWithFastMediumAndSlowCharacters;
            HeroSystemCharacter other = Factory.Factory.BaseCharacter;
            other.DEX.MaxValue = 9;
            sequence.AddCharacter(other);

            sequence.StartCombat();
            HeroSystemCharacter attacker = sequence.ActivePhase.Character;
            attacker.DEX.MaxValue = 20;

            HeroSystemCharacter defender = sequence.ActiveSegment.PhaseNextInDexOrder.Character;
            defender.DEX.MaxValue = 10;
            TestAbortableManuever abortableManuever = new TestAbortableManuever("Abortable Manuever", defender);

          


            Attack attack = attacker.Manuevers["Strike"] as Attack;

            attack?.Target(defender);


            defender.Manuevers["Abort"].Perform();
            defender.Manuevers["Abortable Manuever"].Perform();

            Assert.AreEqual(false,defender.Manuevers["Abort"].CanPerform);
            

            Assert.AreEqual(sequence.InterruptedPhase.Character, attacker);

        }

        [TestMethod]
        public void WhenAttackInProgess_CanOnlyAbortIfCanAbortEvaluatesToTrue()
        {
            //arrange
            CombatSequence sequence = Factory.SequenceWithFastMediumAndSlowCharacters;
            sequence.StartCombat();
            HeroSystemCharacter attacker = sequence.ActivePhase.Character;
            Attack attack = attacker.Manuevers["Strike"] as Attack;
            HeroSystemCharacter defender = sequence.ActiveSegment.PhaseNextInDexOrder.Character;
            TestAbortableManuever abortableManuever = new TestAbortableManuever("Abortable Manuever", defender);

            //act
            attack?.Target(defender);
            defender.Manuevers["Abort"].Perform();
            //assert
            Assert.AreEqual(abortableManuever.Perform(),false);


        }

        [TestMethod]


        public void WhenAborting_OnlyDefensiveManueversAvailable()
        {
            //arrange
            CombatSequence sequence = Factory.SequenceWithFastMediumAndSlowCharacters;
            sequence.StartCombat();
            HeroSystemCharacter attacker = sequence.ActivePhase.Character;
            Attack attack = attacker.Manuevers["Strike"] as Attack;
            HeroSystemCharacter defender = sequence.ActiveSegment.PhaseNextInDexOrder.Character;
            TestAbortableManuever abortableManuever = new TestAbortableManuever("Abortable Manuever", defender);

            //act
            attack?.Target(defender);
            defender.Manuevers["Abort"].Perform();

            List<IManuever> notDefensiveManuevers = defender.AllowedManuevers.Values.Where
                (x => x.Type != ManueverType.Defensive).ToList();

            Assert.AreEqual(0, notDefensiveManuevers.Count);
        }

        [TestMethod]
        public void UsingAbortWithNonDefensiveManuever_ManueverFails()
        {
            //arrange
            CombatSequence sequence = Factory.SequenceWithFastMediumAndSlowCharacters;
            sequence.StartCombat();
            HeroSystemCharacter attacker = sequence.ActivePhase.Character;
            Attack attack = attacker.Manuevers["Strike"] as Attack;
            HeroSystemCharacter defender = sequence.ActiveSegment.PhaseNextInDexOrder.Character;
            TestAbortableManuever abortableManuever = new TestAbortableManuever("Abortable Manuever", defender);

            //act
            attack?.Target(defender);
            defender.Manuevers["Abort"].Perform();

            Attack notAllowedManuever = defender.Manuevers["Strike"] as Attack;

            Assert.AreEqual(notAllowedManuever?.Perform(), false);
        }





    }

    public class TestManuever : Manuever
    {
        public TestManuever( String name, HeroSystemCharacter character, bool isAbortable=false)
            : base(ManueverType.CombatManuever, name, character, isAbortable)
        {
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
    public class TestAbortableManuever : Manuever
    {
        public TestAbortableManuever(String name, HeroSystemCharacter character, bool isAbortable = true)
            : base(ManueverType.CombatManuever, name, character, isAbortable)
        {
            Type = ManueverType.Defensive;
            isAbortable = true;

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
            if (manuever.Name == "Strike")
            {
                return false;
            }
            return true;
        }


    }

}




