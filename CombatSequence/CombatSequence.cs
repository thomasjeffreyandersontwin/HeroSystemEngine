using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using HeroSystemEngine.Character;
using HeroSystemEngine.Manuevers;
using Ploeh.AutoFixture;

namespace HeroSystemsEngine.CombatSequence
{
    public enum Timing { Start, End}
    public enum DurationUnit { Segment, Phase, Turn, Minute, Continuous }
    public delegate void SequenceEventHandler(Object sender);

    public class CombatSequence
    {
        public Dictionary<int, Segment> Segments = new Dictionary<int, Segment>();
        public List<HeroSystemCharacter> Characters = new List<HeroSystemCharacter>();
        List<Segment> SegmentList
        {
            get
            {
                return Segments.Values.OrderBy(i => i.Number).ToList();
            }
        }
        public CombatSequence()
        {
            Segments.Add(1, new Segment(1, this));
            Segments.Add(2, new Segment(2, this));
            Segments.Add(3, new Segment(3, this));
            Segments.Add(4, new Segment(4, this));
            Segments.Add(5, new Segment(5, this));
            Segments.Add(6, new Segment(6, this));
            Segments.Add(7, new Segment(7, this));
            Segments.Add(8, new Segment(8, this));
            Segments.Add(9, new Segment(9, this));
            Segments.Add(10, new Segment(10, this));
            Segments.Add(11, new Segment(11, this));
            Segments.Add(12, new Segment(12, this));
        }
        public void StartCombat()
        {
            IsStarted = true;
            Segments[12].Activate();
           // ActiveSegment = Segments[12];
            ActiveTurn = 1;
        }
        public bool IsStarted { get; set; }

        public void CompleteActivePhase()
        {
            ActivePhase.Complete();
        }
        public Phase ActivePhase => ActiveSegment?.ActivePhase;
        public Phase NextCombatPhase => ActiveSegment?.NextCombatPhase;
        public Phase ActivateNextCombatPhaseInActiveSegment => ActiveSegment.ActivateNextCombatPhase;
        
        public Phase ActivateNextPhaseInSequence
        {
            get
            {
                if (ActiveSegment.NextCombatPhase == null)
                {
                    Segment s = ActivateNextSegment;
                }
                return ActivateNextCombatPhaseInActiveSegment;
            }
        }

        public Segment ActiveSegment { get; set; }
        public Segment NextSegment
        {
            get
            {
                List<Segment> segments = SegmentList;
                if (ActiveSegment == null)
                {
                    return segments.First();

                }
                else
                {
                    return SegmentAfter(ActiveSegment);
                }
            }
        }
        public Segment SegmentAfter(Segment thisSegment)
        {
            int number = thisSegment.Number + 1;
            if (number > 12)
                number = 1;
            return Segments[number];
        }
        public Segment ActivateNextSegment {
            get
            {
                if (ActiveSegment.Number == 12)
                {
                    performPostSegmentTwelve();
                    TurnEnded?.Invoke(this);
                    foreach (var phase in AllPhases)
                    {
                        phase.Finished = false;
                    }
 
                }
                ActiveSegment.Active = false;
                int searchedPhases = 0;
                Segment nextSegment = NextSegment;
                while (searchedPhases <12)
                {
                    nextSegment.Activate();
                    if (nextSegment.CombatPhases.Count != 0 || nextSegment.HeldPhaseWaitingForDexOrSegmentAvailableForEmptySegment != null)
                    {
                        //nextSegment.Activate();
                        if (HeldManuevers.Count > 0)
                        {
                            List<HoldActionManuever> wastedHolds = HeldManuevers.Where(
                            x=>x.SegmentWaitingFor == nextSegment.Number).ToList();
                            foreach (var wastedHold in wastedHolds)
                            {
                                HeldManuevers.Remove(wastedHold);
                                wastedHold.HeldPhase.Character.HeldManuever = null;
                            }
                        }
                        return nextSegment;
                    }
                    searchedPhases++;
                    nextSegment = nextSegment.Sequence.SegmentAfter(nextSegment);

                }
                return null;


            }
        }
        private void performPostSegmentTwelve()
        {
            foreach (var characters in Characters)
            {
              
                characters.Manuevers["Recover"]?.Perform();
            }
        }

        public int ActiveTurn { get; set; }
        

        public void AddCharacters(List<HeroSystemCharacter> characters)
        {
            foreach (var character in characters)
            {
                addCharacterPhaseToSegments(character);   
            }
        }
        private void addCharacterPhaseToSegments(HeroSystemCharacter character)
        {
            character.CombatSequence = this;
            Characters.Add(character);
            foreach (var phaseNumber in ((SPD)character.SPD).SegmentNumbersCharacterHasPhases)
            {
                Segment segment = Segments[phaseNumber];
                Phase phase = segment.AddCharacter(character);
                character.SPD.Phases[segment.Number] = phase;

            }
        }

        public List<Phase> AllPhases
        {
            get

            {
                List<Phase> allPhases= new List<Phase>();
                for (int i =1; i < 12; i++)
                {
                    Segment activeSegment = Segments[i];
                    foreach (var phase in activeSegment.CombatPhases)
                    {
                        allPhases.Add(phase);
                    }
                }
                return allPhases;
            }
        }

        public void AddCharacter(HeroSystemCharacter character)
        {
            addCharacterPhaseToSegments(character);
        }
        
        public List<HoldActionManuever> HeldManuevers = new List<HoldActionManuever>();
        public List<Cover> CoveringManuevers = new List<Cover>();
        public Phase InterruptedPhase { get; set; }
        public List<Interruption> Interruptions
        {
            get
            {
                List<Interruption> list = new List<Interruption>();
                list.AddRange(HeldManuevers);
                list.AddRange(CoveringManuevers);
                return list;
            }
        }

        public event SequenceEventHandler TurnEnded;
    }

    public class Segment
    {
        public CombatSequence Sequence;
        public Segment(int number, CombatSequence sequence)
        {
            Number = number;
            Sequence = sequence;
        }
        public int Number;
        private Dictionary<string, Phase> _combatPhases = new Dictionary<string,Phase>();

        public Phase AddCharacter(HeroSystemCharacter character)
        {
            Phase phase = new Phase(character, this);
            _combatPhases[character.Name]= phase;
            character.SPD.Phases[Number] = phase;
            return phase;
        }

        public List<HeroSystemCharacter> Characters
        {
            get
            {
                return _combatPhases.Values.Select(phase => phase.Character).ToList();
            }
        }

        public List<Phase> CombatPhases
        {
            get { return _combatPhases.Values.OrderByDescending(i => i.Character.DEX.CurrentValue).ToList(); }
        }

        public Phase NextCombatPhase {
            get
            {
                List<Phase> combatPhases = CombatPhases;
                if (ActivePhase == null)
                {

                    return combatPhases.FirstOrDefault();

                }
                else
                {
                    Phase nextPhase;
                    if (Sequence.InterruptedPhase != null)
                    {
                        nextPhase = Sequence.InterruptedPhase;
                        Sequence.InterruptedPhase = null;
                    }
                    else
                    {
                        nextPhase = PhaseNextInDexOrder;
                        if (nextPhase==null) return null;
                    }
                    nextPhase = InterruptNextPhaseWithHeldPhaseIfWaitingForDexBeforeNextPhase(nextPhase);
                    return nextPhase;
                    
                }

            }
        }

        private Phase InterruptNextPhaseWithHeldPhaseIfWaitingForDexBeforeNextPhase(Phase nextPhase)
        {
            HoldActionManuever held = HeldPhaseWaitingForDexOrSegmentAvailableBeforeNextPhase(nextPhase);
            if (held != null)
            {
                held.Initialize();
                Sequence.InterruptedPhase = nextPhase;

                nextPhase = held.HeldPhase;
            }
            return nextPhase;
        }

        public Phase PhaseNextInDexOrder
        {
            get
            {
                List<Phase> combatPhases = CombatPhases;
                Phase nextPhase;
                int element = combatPhases.IndexOf(ActivePhase);
                element = element + 1;
                if (element != combatPhases.Count)
                {
                    nextPhase = combatPhases.ElementAt(element);
                }
                else
                {
                    return null;
                }
                return nextPhase;
            }
        }

        private HoldActionManuever HeldPhaseWaitingForDexOrSegmentAvailableBeforeNextPhase(Phase nextPhase)
        {
            if (Sequence.HeldManuevers?.Count > 0)
            {
                List<HoldActionManuever> held = Sequence.HeldManuevers.Where
                (x => x.DexWaitingFor >= nextPhase.Character.DEX.CurrentValue &&
                      (x.SegmentWaitingFor == 0 || x.SegmentWaitingFor== nextPhase.SegmentPartOf.Number)
                ).OrderBy(x => x.DexWaitingFor).ToList();
                if (held.Count > 0)
                {
                    
                    if (held.Count > 1)
                    {
                        foreach (HoldActionManuever comp in held)
                        {
                            bool success = held[0].HeldPhase.Character.DEX.RollAgainst(comp.Character);
                            if (success == false)
                            {
                                held.Remove(comp);
                            }
                        }
                    }
                    return held[0];
                    
                }
            }
            return null;
        }

        public Phase HeldPhaseWaitingForDexOrSegmentAvailableForEmptySegment
        {
            get
            {  
                if (Sequence.HeldManuevers?.Count > 0)
                {
                    List<HoldActionManuever> held = Sequence.HeldManuevers.Where
                        (x =>  x.SegmentWaitingFor == this.Number)
                          .OrderBy(x => x.DexWaitingFor).ToList();
                    if (held.Count > 0)
                    {
                        {
                            return held[0].HeldPhase;
                        }
                    }
                }
                return null;
            }
        }

        public Phase ActivateNextCombatPhase
        {
            get
            {
                Phase phase = NextCombatPhase;
                if (phase == null)
                {
                   return null;
                }
                else
                {
                    
                    phase.Activate();
                    if (phase.Character.HeldManuever != null)
                    {
                        phase.Character.HeldManuever = null;
                        
                    }
                    if (phase.Character.CoveringManuever != null)
                    {
                        phase.Character.CoveringManuever = null;

                    }
                    return phase;
                }
            }
        
        }

        public Phase ActivePhase { get; set; }

        public static SequenceEventHandler Started;
        public static SequenceEventHandler Ended;

        protected virtual void OnStarted(EventArgs e)
        {
            if (Started != null)
            {
                Started(this);
            }
        }
        public void Activate()
        {
            int lastSegment = Number - 1;
            if (lastSegment == 0)
            {
                lastSegment = 12;
            }
            if (Sequence.Segments.ContainsKey(lastSegment))
            {
                Sequence.Segments[lastSegment].OnEnded(EventArgs.Empty);
            }
            Sequence.ActiveSegment?.Deactivate();
            Sequence.ActiveSegment = this;
            Active = true;
            OnStarted(EventArgs.Empty);
            var x = ActivateNextCombatPhase;
        }

        private void OnEnded(EventArgs e)
        {
            if (Ended != null)
            {
                Ended(this);
            }
        }

        private void Deactivate()
        {
            Active = false;
            Sequence.ActiveSegment?.ActivePhase?.Deactivate();
            Sequence.ActiveSegment = null;
        }

        public bool Active { get; set; }
    }

    public class Phase
    {
        public static event SequenceEventHandler PhaseStartHandler;
        public static event SequenceEventHandler PhaseEndeHandler;
        private double _phaseLeft = 1;
        public double PhaseLeft
        {
            get
            {
                return _phaseLeft;
            }
            set
            {
                _phaseLeft = value;
                if (_phaseLeft == 0)
                {
                    Complete();
                }
            }
        } 
        public Segment SegmentPartOf;
        public bool Active = false;
        public HeroSystemCharacter Character;
        public Phase(HeroSystemCharacter character, Segment segmentPartOf)
        {
            Character = character;
            SegmentPartOf = segmentPartOf;
        }

        public void Activate()
        {
            Character.SegmentNumberThatLastPhaseActivatedOn = SegmentPartOf.Number;
            SegmentPartOf.ActivePhase?.Deactivate();
            SegmentPartOf.ActivePhase = this;
            Active = true;
            Character.ActivePhase = this;
            if (PhaseStartHandler != null)
            {
                PhaseStartHandler(this);
            }
        }
        public void Deactivate()
        {
            SegmentPartOf.ActivePhase = null;
            Active = false;
            Finished = true;
            Character.ActivePhase = null;
            PhaseLeft = 1;
        }

        public bool Finished = false;

        public void Complete()
        {
            if (PhaseEndeHandler != null)
            {
                PhaseEndeHandler(this);
            }
            Phase phase = SegmentPartOf.ActivateNextCombatPhase;
            if (phase == null)
            {
                Segment nextSegment = SegmentPartOf.Sequence.ActivateNextSegment;
            }


        }

       
    }

    public enum InterruptionWith
    {
        Generic, Event, Defensive
    }

    public interface Interruption
    {
        void Interrupt(InterruptionWith interruptionReason=InterruptionWith.Generic);

    }
    public class HoldActionManuever : Manuever , Interruption
    {
        public HoldActionManuever(HeroSystemCharacter character)
            : base(ManueverType.Other, "Hold Action", character, false)
        {
            PhaseActionTakes = 0;
        }

        public int DexWaitingFor { get; set; }
        public int SegmentWaitingFor { get; set; }

        public Phase HeldPhase;
        private double PhaseLeftBeingHeld = 0;

        public override bool canPerform() => true;

        public override void PerformManuever()
        {
            HeldPhase = Character.ActivePhase;
            Character.HeldManuever = this;
            Character.CombatSequence.HeldManuevers.Add(this);
            

            PhaseLeftBeingHeld = HeldPhase.PhaseLeft;
            HeldPhase.Complete();

        }

        public override bool CanAbortDuringCombatManuever(Manuever manuever)
        {
            return false;
        }

        public void Initialize()
        {
            HeldPhase.PhaseLeft = PhaseLeftBeingHeld;
            HeldPhase.SegmentPartOf.Sequence.HeldManuevers.Remove(this);
            HeldPhase.Character.HeldManuever = null;
        }


        public void Interrupt(InterruptionWith interruptionReason)
        {
            if (interruptionReason == InterruptionWith.Generic)
            {
                HeroSystemCharacter interruptedCharacter = Sequence.ActivePhase.Character;
                bool success = Character.DEX.RollAgainst(interruptedCharacter);
                if (success == false)
                {
                    Sequence.InterruptedPhase = HeldPhase;
                    Initialize();
                    return;

                }
            }
            Initialize();
            Sequence.InterruptedPhase = HeldPhase.SegmentPartOf.ActivePhase;
            HeldPhase.Activate();
            
        }

        public HeroSystemCharacter CharacterWaitingFor { get; set; }
    }

    public class AbortManuever : Manuever
    {
        public AbortManuever(HeroSystemCharacter character)
            : base(ManueverType.Other, "Abort", character, false)
        {
            PhaseActionTakes = 0;
        }
        public Phase AbortPhase;
        public override bool CanAbortDuringCombatManuever(Manuever manuever)
        {
            return false;
        }

        public override bool CanPerform
        {
            get
            {
                if (Character.IsAborting)
                {
                    return false;
                }
                CombatSequence sequence = Character.CombatSequence;
                if (sequence.ActiveSegment.Number ==
                    Character.SegmentNumberThatLastPhaseActivatedOn)
                {
                    return false;
                }
                else
                    return true;
            }

        }

        public override void PerformManuever()
        {
            if (Character.ManueverInProgess != null)
            {
                Character.ManueverInProgess?.Deactivate();
                Character.ManueverInProgess = null;
            }
            Character.IsAborting = true;
            AbortPhase = Character.NextPhase;
            Character.CombatSequence.InterruptedPhase = Character.CombatSequence.ActivePhase;
            AbortPhase.Activate();

        }

        public override bool canPerform()
        {
            throw new NotImplementedException();
        }
    }

    public delegate void SequenceTimerAction(SequenceTimer timer);
    public class SequenceTimer
    {
        private Timing Timing;
        public SequenceTimer(DurationUnit durationUnit, int time, CombatSequence sequence, Timing timing = Timing.Start)
        {
            DurationUnit = durationUnit;
            Time = time;
            Sequence = sequence;
            Timing = timing;
        }
        public CombatSequence Sequence;
        public int Time { get; set; }
        public Object Start { get; private set; }
        public int End { get; private set; }
        public DurationUnit DurationUnit { get; set; }
        public int Countdown { get; set; }
        public bool IsTracking { get; set; }
        public void StartTimer()
        {
            if (DurationUnit == DurationUnit.Segment)
            {
                Start = Sequence.ActiveSegment;
                if (Timing == Timing.Start)
                {
                    Segment.Started += new SequenceEventHandler(StopTimerIfDone);
                }
                else
                {
                    Segment.Ended += new SequenceEventHandler(StopTimerIfDone);

                }
            }
            else if (DurationUnit == DurationUnit.Phase)
            {
                Start = Sequence.ActivePhase;
                if (Timing == Timing.Start)
                {

                    Start = Sequence.ActivePhase;
                    Phase.PhaseStartHandler += new SequenceEventHandler(StopTimerIfDone);
                }
                else
                {
                    Phase.PhaseEndeHandler += new SequenceEventHandler(StopTimerIfDone);
                
                }

            }
            else if (DurationUnit == DurationUnit.Turn)
            {
                Sequence.TurnEnded += new SequenceEventHandler(StopTimerIfDone);
            }
        }
        void StopTimerIfDone(object sender)
        {
            if (TimerOverThisSegment)
            {
                if (TimerAction != null)
                {
                    TimerAction(this);
                    TimerAction = null;
                }
            }
        }
        public bool TimerOverThisSegment
        {
            get
            {
                if (DurationUnit == DurationUnit.Segment)
                {
                    Segment start = (Segment) Start;
                    int startNumber = start.Number;
                    int currentNumber = Sequence.ActiveSegment.Number;
                    if (GetFutureSegmentNumber(Time, startNumber) == currentNumber)
                    { 
                        Stop = Sequence.ActiveSegment;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (DurationUnit == DurationUnit.Phase)
                {
                    Phase activePhase = Sequence.ActivePhase;
                    Phase phase = (Phase) Start;
                    if (phase?.Character == activePhase?.Character)
                    {
                        var startNumber = phase.SegmentPartOf.Number;
                        var currentNumber = activePhase.SegmentPartOf.Number;
                        var futPhase = GetFuturePhaseNumber(Time, phase);
                        if (futPhase == currentNumber)
                        {
                            Stop = (Phase)Sequence.ActivePhase;
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    
                }
                else if (DurationUnit == DurationUnit.Turn)
                {
                    if (Sequence.ActiveSegment.Number == 12)
                    {
                        Stop = Sequence.ActiveSegment;
                        return true;
                    }
                }
                    return false;
            }
        }
        public Object Stop { get; set; }
        private double GetFuturePhaseNumber(int time, Phase phase)
        {
            var startNumber = phase.SegmentPartOf.Number;
            startNumber = phase.Character.SPD.SegmentNumbersCharacterHasPhases.IndexOf(startNumber)+1;
            int totalNumber = phase.Character.SPD.Phases.Count;
            int index = 0;
            if (startNumber + time <= totalNumber)
            {
                index= startNumber + time;
            }
            else
            {
                index = (startNumber + time - totalNumber)-1;
            }

            return phase.Character.SPD.SegmentNumbersCharacterHasPhases[index];
           




            


        }
        private static int GetFutureSegmentNumber(int duration, int startNumber)
        {
            if (startNumber + duration <= 12)
            {
                return startNumber + duration;
            }
            else
            {
                return startNumber + duration - 12;
            }
           
        }
        public event SequenceTimerAction TimerAction;


    }
}