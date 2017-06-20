using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using HeroSystemEngine.Dice;
using HeroSystemEngine.Character;
using HeroSystemsEngine.CombatSequence;
using HeroSystemsEngine.Focus;
using HeroSystemsEngine.GameMap;
using HeroSystemsEngine.Perception;

namespace HeroSystemEngine.Manuevers
{
	#region base attacks
	public class Attack : Manuever
	{

		public Attack(String name, HeroSystemCharacter attacker, DamageType damageType, int damageDiceNumber, DefenseType worksAgainstDefense, bool ranged=false)
			:base( ManueverType.Attack, name,  attacker, false)
		{
			_damage = new Damage(damageDiceNumber, damageType, worksAgainstDefense);
			_damage.Attacker = Attacker;
			Ranged = ranged;
			Type = ManueverType.Attack;
		}

		#region Attack Defender
		public AttackResult Result = new AttackResult();
		public HeroSystemCharacter Attacker
		{
			get { return this.Character; }
		}
		private HeroSystemCharacter _defender;
		public virtual HeroSystemCharacter Defender
		{
			get
			{
				return _defender;
			}
			set
			{
				_defender = value;
              
				Attacker.CharacterSenses.Target = value;
			}
		}

		public static CharacterAttackedHandler AttackHandler;
		public static CharacterTargetedHandler TargetingHandler;
		public virtual AttackResult PerformAttack(HeroSystemCharacter defender)
		{
			//to do use target here over direct access
			this.Defender = defender;
			this.Perform();
			return Result;
		}
		public virtual void Target(HeroSystemCharacter defender)
		{
			
			Defender = defender;
			HitStatus = HitStatus.NotSet;
			Character.ActiveManuever = this;
			Character.CharacterSenses.Target = defender;
			if (TargetingHandler != null)
			{
				TargetingHandler(this);
			}


		}

		public override void PerformManuever()
		{
			AttemptToHitDefender();
			if (HitStatus == HitStatus.Hit || HitStatus==HitStatus.RolledWithThePunch)
			{
				RollDamageAndKnockbackAndApplyDamageToDefender();
			}
		}
		public override bool CanAbortDuringCombatManuever(Manuever manuever)
		{
			return false;
		}
		public override Boolean canPerform()
		{
			if (Ranged == false)
			{
				if (Defender != null)
				{
					GameHex AttackerHex = Attacker.Hex;
					GameHex DefenderHex = Defender.Hex;
					if (AttackerHex.DistanceFrom(DefenderHex) > 1)
					{
						return false;
					}
					return true;
				}
				//  GameMapStub activeMap = MapFactory.ActiveGameMap;
				//  return activeMap.OtherCharactersBeside(Attacker.Hex);
				return false;
			}
			else
			{
				bool concealed = defenderIsCompletyConcealedBehindBlockingCover();
				if (concealed) return false;

			}

			return canPerciveDefender();
		}
		
		#endregion

		#region To Hit Defender
		public bool Ranged = false;
		public HitStatus AttemptToHitDefender()
		{
			Result = new AttackResult();
			if (AttackHandler != null)
			{
				AttackHandler(this);
			}

			Result.Target = Defender;
			if (Defender == null)
			{
				HitStatus = HitStatus.NotSet;
				return HitStatus;
			}
			if (HitStatus == HitStatus.NotSet)
			{
				HitStatus = RollToHit();
			}
			updateAttackResultBasedOnHitStatus();
			return HitStatus;
		}
		protected HitStatus RollToHit()
		{
			ToHitRoll = new DicePool(3).Roll();
			bool hit = ToHitRollIsSuccessful(ToHitRoll);
			if (hit == true)
			{
				return HitStatus.Hit;
			}
			else
			{
				return HitStatus.Miss;
			}
		}
		public bool ToHitRollIsSuccessful(int roll)
		{
			return RollRequiredToHitDefender >= roll;
		}
		private void updateAttackResultBasedOnHitStatus()
		{

			if (HitStatus == HitStatus.Hit)
			{
				Result.HitResult = HitResult.Hit;
			}
			else
			{
				if (HitStatus == HitStatus.Miss)
				{
					Result.HitResult = HitResult.Miss;
				}
				else
				{
					if (HitStatus == HitStatus.Blocked)
					{
						Result.HitResult = HitResult.Blocked;
					}
					else
					{
						if (HitStatus == HitStatus.RolledWithThePunch)
						{
							Result.HitResult = HitResult.RolledWithThePunch;
						}
					}
				}
			}
		}
		public int ToHitRoll { get; set; }
		public virtual int RollRequiredToHitDefender
		{
			get
			{
				return RollRequiredToHitWithoutModifiers(Defender);

			}
		}
		public int RollRequiredToHit(HeroSystemCharacter defender)
		{
			ActivateModifier();
			int roll = RollRequiredToHitWithoutModifiers(defender);
			DeactivateModifier();
			return roll;
		}


        public int RollRequiredToHitWithoutModifiers(HeroSystemCharacter defender)
		{
			if (defender != Defender)
			{
				Defender = defender;
			}


			int concealmentModifier = ConcealmentModifier(); ;
			int rangeModifer = 0;
			if (Ranged)
			{
				rangeModifer = Attacker.CharacterSenses.RangeModifierToTarget;
			}

			int result = Attacker.OCV.CurrentValue - defender.DCV.CurrentValue + 11 + rangeModifer - concealmentModifier;
			return result;

		}
		private int ConcealmentModifier()
		{
			int concealmentModifier = 0;

			if (Ranged)
			{
				ProtectingCover concealment = MapFactory.ActiveGameMap.GetConcealmentForCharacterBetweenOtherCharacter(
					Defender, Attacker);
				if (concealment != null)
				{
					ConcealmentAmount coverage = concealment.BlockingCoverProvidedAgainstOtherCharacter(Defender, Attacker);
					concealmentModifier = (int)coverage;
				}
			}
			else
			{
				concealmentModifier = 0;
			}
			return concealmentModifier;
		}
		private bool canPerciveDefender()
		{
			return Attacker.CharacterSenses.CanDetermineLocationOfTarget();
		}
		public virtual HitStatus HitStatus { get; set; } = HitStatus.NotSet;
		private bool defenderIsCompletyConcealedBehindBlockingCover()
		{
			return MapFactory.ActiveGameMap.IsTargetCompletelyBlockedBehindCover(Attacker, Defender);
			
		}
		#endregion

		#region Damage Defender
		public Damage _damage;
		public virtual Damage Damage
		{
			get { return _damage; }
		}
		public virtual int DamageDiceNumber
		{
			set { Damage.DamageDiceNumber = value; }
			get { return Damage.DamageDiceNumber; }
		}
		public virtual AttackResult RollDamageAndKnockbackAndApplyDamageToDefender()
		{
			Result.HitResult = HitResult.Hit;
	 
			DamageAmount damageAmount = RollDamage();
			Dictionary<CharacterStateType, HeroCharacterState> statesIncurredFromDamage = Defender.TakeDamage(damageAmount);

			Result.Results = statesIncurredFromDamage;
			Result.DamageResult = damageAmount;
			KnockbackResult knockbackResults = KnockBackCharacter(Defender, damageAmount, null);
			Result.KnockbackResults = knockbackResults;
			//AnimateAttackResults(damage);
			return Result;
		}
		public virtual DamageAmount RollDamage()
		{
			DamageAmount damageAmount = Damage.RollDamage();
			return damageAmount;
		}
		public virtual KnockbackResult KnockBackCharacter(HeroSystemCharacter defender,  DamageAmount attackDamage, object collisionInfo= null)
		{
			int knockback = 0;
			if (attackDamage.Type == DamageType.Normal)
			{
				knockback = attackDamage.BOD - new DicePool(2).Roll();
			}
			else
			{
				knockback = attackDamage.BOD - new DicePool(3).Roll();
			}
			 

			KnockbackResult results = new KnockbackResult();
			double damageMultiplier = 0;
			results.Knockback = knockback;
			if (results.Knockback > 0)
			{
				defender?.AddState(CharacterStateType.Prone);
				if (collisionInfo != null)
				{
					/**
					if (collisionInfo.Type == KnockbackCollisionType.Wall)
					{
						damageMultiplier = 1;
					}
					else {
						if (collisionInfo.Type == KnockbackCollisionType.Floor)
						{
							damageMultiplier = .5;
						}
						else
						{
							if (collisionInfo.Type == KnockbackCollisionType.Air)
							{
								damageMultiplier = 0;
							}
						}
					} 
					**/
				  }
				
				else
				{
					damageMultiplier = .5;
				}

				NormalDamageDicePool knockbackDice = new NormalDamageDicePool((int)Math.Round(results.Knockback * damageMultiplier));
				knockbackDice.Roll();
				DamageAmount knockBackDamage = knockbackDice.DamageResult;
				defender?.TakeDamage(knockBackDamage);
				results.Damage = knockBackDamage;
				results.Result = KnockbackResultType.KnockBacked;
			}
			else
			{
				results.Result = KnockbackResultType.None;
			}
			return results;

		}
		
		#endregion 

		/**
		private AttackResult AnimateAttackResults(DamageAmount damage)
		{
			if (Character.TableTopCharacter != null)
			{
				//package instructions so that tabletop can render the attack cycle and tell us if the target hit anything due to knockback
				AnimatedAttack attack = (AnimatedAttack)Character.TableTopCharacter.GetAbility(ManueverName);
				AttackInstructions instructions = Character.TableTopCharacterRepository.NewAttackInstructions();
				
				instructions.attacker = HeroSystemCharacterRepository.GetInstance(null).Characters[Defender.Name].TableTopCharacter;
				if (Result.HitResult == HitResult.Hit)
				{
					instructions.AttackHit = true;
					instructions.Impacts = instructions.attacker.StatesThatHaveNotBeenRendered;
					KnockbackCollisionInfo collisionInfo = attack.PlayCompleteAttackCycle(instructions);

					KnockbackResult knockbackResults = KnockBackCharacter(Defender, damage, collisionInfo);
					Result.KnockbackResults = knockbackResults;
				}
				else
				{
					instructions.AttackHit = false;
					attack.PlayCompleteAttackCycle(instructions);
				}
				instructions.attacker.MarkAllAnimatableStatesAsRendered();
			}
			//we dont want any other states resulting from KB (eg stunned) to animate later, so mark them as already rendered
			return Result;

		} 
	**/
	}

	public class MultiAttack : WrappingAttack
	{
		public List<AttackResult> Results = new List<AttackResult>();
		public List<HeroSystemCharacter> Targets = new List<HeroSystemCharacter>();

		public MultiAttack
		(string name, HeroSystemCharacter attacker)
			: base(name, attacker)

		{
		}

		public virtual void AddTarget(HeroSystemCharacter defender)
		{
			Targets.Add(defender);
			Target(defender);
		}
		public virtual void RemoveTarget(HeroSystemCharacter defender)
		{
			Targets.Remove(defender);
		}

		public override void PerformManuever()
		{
			PerformAttackAgainstAllTargetedCharacters();
		}

		public virtual List<AttackResult> PerformAttackAgainstAllTargetedCharacters()
		{
			PhaseLength attackLength = UnderlyingAttack.PhaseActionTakes;
			UnderlyingAttack.PhaseActionTakes = PhaseLength.Zero;

			foreach (var defender in Targets)
			{
				PerformAttack(defender);
			}
			UnderlyingAttack.PhaseActionTakes = attackLength;
			return Results;
		}

		public override AttackResult PerformAttack(HeroSystemCharacter defender)
		{
			AttackResult result;
			HitStatus attackHit = HitStatus.NotSet;
			Defender = defender;
			attackHit = RollToHit();
			UnderlyingAttack.HitStatus = attackHit;
			result = UnderlyingAttack.PerformAttack(defender);
			Results.Add(result);
			return result;
		}
	}
	public class WrappingAttack : Attack
	{
		public WrappingAttack(HeroSystemCharacter character, string name) : base(name, character, DamageType.Normal, 0, DefenseType.PD, true)
		{
		}
		public virtual Attack UnderlyingAttack { get; set; }

		public override Damage Damage
		{
			get { return UnderlyingAttack.Damage; }

		}

		public override void ActivateModifier()
		{

			Modifier.Activate(Character);

			UnderlyingAttack?.ActivateModifier();

		}

		public override void Deactivate()
		{
			base.Deactivate();
			//Modifier.Deactivate(Character);

			UnderlyingAttack?.DeactivateModifier();

		}



		public override HitStatus HitStatus
		{
			get { return UnderlyingAttack.HitStatus; }
			set
			{
				UnderlyingAttack.HitStatus = value;

			}
		}

		public override HeroSystemCharacter Defender
		{
			get { return UnderlyingAttack?.Defender; }
			set { UnderlyingAttack.Defender = value; }
		}
		public WrappingAttack(string name, HeroSystemCharacter attacker) : base(name, attacker, DamageType.Normal, 0, DefenseType.PD, true)
		{
		}

		public override bool canPerform()
		{
		    if (UnderlyingAttack != null)
		    {
		        return UnderlyingAttack.canPerform();
		    }
		    return false;
		}
	}
	
	public delegate void CharacterTargetedHandler(Attack attackThatChangedtarget);
	public delegate void CharacterAttackedHandler(Attack attack);
	#endregion

	#region damage
	public enum DefenseType { PD , ED , RPD , RED  , FD};
	public class Damage
	{
		public Damage()
		{

		}
		public Damage(int damageDiceNumber, DamageType damageType, DefenseType worksAgainstDefense)
		{
			DamageDiceNumber = damageDiceNumber;
			DamageType = damageType;
			WorksAgainstDefense = worksAgainstDefense;
		}


		public HeroSystemCharacter Attacker;
		public DamageType DamageType;
		public DefenseType WorksAgainstDefense;

		public int DamageClass
		{
			get
			{
				int damageClass = 0;
				switch (DamageType)
				{
					case DamageType.Ego:
						damageClass = DamageDiceNumber * 2;
						break;
					case DamageType.Normal:
						damageClass = DamageDiceNumber;
						break;
					case DamageType.Killing:
						damageClass = DamageDiceNumber * 3;
						break;
					case DamageType.NND:
						damageClass = DamageDiceNumber * 2;
						break;
				}
				return damageClass;
			}
			set
			{
				DamageDiceNumber = determineDamageDiceFromDamageClass(value);

			}
		}
		private int _damageDiceNumber;
		public virtual int DamageDiceNumber
		{
			get
			{

				if (Attacker != null)
				{
					int damageDiceBonus = 0;
					int damageClassBonus = Attacker.DamageClassModifier;
					if (damageClassBonus != 0)
					{
						damageDiceBonus = determineDamageDiceFromDamageClass(damageClassBonus);
					}
					return _damageDiceNumber + damageDiceBonus;

				}
				return _damageDiceNumber;
			}
			set
			{
				_damageDiceNumber = value;

			}
		}
		private int determineDamageDiceFromDamageClass(int damageClass)
		{
			int damagedice = 0;
			switch (DamageType)
			{
				case DamageType.Ego:
					damagedice = damageClass / 2;
					break;
				case DamageType.Normal:
					damagedice = damageClass;
					break;
				case DamageType.Killing:
					damagedice = damageClass / 3;
					break;
				case DamageType.NND:
					damagedice = damageClass / 2;
					break;
			}
			return damagedice;

		}

		public DamageAmount RollDamage()
		{
			DamageDicePool pool = DicePoolRepository.Instance.LoadDicePool(DamageType, DamageDiceNumber);
			//to do: Damage roll and take get pushed down into an PerformAttack class MAYBE
			pool.Roll();
			DamageAmount damage = pool.DamageResult;
			damage.WorksAgainstDefense = WorksAgainstDefense;
			return damage;
		}
		public Damage Clone()
		{
			Damage clone = new Damage(DamageDiceNumber, DamageType, WorksAgainstDefense);
			return clone;
		}
	}
	public class DamageAmount
	{
		public int BOD = 0;
		public int STUN = 0;
		public DefenseType WorksAgainstDefense = DefenseType.PD;
		public DamageType Type = DamageType.Normal;

		public DamageAmount Clone()
		{
			DamageAmount clone=  new DamageAmount();
			
			clone.BOD = BOD;
			clone.STUN = STUN;
			clone.WorksAgainstDefense = WorksAgainstDefense;
			clone.Type = Type;
			return clone;
		}
	}
	public enum DamageType { Normal = 1, Killing = 2, Ego = 3, NND = 4,
		SenseAffecting
	}
	#endregion
	
	#region AttackResults
	public class AttackResult
	{
		public HitResult HitResult;
		public DamageAmount DamageResult = new DamageAmount();
		public Dictionary<CharacterStateType, HeroCharacterState> Results = new Dictionary<CharacterStateType, HeroCharacterState>();

		public KnockbackResult KnockbackResults;
		public HeroSystemCharacter Target { get; set; }
	}
	public enum HitResult
	{
		Hit = 1, Miss = 2, Blocked,
		FocusHeld,
		Grabbed,
		EscapedWithCasualStrength,
		NotInControl,
		GrabbedFocus,
		RolledWithThePunch
	}
	public enum HitStatus { NotSet = 0, Hit = 1, Miss = 2, Blocked,
		RolledWithThePunch
	}
	public enum HitResultType { Stunned = 1, Unconsious = 2, Dead = 3, Dying = 4 };
	

	public enum KnockbackResultType { KnockBacked = 1, KnockedDown = 2, None = 3, BreakFall = 4 };
	public class KnockbackResult
	{
		public KnockbackResultType Result = KnockbackResultType.None;
		public int Knockback;
		public DamageAmount Damage;
	}
	#endregion

	#region attack implementation
	
	public enum AttackType { HTH, Ranged }

	public class Strike : Attack
	{
		public Strike(HeroSystemCharacter attacker) : base("Strike", attacker, DamageType.Normal, attacker.STR.CurrentValue / 5, DefenseType.PD, false)
		{ }

		public override Damage Damage
		{
			get
			{
				base.Damage.DamageDiceNumber = Attacker.STR.CurrentValue / 5;
				return base.Damage;
			}
		}
		public override int DamageDiceNumber
		{
			get { return Attacker.STR.CurrentValue / 5; }
			set { }
		}
		public override AttackResult RollDamageAndKnockbackAndApplyDamageToDefender()
		{
			Damage.DamageDiceNumber = DamageDiceNumber;
			return base.RollDamageAndKnockbackAndApplyDamageToDefender();
		}
	}
	public class HandKillingAttack : Attack
	{
		private int _killingDice = 0;
		public HandKillingAttack(HeroSystemCharacter attacker, int damageDiceNumber, DefenseType defenseType) : base(
				"Hand Killing PerformAttack", attacker, DamageType.Killing, damageDiceNumber,
				defenseType, false)
		{
			_killingDice = damageDiceNumber;
			if (Damage.WorksAgainstDefense == DefenseType.PD)
			{
				Damage.WorksAgainstDefense = DefenseType.RPD;
			}
			if (Damage.WorksAgainstDefense == DefenseType.ED)
			{
				Damage.WorksAgainstDefense = DefenseType.RED;
			}

		}

		public override int DamageDiceNumber
		{
			get
			{
				int extraDice = Attacker.STR.CurrentValue / 15;
				if (extraDice > _killingDice)
				{
					extraDice = _killingDice;

				}
				return extraDice + _killingDice;
			}
		}
	}
	public class Disarm : Attack
	{
		public Disarm(HeroSystemCharacter attacker) :
			base("Disarm", attacker, DamageType.Normal, 0, DefenseType.PD, false)
		{
			Type = ManueverType.Defensive;
		}

		private Focus _targetedFocus;
		public Focus TargetedFocus
		{
			get { return _targetedFocus; }
			set
			{
				if (Defender.HeldFoci.Contains(value))
				{
					if (value.FocusType == FocusType.IIF || value.FocusType == FocusType.OIF)
					{
						throw new Exception("cannot disarm an inacessable focus");
					}
					else
					{
						_targetedFocus = value;
						if (_targetedFocus.HandsRequired == HandsRequired.TwoHanded)
						{
							Modifier.OCV.ModiferAmount = -2;
						}
						else
						{
							Modifier.OCV.ModiferAmount = 0;
						}
					}
				}
				else
				{
					throw new Exception("defender is not holding this focus");
				}


			}
		}
		public override void PerformManuever()
		{
			Result = new AttackResult();
			if (Defender != null)
			{
				if (HitStatus == HitStatus.NotSet)
				{
					HitStatus = RollToHit();
				}
			}
			if (HitStatus == HitStatus.Hit)
			{
				Result.HitResult = HitResult.Hit;
				bool disarmed = AttemptDisarm();
				if (disarmed)
				{
					SendTargetedFocusFlying();
					if (Defender.ActivePhase == Attacker.CombatSequence.InterruptedPhase)
					{
						Attacker.CompleteActivePhase();
						Defender.CompleteActivePhase();
					}
					//Attacker.ActivePhase
				}
				else
				{
					Result.HitResult = HitResult.FocusHeld;
				}

			}
			else
			{
				if (HitStatus == HitStatus.Miss)
				{
					Result.HitResult = HitResult.Miss;
				}
				else
				{
					if (HitStatus == HitStatus.Blocked)
					{
						Result.HitResult = HitResult.Blocked;
					}
				}
			}
			HitStatus = HitStatus.NotSet;
			Character.ActiveManuever = null;
		}

		private void SendTargetedFocusFlying()
		{
			// throw new NotImplementedException();
		}

		private bool AttemptDisarm()
		{
			if (Attacker.STR.RollAgainst(Defender))
			{
				TargetedFocus.Drop();
				return true;
			}
			return false;
		}
	}

	public enum GrabFollowUps { Squeeze, Throw }
	public class Grab : Attack
	{
		public Grab(HeroSystemCharacter attacker) :
			base("Grab", attacker, DamageType.Normal, 0, DefenseType.PD, false)
		{
			Modifier.DCV.ModiferAmount = -2;
			Modifier.OCV.ModiferAmount = -1;
			HoldingPenalty.DCV.Multiplier = .5;
			BeingHeldPenalty.DCV.Multiplier = .5;
			DurationUnit =DurationUnit.Phase;
		}

		#region Grab Focus From Defender
		private Focus _targetedFocus;
		public Focus TargetedFocus
		{
			get { return _targetedFocus; }
			set
			{
				if (Defender.HeldFoci.Contains(value))
				{
					if (value.FocusType == FocusType.IIF || value.FocusType == FocusType.OIF)
					{
						throw new Exception("cannot disarm an inacessable focus");
					}
					else
					{
						_targetedFocus = value;
						Modifier.OCV.ModiferAmount += -2;
					}
				}
				else
				{
					throw new Exception("defender is not holding this focus");
				}


			}
		}
		private void grabFocus()
		{
			if (Defender.STR.RollCasualStrengthAgainst(EffectiveSTR / 5) == false)
			{
				
				Defender.HeldFoci.Remove(TargetedFocus);
				Attacker.HeldFoci.Add(TargetedFocus);
				Result.HitResult = HitResult.GrabbedFocus;
				DurationUnit = DurationUnit.Segment;
				
			}
			else
			{
				Result.HitResult = HitResult.NotInControl;
				DurationUnit = DurationUnit.Continuous;
				Grabbed.GrabbedBy = Grabber;
				Grabbed.GrabbedBy = Attacker;
			}
		}
		#endregion

		#region Grab Defender and squeeze or throw 


		public bool UsingOneHand { get; set; }
		public int EffectiveSTR
		{
			get
			{

				if (UsingOneHand == true)
				{

					return Attacker.STR.CurrentValue - 5;
				}
				return Attacker.STR.CurrentValue;
			}
		}

		public HeroSystemCharacter Grabbed
		{
			get { return Defender; }
		}
		public HeroSystemCharacter Grabber
		{
			get { return Attacker; }
		}
		public GrabFollowUps FollowUp
		{ get; set; }
		public override AttackResult RollDamageAndKnockbackAndApplyDamageToDefender()
		{
			performGrab();


			return Result;
		}
		private void performGrab()
		{
			if (TargetedFocus != null)
			{
				grabFocus();
			}
			else
			{
				Damage.DamageDiceNumber = DamageDiceNumber;
				grabAndSqueezeOrThrow();
			}
		}
		private void grabAndSqueezeOrThrow()
		{
			if (Defender.STR.RollCasualStrengthAgainst(EffectiveSTR / 5) == false)
			{
				if (FollowUp == GrabFollowUps.Squeeze || FollowUp == GrabFollowUps.Throw)
				{
					DamageAmount damage = Damage.RollDamage();
					Dictionary<CharacterStateType, HeroCharacterState> statesIncurredFromDamage =
						Defender.TakeDamage(damage);
				}
				if (FollowUp == GrabFollowUps.Squeeze)
				{
					HoldOntoGrabee();
					Modifier.Deactivate(Grabber);
					
					HoldingPenalty.Activate(Grabber);
					BeingHeldPenalty.Activate(Grabbed);
				}
			}
			else
			{
				Result.HitResult = HitResult.EscapedWithCasualStrength;
			}
		}
		private void HoldOntoGrabee()
		{
			DurationUnit = DurationUnit.Continuous;
			Grabbed.GrabbedBy = Grabber;
			Result.HitResult = HitResult.Grabbed;
			Grabbed.GrabbedBy = Attacker;
			Phase.PhaseStartHandler += new SequenceEventHandler(GrabbedCharacterRollCasualStrengthToEscapeOnPhaseStart);
			Attack.TargetingHandler += new CharacterTargetedHandler(ChangeOCVPenaltyOfGrabbedOrGrabberOnTargetChange);
		   
  
		}
		#endregion

		#region apply CV Modifers to Grabber and Grabbed Character
		public ManueverModifier HoldingPenalty = new ManueverModifier();
		public ManueverModifier BeingHeldPenalty = new ManueverModifier();
		public override bool canPerform()
		{
			if (Defender != null)
			{
				GameHex AttackerHex = Attacker.Hex;
				GameHex DefenderHex = Defender.Hex;
				if (AttackerHex.DistanceFrom(DefenderHex) == 0)
				{
					if (Attacker.TimesHumanSize + 1 < Defender.TimesHumanSize)
					{
						return false;
					}
					else
					{
						return true;
					}

				}
				return false;
			}
			return false;
		}
		public override int DamageDiceNumber
		{
			get { return Attacker.STR.STRDamage; }
			set { }
		}
		public void ChangeOCVPenaltyOfGrabbedOrGrabberOnTargetChange(Attack attackWithTargetChanged)
		{
			
			if (attackWithTargetChanged?.Attacker == Grabbed)
			{
				ApplyCorrectOCVPenaltyToGrabbedBasedOnWhoHeIsTargetingAndSTR();
			}
			if (attackWithTargetChanged?.Attacker == Grabber)
			{
				ApplyCorrectOCVPenaltyToGrabberBasedOnWhoHeIsTargeting(attackWithTargetChanged);
			}


		}
		private void ApplyCorrectOCVPenaltyToGrabbedBasedOnWhoHeIsTargetingAndSTR()
		{
			HeroSystemCharacter targetedByGrabbed = ((Attack)Grabbed.ActiveManuever).Defender;
			BeingHeldPenalty.Deactivate(Grabbed);
			if (targetedByGrabbed == Grabber)
			{
				
				if (Grabbed.STR.CurrentValue == Grabber.STR.CurrentValue + 20)
				{
					BeingHeldPenalty.OCV.ModiferAmount = 0;
					BeingHeldPenalty.OCV.Multiplier = 0;
				}
				else
				{
					BeingHeldPenalty.OCV.ModiferAmount = -3;
					BeingHeldPenalty.OCV.Multiplier = 0;
				}

			}
			else
			{
				if (Grabbed.STR.CurrentValue == Grabber.STR.CurrentValue + 20)
				{
					BeingHeldPenalty.OCV.ModiferAmount = -3;
					BeingHeldPenalty.OCV.Multiplier = 0;

				}
				else
				{
					BeingHeldPenalty.OCV.ModiferAmount = 0;
					BeingHeldPenalty.OCV.Multiplier = .5;
					BeingHeldPenalty.DCV.Multiplier = .5;

				}
			}
			BeingHeldPenalty.Activate(Grabbed);

		}
		private void ApplyCorrectOCVPenaltyToGrabberBasedOnWhoHeIsTargeting(Attack attackWithTargetChanged)
		{
			HoldingPenalty.Deactivate(Grabber);
			if (attackWithTargetChanged.Defender != Grabbed)
			{
				HoldingPenalty.OCV.Multiplier = .5;
			}
			else if (attackWithTargetChanged.Defender == Grabbed)
			{
				HoldingPenalty.OCV.ModiferAmount = 0;
			}
			HoldingPenalty.Activate(Grabber);

		}
		#endregion

		#region Escape from Grab
		public void GrabbedCharacterRollCasualStrengthToEscapeOnPhaseStart(object sender)
		{
			Phase startingPhase = sender as Phase;
			if (startingPhase.Character == Defender)
			{
				if (Grabbed.STR.RollCasualStrengthAgainst(Attacker) == true)
				{
					Escape();
				}

			}    
		}
		public void Escape()
		{
		   BeingHeldPenalty.Deactivate(Grabbed);
		   HoldingPenalty.Deactivate(Grabber);         
		   Grabbed.GrabbedBy = null;

			Phase.PhaseStartHandler -= new SequenceEventHandler(GrabbedCharacterRollCasualStrengthToEscapeOnPhaseStart);
		   Attack.TargetingHandler -= new CharacterTargetedHandler(ChangeOCVPenaltyOfGrabbedOrGrabberOnTargetChange);
		   Attacker.ActiveManuever = null;
		   DurationUnit = DurationUnit.Phase;

		}
		#endregion
	}
		
	public class PullFocusAwayFromEnemy : Manuever {

	   
		public PullFocusAwayFromEnemy(HeroSystemCharacter character) :
			base(ManueverType.CombatManuever, "Pull Focus Away", character, true)
		{
		}

		public override bool canPerform()
		{
			return FocusBeingFoughtOver != null;
		}

		public override void PerformManuever()
		{
			Focus focus = FocusBeingFoughtOver;
			if (Enemy.STR.RollCasualStrengthAgainst(Character) == false)
			{
				if (Enemy.HeldFoci.Contains(focus))
				{
					Enemy.HeldFoci.Remove(focus);
				}
				if (Character.HeldFoci.Contains(focus) == false)
				{
					Character.HeldFoci.Add(focus);
				}
				if (Enemy.IsGrabbed)
				{
					Character.ActiveManuever.DeactivateModifier();
					Enemy.GrabbedBy = null;
				}
				if (Character.IsGrabbed)
				{
					Enemy.ActiveManuever.DeactivateModifier();
					Character.GrabbedBy = null;
				}

			}  

		}

		public HeroSystemCharacter Enemy
		{
			get
			{
				Grab grab = null;
				if (Character.IsGrabbed)
				{
					return Character.GrabbedBy;

				}
				else if (Character.ActiveManuever.GetType() == typeof(Grab))
				{
					return ((Grab)Character.ActiveManuever).Grabbed;
				}
				return null;
			}
		}

		public Focus FocusBeingFoughtOver {
			get
			{
				Grab grab = null;
				if (Character.IsGrabbed)
				{
					grab = (Grab)Character.GrabbedBy.ActiveManuever;

				}
				else if (Character.ActiveManuever?.GetType() == typeof(Grab))
				{
					grab = (Grab)Character.ActiveManuever;
				}

				return grab?.TargetedFocus;
			}
		}
	}
	public class Squeeze : Attack
	{
		public Grab Grab;
		public Squeeze(HeroSystemCharacter attacker) : 
			base("Squeeze", attacker, DamageType.Normal, 0, DefenseType.PD, false)
		{
		}

		public override int DamageDiceNumber
		{
			get { return Attacker.STR.STRDamage; }
			set { }
		}



		public override HeroSystemCharacter Defender
		{
			get { return Grab?.Defender; }
			set { }
		}

		public override void PerformManuever()
		{

		   base.PerformManuever();
			Character.ActiveManuever = Character.Manuevers["Grab"] as Manuever;

		}

		public override AttackResult RollDamageAndKnockbackAndApplyDamageToDefender()
		{
			Damage.DamageDiceNumber = DamageDiceNumber;
			return base.RollDamageAndKnockbackAndApplyDamageToDefender();
		}
        

	}
	public class Throw : Squeeze
	{
		public Throw(HeroSystemCharacter attacker) :
			base(attacker)
		{
			Name = "Throw";
		}


	}
	public class GrabBy : Manuever
	{
		private HeroSystemCharacter heroSystemCharacter;

		public GrabBy(HeroSystemCharacter character):base(ManueverType.CombatManuever, "Grab By", character)
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

	public class Haymaker : WrappingAttack
	{
		private HeroSystemCharacter heroSystemCharacter;

		public Haymaker(HeroSystemCharacter character) : base("Haymaker", character)
		{
			DurationUnit = DurationUnit.Phase;
			Modifier.DamageModifier.ModiferAmount = 4;
		}


		public override void PerformManuever()
		{
			SequenceTimer timer = new SequenceTimer
			(DurationUnit.Segment, 1, Attacker.ActivePhase.SegmentPartOf.Sequence
				, Timing.End);

			timer.TimerAction += LandHaymaker;
			timer.StartTimer();
			Character.ManueverInProgess = this;

		}


		private void LandHaymaker(Object sender)
		{
			  
			UnderlyingAttack.PerformManuever();
			Character.ManueverInProgess = null;


		}
	}

	public class MoveBy : Attack
	{
		private HeroSystemCharacter heroSystemCharacter;

		public MoveBy(HeroSystemCharacter character) : base( "Move By", character, DamageType.Normal, 0, DefenseType.PD)
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

	public class MoveThrough : Attack
	{

		public MoveThrough(HeroSystemCharacter character) : base( "Move Through", character,DamageType.Normal, 0,DefenseType.PD)
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

	public class Set : Manuever
	{
		private Brace Brace;
		private HeroSystemCharacter heroSystemCharacter;
		public Set(HeroSystemCharacter character) : base(ManueverType.CombatManuever, "Set", character)
		{
			Modifier.OCV.ModiferAmount = 1;
			PhaseActionTakes = PhaseLength.Full;
			DurationUnit = DurationUnit.Continuous;
		}

		public static HeroSystemCharacter Target { get; set; }

		public bool IsBracing
		{
			get { return Brace != null; }
			set
			{
				if (value == true)
				{
					Brace = Character.Manuevers["Brace"] as Brace;
				}
				else
				{
					Brace = null;
				}
			}
		}

		public override bool canPerform()
		{
			return true;
		}
		public override void PerformManuever()
		{
			Brace?.Perform();
			Character.IsPerformingSet = true;
			Attack.TargetingHandler += new CharacterTargetedHandler(RemoveSetIfNotUsingRangedAttack);
			Manuever.ManueverPerformingHandler += new ManueverPerformingHandler(RemoveSetIfNotAimingOrAttackingThisPhase);

		}

		public void RemoveSetIfNotAimingOrAttackingThisPhase(Manuever manuever)
		{
			if (manuever.GetType() != typeof(Attack))
			{
				Deactivate();
			}
		}

		public void RemoveSetIfNotUsingRangedAttack(Attack attackWithTargetChanged)
		{
			bool deactivate = attackWithTargetChanged.Ranged != true
							  || attackWithTargetChanged.Defender != Target;
			if (deactivate)
			{
				Deactivate();
			}
		}

		public override void Deactivate()
		{
			DeactivateModifier();
			Attack.TargetingHandler -= new CharacterTargetedHandler(RemoveSetIfNotUsingRangedAttack);
			Manuever.ManueverPerformingHandler -= new ManueverPerformingHandler(RemoveSetIfNotAimingOrAttackingThisPhase);


		}


	}

	public class BlazingAway : MultiAttack
	{

		public BlazingAway(HeroSystemCharacter character) : base("Blazing Away", character)
		{
		}

		public override int RollRequiredToHitDefender
		{
			get
			{
				return 3;

			}

		}

	}

	public class Club : WrappingAttack
	{
		public Club(HeroSystemCharacter attacker) : base("Club", attacker)
		{
			_clubDamage.DamageType = DamageType.Normal;
		}

		private Damage _clubDamage = new Damage();

		public override Damage Damage => _clubDamage;


		public override Attack UnderlyingAttack
		{
			get { return base.UnderlyingAttack; }
			set
			{
				base.UnderlyingAttack = value;
				_clubDamage.WorksAgainstDefense = base.UnderlyingAttack.Damage.WorksAgainstDefense;
				_clubDamage.DamageClass = base.UnderlyingAttack.Damage.DamageClass;

			}


		}
	}

	class RapidFire : RapidAttack
	{
		public RapidFire(HeroSystemCharacter character) : base(character, AttackType.Ranged, "Rapid Fire")
		{
            
		}
	}

	class Sweep : RapidAttack
	{
		public Sweep(HeroSystemCharacter character) : base(character, AttackType.HTH, "Sweep")
		{
		}

		public override bool canPerform()
		{
			if (UnderlyingAttack.GetType() == typeof(MoveBy) || UnderlyingAttack.GetType() == typeof(Haymaker))
			{
				return false;
			}
			return base.canPerform();
		}

		public override void AddTarget(HeroSystemCharacter defender)
		{
			if (UnderlyingAttack.GetType() == typeof(MoveThrough))
			{
				HeroSystemCharacter lastTargeted = Targets.LastOrDefault();
				if (lastTargeted != null)
				{
					if (defender.Hex.IsBesideOtherHex(lastTargeted.Hex) == true)
					{
						base.AddTarget(defender);
					}
				}
				else
				{
					base.AddTarget(defender);
				}
			}
			else
			{
				if (UnderlyingAttack.GetType() == typeof(Grab))
				{
					if (Targets.Count < Attacker.Limbs )
					{
						base.AddTarget(defender);
					}
				}
				else
				{
					base.AddTarget(defender);
				}
			}

		}
	}

	class RapidAttack : MultiAttack
	{
		public AttackType AttackType;
		public RapidAttack(HeroSystemCharacter character, AttackType type, string name) : base(name, character)
		{
			PhaseActionTakes = PhaseLength.Full;
			Modifier.DCV.Multiplier = .5;
			AttackType = type;


		}



		#region update OCV and DCV modifiers based on number of targets
		public override Attack UnderlyingAttack
		{
			get
			{
				return base.UnderlyingAttack;

			}
			set
			{
				if (AttackType == AttackType.Ranged)
				{
					if (value.Ranged == false) { return; }
				}
				if (AttackType == AttackType.HTH)
				{
					if (value.Ranged == true) { return; }
				}

				base.UnderlyingAttack = value;
				UnderlyingAttackBaseModifier = new ManueverModifier();
				UnderlyingAttackBaseModifier.OCV.ModiferAmount = UnderlyingAttack.Modifier.OCV.ModiferAmount;
				UnderlyingAttackBaseModifier.DCV.ModiferAmount = UnderlyingAttack.Modifier.DCV.ModiferAmount;
			}

		}
		public override void AddTarget(HeroSystemCharacter defender)
		{
			base.AddTarget(defender);
			updateOCVAndDCVBonusBasedOnTargeted();
		}
		public override void RemoveTarget(HeroSystemCharacter defender)
		{
			base.RemoveTarget(defender);
			updateOCVAndDCVBonusBasedOnTargeted();
		}
		private void updateOCVAndDCVBonusBasedOnTargeted()
		{

			resetUnderlyingAttackModifersToZero();
			applyRapidFireOCVPenaltyAndUnderlyingAttackCVPenaltiesForEachTarget();
			applyUnderlyingAttackCVBonusOnce();
		}
		private void resetUnderlyingAttackModifersToZero()
		{
			UnderlyingAttack.Modifier.OCV.ModiferAmount = 0;
			UnderlyingAttack.Modifier.DCV.ModiferAmount = 0;
		}
		private void applyUnderlyingAttackCVBonusOnce()
		{
			if (UnderlyingAttackBaseModifier.OCV.ModiferAmount > 0)
			{
				UnderlyingAttack.Modifier.OCV.ModiferAmount += UnderlyingAttackBaseModifier.OCV.ModiferAmount;
			}
			if (UnderlyingAttackBaseModifier.DCV.ModiferAmount > 0)
			{
				UnderlyingAttack.Modifier.DCV.ModiferAmount += UnderlyingAttackBaseModifier.DCV.ModiferAmount;
			}
		}
		private void applyRapidFireOCVPenaltyAndUnderlyingAttackCVPenaltiesForEachTarget()
		{
			Modifier.OCV.ModiferAmount = 0;
			Modifier.DCV.ModiferAmount = 0;
			foreach (var defender in Targets)
			{
				Modifier.OCV.ModiferAmount -= 2;
				if (UnderlyingAttackBaseModifier.OCV.ModiferAmount < 0)
				{
					UnderlyingAttack.Modifier.OCV.ModiferAmount += UnderlyingAttackBaseModifier.OCV.ModiferAmount;
				}
				if (UnderlyingAttackBaseModifier.DCV.ModiferAmount < 0)
				{
					UnderlyingAttack.Modifier.DCV.ModiferAmount += UnderlyingAttackBaseModifier.DCV.ModiferAmount;
				}
			}
			UnderlyingAttack.Modifier.DCV.Multiplier = Modifier.DCV.Multiplier;
		}
		public ManueverModifier UnderlyingAttackBaseModifier = new ManueverModifier();
		#endregion

		#region attack all targets until first miss and choose most severe knockback per target
		public override List<AttackResult> PerformAttackAgainstAllTargetedCharacters()
		{
			PhaseLength attackLength = UnderlyingAttack.PhaseActionTakes;
			UnderlyingAttack.PhaseActionTakes = PhaseLength.Zero;

			performAttackWithCumulativeMissesAndRemoveKnockbackTemporarily();
			InflictKnockbackDamageWithGreatestDistanceToEachTarget();

			UnderlyingAttack.PhaseActionTakes = attackLength;
			return Results;
		}
		private void performAttackWithCumulativeMissesAndRemoveKnockbackTemporarily()
		{
			bool missedLastAttack = false;
			foreach (var defender in Targets)
			{
				AttackResult result;
				if (missedLastAttack == true)
				{
					addMissToAttackResults(defender);
				}
				else
				{
					result = PerformAttack(defender);
					if (result.HitResult == HitResult.Miss)
					{
						missedLastAttack = true;
					}
					else
					{
						removeKnockbackDamageFromAttackForNow(defender, result);
					}
				}
			}
		}
		private void InflictKnockbackDamageWithGreatestDistanceToEachTarget()
		{
			var knockbacks = from x in Results select new { KnockbackResults = x.KnockbackResults, Target = x.Target };

			var results = from k in knockbacks
						  group k by k.Target
				into g
						  select new { target = g.Key, TargetKnockbackResults = g.ToList() };

			foreach (var result in results)
			{
				List<KnockbackResult> targetSpecificKnockbackResults = (from x in result.TargetKnockbackResults
																		orderby x.KnockbackResults?.Knockback
																		select x.KnockbackResults).ToList();

				if (targetSpecificKnockbackResults.Count > 0 && targetSpecificKnockbackResults[0] != null)
				{
					result.target.TakeDamage(targetSpecificKnockbackResults?.FirstOrDefault()?.Damage);
				}
			}
		}
		private void removeKnockbackDamageFromAttackForNow(HeroSystemCharacter defender, AttackResult result)
		{
			;
			defender.STUN.CurrentValue += result.KnockbackResults.Damage.STUN;
			defender.BOD.CurrentValue += result.KnockbackResults.Damage.BOD;
		}
		private void addMissToAttackResults(HeroSystemCharacter defender)
		{
			AttackResult result;
			result = new AttackResult();
			result.HitResult = HitResult.Miss;
			result.Target = defender;
			Results.Add(result);
		}
		#endregion
	}

	public class Cover : WrappingAttack, Interruption
	{

		public Cover(HeroSystemCharacter character) : base("Cover", character)
		{
		}

		public override ManueverModifier Modifier => UnderlyingAttack.Modifier;

		public override AttackResult RollDamageAndKnockbackAndApplyDamageToDefender()
		{
			Result.HitResult = HitResult.Hit;
			Attacker.ManueverInProgess = this;
			Character.CoveringManuever = this;
			Character.CombatSequence.CoveringManuevers.Add(this);
			return Result;
		}

		public void Interrupt(InterruptionWith interruptionReason = InterruptionWith.Generic)
		{
			UnderlyingAttack.RollDamageAndKnockbackAndApplyDamageToDefender();
			DeactivateModifier();
			HeroSystemCharacter interruptedCharacter = Sequence.ActivePhase.Character;
			bool success = Character.DEX.RollAgainst(interruptedCharacter);


		}

		public override bool canPerform()
		{
		    if (UnderlyingAttack != null)
		    {
		        return UnderlyingAttack.CanPerform;
		    }
		    return false;
		}
	}

	class PullingAPunch : WrappingAttack
	{


		public PullingAPunch(HeroSystemCharacter character) : base("Pulling A Punch", character)
		{
		}

		public override DamageAmount RollDamage()
		{
			DamageAmount baseAmount = base.RollDamage();
			if (ToHitRoll != RollRequiredToHitDefender)
			{
				baseAmount.BOD = baseAmount.BOD / 2;
			}

			return baseAmount;


		}

		public override Attack UnderlyingAttack
		{
			get
			{
				return base.UnderlyingAttack;
			}
			set
			{
				base.UnderlyingAttack = value;
				Modifier.OCV.ModiferAmount = Damage.DamageClass / 5 * -1;

			}
		}
	}

	class WeaponManuever : Attack
	{
		public WeaponManuever(HeroSystemCharacter attacker) :
			base("Weapon Manuever", attacker, DamageType.Normal, 0, DefenseType.PD)
		{

		}

		public override bool canPerform()
		{
			if (Character.IsGrabbed)
			{
				Grab grab = Character.GrabbedBy.ActiveManuever as Grab;
				if (grab.TargetedFocus == this.Weapon)
				{
					return false;
				}
				return true;
			}
			else
			{
				if (Attacker.HeldFoci.Contains(Weapon))
				{
					return true;
				}
				return false;
			}
		}

		public Weapon Weapon { get; set; }
	}

	class SnapShot : WrappingAttack
	{
		private HeroSystemCharacter heroSystemCharacter;
		public ProtectingCover BlockingCover;
		public SnapShot(HeroSystemCharacter character) : base("Snap Shot", character)
		{
			Modifier.OCV.ModiferAmount = -1;
		}

		public override bool canPerform()
		{
			IGameMap map = MapFactory.ActiveGameMap;
			ProtectingCover cover = map.GetConcealmentForCharacterBetweenOtherCharacter(Attacker, Defender);
			if (cover != null && cover.CharacterCanPeekAroundAndSeeOtherCharacter(Attacker, Defender))
			{
				bool perform = false;
				Attacker.PeekAroundCoverToViewDefender(cover, Defender);
				if (IsAwareOfDefender == true || Character.PER.Roll())
				{
					perform = base.canPerform();
					Attacker.DuckBehindCoverHidingFrom(cover, Defender);
					return perform;
				}
				else
				{
					perform = false;
				}
			}
			return false;

		}

		public override AttackResult PerformAttack(HeroSystemCharacter defender)
		{
			IsAwareOfDefender = true;
			return base.PerformAttack(defender);
		}

		public override void Target(HeroSystemCharacter defender)
		{
			IsAwareOfDefender = true;
			base.Target(defender);
		}

		public override void PerformManuever()
		{
			BlockingCover = MapFactory.ActiveGameMap.GetConcealmentForCharacterBetweenOtherCharacter(Attacker, Defender);
			if (BlockingCover != null)
			{
				if (BlockingCover.CharacterCanPeekAroundAndSeeOtherCharacter(Attacker, Defender))
				{
					Attacker.PeekAroundCoverToViewDefender(BlockingCover, Defender);
				}
			}
			UnderlyingAttack.PerformManuever();
			SequenceTimer timer = new SequenceTimer(DurationUnit.Segment, 1, Sequence);

			timer.TimerAction += new SequenceTimerAction(DuckBehindCover);
			timer.StartTimer();
		}



		public void DuckBehindCover(SequenceTimer timer)
		{
			Attacker.DuckBehindCoverHidingFrom(BlockingCover, Defender);
			BlockingCover = null;
		}
		public void Target(HeroSystemCharacter defender, bool isAwareOfDefender = true)
		{
			IsAwareOfDefender = isAwareOfDefender;
			base.Target(defender);
		}

		public bool IsAwareOfDefender { get; set; }
	}

	class SuppressionFire : Manuever
	{
		private HeroSystemCharacter heroSystemCharacter;

		public SuppressionFire(HeroSystemCharacter character) : base(ManueverType.CombatManuever, "Suppression Fire", character)
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

    class MultiPowerAttack : Attack
    {
        public List<Attack> UnderlyingAttacks = new List<Attack>();

        public void AddAttack(Attack attack)
        {
            if(UnderlyingAttacks.Where(x=>x.Name == attack.Name).ToList().Count > 0)
            {
                return;
            }

            if (UnderlyingAttacks.Where(x => x.Ranged != attack.Ranged).ToList().Count > 0)
            {
                return;
            }


            UnderlyingAttacks.Add(attack);
        }
        public MultiPowerAttack(HeroSystemCharacter attacker) : base("MultiPowerAttack", attacker, DamageType.Normal, 0, DefenseType.PD, false)
        {
        }

        public override void PerformManuever()
        {
            PerformAAllttackAgainstTargetedCharacter();
        }

        public virtual AttackResult PerformAAllttackAgainstTargetedCharacter()
        {
            DeterminePhaseLengthFromLongestUnderlyingAttack();
            DetermIneCVModifiersFromAttackWithLowestModifier();

            List<KnockbackResult>  knockbackResults = new List<KnockbackResult>(); 
            foreach (var attack in UnderlyingAttacks)
            {
                attack.Defender = Defender;
                attack.HitStatus =HitStatus.Hit;
                attack.PerformManuever();
                AttackResult result = attack.Result;
                Result.DamageResult.STUN += result.DamageResult.STUN;
                Result.DamageResult.BOD += result.DamageResult.BOD;
                KnockbackResult kr = result.KnockbackResults;
                knockbackResults.Add(kr);
                removeKnockbackDamageFromAttackForNow(kr);
                        
            }
            InflictKnockbackDamageWithGreatestDistance(knockbackResults);
            return Result;

        }



        private void InflictKnockbackDamageWithGreatestDistance(List<KnockbackResult> knockbackResults)
        {
            KnockbackResult kr = knockbackResults.OrderByDescending(x => x.Knockback).FirstOrDefault();

            Defender.TakeDamage(kr.Damage);
            Result.KnockbackResults = kr;
        }
        private void removeKnockbackDamageFromAttackForNow(KnockbackResult KnockbackResults)
        {
            if (KnockbackResults.Damage != null)
            {
                Defender.STUN.CurrentValue += KnockbackResults.Damage.STUN - Defender.PD.CurrentValue;
                Defender.BOD.CurrentValue += KnockbackResults.Damage.BOD - Defender.PD.CurrentValue;
            }
        }

        private void DetermIneCVModifiersFromAttackWithLowestModifier()
        {
            Modifier.OCV.ModiferAmount =
                UnderlyingAttacks.OrderBy(x => x.Modifier.OCV.ModiferAmount).FirstOrDefault().Modifier.OCV.ModiferAmount;

            Modifier.DCV.ModiferAmount =
                UnderlyingAttacks.OrderBy(x => x.Modifier.DCV.ModiferAmount).FirstOrDefault().Modifier.DCV.ModiferAmount;
            Activate();
        }

        public override void Deactivate()
        {
            Modifier.DCV.ModiferAmount = 0;
            Modifier.OCV.ModiferAmount = 0;

            DeactivateModifier();
            if (Character.ManueverInProgess == this)
            {
                Character.ManueverInProgess = null;
            }
        }

        private void DeterminePhaseLengthFromLongestUnderlyingAttack()
        {
            if (UnderlyingAttacks.Where(x => x.PhaseActionTakes == PhaseLength.Full).ToList().Count > 0)
            {
                PhaseActionTakes = PhaseLength.Full;
            }
            else
            {
                PhaseActionTakes = PhaseLength.Zero;
            }

            UnderlyingAttacks.ForEach(x => x.PhaseActionTakes = PhaseLength.Zero);
        }
    }
	
	#endregion
}
