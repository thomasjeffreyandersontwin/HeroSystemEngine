using System;
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
    public delegate void MovementCompletionHandler();
    public enum MovementDistance { HalfDistance, FullDistance}
    public enum VelocityChange { Accelerate, Decelerate,
        Coasting
    }
    public class Movement
    {
        private HeroSystemCharacter Character;
        public string Name;
        public int Inches;
        private bool TurningModeRequired;
        public int InchesMoving;
        private bool _isActive;
        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                _isActive = value;
                MovementInchAllowance = InchesMoving;
                Character.CharacterMovement.ActiveMovement = this;
            }
        }

        public MovementCompletionHandler Completed;
        public int Velocity;

        public int KPH => Character.SPD.CurrentValue* Inches * 5 * 60 / 500;
        public int MPH => (int) (Character.SPD.CurrentValue * Inches * 5 * 60 / 804.5);

        public Movement(HeroSystemCharacter character, string name, int inches, bool turningModeRequired)
        {
            this.Character = character;
            this.Name = name;
            this.Inches = inches;
            this.TurningModeRequired = turningModeRequired;

            MovementManuever manuever = new MovementManuever(this.Character,this);  
        }

        public void MoveCharacter(MovementDirection direction, int inchesMoving, VelocityChange velocityChange =  VelocityChange.Accelerate, int maxVelocityChangeForThisMove = 0)
        {
            if (IsActive)
            {
                if (MovementInchAllowance < inchesMoving)
                {
                    inchesMoving = MovementInchAllowance;
                }

                if (velocityChange != VelocityChange.Coasting)
                {
                    AdjustVelocityWhileMoving(inchesMoving, velocityChange, maxVelocityChangeForThisMove);
                }
                else
                {
                    Coast(inchesMoving);
                }
                MovementInchAllowance -= inchesMoving;

                if (MovementInchAllowance == 0)
                {
                    IsActive = false;
                    Completed?.Invoke();

                }
                 
            }

        }

        public void Coast(int inchesMoving)
        {
            for (int inchMoving = 1; inchMoving <= inchesMoving; inchMoving++)
            {
                MapFactory.ActiveGameMap.MoveGameObject(MovementDirection.Forward, 1, Character, Velocity);
            }
        }

        private void AdjustVelocityWhileMoving(int inchesMoving, VelocityChange accelerate,
            int maxVelocityChangeForThisMove)
        {
            int startingVelocity = Velocity;
            int velocityChangedThisMove = 0;
            int velocityChangeEachInchOfMovement = 0;
            if (accelerate == VelocityChange.Accelerate)
            {
                velocityChangeEachInchOfMovement = 5;
            }
            else
            {
                velocityChangeEachInchOfMovement = -5;
            }
            for (int inchMoving = 1; inchMoving <= inchesMoving; inchMoving++)
            {
                if (maxVelocityChangeForThisMove == 0 || velocityChangedThisMove < maxVelocityChangeForThisMove)
                {
                    Velocity = Velocity + velocityChangeEachInchOfMovement;
                    velocityChangedThisMove += velocityChangeEachInchOfMovement;
                }
                MapFactory.ActiveGameMap.MoveGameObject(MovementDirection.Forward, 1, Character, Velocity);
            }
        }

        public void Activate(int inchesMoving)
        {
            InchesMoving = inchesMoving;
            IsActive = true;
            
        }

        public int MovementInchAllowance { get; set; }
    }

    public enum MovementDirection
    {
        Forward,
        Backward,
        StrafeLeft,
        StrafeRight,
        Up,
        Down
    }

    public class CharacterMovement
    {
        public CharacterMovement(HeroSystemCharacter character)
        {
            Run = new Movement(character, "Run", 6, false);
            Swim = new Movement(character, "Swim", 2, false);
            Leap = new Movement(character, "Leap", 2, false);
        }
        public Movement Run;
        public Movement Swim;
        public Movement Leap;
        public List<Movement> Movements = new List<Movement>();
        public Movement ActiveMovement;

        public int Velocity
        {
            get
            {
                return ActiveMovement.Velocity;
            }
            set
            {
                ActiveMovement.Velocity = value;
                
            }
            
        }
    }

    class MovementManuever : Manuever
    {
        public MovementManuever(HeroSystemCharacter character, Movement movement)
            : base(ManueverType.MovementManuever, movement.Name, character,false)
        {
            Movement = movement;
        }
        private Movement Movement;

        public override bool canPerform()
        {
            return InchesMoving <= Movement.Inches|| Distance!=null;
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
                if (Distance == MovementDistance.FullDistance)
                {
                    PhaseActionTakes = PhaseLength.Full;
                }
                else
                {
                    PhaseActionTakes = PhaseLength.Half;
                }

            }
        }

        private int _inchesMoving = 0;
            
        public int InchesMoving
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
                inchesMoving = InchesMoving;
            }
            
            Movement.Activate(inchesMoving);
            Movement.Completed += CompleteManuever;

        }
        public void ActivateMovement()
        {
            
        }
        public override void CompleteManuever()
        {
            base.CompleteManuever();

            Movement.Completed -= CompleteManuever;
        }

    }


}
