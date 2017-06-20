using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HeroSystemEngine.Character;
using HeroSystemEngine.Dice;
using HeroSystemEngine.Manuevers;
using HeroSystemsEngine.CombatSequence;
using HeroSystemsEngine.GameMap;
using Ploeh.AutoFixture;

namespace HeroSystemsEngine.Movement
{
    [TestClass]
    class BaseMovementTest
    {
        protected HeroSystemCharacter Character;
        protected Movement Run;
        protected Movement Flight;
        protected MovementManuever RunManuever;
        protected MovementManuever FlightManuever;
        protected CharacterTestObjectFactory Factory = new CharacterTestObjectFactory();
        protected CombatSequence.CombatSequence Sequence;
        protected CharacterMovement CharacterMovement;
        protected GameHex CharacterHex;
       

    [TestInitialize]
        public void FastCharacterWithRunAndFlyAndCharacterInCombat()
        {


            Character = Factory.BaseCharacter;
            Character.SPD.MaxValue = 6;
            Run = Character.CharacterMovement.Run;
            RunManuever = Character.Manuevers["Run"] as MovementManuever;

            Flight = new Movement(Character, "Flight", 100, true);
            Flight.NonCombatModifer = 4;
            FlightManuever = Character.Manuevers["Flight"] as MovementManuever;

            Sequence = new CombatSequence.CombatSequence();
            Sequence.AddCharacter(Character);
            Sequence.StartCombat();

            CharacterMovement = Character.CharacterMovement;
            Character.Hex = new GameHex(0,0,0);
            CharacterHex = Character.Hex;




        }
    }

    [TestClass]
    class MovementTest : BaseMovementTest
    {
        [TestMethod]
        public void CharacterMovesUptoHalfHisTotalInches_CharacterHasHalfPhaseLeft()
        {
            RunManuever.InchesMovingThisPhase = 3;
            Assert.AreEqual(RunManuever.PhaseActionTakes, PhaseLength.Half);
        }


        [TestMethod]
        public void CharacterMovesPastInchesLefts_CharacterOnlyMoveslAllowedAmount()
        {
            RunManuever.InchesMovingThisPhase = 3;
            RunManuever.Perform();

            Run.MoveCharacter(6);

            Assert.AreEqual(Run.InchesMovingThisPhase,3);

        }

        [TestMethod]
        public void CharacterMovesMoreThanHalfHisTotalInches_CharacterUsesCompletePhase()
        {
            RunManuever.InchesMovingThisPhase = 6;
            Assert.AreEqual(RunManuever.PhaseActionTakes, PhaseLength.Full);
        }

        [TestMethod]
        public void CharacterMoves_KPHIsDeterminedByMultiplyingSPDByMovement()
        {
            int KPH = Character.SPD.CurrentValue * Run.Inches * 5 * 60 / 500;
            Assert.AreEqual(KPH, Run.KPH);
        }

        [TestMethod]
        public void CharacterMovesInSegmentedMode_CharacterMovesEachSegmentBasedOnTotalMovementPerTurnDividedByTwelve()
        {
            Run.Inches = 18;
            RunManuever.SegmentMovementMode = true;

            SequenceTimer watchEachSeqgmentforRunning = new SequenceTimer
                (DurationUnit.Segment, 1, Sequence, Timing.Start,6);
            int segmentWatching = 0;
            RunManuever.InchesMovingThisPhase = 18;
            int inchesPerSegment = RunManuever.InchesMovingThisPhase * Character.SPD.CurrentValue/ Character.NextPhase.SegmentPartOf.Number;

            int next = 2;
            watchEachSeqgmentforRunning.TimerAction += delegate (SequenceTimer seq)
           {
               segmentWatching++;
               Assert.AreEqual(segmentWatching, Sequence.ActiveSegment.Number);
             //  Assert.AreEqual(inchesPerSegment * segmentWatching, Run.InchesMovingThisPhase - Run.InchesMovingThisPhaseNotYetUsed);

               if (segmentWatching == next)
               {
                   seq.KillTimer();
               }
           };

            watchEachSeqgmentforRunning.StartTimer();

            RunManuever.Perform();
            Run.RaceForward();


        }
    }

    [TestClass]
    class AccelerateOrDecelerateTest : BaseMovementTest
    {
        [TestMethod]
        public void CharacterMovesAndAccelerates_VelocityCannotExceedMaxMovementPerPhase()
        {
            FlightManuever.Distance = MovementDistance.HalfDistance;
            FlightManuever.Perform();
            Flight.MoveCharacter(25);

            Assert.AreEqual(100, Character.CharacterMovement.Velocity);

        }


        [TestMethod]
        public void CharacterMovesAndAcceleratesOrDecelerates_AcceleratesAndDeceleratesAtFiveInchesPerHexMoved()
        {
            Flight.Activate(100);
            Flight.MoveCharacter(1);

            Assert.AreEqual(5, CharacterMovement.Velocity);

            Flight.MoveCharacter(18, AccelerationDirection.Accelerate);
            Assert.AreEqual(95, CharacterMovement.Velocity);


            Flight.MoveCharacter(10, AccelerationDirection.Decelerate);
            Assert.AreEqual(45, CharacterMovement.Velocity);

        }

        [TestMethod]
        public void CharacterMoving_MustContinueToUseMovementUntilCharacterIsAbleToFullyDecelerate()
        {
            FlightManuever.Perform();
            Flight.MoveCharacter(100);
            Strike s = Character.Manuevers["Strike"] as Strike;
            ;
            Assert.AreEqual(false, s.CanPerform);
            Assert.AreEqual(1, Character.AllowedManuevers.Count);
            FlightManuever.Perform();

            Flight.StopMovingCharacter();

        }

        [TestMethod]
        public void ChacterMovingWithMovementAffectedByGravity_DeceleratesWhenClimbing()
        {

            Flight.Activate(100);
            Flight.TurnTowards(0, PitchHexFacing.StraightUp);

            Flight.MoveCharacter(100);

            //half because of gravity
            Assert.AreEqual(50,Flight.InchesMovingThisPhase);
            //flight is fininshed
            Assert.IsNull(Character.ManueverInProgess);


        }

        [TestMethod]
        public void ChacterMovingWithMovementAffectedByGravity_AcceleratesWhenDiving()
        {

            Flight.Activate(100);
            Flight.TurnTowards(0, PitchHexFacing.DiagonalDown);

            Flight.MoveCharacter(100);

            
            Assert.AreEqual(200, Flight.InchesMovingThisPhase);
            Assert.AreEqual(200, Flight.Velocity);
            //flight is fininshed
            Assert.IsNull(Character.ManueverInProgess);


        }
        
        public void
            ChacterMovingWithMovementAffectedByGravityAndAcceleratesWhenDiving_CanOnlyDecelerateAtOriginalFLightSpeed()
        {
            FlightManuever.Perform();
            Flight.Facing.Pitch = PitchHexFacing.Level;
            //Flight.ChracterFacing = PitchHexFacing.Level;
            Flight.Activate(100);

            Flight.TurnTowards(0, PitchHexFacing.StraightDown);

            Flight.RaceForward();

            Flight.StopMovingCharacter();

            Assert.AreEqual(100, Flight.Velocity);
            Assert.AreEqual(FlightManuever,Character.ManueverInProgess);

            Flight.StopMovingCharacter();
            Assert.AreEqual(null, Character.ManueverInProgess);

        }


    }

    [TestClass]
    class TurnMovementTest : BaseMovementTest
    {

        [TestInitialize]
        public void startFlying()
        {
            Flight.Activate(100);
            Flight.Facing.Yaw = YawHexFacing.North;
            
        }

       
        [TestMethod]
        public void CharacterTurns_FacingIsUpdated()
        {
            //turn one increment
            Flight.Turn(TurnDirection.Left);
            Assert.AreEqual(Flight.Facing.Yaw, YawHexFacing.NorthEast);

            //turn more than one increment
            Flight.Turn(TurnDirection.Left, 2);
            Assert.AreEqual(Flight.Facing.Yaw, YawHexFacing.SouthEast);

            //turn up
            Flight.Turn(TurnDirection.Up);
            Assert.AreEqual(Flight.Facing.Pitch, PitchHexFacing.DiagonalUp);

            //turning past original pitch works
            Flight.Turn(TurnDirection.Right, 4);
            Assert.AreEqual(Flight.Facing.Yaw, YawHexFacing.NorthWest);
           
            //turning right right doesnt impact up and down
            Assert.AreEqual(Flight.Facing.Pitch, PitchHexFacing.DiagonalUp);

            //turn yaw and pitch at same time
            Flight.Turn(TurnDirection.UpLeft);
            Assert.AreEqual(Flight.Facing.Pitch, PitchHexFacing.StraightUp);
            Assert.AreEqual(Flight.Facing.Yaw, YawHexFacing.North);

            //turning past last yaw and pitch works
            Flight.Turn(TurnDirection.DownRight, 6);
            Assert.AreEqual(Flight.Facing.Pitch, PitchHexFacing.UpsideDown);
            Assert.AreEqual(Flight.Facing.Yaw, YawHexFacing.East);


        }


        [TestMethod]
        public void CharacterTurnsTowardsLeftYawFacing_CharacterIsAtTargetedYawFacing()
        {
            Flight.Facing.Yaw = YawHexFacing.East;
            Flight.TurnTowards(YawHexFacing.SouthWest);
            Assert.AreEqual(YawHexFacing.SouthWest, Flight.Facing.Yaw);


            

        }

        [TestMethod]
        public void CharacterTurnsTowardsRightYawFacing__CharacterIsAtTargetedYawFacing()
        {
            Flight.Facing.Yaw = YawHexFacing.NorthWest;

            Flight.TurnTowards(YawHexFacing.South);
            Assert.AreEqual(YawHexFacing.South, Flight.Facing.Yaw);
        }

        [TestMethod]
        public void CharacterTurnsTowardsRightYawFacingThatTakesLessTurnsIfGoingLeft__CharacterIsAtTargetedYawFacingUsingRightTurns()
        {
            Flight.Facing.Yaw = YawHexFacing.NorthWest;

            TurnDirection direction = Flight.TurnTowards(YawHexFacing.NorthEast);
            Assert.AreEqual(YawHexFacing.NorthEast, Flight.Facing.Yaw);
            Assert.AreEqual(TurnDirection.Left.Yaw, direction.Yaw);

        }

        [TestMethod]
        public void CharacterTurnsTowardsLeftYawFacingThatTakesLessTurnsIfGoingRight__CharacterIsAtTargetedYawFacingUsingRightTurns()
        {
            Flight.Facing.Yaw = YawHexFacing.SouthEast;

            TurnDirection direction = Flight.TurnTowards(YawHexFacing.North);
            Assert.AreEqual(YawHexFacing.North, Flight.Facing.Yaw);
            Assert.AreEqual(TurnDirection.Right.Yaw, direction.Yaw);

        }


        [TestMethod]
        public void CharacterTurnsTowardsPitchFacing_CharacterIsAtTargetedPitchFacing()
        {
            Flight.Facing.Pitch = PitchHexFacing.Level;

            TurnDirection direction = Flight.TurnTowards(0,PitchHexFacing.StraightUp);
            Assert.AreEqual(PitchHexFacing.StraightUp, Flight.Facing.Pitch);
            Assert.AreEqual(TurnDirection.Up.Pitch, direction.Pitch);
        }

        [TestMethod]
        public void CharacterTurnsTowardsUpPitchFacingThatTakesLessTurnsIfTurningDown__CharacterIsAtTargetedPitchFacingUsingDownwardTurns()
        {
            Flight.Facing.Pitch = PitchHexFacing.DiagonalUp;

            TurnDirection direction = Flight.TurnTowards(0, PitchHexFacing.DiagonalDown);
            Assert.AreEqual(PitchHexFacing.DiagonalDown, Flight.Facing.Pitch);
            Assert.AreEqual(TurnDirection.Down.Pitch, direction.Pitch);

        }

        [TestMethod]
        public void CharacterTurnsTowardsDownPitchFacingThatTakesLessTurnsIfTurningUp__CharacterIsAtTargetedPitchFacingUsingUpwardTurns()
        {
            Flight.Facing.Pitch = PitchHexFacing.DiagonalDown;

            TurnDirection direction = Flight.TurnTowards(0, PitchHexFacing.StraightDown);
            Assert.AreEqual(PitchHexFacing.StraightDown, Flight.Facing.Pitch);
            Assert.AreEqual(TurnDirection.Down.Pitch, direction.Pitch);

        }

        [TestMethod]
        public void CharacterTurnBothPitchAndYaw_CharacterFacingIsUpdated()
        {
            Flight.Facing.Pitch = PitchHexFacing.Level;
            Flight.Facing.Yaw = YawHexFacing.North;

            TurnDirection direction = Flight.TurnTowards(YawHexFacing.West, PitchHexFacing.StraightDown);
            Assert.AreEqual(PitchHexFacing.StraightDown, Flight.Facing.Pitch);
            Assert.AreEqual(TurnDirection.Down.Pitch, direction.Pitch);

            Assert.AreEqual(YawHexFacing.West, Flight.Facing.Yaw);
            Assert.AreEqual(TurnDirection.Right.Yaw, direction.Yaw);

        }
        [TestMethod]
        public void CharacterTurnsTowardsYawFacing_CharacterTurnsDirectionBasedOnMinimumTurnsRequired()
        {
            Flight.Facing.Pitch = PitchHexFacing.DiagonalUp;
            Flight.Facing.Yaw = YawHexFacing.NorthWest;

            TurnDirection direction = Flight.TurnTowards(YawHexFacing.NorthEast, PitchHexFacing.StraightDown);

            Assert.AreEqual(3,Flight.TurnsMade);


        }
        [TestMethod]
        public void
            CharacterMovesWithMovementThatRequiresTurnMode_CanOnlyTurnOneAfterEachTimeHeHasTravelledOneFithHisTurnThatPhase()
        {
            int expectedTurnMode = 20;
            Flight.Facing.Pitch=PitchHexFacing.Level;
            //first turn
            Assert.AreEqual(true, Flight.CanTurn);
            Flight.Turn(TurnDirection.Left);

            //Accelerate and make second turn
            Flight.MoveCharacter(10);
            Assert.AreEqual(true, Flight.CanTurn);
            Flight.Turn(TurnDirection.Left);

            //full speed, try to move before turn mode and fail
            Flight.MoveCharacter(10);
            Assert.AreEqual(false, Flight.CanTurn);
            Assert.AreEqual(expectedTurnMode - 10, Flight.InchesOfTravelRequiredBeforeNextTurn);
            
            //move past turn mode, character can turn whenever he wants
            Flight.MoveCharacter(25);
            Assert.AreEqual(0, Flight.InchesOfTravelRequiredBeforeNextTurn);


        }

        [TestMethod]
        public void
            CharacterMovesWithMovementThatRequiresTurnModeAndIsMovingQuicklyAndMakesLargeTurn_CharacterWillTravelTurnModePerTurnIncrementAsPartOfTurn()
        {
            FlightManuever.InchesMovingThisPhase = 50;
            FlightManuever.Perform();
            Flight.Facing.Pitch = PitchHexFacing.Level;
            Flight.Facing.Yaw = YawHexFacing.East;
            Flight.RaceForward();

            FlightManuever.InchesMovingThisPhase = 50;
            FlightManuever.Perform();
            Flight.TurnTowards(YawHexFacing.SouthWest, 0,AccelerationDirection.Coasting);
            Assert.AreEqual(10, Flight.InchesMovingThisPhaseNotYetUsed);
        }

        [TestMethod]
        public void
            CharacterMovesWithMovementThatRequiresTurnModeAndIsMovingQuicklyAndMakesLargeTurnAndDeceleratesWhileTurning_CanTurnWithMuchLessMovement ()
        {
            FlightManuever.InchesMovingThisPhase = 50;
            FlightManuever.Perform();
            Flight.Facing.Pitch = PitchHexFacing.Level;
            Flight.Facing.Yaw = YawHexFacing.East;
            Flight.RaceForward();

            FlightManuever.InchesMovingThisPhase = 50;
            FlightManuever.Perform();
            Flight.TurnTowards(YawHexFacing.SouthWest, 0, AccelerationDirection.Decelerate);
            Assert.AreEqual(30, Flight.InchesMovingThisPhaseNotYetUsed);

        }

    }
    [TestClass]
    class NonCombatMovementTest : BaseMovementTest
    {
        [TestMethod]
        public void CharacterMovesUsingNonCombatMovement_AcceleratesAtCombatMovmenetToMovesAtCombatMoveTimesNonCombatMultiplier()
        {
            FlightManuever.Distance = MovementDistance.FullDistance;
            FlightManuever.Mode = MovementMode.NonCombatMovement;
            FlightManuever.Distance= MovementDistance.FullDistance;
            FlightManuever.Perform();
            Flight.RaceForward();

            Assert.AreEqual(100,Flight.InchesOfTravelSinceLastMove);
            FlightManuever.Perform();
            Flight.RaceForward();
            Assert.AreEqual(200, Flight.InchesOfTravelSinceLastMove);

            FlightManuever.Perform();
            Flight.RaceForward();
            Assert.AreEqual(300, Flight.InchesOfTravelSinceLastMove);

            FlightManuever.Perform();
            Flight.RaceForward();
            Assert.AreEqual(400, Flight.InchesOfTravelSinceLastMove);



        }

        [TestMethod]
        public void CharacterMovesUsingNonCombatMovement_Has0OCVofZero()
        {
            FlightManuever.Distance = MovementDistance.FullDistance;
            FlightManuever.Mode = MovementMode.NonCombatMovement;
            FlightManuever.Perform();

            Assert.AreEqual(Character.OCV.CurrentValue, 0);
        }

        [TestMethod]
        public void CharacterMovesUsingNonCombatMovement_AndDCVBasedOnVelocity()
        {
            FlightManuever.Distance = MovementDistance.FullDistance;
            FlightManuever.Mode = MovementMode.NonCombatMovement;

            FlightManuever.Perform();
            Flight.RaceForward();
            Assert.AreEqual(Character.DCV.CurrentValue, 11);

            FlightManuever.Perform();
            Flight.RaceForward();
            Assert.AreEqual(Character.DCV.CurrentValue, 13);


            FlightManuever.Perform();
            Flight.RaceForward();
            Assert.AreEqual(Character.DCV.CurrentValue, 13);

            FlightManuever.Perform();
            Flight.RaceForward();
            Assert.AreEqual(Character.DCV.CurrentValue, 15);
        }




        [TestMethod]
        public void
            AttackerAnddefenderIsTravellingInSameDirectionWITHNonCombatMovment_VelocityBonusIsDecreasedBasedOnAttackersSpeed
            ()
        {

        }

        [TestMethod]
        public void
            AttackerAndDefenderIsTravellingInOppositeDirectionWITHNonCombatMovment_VelocityBonusIsIncreasedBasedOnAttackerSpeed
            ()
        {

        }

        [TestMethod]
        public void
            CharacterMovesInNonCombatWithMovementThatRequiresTurnMode_CanOnlyTurnOnceHeHasTravelledHalfHisTurnThatPhase
            ()
        {
        }

    }

    [TestClass]
    class MovementAndSTRTest
    {
        [TestMethod]
        public void CharacterSpendsMovementOnEnhancingSTR_GainsSTRBenefitOfOnePerTwoInchesSpent()
        {

        }


        [TestMethod]
        public void
            MovingCharacterAttemptsToGrabAndSlowDownMovingTarget_BothAddsSTRFromMovementToDetermineGrabSuccess()
        {


        }

        [TestMethod]
        public void
            MovingCharacterAttemptsToGrabAndSlowDownMovingTarget_TargetMayMakeEscapeWithFullSTRIncludingMovement
            ()
        {


        }

        [TestMethod]
        public void
            MovingCharacterAttemptsToGrabAndSlowDownMovingTarget_targetLosesSpeedPerPhaseEqualToCharactersSTRRollWithMovementAdded
            ()
        {


        }

        [TestMethod]
        public void MovingCharacterEncountersObstacle_CharacterCanMoveObstacleIfHeSucceedsInCasualSTRRoll()
        {


        }

        [TestMethod]
        public void MovingCharacterEncountersObstacle_CrashedIntoObstacleAndTakesDamageIfCasualSTRRollFailed()
        {


        }


    }

    [TestClass]
    class MovementSkillLevelTest
    {
        public void CharacterMovesAndUsesMovementSkillToAccelerateOrDecelerate_CharacterCanIncreasesAccelrerateOrDecelerationRateByOneInchPerHex
            ()
        {

        }

        public void
          CharacterMovesWithMovementThatRequiresTurnModeAndUsesSkillToReduceTurnMode_ReducesByOneForEachSkillUsed()
        {

          //  Flight.Turn(TurnDirection.Down, 2);
        }

        public void CharacterMovesAndUsesSkillToImproveLandingInHex_CharacterIncreasesChanceToLandInHexByOne()
        {
        }
        public void CharacterMovesAndPerformsManueverThatIncreasesDefense_CharacterCanApplyMovementSkillToCharacterDCV()
        {
        }

    }

    [TestClass]
    class NormalMovementTest
    {
        [TestMethod]
        public void CharactersHaveNormalMovementswithSpecificSpeeds()
        {
        }

        public void CharacterRuns_MayTurnAsOftenAsCharacterWants()
        {

        }


        public void CharacterRunsOnTreacherousGround_CharacterTurningIsLimitedBasedOnTurnMode()
        {

        }


        public void CharacterStandingJumps_CanJumpHalfDIstanceBasedOnSTR()
        {

        }

        public void CharacterSwims_MayTurnAsOftenAsCharacterWants()
        {

        }

    }


    [TestClass]
    class PoweredMovementTest
    {
        public void CharacterFliesOrGlides_CharacterTurningIsLimitedBasedOnTurnMode()
        {
        }

        public void CharacterGlides_CharacterCanOnlyGain1D6Altitude()
        {
        }

        public void CharacterGlides_CanOnlyGainVelocityByLosingAltitude()
        {
        }

        public void CharacterSwingsInAStraightLine_CharacterCanOnlyMakeMinorAdjustmentsToCharacterTravelPath()
        {
        }

        public void CharacterSwingsInAnArc_CharacterTurnRateIsLimitedByTravelMode()
        {
        }
    }

    [TestClass]
    class LeapTest
    {
        public void CharacterJumps_CanJumpDIstanceBasedOnSTR()
        {

        }

        public void
            CharacterJumpsToComplexArea_RequiresAttackRollWithRangedModiferesAgainstHexToSuccessfullyLandInHex()
        {

        }


    }


}
