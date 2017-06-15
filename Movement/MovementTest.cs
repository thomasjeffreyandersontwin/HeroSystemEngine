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


        [TestInitialize]
        public void FastCharacterWithRunAndFlyAndCharacterInCombat()
        {
            
            
            Character = Factory.BaseCharacter;
            Character.SPD.MaxValue = 6;
            Run = Character.CharacterMovement.Run;
            RunManuever = Character.Manuevers["Run"] as MovementManuever;

            Flight = new Movement(Character, "Flight", 20, true);
            FlightManuever = Character.Manuevers["Flight"] as MovementManuever;

            Sequence = new CombatSequence.CombatSequence();
            Sequence.AddCharacter(Character);
            Sequence.StartCombat();

            CharacterMovement = Character.CharacterMovement;

        }
    }
    
    [TestClass]
    class MovementTest : BaseMovementTest
    {   

        [TestMethod]
        public void CharacterMovesUptoHalfHisTotalInches_CharacterHasHalfPhaseLeft()
        {
            RunManuever.InchesMoving = 3;
            Assert.AreEqual(RunManuever.PhaseActionTakes,  PhaseLength.Half);
        }

        [TestMethod]
        public void CharacterMovesMoreThanHalfHisTotalInches_CharacterUsesCompletePhase()
        {
            RunManuever.InchesMoving = 6;
            Assert.AreEqual(RunManuever.PhaseActionTakes, PhaseLength.Full);
        }



        [TestMethod]
        public void CharacterMoves_KPHIsDeterminedByMultiplyingSPDByMovement()
        {
            int KPH = Character.SPD.CurrentValue * Run.Inches * 5 * 60 / 500;
            Assert.AreEqual(KPH, Run.KPH);
        }

        [TestMethod]
        public void CharacterMovesAndAcceleratesOrDecelerates_AcceleratesAndDeceleratesAtFiveInchesPerHexMoved()
        {
            Flight.Activate(100);
            Flight.MoveCharacter(MovementDirection.Forward, 1);

            Assert.AreEqual(5, CharacterMovement.Velocity);

            Flight.MoveCharacter(MovementDirection.StrafeLeft, 18, VelocityChange.Accelerate);
            Assert.AreEqual(95, CharacterMovement.Velocity);


            Flight.MoveCharacter(MovementDirection.StrafeRight, 10, VelocityChange.Decelerate);
            Assert.AreEqual(45, CharacterMovement.Velocity);

        }

        [TestMethod]
        public void CharacterMoving_CannotTurnOffMovementUntilAbleToFullyDecelerate()
        {
            FlightManuever.Perform();
            Flight.MoveCharacter(MovementDirection.Forward, 100);

            Assert.AreEqual(1, Character.AllowedManuevers);








        }

        [TestMethod]
        public void CharacterMovingWithMovementAffectedByGravity_DeceleratesWhenClimbingAndAcceleratesWhenDiving()
        {

        }

        [TestMethod]
        public void CharacterMovesInSegmentedMode_CharacterMovesEachSegmentBasedOnTotalMovmeentPerTurnDividedByTwelve()
        {
        }

        [TestMethod]
        public void CharacterMovesWithMovementThatRequiresTurnMode_CanOnlyTurnOnceHeHasTravelledOneFithHisTurnThatPhase()
        { }

        [TestMethod]
        public void CharacterMovesWithMovementThatRequiresTurnModeFliesDiagonallyTowardsGround_MustFlyTurnModeToLevelOut()
        { }

        [TestMethod]
        public void CharacterMovesWithMovementThatRequiresTurnModeFliesStraightDownTowardsGround_MustFlyTTwiceTurnModeToLevelOut()
        { }

        public void CharacterMovesWithMovementThatRequiresTurnModeAndUsesSkillToReduceTurnMode_ReducesByOneForEachSkillUsed()
        {
        }

        public void CharacterMovesAndUsesMovementSkillToAccelerateOrDecelerate_CharacterCanIncreasesAccelrerateOrDecelerationRateByOneInchPerHex() { }


        public void CharacterMovesAndPerformsManueverThatIncreasesDefense_CharacterCanApplyMovementSkillToCharacterDCV() { }

        public void CharacterMovesAndUsesSkillToImproveLandingInHex_CharacterIncreasesChanceToLandInHexByOne() { }



    }

    class NonCombatMovementTest
    {
        [TestMethod]
        public void CharacterMovesUsingNonCombatMovement_MovesAtCombatMoveTimesNonCombatMultiplier()
        {
        }

        [TestMethod]
        public void CharacterMovesUsingNonCombatMovement_Has0OCVAndDCVBasedOnVelocity()
        {

        }

        [TestMethod]
        public void CharacterNonCombatMovesAndAccelerates_AccelerationIsLimitedToCombatSpeed()
        {

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
        public void CharacterMovesInNonCombatWithMovementThatRequiresTurnMode_CanOnlyTurnOnceHeHasTravelledHalfHisTurnThatPhase()
        { }

    }

    [TestClass]
    class MovementAndSTRTest
    {
        [TestMethod]
        public void CharacterSpendsMovementOnEnhancingSTR_GainsSTRBenefitOfOnePerTwoInchesSpent()
        {

        }


        [TestMethod]
        public void MovingCharacterAttemptsToGrabAndSlowDownMovingTarget_BothAddsSTRFromMovementToDetermineGrabSuccess()
        {


        }

        [TestMethod]
        public void MovingCharacterAttemptsToGrabAndSlowDownMovingTarget_TargetMayMakeEscapeWithFullSTRIncludingMovement
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

        public void CharacterGlides_CanOnlyGainVelocityByLosingVelocity()
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

        public void CharacterJumpsToComplexArea_RequiresAttackRollWithRangedModiferesAgainstHexToSuccessfullyLandInHex()
        {

        }


    }

}
