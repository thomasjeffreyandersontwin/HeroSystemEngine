using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Linq;
using HeroSystemEngine.Dice;
using HeroSystemEngine.Character;
using HeroSystemsEngine.CombatSequence;
using HeroSystemsEngine.Focus;
using HeroSystemsEngine.GameMap;
//using HeroVirtualTabletop.AnimatedAbility;
//using HeroVirtualTabletop.PerformAttack;

namespace HeroSystemEngine.Manuevers
{

    #region base manuever 
    public interface IManuever
    {
        HeroSystemCharacter Character { get; set; }
        PhaseLength PhaseActionTakes { get; set; }
        Boolean IsAbortable { get; set; }
        Boolean IsDefault { get; set; }
        String Name { get; set; }
        ManueverType Type { get; set; }
        void ActivateModifier();
        void DeactivateModifier();
        void Deactivate();
        bool CanPerform { get; }
        bool Perform();
        bool CanAbortDuringCombatManuever(Manuever manuever);
        ManueverModifier Modifier { get; set; }

    }
    public abstract class Manuever :IManuever
    {
        public Manuever(ManueverType type, String name, HeroSystemCharacter character, bool isAbortable = false)
        {
            IsAbortable = isAbortable;
            Name = name;
            Character = character;
            Type = type;
            Character.AddManuever(this);
            PhaseActionTakes = PhaseLength.Half;
            Modifier = new ManueverModifier();


        }
        public Manuever()
        {
            DurationUnit = DurationUnit.Phase;
        }

        #region Combat Duration
        public CombatSequence Sequence => Character?.CombatSequence;
        public DurationUnit DurationUnit;
        public PhaseLength PhaseActionTakes { get; set; }
        private void ReducePhaseAmountLeftForCharacter()
        {
            double phaseTaken = PhaseTakenAsNumber;
            if (this.Character.ActivePhase != null)
            {
                this.Character.ActivePhase.PhaseLeft -= phaseTaken;
                if (this.Type == ManueverType.Defensive || this.Type == ManueverType.Attack)
                {
                    if (this.PhaseActionTakes == PhaseLength.Half)
                    {
                        this.Character.ActivePhase.PhaseLeft = 0;
                    }
                }
            }
        }
        private double PhaseTakenAsNumber
        {
            get
            {
                double phaseTaken = 0;
                switch (this.PhaseActionTakes)
                {
                    case PhaseLength.Zero:
                        phaseTaken = 0;
                        break;
                    case PhaseLength.Full:
                        phaseTaken = 1;
                        break;
                    case PhaseLength.Half:
                        phaseTaken = .5;
                        break;
                }
                return phaseTaken;
            }
        }
        #endregion

        #region Identifers
        public Boolean IsDefault { get; set; }
        public String Name { get; set; }
        public ManueverType Type { get; set; }
        #endregion

        #region perform manuever
        public HeroSystemCharacter Character { get; set; }

        public virtual bool CanPerform
        {
            get
            {

                if (NotTheCharactersPhaseAndCharacterIsPerformingADefensiveManuever())
                {
                    return true;
                }
                else
                {
                    return
                        CharacterHasPhaseLeftAvailableAndIfAbortingIsPerformingADefensiveManueverAndCanPerformTheManuever
                            ();
                }
            }


        }
        private bool CharacterHasPhaseLeftAvailableAndIfAbortingIsPerformingADefensiveManueverAndCanPerformTheManuever()
        {
            if (this.Character.ActivePhase != null)
            {
                if (PhaseTakenAsNumber <= this.Character.ActivePhase.PhaseLeft)
                {
                    if (Character.IsAborting && Type != ManueverType.Defensive)
                    {
                        return false;
                    }
                    return canPerform();
                }
                else
                {
                    return false;
                }
            }

            else
            {
                return canPerform();
            }
        }
        private bool NotTheCharactersPhaseAndCharacterIsPerformingADefensiveManuever()
        {
            if (Character.CombatSequence != null && Character.CombatSequence.IsStarted)
            {
                if (this.Character.ActivePhase == null)
                {
                    if (Type == ManueverType.Defensive)
                    {
                        return canPerform();
                    }
                    else
                    {
                        return false;
                    }

                }

            }
            return false;
        }
        public abstract Boolean canPerform();

        public bool StartManuever()
        {
            if (AbortingAndAbortingAllowed() == false) return false;
            if (CanPerform)
            {
                ManueverPerformingHandler?.Invoke(this);

                Activate();
                return true;

            }
            return false;
        }

        public virtual bool Perform()
        {
            if(StartManuever()== false) return false;
            if (CanPerform)
            {
                PerformManuever();
                CompleteManuever();
                return true;
            }
            else
            {
                return false;
            }
        }

        public virtual void CompleteManuever()
        {
            registerBonusDeactivationOnManueverDurationComplete();
            ReducePhaseAmountLeftForCharacter();

            if (DurationUnit == DurationUnit.Continuous)
            {
                Character.ManueverInProgess = this;
            }
        }

        public void Activate()
        {
            ActivateModifier();
            Character.ActiveManuever = this;
        }

        public abstract void PerformManuever();
        public static ManueverPerformingHandler ManueverPerformingHandler;
        #endregion

        #region abort Manuever
        private bool AbortingAndAbortingAllowed()
        {
            if (Character.IsAborting == true)
            {
                Manuever manuever = Character.CombatSequence.InterruptedPhase.Character.ActiveManuever;
                if (CanAbortDuringCombatManuever(manuever) == false)
                {
                    return false;
                }
                Character.IsAborting = false;
            }
            return true;
        }
        public Boolean IsAbortable { get; set; }
        public virtual bool CanAbortDuringCombatManuever(Manuever manuever)
        {
            return false;
        }
        #endregion

        #region manuever modifier
        public virtual ManueverModifier Modifier { get; set; }
        public virtual void Deactivate()
        {
            DeactivateModifier();
            if (Character.ManueverInProgess == this)
            {
                Character.ManueverInProgess = null;
            }
        }
        public virtual void ActivateModifier()
        {

            Modifier.Activate(Character);

        }
        public virtual void DeactivateModifier()
        {
            Modifier.Deactivate(this.Character);
        }
        private void registerBonusDeactivationOnManueverDurationComplete()
        {
            Phase.PhaseStartHandler += new SequenceEventHandler(DeactivateBonusOnNewPhase);
            Segment.Started += new SequenceEventHandler(DeactivateBonusOnNewSegment);
        }
        void DeactivateBonusOnNewPhase(object sender)
        {
            Phase phaseStarting = sender as Phase;
            if (phaseStarting.Character.ActiveManuever == this)
            {
                if (DurationUnit == DurationUnit.Phase)
                {

                    DeactivateModifier();
                    Phase.PhaseStartHandler -= new SequenceEventHandler(DeactivateBonusOnNewPhase);
                }
            }

        }
        void DeactivateBonusOnNewSegment(object sender)
        {
            Segment segmentStarting = sender as Segment;
            foreach (var character in segmentStarting.Characters)
            {
                if (character.ActiveManuever == this)
                {   
                    if (DurationUnit == DurationUnit.Segment)
                    {
                        DeactivateModifier();
                        Segment.Started -= new SequenceEventHandler(DeactivateBonusOnNewSegment);
                    }
                }
            }
        }
        
        #endregion

    }
    public abstract class DefensiveCombatManuever : Manuever
    {
        public DefensiveCombatManuever(HeroSystemCharacter character, string name)
            : base(ManueverType.Defensive, name, character, true)
        {

        }
        public override bool canPerform()
        {

            if (AttackDefendingAgainst != null)
            {
                if (AttackDefendingAgainst.HitStatus != HitStatus.NotSet)
                {
                    return false;
                }
                return true;
            }
            return true;

        }

        public Attack AttackDefendingAgainst
        {
            get
            {
                Attack attackBeingDodged = null;
                if (Character.CombatSequence?.InterruptedPhase != null)
                {
                    attackBeingDodged = Character.CombatSequence?.InterruptedPhase?.Character.ActiveManuever as Attack;
                }
                else if (Character.CombatSequence?.ActivePhase.Character.ActiveManuever != null)
                {
                    attackBeingDodged = Character.CombatSequence?.ActivePhase.Character.ActiveManuever as Attack;
                }
                return attackBeingDodged;
            }
        }
        public override void PerformManuever()
        {
            performDefensiveManuever();
            if (Character.CombatSequence.InterruptedPhase != null)
            {
                continueAttackInteruptedByDefense();
            }

            Attack.AttackHandler -= new CharacterAttackedHandler(DefendAgainstNextAttack);
            Attack.AttackHandler += new CharacterAttackedHandler(DefendAgainstNextAttack);

        }
        public abstract void DefendAgainstNextAttack(object sender);
        public abstract void performDefensiveManuever();
        public virtual void continueAttackInteruptedByDefense()
        {
            Attack a = AttackDefendingAgainst;

            if (AttackDefendingAgainst?.Character != null)
            {
                AttackDefendingAgainst.Character.ActiveManuever = null;
            }
            if (Character?.ActivePhase?.SegmentPartOf?.Sequence != null)
            {
                Character.ActivePhase.SegmentPartOf.Sequence.InterruptedPhase = null;
            }
            a.Perform();
        }
    }
    public delegate void ManueverPerformingHandler(Manuever manuever);
    public enum PhaseLength { Zero, Half, Full };
    public enum ManueverType {CombatManuever, MovementManuever = 2, Other = 3, Defensive , Attack};
    #endregion

    #region modifiers
    public abstract class Modifier
    {
        public string Charasteristic;
        public int ModiferAmount;
        public double Multiplier;
        public int CumulativeModifierApplied;
        public abstract void ApplyModifierTo(HeroSystemCharacter character);
        public abstract void RemoveModifierFrom(HeroSystemCharacter character);
    }
    public class ManueverModifier
    {

        #region init characteristic modifiers
        public CharasteristicModifier STR = new CharasteristicModifier("STR");
        public CharasteristicModifier DEX = new CharasteristicModifier("DEX");
        public CharasteristicModifier CON = new CharasteristicModifier("CON");
        public CharasteristicModifier BOD = new CharasteristicModifier("BOD");
        public CharasteristicModifier INT = new CharasteristicModifier("INT");
        public CharasteristicModifier PRE = new CharasteristicModifier("PRE");
        public CharasteristicModifier EGO = new CharasteristicModifier("EGO");
        public CharasteristicModifier COM = new CharasteristicModifier("COM");
        public CharasteristicModifier PD = new CharasteristicModifier("PD");
        public CharasteristicModifier ED = new CharasteristicModifier("ED");
        public CharasteristicModifier SPD = new CharasteristicModifier("SPD");
        public CharasteristicModifier END = new CharasteristicModifier("END");
        public CharasteristicModifier STUN = new CharasteristicModifier("STUN");
        public CharasteristicModifier REC = new CharasteristicModifier("REC");
        public CharasteristicModifier OCV = new CharasteristicModifier("OCV");
        public CharasteristicModifier DCV = new CharasteristicModifier("DCV");
        public CharasteristicModifier ECV = new CharasteristicModifier("ECV");
        public RangeModiferModifier RangedModifer = new RangeModiferModifier();
        public DamageModifier DamageModifier = new DamageModifier();
        #endregion

        public void Activate(HeroSystemCharacter character)
        {
            STR.ApplyModifierTo(character);
            DEX.ApplyModifierTo(character);
            CON.ApplyModifierTo(character);
            BOD.ApplyModifierTo(character);
            INT.ApplyModifierTo(character);
            PRE.ApplyModifierTo(character);
            EGO.ApplyModifierTo(character);
            COM.ApplyModifierTo(character);
            SPD.ApplyModifierTo(character);
            END.ApplyModifierTo(character);
            REC.ApplyModifierTo(character);
            PD.ApplyModifierTo(character);
            ED.ApplyModifierTo(character);
            DCV.ApplyModifierTo(character);
            OCV.ApplyModifierTo(character);
            ECV.ApplyModifierTo(character);
            RangedModifer.ApplyModifierTo(character);
            DamageModifier.ApplyModifierTo(character);



        }
        public void Deactivate(HeroSystemCharacter character)
        {
            STR.RemoveModifierFrom(character);
            DEX.RemoveModifierFrom(character);
            CON.RemoveModifierFrom(character);
            BOD.RemoveModifierFrom(character);
            INT.RemoveModifierFrom(character);
            PRE.RemoveModifierFrom(character);
            EGO.RemoveModifierFrom(character);
            COM.RemoveModifierFrom(character);
            SPD.RemoveModifierFrom(character);
            END.RemoveModifierFrom(character);
            REC.RemoveModifierFrom(character);
            PD.RemoveModifierFrom(character);
            ED.RemoveModifierFrom(character);
            DCV.RemoveModifierFrom(character);
            OCV.RemoveModifierFrom(character);
            ECV.RemoveModifierFrom(character);
            RangedModifer.RemoveModifierFrom(character);
            DamageModifier.RemoveModifierFrom(character);

        }
    }
    public class CharasteristicModifier : Modifier
    {
        public CharasteristicModifier(string charasteristic)
        {
            Charasteristic = charasteristic;
        }

        public override void ApplyModifierTo(HeroSystemCharacter character)
        {
            int deltaMod = ModiferAmount - CumulativeModifierApplied;

            character.Characteristics[Charasteristic].Modifier += deltaMod;


            if (MultiplierApplied == 0 && Multiplier != 0)
            {
                MultiplierApplied = Multiplier;
                if (character.Characteristics[Charasteristic].Multiplier == 0)
                {
                    character.Characteristics[Charasteristic].Multiplier = Multiplier;
                }
                else
                {
                     character.Characteristics[Charasteristic].Multiplier *= Multiplier;
                }
            }
  
        CumulativeModifierApplied += deltaMod;
        }
        public double MultiplierApplied { get; set; }
        public override void RemoveModifierFrom(HeroSystemCharacter character)
        {
            character.Characteristics[Charasteristic].Modifier -= CumulativeModifierApplied;
            if(character.Characteristics[Charasteristic].Multiplier !=0 && MultiplierApplied!=0)
            { 
            character.Characteristics[Charasteristic].Multiplier /= MultiplierApplied;
            }
            CumulativeModifierApplied = 0;
            MultiplierApplied = 0;
        }
    }
    public enum HealthType
    {
        STUN,
        BOD,
        ALL
    }
    public class DamageModifier : Modifier
    {
        public HealthType AppliesTo = HealthType.ALL;
        public override void ApplyModifierTo(HeroSystemCharacter character)
        {
            character.DamageClassModifier += ModiferAmount;
            CumulativeModifierApplied += ModiferAmount;
            character.DamageMultiplier = Multiplier;
        }

        public override void RemoveModifierFrom(HeroSystemCharacter character)
        {
            character.DamageClassModifier -= CumulativeModifierApplied;
            CumulativeModifierApplied = 0;
            character.DamageMultiplier = 0;
           // character.DamageModifierAppliesTo = HealthType.ALL;
        }


    }
    public class RangeModiferModifier : Modifier
    {
        public override void ApplyModifierTo(HeroSystemCharacter character)
        {
            character.RangedModModifier += ModiferAmount;
            CumulativeModifierApplied += ModiferAmount;
            character.RangedModifierMultiplier = Multiplier;
        }

        public override void RemoveModifierFrom(HeroSystemCharacter character)
        {
            character.RangedModModifier -= CumulativeModifierApplied;
            character.RangedModifierMultiplier = Multiplier;
        }


    }
    #endregion

    #region manuever implementationS
    public class CasualStrength : Manuever
    {
        public CasualStrength(HeroSystemCharacter character) :
            base(ManueverType.Other, "Casual Strength", character, false)
        {

        }

        public bool Success { get; set; }
        public int Difficulity;

        public override bool canPerform()
        {
            return true;
        }
        public override void PerformManuever()
        {
            Character.STR.RollCasualStrengthAgainst(Difficulity);
        }

    }
    public class Block : DefensiveCombatManuever
    {
        public Block(HeroSystemCharacter character) : base(character, "Block")
        {

        }

        public HeroSystemCharacter Blocker => Character;

        public bool BlockIsSuccessful(int roll)
        {
            return RequiredToHitRoll >= roll;
        }
        public int RequiredToHitRoll
        {
            get
            {
                return RequiredRollToBlockAttacker(AttackDefendingAgainst?.Attacker);

            }

        }
        public int RequiredRollToBlockAttacker(HeroSystemCharacter attacker)
        {


            int roll = 0;
            if (Character?.OCV?.CurrentValue != null && attacker?.OCV?.CurrentValue != null)
                roll = Character.OCV.CurrentValue - attacker.OCV.CurrentValue + 11;
            return roll;

        }
        public BlockStatus BlockStatus = BlockStatus.NotSet;

        public override bool canPerform()
        {
            if (base.canPerform() == true)
            {
                if (AttackDefendingAgainst?.Ranged == true)
                {
                    return false;
                }
            }
            return true;
        }
        public override void performDefensiveManuever()
        {
            int roll = new DicePool(3).Roll();
            int required = Blocker.OCV.CurrentValue - AttackDefendingAgainst.Attacker.OCV.CurrentValue + 11;
            if (roll <= required)
            {
                BlockStatus = BlockStatus.BlockSuccessful;
                AttackDefendingAgainst.Result.HitResult = HitResult.Blocked;
                AttackDefendingAgainst.HitStatus = HitStatus.Blocked;
            }
            else
            {
                BlockStatus = BlockStatus.BlockFailed;
                Blocker.ActiveManuever = null;
                Modifier.OCV.ModiferAmount = 0;

            }


        }
        public override void DefendAgainstNextAttack(object sender)
        {
            Attack a = sender as Attack;
            if (a.Defender.IsBlocking)
            {
                Block b = a.Defender.ActiveManuever as Block;
                b.Modifier.OCV.ModiferAmount += -2;
                b.ActivateModifier();
                b.BlockStatus = BlockStatus.NotSet;
                b.performDefensiveManuever();
                
            }
        }
        public override bool CanAbortDuringCombatManuever(Manuever manuever)
        {
            if (AttackDefendingAgainst.Result.HitResult == HitResult.Hit)
            {
                return false;
            }
            return true;
        }  
    }
    public enum BlockStatus { BlockSuccessful, BlockFailed, NotSet }
    public class Brace : Manuever
    {
        public Brace(HeroSystemCharacter character)
            : base(ManueverType.CombatManuever, "Brace", character, false)
        {
            PhaseActionTakes = PhaseLength.Zero;
            Modifier.RangedModifer.ModiferAmount = +2;
            Modifier.DCV.Multiplier = .5;
        }

        public override bool canPerform()
        {
            return true;
        }
        public override void PerformManuever()
        {

        }
    }
    public class Dodge : DefensiveCombatManuever
    {
        public Dodge(HeroSystemCharacter character) : base(character, "Dodge" )
       {
            Modifier.DCV.ModiferAmount = +3;
        }
        
        public override void PerformManuever()
        {
            
        }
        public override void performDefensiveManuever()
        {
            throw new NotImplementedException();
        }
        public override void DefendAgainstNextAttack(object sender)
        {
        }
        public override bool CanAbortDuringCombatManuever(Manuever manuever)
        {
            throw new NotImplementedException();
        }
    }
    class DiveForCover : Manuever
    {
        

        public DiveForCover(HeroSystemCharacter character) : base(ManueverType.CombatManuever, "Dive For Cover", character)
        {
        }

        public override bool canPerform()
        {
            return true;
        }

        public override void PerformManuever()
        {
            throw new NotImplementedException();
        }
    }   
    class HipShot : Manuever
    {

        public HipShot(HeroSystemCharacter character) : base(ManueverType.Other,  "Hip Shot", character)
        {
            PhaseActionTakes = PhaseLength.Zero;
            Type=ManueverType.Other;
        }
        
        private void RemoveHipShotModifiers(object sender)
        {
            Segment seg = sender as Segment;
            
                Character.DEX.Modifier -= 1;
                Character.OCV.Modifier -= -1;
                Segment.Ended -= new SequenceEventHandler(RemoveHipShotModifiers);
            

        }

        public override void PerformManuever()
        {

            Character.DEX.Modifier += 1;
            Character.OCV.Modifier += -1;
            Segment.Ended += new SequenceEventHandler(RemoveHipShotModifiers);
            Sequence.ActiveSegment.CombatPhases.FirstOrDefault()?.Activate();

        }
        public override bool CanPerform
        {
            get
            {
                if (!Character.IsAborting)
                {

                    if (Character.SPD.Phases.ContainsKey(Sequence.ActiveSegment.Number))
                    {
                        if (Character.SPD.Phases[Sequence.ActiveSegment.Number].Finished == false)
                        {
                            return true;
                        }
                    }
                    return false;
                }
                return false;
            }
        }
        public override bool canPerform()
        {
            throw new NotImplementedException();
        }
    }
    class Hurry : Manuever
    {
        public Hurry(HeroSystemCharacter character) : base(ManueverType.Other, "Hurry", character)
        {
            PhaseActionTakes = PhaseLength.Zero;
            Type = ManueverType.Other;
        }

        public int DexBonus;
        public override void PerformManuever()
        {
            DexBonus = new DicePool(6).Roll();
            Character.DEX.Modifier += DexBonus;
            Character.GlobalModifier+= -2;
            Segment.Ended += new SequenceEventHandler(RemoveHurryModifiers);
            Sequence.ActiveSegment.CombatPhases.FirstOrDefault()?.Activate();

        }

        private void RemoveHurryModifiers(object sender)
        {
            Segment seg = sender as Segment;

            Character.DEX.Modifier -= DexBonus;
            Character.GlobalModifier -= -2;
            Segment.Ended -= new SequenceEventHandler(RemoveHurryModifiers);


        }
        public override bool CanPerform
        {
            get
            {
                if (!Character.IsAborting)
                {

                    if (Character.SPD.Phases.ContainsKey(Sequence.ActiveSegment.Number))
                    {
                        if (Character.SPD.Phases[Sequence.ActiveSegment.Number].Finished == false)
                        {
                            return true;
                        }
                    }
                    return false;
                }
                return false;
            }
        }
        public override bool canPerform()
        {
            throw new NotImplementedException();
        }
    }
    public class Recover : Manuever
    {

        public Recover(HeroSystemCharacter character) : base(ManueverType.CombatManuever, "Recover", character, false)

        {

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
        public override bool Perform()
        {
            return true;
        }
    }
    class RollWithAPunch : DefensiveCombatManuever
    {
   
        public RollWithAPunch(HeroSystemCharacter character) : base(character, "Roll With A Punch" )
        {
            PhaseActionTakes = PhaseLength.Full;
            Modifier.OCV.ModiferAmount = -2;
        }

        public bool RollSucessful { get; set; }
        public RollStatus RollStatus { get; set; }

        public override bool canPerform()
        {
            if (AttackDefendingAgainst != null)
            {
                if (AttackDefendingAgainst.HitStatus == HitStatus.Hit)
                {
                    if (AttackDefendingAgainst.Damage.WorksAgainstDefense == DefenseType.PD)
                    {
                        return true;
                    }
                    return false;
                }
                return false;
            }
            return false;
        }
        public override bool CanAbortDuringCombatManuever(Manuever manuever)
        {
            if (AttackDefendingAgainst.Result.HitResult == HitResult.Hit)
            {
                return true;
            }
            return false;
        }
        public override void DefendAgainstNextAttack(object sender)
        {

        }
        public override void performDefensiveManuever()
        {
            int roll = new DicePool(3).Roll();
            int required = Character.OCV.CurrentValue - AttackDefendingAgainst.Attacker.OCV.CurrentValue + 11;
            if (roll <= required)
            {

                RollStatus = RollStatus.RollSuccessful;
                AttackDefendingAgainst.Result.HitResult = HitResult.RolledWithThePunch;
                AttackDefendingAgainst.HitStatus = HitStatus.RolledWithThePunch;

            }
            else
            {
                RollStatus = RollStatus.RollFailed;
                Character.ActiveManuever = null;
            }
        }
        public override void continueAttackInteruptedByDefense()
        {
            Attack a = AttackDefendingAgainst;
            if (Character?.ActivePhase?.SegmentPartOf?.Sequence != null)
            {
                Sequence.InterruptedPhase.Activate();
                Sequence.InterruptedPhase = null;

            }

            a.Perform();
        }
    }
    public enum RollStatus { RollSuccessful, RollFailed }
    #endregion s





}


