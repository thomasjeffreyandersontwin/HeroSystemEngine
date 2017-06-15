using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HeroSystemEngine.Character;
using HeroSystemEngine.Manuevers;
using HeroSystemEngine.Dice;
using HeroSystemsEngine.CombatSequence;
using HeroSystemsEngine.GameMap;
using HeroSystemsEngine.Focus;
using HeroSystemsEngine.Movement;
using HeroSystemsEngine.Perception;

namespace HeroSystemEngine.Character
{
    public class Charasteristic
    {
        private int _maxValue;

        public virtual bool Roll(int modifier=0)
        {
            DicePool rollDicePool = new DicePool(3);
            int roll = rollDicePool.Roll();
            var required = RollRequired(modifier);
            if (roll <= required)
            {
                return true;
            }
            else
            {
                return false;

            }


        }

        public virtual int RollRequired(int modifier=0)
        {
            int required = 9 + (CurrentValue / 5) + modifier;
            return required;
        }

        public virtual int MaxValue
        {
            get { return _maxValue; }
            set
            {
                _maxValue = value;
                //if  MaxValue < CurrentValue)
                //{
                CurrentValue = MaxValue;
                //}
            }
        }

        public int Modifier { get; set; }
        public double Multiplier { get; set; }
        private int _currentValue=0;
        /// <summary>
        /// ath
        /// </summary>
        
        public virtual int CurrentValue
        {
            get
            {

                double ret =  _currentValue + Modifier;
                if (Multiplier != 0)
                {
                    ret = ret * Multiplier;

                }
                return (int) Math.Ceiling(ret);


            }
            set { _currentValue = value; }
        }

        public string Name;
        public HeroSystemCharacter Character;
        public Charasteristic(string name, HeroSystemCharacter character)
        {
            Name = name;
            Character = character;
            character.Characteristics[Name] = this;
        }

        public void Deduct(int amount)
        {
            CurrentValue -= amount;
        }

        public virtual bool RollAgainst(HeroSystemCharacter versusCharacter)
        {
            return RollAgainst((CurrentValue / 5) ,versusCharacter.Characteristics[this.Name].CurrentValue / 5); 
        }

        public virtual bool RollAgainst(int number1,int number2)
        {
            int success = 11 + number1 - (number2);

            DicePool rollDicePool = new DicePool(3);

            int roll = rollDicePool.Roll();

            if (roll <= success)
            {
                return true;
            }
            else return false;
        }
    }

    public class SPD : Charasteristic
    {
        public SPD(HeroSystemCharacter character) : base("SPD", character)
        {
        }

        public  override int CurrentValue
        {
            get { return base.CurrentValue; }
            set
            {
                base.CurrentValue = value;
                setCharacterPhaseNumbers();
            }
        }

        public void setCharacterPhaseNumbers()
        {
            Phases = new Dictionary<int, Phase>();
            switch (CurrentValue)
            {
                case 1:
                    Phases.Add(12,null);
                    break;
                case 2:
                    Phases.Add(6, null);
                    Phases.Add(12, null);
                    break;
                case 3:
                    Phases.Add(4, null);
                    Phases.Add(8, null);
                    Phases.Add(12, null);
                    break;
                case 4:
                    Phases.Add(3, null);
                    Phases.Add(6, null);
                    Phases.Add(9, null);
                    Phases.Add(12, null);
                    break;
                case 5:
                    Phases.Add(3, null);
                    Phases.Add(5, null);
                    Phases.Add(8, null);
                    Phases.Add(10, null);
                    Phases.Add(12, null);
                    break;
                case 6:
                    Phases.Add(2, null);
                    Phases.Add(4, null);
                    Phases.Add(6, null);
                    Phases.Add(8, null);
                    Phases.Add(10, null);
                    Phases.Add(12, null);
                    break;
                case 7:
                    Phases.Add(2, null);
                    Phases.Add(4, null);
                    Phases.Add(6, null);
                    Phases.Add(7, null);
                    Phases.Add(9, null);
                    Phases.Add(11, null);
                    Phases.Add(12, null);
                    break;
                case 8:
                    Phases.Add(2, null);
                    Phases.Add(3, null);
                    Phases.Add(5, null);
                    Phases.Add(6, null);
                    Phases.Add(8, null);
                    Phases.Add(9, null);
                    Phases.Add(11, null);
                    Phases.Add(12, null);
                    break;
                case 9:
                    Phases.Add(2, null);
                    Phases.Add(3, null);
                    Phases.Add(4, null);
                    Phases.Add(6, null);
                    Phases.Add(7, null);
                    Phases.Add(8, null);
                    Phases.Add(9, null);
                    Phases.Add(10, null);
                    Phases.Add(11, null);
                    Phases.Add(12, null);
                    break;
                case 10:
                    Phases.Add(2, null);
                    Phases.Add(3, null);
                    Phases.Add(4, null);
                    Phases.Add(5, null);
                    Phases.Add(6, null);
                    Phases.Add(8, null);
                    Phases.Add(9, null);
                    Phases.Add(10, null);
                    Phases.Add(11, null);
                    Phases.Add(12, null);
                    break;
                case 11:
                    Phases.Add(2, null);
                    Phases.Add(3, null);
                    Phases.Add(4, null);
                    Phases.Add(5, null);
                    Phases.Add(6, null);
                    Phases.Add(7, null);
                    Phases.Add(8, null);
                    Phases.Add(9, null);
                    Phases.Add(10, null);
                    Phases.Add(11, null);
                    Phases.Add(12, null);
                    break;
                case 12:
                    Phases.Add(1, null);
                    Phases.Add(2, null);
                    Phases.Add(3, null);
                    Phases.Add(4, null);
                    Phases.Add(5, null);
                    Phases.Add(6, null);
                    Phases.Add(7, null);
                    Phases.Add(8, null);
                    Phases.Add(9, null);
                    Phases.Add(10, null);
                    Phases.Add(11, null);
                    Phases.Add(12, null);
                    break;
            }

        }

        public  Dictionary<int, Phase> Phases = new Dictionary<int, Phase>();

        public List<int> SegmentNumbersCharacterHasPhases
        {
            get
            {
                List<int> i= Phases.Keys.ToList();
                i.Sort();
                return i;
            }
        }
       }
    public class STR : Charasteristic
    {
        public int STRDamage => CurrentValue / 5;
        public STR(HeroSystemCharacter character) : base("STR",character)
        {

        }

        public override bool RollAgainst(HeroSystemCharacter versusCharacter)
        {
            return RollAgainst(this.STRDamage,versusCharacter.STR.STRDamage);

        }
        public override bool RollAgainst(int number1, int number2)
        {
            DamageDicePool characterStrDicePool = new NormalDamageDicePool(number1);
            characterStrDicePool.Roll();
            int characterResult = characterStrDicePool.DamageResult.BOD;

            DamageDicePool vsCharacterStrDicePool = new NormalDamageDicePool(number2);
            vsCharacterStrDicePool.Roll();
            int vsCharacterResult = vsCharacterStrDicePool.DamageResult.BOD;

            if (characterResult > vsCharacterResult)
            {
                return true;
            }
            else
            {
                return false;
            }

        }
        internal void RollAgainst(STR sTR, double v)
        {
            throw new NotImplementedException();
        }

        public bool RollCasualStrengthAgainst(HeroSystemCharacter versusCharacter)
        {
            return RollAgainst(this.STRDamage/2, versusCharacter.STR.STRDamage);
        }
        public bool RollCasualStrengthAgainst(int difficulty)
        {
            return RollAgainst(this.STRDamage / 2, difficulty);
        }
    }
    public class PER : Charasteristic
    {
        public PER(HeroSystemCharacter character) : base("PER", character)
        {
        }

        public override int MaxValue
            {
                get { return Character.INT.MaxValue; }
                set { }
            }

        public override int CurrentValue
        {
            get { return Character.INT.CurrentValue; }
            set { }
        }

        public override bool Roll(int modifier = 0)
        {
            return base.Roll(Modifier+ modifier);
        }
    }
    
    
    //public enum CharacterStateType { Stunned = AnimatableCharacterStateType.Stunned, Unconsious = AnimatableCharacterStateType.Unconsious,
    //Dead = AnimatableCharacterStateType.Dead, Dying = AnimatableCharacterStateType.Dying, KnockBacked = AnimatableCharacterStateType.KnockBacked, KnockedDown = AnimatableCharacterStateType.KnockedDown, Attacking = AnimatableCharacterStateType.Attacking, Prone =8 };

    public enum CharacterStateType
    {
        Stunned,
        Unconsious,
        Dead,
        Dying,
        KnockBacked,
        KnockedDown,
        Prone
    };

    public class HeroCharacterState
    {
        public HeroSystemCharacter Character;
        public CharacterStateType Type;
        public HeroCharacterState(HeroSystemCharacter character, CharacterStateType stateType)
        {
            Type = stateType;
            Character = character;
        }

        public void RemoveFromCharacter()
        {
            Character.RemoveState(this.Type);
            Character = null;
        }
    }

    public class HeroSystemCharacterRepository
    {
        //public HeroTableTopCharacterRepository TableTopCharacterRepository;
        public Dictionary<string, HeroSystemCharacter> Characters = new Dictionary<string, HeroSystemCharacter>();

        private static HeroSystemCharacterRepository _instance;

        public static HeroSystemCharacterRepository GetInstance()
        {
            if (_instance == null)
            {
                //_instance = new HeroSystemCharacterRepository(tableTopCharacterRepository);
                _instance = new HeroSystemCharacterRepository();
            }
            return _instance;
        }

        public void DeleteCharacter(HeroSystemCharacter character)
        {
            if (Characters.ContainsKey(character.Name))
            {
                Characters.Remove(character.Name);
            }
        }

        public void AddCharacter(HeroSystemCharacter character)
        {
            if (Characters.ContainsKey(character.Name))
            {
                Characters.Remove(character.Name);
            }
            Characters[character.Name] = character;

        }

        // public HeroSystemCharacterRepository(HeroTableTopCharacterRepository tableTopCharacterRepository)
        public HeroSystemCharacterRepository()
        {
            //TableTopCharacterRepository = tableTopCharacterRepository;
            AddCharacter(LoadBaseCharacter());

        }

        public HeroSystemCharacter LoadBaseCharacter(string name)
        {
            HeroSystemCharacter character = LoadBaseCharacter();
            character.Name = name;
            return character;

        }

        public HeroSystemCharacter LoadBaseCharacter()
        {

            HeroSystemCharacter baseChar = new HeroSystemCharacter("Default Character");
            // HeroSystemCharacter baseChar = new HeroSystemCharacter("Default Character", TableTopCharacterRepository);
            baseChar.STR.MaxValue = 10;
            baseChar.CON.MaxValue = 10;
            baseChar.DEX.MaxValue = 10;
            baseChar.BOD.MaxValue = 10;
            baseChar.PRE.MaxValue = 10;
            baseChar.INT.MaxValue = 10;
            baseChar.EGO.MaxValue = 10;
            baseChar.COM.MaxValue = 10;

            baseChar.PD.MaxValue = 2;
            baseChar.ED.MaxValue = 2;
            baseChar.SPD.MaxValue = 2;
            baseChar.STUN.MaxValue = 20;
            baseChar.END.MaxValue = 20;

            baseChar.DCV.MaxValue = 3;
            baseChar.OCV.MaxValue = 3;

            baseChar.RPD.MaxValue = 10;
            baseChar.RED.MaxValue = 10;

            Attack strike = new Strike(baseChar);


            return baseChar;
        }
    }

    public class HeroSystemCharacter : ITargetable
    {
        public string Name { get; set; }
        #region initialization / constructor
        public HeroSystemCharacter()
        {
            initalizeCharasteristics();
            initializeManuevers();
            CharacterSenses = new CharacterSenses(this);
            CharacterMovement = new CharacterMovement(this);

            Size = new Size();
            Size.MultiplierOfHexSize = 1;

            SegmentNumberThatLastPhaseActivatedOn = 0;

            PerceptionModifiers =new Dictionary<SenseGroupType, double>();
            PerceptionMultipliers = new Dictionary<SenseGroupType, double>();

            Hex = new GameHex(1, 1, 1);

        }

        private void initializeManuevers()
        {
            new HoldActionManuever(this);
            new Strike(this);
            new AbortManuever(this);
            new Recover(this);
            new Dodge(this);
            new Block(this);
            new Disarm(this);
            new Brace(this);
            new Grab(this);
            new Throw(this);
            Squeeze squeeze = new Squeeze(this);
            squeeze.Grab = Manuevers["Grab"] as Grab;
            new PullFocusAwayFromEnemy(this);
            new WeaponManuever(this);
            new GrabBy(this);
            new MoveBy(this);
            new Set(this);
            new BlazingAway(this);
            new Cover(this);
            new DiveForCover(this);
            new HipShot(this);
            new Hurry(this);
            new PullingAPunch(this);
            new RapidFire(this);
             new RollWithAPunch(this);
             new SnapShot(this);
             new SuppressionFire(this);
            new Sweep(this);
            new Haymaker(this);
            new Club(this);
             new MoveThrough(this);

        }
        private void initalizeCharasteristics()
        {
            STR = new STR(this);
            DEX = new Charasteristic("DEX", this);
            CON = new Charasteristic("CON", this);
            BOD = new Charasteristic("BOD", this);
            INT = new Charasteristic("INT", this);
            EGO = new Charasteristic("EGO", this);
            PRE = new Charasteristic("PRE", this);
            COM = new Charasteristic("COM", this);
            SPD = new SPD(this);
            PD = new Charasteristic("PD", this);
            ED = new Charasteristic("ED", this);
            REC = new Charasteristic("REC", this);
            END = new Charasteristic("END", this);
            STUN = new Charasteristic("STUN", this);

            RPD = new Charasteristic("RPD", this);
            RED = new Charasteristic("RED", this);
            DCV = new Charasteristic("DCV", this);
            OCV = new Charasteristic("OCV", this);
            ECV = new Charasteristic("ECV", this);
            PER = new PER(this);
        }
        private void initializeMovements()
        {
            CharacterMovement = new CharacterMovement(this);
            
        }


        public HeroSystemCharacter(string name) :this()
        {
            //TableTopCharacterRepository = tableTopCharacterRepository;
            Name = name;
        }
        #endregion

        #region Physical Attributes
        public Size Size { get; set; }
        public int TimesHumanSize { get; set; }
        public int Limbs { get; set; }
        #endregion

        #region Charasteristics
        public Dictionary<String, Charasteristic> Characteristics = new Dictionary<string, Charasteristic>();
        public STR STR;
        public Charasteristic CON ;  
        public Charasteristic INT ;
        public Charasteristic EGO ;
        public Charasteristic PRE ;
        public Charasteristic COM ;
        public Charasteristic REC ;
        
        



        #endregion

        #region Direct Modifiers
        public int GlobalModifier { get; set; }
        public int DamageClassModifier { get; set; }
        public double DamageMultiplier { get; set; }
        public double RangedModModifier { get; set; }
        public double RangedModifierMultiplier { get; set; }
        public Dictionary<SenseGroupType, double> PerceptionModifiers { get; set; }
        public Dictionary<SenseGroupType, double> PerceptionMultipliers { get; set; }
        #endregion

        #region Performs Manuevers
        public Charasteristic DCV;
        public Charasteristic OCV;
        public Charasteristic ECV;
        public Charasteristic END;

        public AttackResult Attack(String manueverName, HeroSystemCharacter defender)
        {
            Attack attack = (Attack)Manuevers[manueverName];
            AttackResult result = attack.PerformAttack(defender);
            return result;

        }
        public void Perform(string manueverName)
        {
            Manuevers[manueverName].Perform();
        }

        public Dictionary<String, IManuever> Manuevers = new Dictionary<String, IManuever>();
        public void AddManuever(Manuever manuever)
        {
            RemoveManuever(manuever);
            Manuevers.Add(manuever.Name, manuever);
            /**
            if (TableTopCharacter != null)
            {
                AnimatedAbility ability = TableTopCharacter.Abilities[manuever.Name];

                if (ability == null)
                {
                    TableTopCharacterRepository.NewAbility(TableTopCharacter);
                }
            }
            **/

        }
        public void RemoveManuever(Manuever manuever)
        {
            if (Manuevers.ContainsKey(manuever.Name) == true)
            {
                Manuevers.Remove(manuever.Name);
                /**
                AnimatedAbility ability = TableTopCharacter.Abilities[manuever.Name];
                if (ability != null)
                {
                    TableTopCharacter.RemoveAnimatedAbility(ability);
                }
                */
            }
        }
        public Dictionary<String, IManuever> AllowedManuevers
        {
            get
            { 
                return Manuevers.Values.Where(x => x.CanPerform == true).ToDictionary(x => x.Name);

                
            }
    }
       
        private void InterruptManueverInProgess()
        {
            ManueverInProgess?.Deactivate();
            ManueverInProgess = null;
        }
        public Manuever ActiveManuever { get; set; }
        public Manuever ManueverInProgess { get; set; }

        public bool IsPerformingSet { get; set; }
        public bool IsBlocking => ActiveManuever?.GetType() == typeof(Block);
        public bool IsRolling => ActiveManuever?.GetType() == typeof(RollWithAPunch);
        public bool IsAborting { get; set; }
        public bool IsHaymakering { get; set; }

        public void DuckBehindCoverHidingFrom(ProtectingCover cover, HeroSystemCharacter other)
        {
            cover.UpdateAmountOfConcealmentBeingProvidedToCharacterUnderCoverFromOtherCharacter(ConcealmentAmount.Full, this, other);
        }

        #endregion

        #region Participates in CombatSequence
        public Charasteristic DEX;
        public SPD SPD;

        public bool IsHolding => _heldManuever != null;
        private HoldActionManuever _heldManuever;
        public HoldActionManuever HeldManuever
        {
            get { return _heldManuever; }
            set
            {
                CombatSequence.HeldManuevers.Remove(_heldManuever);
                _heldManuever = value;
                if (value != null)
                {
                    CombatSequence.HeldManuevers.Add(value);
                }

            }
        }

        private Cover _coveringManuever;
        public Cover CoveringManuever
        {
            get { return _coveringManuever; }
            set
            {
                CombatSequence.CoveringManuevers.Remove(_coveringManuever);
                _coveringManuever = value;
                if (value != null)
                {
                    CombatSequence.CoveringManuevers.Add(value);
                }
            }
        }

        public Phase ActivePhase { get; set; }
        public Phase NextPhase
        {
            get
            {
                int lastSegmentNumber = 0;


                if (SegmentNumberThatLastPhaseActivatedOn != 0)
                {
                    lastSegmentNumber = SegmentNumberThatLastPhaseActivatedOn;
                }
                else
                {
                    lastSegmentNumber = 12;
                }
                int index = SPD.SegmentNumbersCharacterHasPhases.IndexOf(lastSegmentNumber) + 1;

                if (index == SPD.Phases.Count)
                {
                    index = 1;
                }
                int nextSegmentNumber = SPD.SegmentNumbersCharacterHasPhases[index];




                Phase phase = CombatSequence.Segments[nextSegmentNumber].CombatPhases.FirstOrDefault
                    (i => i.Character == this);
                return phase;

            }
        }
        public int SegmentNumberThatLastPhaseActivatedOn { get; set; }
        public CombatSequence CombatSequence { get; set; }
        public void CompleteActivePhase()
        {
            ActivePhase.Complete();
        }
        #endregion 

        #region Sense Perception
        public PER PER { get; set; }
        public CharacterSenses CharacterSenses { get; private set; }
        public void PeekAroundCoverToViewDefender(ProtectingCover cover, HeroSystemCharacter other)
        {
            cover.UpdateAmountOfConcealmentBeingProvidedToCharacterUnderCoverFromOtherCharacter(ConcealmentAmount.Partial, this, other);
        }
        #endregion

        #region Takes Damage
        public Charasteristic BOD;
        public Charasteristic STUN;
        public Charasteristic PD;
        public Charasteristic ED;
        public Charasteristic RPD;
        public Charasteristic RED;

        public Dictionary<CharacterStateType, HeroCharacterState> TakeDamage(DamageAmount damageAmount)
        {
            DamageAmount damageDone = DeductDefenseFromDamage(damageAmount);
            if (IsRolling == true)
            {
                damageDone.STUN /=2;
                damageDone.BOD /= 2;
                ActiveManuever = null;

            }

            STUN.Deduct(damageDone.STUN);
            BOD.Deduct(damageDone.BOD);

            var statesResultingFromDamage = applyDamageStateBasedOnDamageAmount(damageDone);
            return statesResultingFromDamage;
        }
        private Dictionary<CharacterStateType, HeroCharacterState> applyDamageStateBasedOnDamageAmount(DamageAmount damageDone)
        {
            Dictionary<CharacterStateType, HeroCharacterState> statesResultingFromDamage =
                new Dictionary<CharacterStateType, HeroCharacterState>();
            HeroCharacterState stateFromDamage = null;
            if (damageDone.STUN > CON.CurrentValue)
            {
                stateFromDamage = AddState(CharacterStateType.Stunned, false);
                statesResultingFromDamage.Add(stateFromDamage.Type, stateFromDamage);
                InterruptManueverInProgess();
            }
            if (STUN.CurrentValue <= 0)
            {
                stateFromDamage = AddState(CharacterStateType.Unconsious, false);
                statesResultingFromDamage.Add(stateFromDamage.Type, stateFromDamage);
                InterruptManueverInProgess();
            }
            if (BOD.CurrentValue <= 0 && BOD.CurrentValue > (BOD.MaxValue * -1))
            {
                stateFromDamage = AddState(CharacterStateType.Dying, false);
                statesResultingFromDamage.Add(stateFromDamage.Type, stateFromDamage);
            }
            else
            {
                if (BOD.CurrentValue < 0 && BOD.CurrentValue < (BOD.MaxValue * -1))
                {
                    RemoveState(CharacterStateType.Dying);
                    stateFromDamage = AddState(CharacterStateType.Dead, false);
                    ;
                    statesResultingFromDamage.Add(stateFromDamage.Type, stateFromDamage);
                }
            }
            return statesResultingFromDamage;
        }
        public DamageAmount DeductDefenseFromDamage(DamageAmount damageAmount)
        {
//to do: put state logic into body and stun charasteristics
            Charasteristic defense = determineDefense(damageAmount);
            DamageAmount damageDone = damageAmount.Clone();
            damageDone.STUN -= defense.CurrentValue;
            damageDone.BOD -= defense.CurrentValue;
            return damageDone;
        }
        private Charasteristic determineDefense(DamageAmount damage)
        {
            switch (damage.WorksAgainstDefense)
            {
                case DefenseType.PD:
                    return PD;
                case DefenseType.ED:
                    return ED;
                case DefenseType.RED:
                    return RED;
                case DefenseType.RPD:
                    return RPD;
            }
            return null;

        }
        public Dictionary<CharacterStateType, HeroCharacterState> State =
            new Dictionary<CharacterStateType, HeroCharacterState>();
        public HeroCharacterState AddState(CharacterStateType stateKey, bool renderImmediately = true)
        {
            HeroCharacterState state;
            if (State.ContainsKey(stateKey) == false)
            {
                state = new HeroCharacterState(this, stateKey);
                State.Add(stateKey, state);
                /**
                if (TableTopCharacter != null)
                {
                    TableTopCharacter.AddState((AnimatableCharacterStateType)stateKey, renderImmediately);
                }
                **/
            }
            else
            {
                state = State[stateKey];
            }
            return state;
        }
        public void RemoveState(CharacterStateType stateKey)
        {
            if (State.ContainsKey(stateKey) == true)
            {
                State.Remove(stateKey);
                /**
                if (TableTopCharacter != null)
                {
                    TableTopCharacter.RemoveState((AnimatableCharacterStateType)stateKey);
                }
                **/

            }
        }

        public HeroSystemCharacter GrabbedBy { get; set; }
        public bool IsGrabbed { get { return GrabbedBy != null; } }
        #endregion

        #region Movement
        public CharacterMovement CharacterMovement { get; internal set; }
        public GameHex Hex { get; set; }
        #endregion

        public List<Focus> HeldFoci = new List<Focus>();
        public void HoldFocus(Focus focus)
        {
            HeldFoci.Add(focus);
            focus.Holder = this;
        }

        /**public HeroTableTopCharacter _tableTopCharacter;
        public HeroTableTopCharacter TableTopCharacter
        {
            get
            {
                if (_tableTopCharacter == null)
                {
                    if (TableTopCharacterRepository != null)
                    {
                        _tableTopCharacter = TableTopCharacterRepository.ReturnCharacter(Name);
                    }
                }
                if (_tableTopCharacter == null)
                {
                    if (TableTopCharacterRepository != null)
                    {
                      _tableTopCharacter = TableTopCharacterRepository.NewCharacter();
                        if (_tableTopCharacter != null) {
                            _tableTopCharacter.Name = Name;
                        }
                    }
                }
                return _tableTopCharacter;
            }
            set
            {
                _tableTopCharacter = value;
            }
            
        }
   

        public HeroTableTopCharacterRepository TableTopCharacterRepository;
           public HeroSystemCharacter(string name, HeroTableTopCharacterRepository tableTopCharacterRepository)
         **/



    }


    }

