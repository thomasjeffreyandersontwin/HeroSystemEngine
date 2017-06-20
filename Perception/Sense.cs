using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HeroSystemEngine.Character;
using HeroSystemEngine.Dice;
using HeroSystemEngine.Manuevers;
using HeroSystemsEngine.GameMap;

namespace HeroSystemsEngine.Perception
{
    #region Powers
    public enum SensingPower
    {
        PrecognitiveClairsentience,
        RetrocognitiveClairsentience,
        Detect,
        NRayPerception,
        SpatialAwareness,


    }
    public class SenseAffectingPower : Attack
    {
        public SenseAffectingPower(HeroSystemCharacter attacker, int damageDiceNumber, bool ranged)
            : base("Blinding Power", attacker, DamageType.SenseAffecting, damageDiceNumber, DefenseType.FD, ranged)
        {
            AffectsPower = SensingPower.NRayPerception;
        }

        public SenseGroupType? AffectsGroup { get; set; }
        public SensingPower? AffectsPower { get; set; }

        public override void PerformManuever()
        {
            AffectSense(Defender);
        }

        public void AffectSense(HeroSystemCharacter defender)
        {

            if (AffectsGroup != null)
            {
                SenseGroup affected = defender.CharacterSenses.SenseGroupList.Where(x => x.Type == AffectsGroup).ToList().FirstOrDefault();
                affected.IsDisabled=true;
            }
            else if (AffectsPower != null)
            {
                List<Sense> powerSenses = defender.CharacterSenses.Senses.Where(x => x.Power == AffectsPower).ToList();
                powerSenses.ForEach((Sense sense) => { sense.IsDisabled = true; });
            }
        }

    }
    #endregion

    #region Modifiers
    public enum SightPerceptionModifiers
    {
        ExtremelyHighContrast = 5,
        HighContrast = 1,
        LowContrast = -1,
        Night = -2,
        DarkNight = -4,
        MovingObject = 1,
        Binoculars = +2,
        Telescope = +3,
        HalfPhaseLooking = +1,
        FullPhaseLooking = +2
    }
    public class PerceptionModier : Modifier
    {
        public CharasteristicModifier OCV = new CharasteristicModifier("OCV");
        public CharasteristicModifier DCV = new CharasteristicModifier("DCV");
        public override void ApplyModifierTo(HeroSystemCharacter character)
        {
            OCV.ApplyModifierTo(character);
            DCV.ApplyModifierTo(character);
        }

        public override void RemoveModifierFrom(HeroSystemCharacter character)
        {
            OCV.RemoveModifierFrom(character);
            DCV.RemoveModifierFrom(character);
        }
    }
    public enum SizeDirectionThanHex { Larger, Smaller, Normal }
    public class Size
    {
        public SizeDirectionThanHex LargerOrSmallerThanHex;

        public double PerceptionModifer
        {
            get
            {
                if (LargerOrSmallerThanHex == SizeDirectionThanHex.Larger)
                {
                    return Math.Log(MultiplierOfHexSize, 2) * 2 + 2;
                }
                else if (LargerOrSmallerThanHex == SizeDirectionThanHex.Smaller)
                {
                    return -1 * (Math.Log(MultiplierOfHexSize, 2) * 2);
                }
                return 0;
            }

        }

        public int MultiplierOfHexSize;


    }
    #endregion

    #region core sense
    public enum LocationPrecision { ExactLocation, GeneralLocation, CantPercieveLocation }
    public enum DiscriminatoryLevel { FullyDiscriminate, PartlyDiscriminate, NonDiscriminate }
    public enum ArcOfPerception { OneEighty, ThreeSixty }
    public enum SenseGroupType { Hearing, Mental, Radio, Sight, SmellTaste, Touch, Unusual }

    public class CharacterSenses
    {
        
        public CharacterSenses(HeroSystemCharacter character)
        {
            Character = character;
            intitializeSenses();
        }


        #region locate and percieve target using senses
        public LocationPrecision DetermineLocationOfTarget(ITargetable target)
        {
            Sense effectiveSense = SenseThatSuccessfullyDeterminesLocationOfTarget;
            if (effectiveSense != null)
            {
                return effectiveSense.DetermineLocationOfTarget();
            }
            else
            {
                return LocationPrecision.CantPercieveLocation;


            }

        }
        public bool Perceive()
        {
            return SenseThatSucessfullyPercievesTarget != null;
        }

        private ITargetable _target;
        public ITargetable Target
        {
            get { return _target;}
            set
            {
                _target = value;
                if (_target != null)
                {
                    ModifyCombatValuesBasedOnAbilityToSeeTarget();
                }   

            }
        }
        public bool CanDetermineLocationOfTarget()
        {
            if (DetermineLocationOfTarget(Target) == LocationPrecision.CantPercieveLocation)
            {
                return false;
            }
            return true;
        }

        public void ModifyCombatValuesBasedOnAbilityToSeeTarget()
        {
            if (PerceptionModier == null)
            {
                PerceptionModier = new PerceptionModier();

            }
            else
            {
                PerceptionModier.RemoveModifierFrom(Character);
            }

            Attack attack = Character.ActiveManuever as Attack;

            LocationPrecision precision = DetermineLocationOfTarget(attack?.Defender);
            
            switch (precision)
            {
                case LocationPrecision.CantPercieveLocation:

                    if (attack == null || attack.Ranged)
                    {
                        PerceptionModier.OCV.Multiplier = 0;
                        PerceptionModier.OCV.ModiferAmount = -Character.OCV.CurrentValue;
                        PerceptionModier.DCV.Multiplier = .5;
                    }
                    else
                    {
                        PerceptionModier.OCV.Multiplier = .5;
                        PerceptionModier.DCV.Multiplier = .5;
                        PerceptionModier.OCV.ModiferAmount = 0;
                        PerceptionModier.DCV.ModiferAmount = 0;
                    }
                    break;
                case LocationPrecision.GeneralLocation:
                    if (attack == null  || attack.Ranged )
                    {
                        PerceptionModier.OCV.Multiplier = 0.5;

                        PerceptionModier.OCV.ModiferAmount = 0;
                        PerceptionModier.DCV.Multiplier = 0;
                        PerceptionModier.DCV.ModiferAmount = 0;
                    }
                    else
                    {
                        PerceptionModier.OCV.Multiplier = 0.5;
                        PerceptionModier.OCV.ModiferAmount = 0;
                        PerceptionModier.DCV.Multiplier = 0;
                        PerceptionModier.DCV.ModiferAmount = -1;
                    }
                    break;

                case LocationPrecision.ExactLocation:
                    PerceptionModier.OCV.Multiplier = 0;
                    PerceptionModier.OCV.ModiferAmount = 0;
                    PerceptionModier.DCV.Multiplier = 0;
                    PerceptionModier.DCV.ModiferAmount = 0;

                    break;
            }
            PerceptionModier.ApplyModifierTo(Character);
        }
        public int RangeModifierToTarget
        {
            get
            {
                int rangeModifer = 0;

                float distance;
                if (Target != null)
                {
                     distance = Character.Hex.DistanceFrom(Target?.Hex);
                }
                else
                {
                    distance = 0;
                }
                rangeModifer = Convert.ToInt32((Math.Round(distance - 3) / 3));
                rangeModifer = rangeModifer - (int)Character.RangedModModifier;
                if (Character.RangedModifierMultiplier > 0)
                {
                    rangeModifer = (int)(rangeModifer * Character.RangedModifierMultiplier);
                }

                if (rangeModifer < 0) rangeModifer = 0;
                return rangeModifer * -1;
            }
        }
        private PerceptionModier PerceptionModier;
        public bool IsDisabled
        {
            set
            {
                Senses.ForEach((Sense sense) => { sense.IsDisabled = value; });
            }
        }
        public int RollRequiredToPercieveTarget
        {
            get
            {

                Sense effectiveSense = MostEffectiveSenseToPercieveTarget;
                return effectiveSense.RollRequiredToPercieveTarget;
            }
        }

        public Sense SenseThatSuccessfullyDeterminesLocationOfTarget
        {
            get
            {

                if (ActiveTargetingSenses.Count > 0)
                {
                    return ActiveTargetingSenses.FirstOrDefault();
                }

                List<Sense> senseList = ActiveNonTargetingDetectingSenses;
                LocationPrecision precision;
                foreach (var sense in senseList)
                {
                    precision = sense.DetermineLocationOfTarget();
                    if (precision == LocationPrecision.GeneralLocation) return sense;
                }
                return null;
            }
        }
        public Sense SenseThatSucessfullyPercievesTarget
        {
            get
            {
                List<Sense> senseList = ActiveTargetingSenses;
                bool perceive;
                foreach (var sense in senseList)
                {
                    perceive = sense.Perceive();
                    if (perceive) return sense;
                }


                senseList = ActiveNonTargetingDetectingSenses;
                foreach (var sense in senseList)
                {
                    perceive = sense.Perceive();
                    if (perceive) return sense;
                }
                return null;
            }
        }
        public Sense MostEffectiveSenseToPercieveTarget
        {
            get
            {
                if (ActiveTargetingSenses.Count == 0)
                {
                    return ActiveNonTargetingDetectingSenses.FirstOrDefault();
                }
                else
                {
                    return ActiveTargetingSenses.FirstOrDefault();
                }
            }
        }
        private List<Sense> ActiveNonTargetingDetectingSenses => ActiveRangedSenses.Where(x => x.IsTargetingSense == false).OrderByDescending(x => x.PerceptionModifer).ToList();
        private List<Sense> ActiveTargetingSenses => ActiveRangedSenses.Where(x => x.IsTargetingSense).OrderByDescending(x => x.PerceptionModifer).ToList();
        private List<Sense> ActiveRangedSenses => Senses.Where(x => x.IsRanged && x.IsDisabled == false).ToList();

        
        #endregion

        #region sense and sense groups
        private void intitializeSenses()
        {
            HearingGroup = new SenseGroup(SenseGroupType.Hearing, this, false, true);
            SenseGroups.Add("Hearing",HearingGroup);
            Hearing = HearingGroup.CreateSenseWithNameForGroup("Hearing");
            Hearing.ArcOfPerception = ArcOfPerception.ThreeSixty;

            
            SightGroup = new SenseGroup(SenseGroupType.Sight, this, true, true);
            SenseGroups.Add("Sight", SightGroup);
            Sight = SightGroup.CreateSenseWithNameForGroup("Sight");
            

            SmellTasteGroup = new SenseGroup(SenseGroupType.SmellTaste, this, false, true);
            SmellTaste = SmellTasteGroup.CreateSenseWithNameForGroup("SmellTaste");
            SmellTaste.ArcOfPerception = ArcOfPerception.ThreeSixty;
            SenseGroups.Add("SmellTaste", SmellTasteGroup);

            TouchGroup = new SenseGroup(SenseGroupType.Touch, this, false, false);
            SenseGroups.Add("Touch", HearingGroup);
            Touch = TouchGroup.CreateSenseWithNameForGroup("Touch");
            Touch.ArcOfPerception = ArcOfPerception.ThreeSixty;

            MentalGroup = new SenseGroup(SenseGroupType.Mental, this, false, true);
            SenseGroups.Add("Mental",MentalGroup);
            RadioGroup = new SenseGroup(SenseGroupType.Radio, this, false, true);
            SenseGroups.Add("Radio",RadioGroup);
        }
        public Dictionary<String, SenseGroup> SenseGroups = new Dictionary<string, SenseGroup>();
        public List<SenseGroup> SenseGroupList => SenseGroups.Values.ToList();

        public SenseGroup SmellTasteGroup { get; set; }
        public SenseGroup SightGroup { get; internal set; }
        public SenseGroup TouchGroup { get; internal set; }
        public SenseGroup HearingGroup { get; internal set; }
        public SenseGroup MentalGroup { get; internal set; }
        public SenseGroup RadioGroup { get; internal set; }

        public List<Sense> Senses
        {
            get
            {


                List<Sense> senseList = new List<Sense>();

                foreach (var group in SenseGroups.Values)
                {
                    foreach (var sense in group.Senses.Values)
                    {
                        senseList.Add(sense);
                    }
                }


                return senseList;

            }
        }

        public List<Sense> TargetingSenses
        {

            get { return Senses.Where(y => y.IsTargetingSense).ToList(); }
        }
        public Sense Sight
        {
            get
            {
                return SenseGroups["Sight"].Senses["Sight"];

            }
            set
            {
                SenseGroups["Sight"].Senses["Sight"] = value;

            }
        }
        public Sense Touch
        {
            get
            {
                return SenseGroups["Touch"].Senses["Touch"];

            }
            set
            {
                SenseGroups["Touch"].Senses["Touch"] = value;

            }
        }
        public Sense Hearing
        {
            get
            {
                return SenseGroups["Hearing"].Senses["Hearing"];

            }
            set
            {
                SenseGroups["Hearing"].Senses["Hearing"] = value;

            }
        }
        public Sense SmellTaste { get; set; }
        #endregion

        public HeroSystemCharacter Character { get; set; }

        
    }
    public interface ISense
    {
        HeroSystemCharacter Targeter { get; }

        bool IsDisabled { get; set; }
        string Name { get; set; }

        SenseGroup SenseGroup { get; set; }
        SenseGroupType Type { get; }

        DiscriminatoryLevel Discriminate { get; set; }
        ArcOfPerception ArcOfPerception { get; set; }
        bool IsRanged { get; set; }
        bool IsTargetingSense { get; set; }
        int PerceptionModifer { get; set; }

        LocationPrecision DetermineLocationOfTarget();
        bool Perceive();
        int RollRequiredToPercieveTarget { get; }
        ITargetable Target { get; set; }
    }

    public class Sense : ISense
    {
        
        public HeroSystemCharacter Targeter => SenseGroup.CharacterSenses.Character;
        public bool IsDisabled { get; set; }

        public Sense(SenseGroup group)
        {
            SenseGroup = group;
            Power = null;

        }
        public Sense(SenseGroup group, string name, bool isTargetingSense, bool isRanged,
            DiscriminatoryLevel discriminate) : this(group)
        {
            SenseGroup = group;
            Name = name;
            IsTargetingSense = isTargetingSense;
            IsRanged = isRanged;
            Discriminate = discriminate;

        }

        public SenseGroup SenseGroup { get; set; }
        public SenseGroupType Type => SenseGroup.Type;
        public SensingPower? Power { get; set; }
        public string Name { get; set; }

        #region sensing attributes
        public DiscriminatoryLevel Discriminate { get; set; }
        public ArcOfPerception ArcOfPerception { get; set; }
        private bool _isRanged;
        public bool IsRanged
        {
            get { return _isRanged; }
            set
            {
                if (value == false)
                {
                    IsTargetingSense = false;
                }
                _isRanged = value;
            }
        }
        public bool IsTargetingSense { get; set;}
        #endregion

        #region percieve and locate target
        public LocationPrecision DetermineLocationOfTarget()
        {
            if (IsRanged)
            {
                if (IsTargetingSense)
                {
                    //to do are perception rolls required if in combat? or just pre- combat?
                   /* if (ConditionBasedPerceptionModifier < 0)
                    {
                        if (Perceive() == true)
                        {
                            return LocationPrecision.ExactLocation;
                        }
                        else
                        {
                            return LocationPrecision.CantPercieveLocation;
                        }
                    }   
                    */
                    return LocationPrecision.ExactLocation;
                }
                if (Perceive() == true)
                {
                    return LocationPrecision.GeneralLocation;
                }
                else
                {
                    return LocationPrecision.CantPercieveLocation;
                }
            }
            return LocationPrecision.CantPercieveLocation;

        }
        public bool Perceive()
        {
            if (
                Targeter.PER.Roll(AllModifersToPercieveTarget))

            {
                return true;
            }
            else
            {
                return false;
            }


        }
        public ITargetable Target
        {

            get
            {
                return Targeter.CharacterSenses.Target;
                ;
            }
            set { Targeter.CharacterSenses.Target = value; }
        }
        
        
        public bool IsRollRequiredToPercieveTarget => AllModifersToPercieveTarget < 0;
        public int RollRequiredToPercieveTarget => Targeter.PER.RollRequired(AllModifersToPercieveTarget);

        public int AllModifersToPercieveTarget
        {
            get
            {
                var conditionModifications = ConditionBasedPerceptionModifier;
                return (int) TargetPerceptionModifier + PerceptionModifer +
                       SenseGroup.CharacterSenses.RangeModifierToTarget + conditionModifications;
            }
        }
        public int ConditionBasedPerceptionModifier
        {
            get
            {
                var conditionModifications = 0;
                if (this.Type == SenseGroupType.Sight)
                {
                    foreach (var condition in MapFactory.ActiveGameMap.SightConditions)
                    {
                        conditionModifications += (int) condition;
                    }
                }
                return conditionModifications;
            }
        }
        public int TargetPerceptionModifier
        {

            get
            {
                if (Target!= null)
                {
                    if (Target.PerceptionModifiers.ContainsKey(Type))
                    {
                        return (int)Target.PerceptionModifiers[Type];
                    }
                    else
                    {
                        return 0;
                    }
                }
                else return 0;
            }
        }
        public int PerceptionModifer { get; set; }


        #endregion
    }

    public class SenseGroup 
    {
         
        public CharacterSenses CharacterSenses { get; set; }
        public SenseGroupType Type;
        public Dictionary<String, Sense> Senses = new Dictionary<string, Sense>();


        public SenseGroup(SenseGroupType type, CharacterSenses characterSenses, bool isTargetingSense, bool isRanged)
        {
            CharacterSenses = characterSenses;
            IsTargetingSense = isTargetingSense;
            IsRanged = isRanged;
            Type = type;

        }


        public Sense CreateSenseWithNameForGroup(string name)
        {
            Sense sense = new Sense(this, name, IsTargetingSense, IsRanged, Discriminate);
            Senses.Add(name, sense);
            return sense;
        }

        public Sense AssignUniquesSenseToThisGroup(Sense sense)
        {
            //sense.ArcOfPerception = ArcOfPerception;
            sense.IsRanged = IsRanged;
            sense.IsTargetingSense = IsTargetingSense;
            sense.Discriminate = Discriminate;
            return sense;
        }

        public ArcOfPerception ArcOfPerception { get; set; }
        public bool IsRanged { get; set; }
        public bool IsTargetingSense { get; set; }
        public DiscriminatoryLevel Discriminate;


        public bool IsDisabled
        {
            set
            {
                Senses.Values.ToList().ForEach((Sense sense) => { sense.IsDisabled = value; });
            }
        }


    }
    public interface ITargetable
    {
        Size Size { get; set; }

        Dictionary<SenseGroupType, double> PerceptionModifiers { get; set; }
        Dictionary<SenseGroupType, double> PerceptionMultipliers { get; set; }
        GameHex Hex { get; set; }
        string Name { get; set; }
    }
    #endregion







}


