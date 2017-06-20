using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using HeroSystemEngine.Character;
using HeroSystemEngine.Manuevers;
using HeroSystemsEngine.CombatSequence;
using HeroSystemsEngine.GameMap;

namespace HeroSystemsEngine.Movement
{


    public class TurnDirection
    {

        public int Yaw;
        public int Pitch;

        public TurnDirection(int yaw, int pitch)
        { 
                Yaw = yaw;
                Pitch = pitch;
            
        }

        public static TurnDirection Left = new TurnDirection(-1, 0);
        public static TurnDirection Right = new TurnDirection(1, 0);

        public static TurnDirection Up = new TurnDirection(0, 1);
        public static TurnDirection UpLeft = new TurnDirection(-1, 1);
        public static TurnDirection UpRight = new TurnDirection(1, -1);

        public static TurnDirection Down = new TurnDirection(0, -1);
        public static TurnDirection DownLeft = new TurnDirection(1, -1);
        public static TurnDirection DownRight = new TurnDirection(1, -1);


        public static TurnDirection operator +(TurnDirection a, TurnDirection b)
        {
            return new TurnDirection(a.Yaw + b.Yaw, a.Pitch + b.Pitch);
        }

        public static TurnDirection operator -(TurnDirection a, TurnDirection b)
        {
            return new TurnDirection(a.Yaw - b.Yaw, a.Pitch - b.Pitch);
        }

    }
    public enum YawHexFacing { North=1, NorthWest=2, West=3,SouthWest=4, South=5, SouthEast=6, East=7, NorthEast=8 }
    public enum PitchHexFacing { Level=1, DiagonalUp=2, StraightUp=3, BackDiagonalUp=4, UpsideDown=5, BackDiagonalDown=6, StraightDown=7, DiagonalDown=8}
    public enum MovementMode { NonCombatMovement, CombatMovement }

    public delegate void MovementCompletionHandler();
    public enum MovementDistance { HalfDistance, FullDistance}
    public enum AccelerationDirection { Accelerate, Decelerate,Coasting }

    public class HexFacing
    {
        public YawHexFacing Yaw = YawHexFacing.North;
        public PitchHexFacing Pitch = PitchHexFacing.Level;

    }

    public class MovementManuever : Manuever
    {
        public MovementManuever(HeroSystemCharacter character, Movement movement)
            : base(ManueverType.MovementManuever, movement.Name, character, false)
        {
            Movement = movement;
        }
        private Movement Movement;

        public override bool canPerform()
        {
            return InchesMovingThisPhase <= Movement.Inches || Distance != null;
        }
        public override bool Perform()
        {
            if (StartManuever() == false) return false;
            if (CanPerform)
            {
                PerformManuever();
                return true;
            }
            else
            {
                return false;
            }
        }
        private MovementDistance? _distance;
        public MovementDistance? Distance
        {
            get
            {
                return _distance;

            }
            set
            {
                _inchesMoving = 0;
                _distance = Distance;


            }
        }

        private int _inchesMoving = 0;

        public override ManueverModifier Modifier
        {
            set
            {
                if (Movement != null)
                {
                    Movement.NonCombatCVModifier = Modifier;
                }

            }
            get
            {
                return Movement.NonCombatCVModifier;
                
            }  
        }
        public MovementMode Mode
        {
            get { return Movement.Mode; }
            set
            {
                Movement.Mode = value;
            }
        }

        private bool _segmentedModeType;

        public bool SegmentMovementMode
        {
            get { return Movement.IsMovingPerSegment; }
            set { Movement.IsMovingPerSegment = value; }
        }

        public int InchesMovingThisPhase
        {
            get
            {
                return _inchesMoving;
            }
            set
            {
                _distance = null;
                _inchesMoving = value;
                if (_inchesMoving > Movement.Inches / 2)
                {
                    PhaseActionTakes = PhaseLength.Full;
                }
                else
                {
                    PhaseActionTakes = PhaseLength.Half;
                }

            }
        }


        public override void PerformManuever()
        {
            int inchesMoving;
            if (Distance != null)
            {

                if (Distance == MovementDistance.HalfDistance)
                {
                    inchesMoving = Movement.Inches;
                }
                else
                {
                    inchesMoving = Movement.Inches / 2;
                }
            }
            else
            {
                inchesMoving = InchesMovingThisPhase;
            }

            Movement.Activate(inchesMoving);
            Movement.Completed += CompleteManuever;
            Character.ManueverInProgess = this;

        }
  
        public override void CompleteManuever()
        {
            if (SegmentMovementMode)
            {
                if (Movement.InchesMovingPerSegment == Movement.InchesOfTravelSinceLastMove)
                {
                    PhaseActionTakes = PhaseLength.Full;
                }
            }
            else
            {
                if (Movement.InchesMovingThisPhase <= Movement.Inches / 2)
                {
                    PhaseActionTakes = PhaseLength.Half;
                }
                else
                {
                    PhaseActionTakes = PhaseLength.Full;
                }
            }
            if (Movement.Velocity == 0)
            {
                base.CompleteManuever();
            }
            Movement.Completed -= CompleteManuever;
            if (Movement.Velocity == 0)
            {
                if (Character.ManueverInProgess == this)
                {
                    Character.ManueverInProgess = null;
                }
            }
        }

    }


    public class CharacterMovement
    {
        public CharacterMovement(HeroSystemCharacter character)
        {
            Character = character;
            Run = new Movement(character, "Run", 6, false);
            Swim = new Movement(character, "Swim", 2, false);
            Leap = new Movement(character, "Leap", 2, false);
        }

        #region Movements
        public Movement Run;
        public Movement Swim;
        public Movement Leap;
        public List<Movement> Movements = new List<Movement>();
        public Movement ActiveMovement;
#endregion'
        private HeroSystemCharacter Character;

        #region Move Character
        public int Inches { get { return ActiveMovement.Inches; } set { ActiveMovement.Inches = value; } }
        public int InchesMoving { get { return ActiveMovement.InchesMovingThisPhase; } set { ActiveMovement.InchesMovingThisPhase=value;} }
        public void MoveCharacter(int inchesMoving, AccelerationDirection accelerationDirection = AccelerationDirection.Accelerate, int maxVelocityChangeCharacterWantsToMake = 0)
        {
            ActiveMovement?.MoveCharacter(inchesMoving, accelerationDirection);

        }
        public void RaceForward() { ActiveMovement?.RaceForward();}
        public void Coast(int inchesMoving = 0) { ActiveMovement.Coast();}
        public void StopMovingCharacter() { ActiveMovement.StopMovingCharacter();}
        #endregion

        #region Adjust Velocity 
        public int? Velocity
        {
            get
            {
                return ActiveMovement?.Velocity;
            }
            set
            {
                ActiveMovement.Velocity = (int)value;

            }

        }
        public AccelerationDirection AccelerationDirection { get { return ActiveMovement.AccelerationDirection; } set { ActiveMovement.AccelerationDirection = value; } }
        public int MaximimAcceleration { get { return ActiveMovement.MaximimAcceleration; } }
        public int VelocityChangeRate { get { return ActiveMovement.VelocityChangeRate; } }
        #endregion

        #region Turn Character
        public int TurnMode { get { return ActiveMovement.TurnMode; } }
        public bool CanTurn { get { return ActiveMovement.CanTurn; } }
        public int InchesOfTravelSinceLastTurn {get { return ActiveMovement.InchesOfTravelRequiredBeforeNextTurn; }}
        public int InchesOfTravelRequiredBeforeNextTurn { get { return ActiveMovement.InchesOfTravelRequiredBeforeNextTurn; } }
        public void Turn(TurnDirection turnDirection, int turnNumber) { ActiveMovement.Turn(turnDirection,turnNumber);}
        public void Turn(TurnDirection turnDirection){ActiveMovement.Turn(turnDirection);
        }

        public TurnDirection TurnTowards(YawHexFacing targetYaw, PitchHexFacing targetPitch = 0) { return ActiveMovement.TurnTowards(targetYaw,targetPitch); }
        public HexFacing Facing { get; internal set; }

        public int TurnsMade { get; set; }
        public bool Descending { get { return ActiveMovement.Descending; } }
        public bool Ascending { get { return ActiveMovement.Ascending; } }
        #endregion
    }

    public class Movement
    {
        public Movement(HeroSystemCharacter character, string name, int inches, bool turningModeRequired)
        {
            this.Character = character;
            this.Name = name;
            this.Inches = inches;
            this.TurningModeRequired = turningModeRequired;
            Facing = new HexFacing();

            MovementManuever manuever = new MovementManuever(this.Character, this);
            NonCombatCVModifier = new ManueverModifier();
        }

        private HeroSystemCharacter Character;
        public string Name;

        private bool _isActive;
        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                _isActive = value;
                if (value)
                {

                    Character.CharacterMovement.ActiveMovement = this;
                    InchesOfTravelSinceLastMove = 0;
                    ConductedFirstTurnThisMove = false;


                }
                else
                {
                    Character.CharacterMovement.ActiveMovement = null;
                }
            }
        }
        public void Activate(int totalMovement)
        {
            IsActive = true;
            if (totalMovement < Inches)
            {
                InchesMovingThisPhaseNotYetUsed = totalMovement;
            }

        }

        public int KPH => Character.SPD.CurrentValue * Inches * 5 * 60 / 500;
        public int MPH => (int) (Character.SPD.CurrentValue * Inches * 5 * 60 / 804.5);

        #region Move Character


        

        public int NonCombatInches
        {
            get
            {
                return inches * NonCombatModifer;
            }
        }

        

        private MovementMode _mode = MovementMode.CombatMovement;
        public MovementMode Mode 
        {
            get { return _mode; }
            set
            {
                _mode = value;
                if (_mode == MovementMode.NonCombatMovement)
                {
                    NonCombatCVModifier.OCV.SetToZero = true;



                }
            }
        }

        public int DetermineDCVFromRelativeVelocity
        {
            get
            {
                int nonCombatInchesPerTurn = MaximumNonCombatMovementInches * Character.SPD.CurrentValue;
                if (nonCombatInchesPerTurn < 32)
                {

                    return 1;
                }
                if (nonCombatInchesPerTurn < 64)
                {

                    return 3;
                }
                if (nonCombatInchesPerTurn < 125)
                {

                    return 5;
                }
                if (nonCombatInchesPerTurn < 250)
                {

                    return 7;
                }
                
                if (nonCombatInchesPerTurn < 500)
                {

                    return 9;
                }
                if (nonCombatInchesPerTurn < 1000)
                {

                    return 11;
                }
                if (nonCombatInchesPerTurn < 2000)
                {

                    return 13;
                }
                if (nonCombatInchesPerTurn < 4000)
                {

                    return 15;
                }
                if (nonCombatInchesPerTurn < 8000)
                {

                    return 17;
                }
                if (nonCombatInchesPerTurn < 1600)
                {

                    return 19;
                }
                return 0;

            }
        }

        private int inches;
        public int Inches
        {
            get
            {

                return inches;

            }
            set { inches = value; }
        }
        private int _inchesMovingThisPhase;
        public int InchesMovingThisPhase
        {
            get { return _inchesMovingThisPhase; }
            set
            {
                _inchesMovingThisPhase = value;
                if (InchesMovingThisPhaseNotYetUsed == 0)
                {
                    InchesMovingThisPhaseNotYetUsed = InchesMovingThisPhase;
                }
            }
        }
        private int InchesMovingThisMove
        {
            get
            {
                int inchesMovingThisMove = 0;
                if (IsMovingPerSegment)
                {
                    inchesMovingThisMove = InchesMovingPerSegment;
                }
                else
                {
                    inchesMovingThisMove = InchesMovingThisPhase;
                }
                if (Mode == MovementMode.NonCombatMovement)
                {
                    if (inchesMovingThisMove > MaximumNonCombatMovementInches)
                    {
                        return MaximumNonCombatMovementInches;
                    }
                }

                return inchesMovingThisMove;
            }
        }
        private int MaximumNonCombatMovementInches
        {
            get { return Velocity + MaximimAcceleration; }
        }

        public int InchesMovingThisPhaseNotYetUsed { get; set; }

        public int InchesMovingPerSegment
        {
            get { return (InchesMovingThisPhase * Character.SPD.CurrentValue) / 12; }
        }
        public bool IsMovingPerSegment
        {
            get { return _isMovingPerSegment; }
            set
            {
                _isMovingPerSegment = value;
            }
        }

        private double GravityMovementBonus
        {
            get
            {
                double gravityBonus = 1;
                if (Descending)
                {
                    gravityBonus = gravityBonus * 2;
                }
                if (Ascending)
                {
                    gravityBonus = gravityBonus / 2;
                }
                return gravityBonus;
            }
        }

        public void MoveCharacter(int inchesMoving,
            AccelerationDirection accelerationDirection = AccelerationDirection.Accelerate
            )
        {
            if (IsActive)
            {
                initializeMovementParameters(inchesMoving, accelerationDirection);
                limitInchesMovingToAvailableMovementAllowance();
                move();
                updateMovementAllowanceAndNotifyMovementManueverOfCompletion();
            }

        }

        private void initializeMovementParameters(int inchesMoving, AccelerationDirection accelerationDirection)
        {
            VelocityChangedThisMove = 0;

            AccelerationDirection = accelerationDirection;
            
            InchesMovingThisPhase = (int) (inchesMoving * GravityMovementBonus);
        }
        private void move()
        {
            if (AccelerationDirection != AccelerationDirection.Coasting)
            {
                moveAndAdjustVelocity();
            }
            else
            {
                Coast();
            }
        }
        private void limitInchesMovingToAvailableMovementAllowance()
        {
            if (InchesMovingThisPhaseNotYetUsed * GravityMovementBonus < InchesMovingThisPhase)
            {
                InchesMovingThisPhase = InchesMovingThisPhaseNotYetUsed;
            }
        }
        private void updateMovementAllowanceAndNotifyMovementManueverOfCompletion()
        {

            InchesMovingThisPhaseNotYetUsed -= (int)(InchesMovingThisMove / GravityMovementBonus);
            if (IsMovingPerSegment)
            {
                if (InchesMovingThisMove == InchesMovingPerSegment)
                {
                    IsActive = false;
                    Completed?.Invoke();
                    return;
                }
            }
            if (InchesMovingThisPhaseNotYetUsed == 0 )
            {
                if (Velocity == 0)
                {
                    IsActive = false;
                }
                Completed?.Invoke();
                Completed = null;
            }
        }

        public void RaceForward()
        {
            if (Mode == MovementMode.NonCombatMovement)
            {
                MoveCharacter(NonCombatInches);
            }
            else
            {
                MoveCharacter(Inches);
            }
        }
        public void Coast(int inchesMoving = 0)
        {
            if (inchesMoving == 0)
            {
                inchesMoving = InchesMovingThisPhase;
            }
            for (int inchMoving = 1; inchMoving <= inchesMoving; inchMoving++)
            {
                MapFactory.ActiveGameMap.MoveGameObject(Character, Velocity, Facing.Yaw, Facing.Pitch);
                InchesOfTravelSinceLastMove++;
            }
        }
        public void StopMovingCharacter()
        {
            int travelRequired = Velocity / 5;
            MoveCharacter(travelRequired, AccelerationDirection.Decelerate);

        }

        public MovementCompletionHandler Completed;

        #endregion

        #region Adjust Velocity

        public int Velocity;
        public AccelerationDirection AccelerationDirection { get; set; }

        public int MaximimAcceleration
        {
            get
            {
                int maximimAccelerationAllowedThisMove;

                if (AccelerationDirection == AccelerationDirection.Accelerate)
                {
                    if (Mode == MovementMode.CombatMovement)
                    {
                        maximimAccelerationAllowedThisMove = Inches * (int) GravityMovementBonus - Velocity;
                    }
                    else
                    {
                        maximimAccelerationAllowedThisMove = Inches * (int) GravityMovementBonus;
                    }
                }
                else
                {
                    maximimAccelerationAllowedThisMove = Inches;
                }
                return maximimAccelerationAllowedThisMove;
            }
        }
        public int VelocityChangeRate
        {
            get
            {
                int velocityChangeEachInchOfMovement = 0;
                if (AccelerationDirection == HeroSystemsEngine.Movement.AccelerationDirection.Accelerate)
                {
                    velocityChangeEachInchOfMovement = 5;
                }
                else
                {
                    velocityChangeEachInchOfMovement = -5;
                }
                return velocityChangeEachInchOfMovement;
            }
        }

        private void moveAndAdjustVelocity()
        {
            if (Mode == MovementMode.NonCombatMovement)
            {
                NonCombatCVModifier.DCV.SetToZero = true;
                NonCombatCVModifier.DCV.ModiferAmount = DetermineDCVFromRelativeVelocity;
                NonCombatCVModifier.Activate(Character);
            }
            int maximimAcceleration = MaximimAcceleration;
            int inchesMoving = InchesMovingThisMove;

            for (int inchMoving = 1; inchMoving <= inchesMoving; inchMoving++)
            {
                VelocityChangedThisMove = adjustVelocity(maximimAcceleration);
                MapFactory.ActiveGameMap.MoveGameObject(Character, Velocity, Facing.Yaw, Facing.Pitch);
                InchesOfTravelSinceLastMove++;
            }
            if (IsMovingPerSegment)
            {
                startTimerThatMovesCharacterEachSegment();
            }
           
        }

        public ManueverModifier NonCombatCVModifier { get; set; }

        private void startTimerThatMovesCharacterEachSegment()
        {
            int segmentsUntilnextPhase = Character.NextPhase.SegmentPartOf.Number - Character.ActivePhase.SegmentPartOf.Number;
            SequenceTimer timer = new SequenceTimer
                (DurationUnit.Segment, segmentsUntilnextPhase, Character.CombatSequence);
            timer.TimerAction += delegate(SequenceTimer t)
            {
                move();
                updateMovementAllowanceAndNotifyMovementManueverOfCompletion();
            };
            timer.StartTimer();
        }

        private int adjustVelocity(int maximimAccelerationAllowedThisMove)
        {
            if (Math.Abs(VelocityChangedThisMove) < maximimAccelerationAllowedThisMove)
            {
                Velocity = Velocity + VelocityChangeRate;
                VelocityChangedThisMove += VelocityChangeRate;
            }
            return VelocityChangedThisMove;
        }


        #endregion

        #region Turn Character

        private bool TurningModeRequired;
        public int TurnMode
        {
            get { return Velocity / 5; }
        }
        public bool CanTurn
        {
            get
            {
                if (ConductedFirstTurnThisMove == false)
                {
                    return true;
                }
                if (InchesOfTravelSinceLastMove >= TurnMode)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private bool ConductedFirstTurnThisMove;
        private int VelocityChangedThisMove;
        private bool _isMovingPerSegment;
        private int _segmentedIchesMoving;
        
        public int InchesOfTravelSinceLastMove { get; internal set; }
         public int InchesOfTravelRequiredBeforeNextTurn
        {
            get
            {
                if ((TurnMode - InchesOfTravelSinceLastMove) >= 0)
                {
                    return TurnMode - InchesOfTravelSinceLastMove;
                }
                return 0;
            }
        }
        public void Turn(TurnDirection turnDirection, int turnNumber)
        {
            for (int turn = 1; turn <= turnNumber; turn++)
            {
                Turn(turnDirection);
            }
        }

        public void Turn(TurnDirection turnDirection)
        {
            if (CanTurn)
            {
                UpdateFacing(turnDirection);
                InchesOfTravelSinceLastMove = 0;
                if (ConductedFirstTurnThisMove == false)
                {
                    ConductedFirstTurnThisMove = true;
                }

            }
        }

        public TurnDirection TurnTowards(YawHexFacing targetYaw, PitchHexFacing targetPitch = 0,
            AccelerationDirection accelerationDirection = AccelerationDirection.Accelerate,
            int maxVelocityChangeCharacterWantsToMake = 0)
        {
            TurnDirection combineDirection = new TurnDirection(0, 0);
            TurnDirection yawDirectionToTurn = new TurnDirection(0, 0);
            TurnDirection pitchDirectionToTurn = new TurnDirection(0, 0);

            if (targetYaw != 0)

            {
                int completeYaw = YawHexFacing.NorthEast - YawHexFacing.North + 1;
                int yawDelta = yawDelta = Math.Abs(Facing.Yaw - targetYaw);
                int YawDeltaTurningOppositeDirection = 0;
                if (yawDelta > completeYaw / 2)
                {
                    YawDeltaTurningOppositeDirection = Math.Abs((int) targetYaw - yawDelta);

                }
                if (YawDeltaTurningOppositeDirection == 0)
                {
                    if (Facing.Yaw > targetYaw)
                    {
                        yawDirectionToTurn = TurnDirection.Left;
                    }
                    else
                    {
                        yawDirectionToTurn = TurnDirection.Right;
                    }
                }
                else
                {

                    if (Facing.Yaw > targetYaw)
                    {
                        yawDirectionToTurn = TurnDirection.Right;
                    }
                    else
                    {
                        yawDirectionToTurn = TurnDirection.Left;
                    }
                    ;
                }

            }


            if (targetPitch != 0)
            {
                int completePitch = PitchHexFacing.DiagonalDown - PitchHexFacing.Level + 1;
                int pitchDelta = Math.Abs(Facing.Pitch - targetPitch);
                int PitchDeltaTurningOppositeDirection = 0;
                if (pitchDelta > completePitch / 2)
                {
                    PitchDeltaTurningOppositeDirection = Math.Abs((int) targetPitch - pitchDelta);

                }
                ;
                if (PitchDeltaTurningOppositeDirection == 0)
                {
                    if (Facing.Pitch > targetPitch)
                    {
                        pitchDirectionToTurn = TurnDirection.Down;
                    }
                    else
                    {
                        pitchDirectionToTurn = TurnDirection.Up;
                    }
                }
                else
                {

                    if (Facing.Pitch > targetPitch)
                    {
                        pitchDirectionToTurn = TurnDirection.Up;
                    }
                    else
                    {
                        pitchDirectionToTurn = TurnDirection.Down;
                    }
                    ;
                }

            }

            combineDirection = yawDirectionToTurn + pitchDirectionToTurn;
            TurnDirection originaTurnDirection = new TurnDirection(0, 0) + combineDirection;


            TurnsMade = 0;
            while (true)
            {
                if (CanTurn == false)
                {
                    MoveCharacter(TurnMode, accelerationDirection );
                }

                Turn(combineDirection);
                TurnsMade++;
                if (Facing.Yaw == targetYaw)
                {
                    combineDirection = combineDirection - yawDirectionToTurn;
                    if (Facing.Pitch == targetPitch || targetPitch == 0)
                    {
                        break;
                    }

                }
                if (Facing.Pitch == targetPitch)
                {
                    combineDirection = combineDirection - pitchDirectionToTurn;
                    if (Facing.Yaw == targetYaw || targetYaw == 0)
                    {
                        break;
                    }
                }


            }
            return originaTurnDirection;
        }

        private void UpdateFacing(TurnDirection turnDirection)
        {
            Facing.Yaw += turnDirection.Yaw;
            if (Facing.Yaw < YawHexFacing.North)
            {
                Facing.Yaw = YawHexFacing.NorthEast;
            }
            if (Facing.Yaw > YawHexFacing.NorthEast)
            {
                Facing.Yaw = YawHexFacing.North;
            }
            Facing.Pitch += turnDirection.Pitch;
            if (Facing.Pitch < PitchHexFacing.Level)
            {
                Facing.Pitch = PitchHexFacing.DiagonalDown;
            }
            if (Facing.Pitch > PitchHexFacing.DiagonalDown)
            {
                Facing.Pitch = PitchHexFacing.Level;
            }
        }

        public HexFacing Facing { get; internal set; }
        public int TurnsMade { get; set; }

        public bool Descending
            =>
                Facing.Pitch == PitchHexFacing.BackDiagonalDown || Facing.Pitch == PitchHexFacing.StraightDown ||
                Facing.Pitch == PitchHexFacing.DiagonalDown;

        public bool Ascending
            =>
                Facing.Pitch == PitchHexFacing.StraightUp || Facing.Pitch == PitchHexFacing.DiagonalUp ||
                Facing.Pitch == PitchHexFacing.BackDiagonalUp;

        public int NonCombatModifer { get; set; }

        #endregion
    }

}
