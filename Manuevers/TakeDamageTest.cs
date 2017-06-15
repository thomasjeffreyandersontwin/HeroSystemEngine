using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HeroSystemEngine.Character;
using HeroSystemEngine.Dice;
using HeroSystemEngine.Manuevers;
using HeroSystemsEngine.GameMap;


namespace HeroSystemEngine
{
   
    [TestClass]
    public class CalculateNormalDamageTest
    {
        HeroSystemCharacter Attacker;
        HeroSystemCharacter Defender;
        [TestInitialize]
        public void TestGiven()
        {
            Defender = HeroSystemCharacterRepository.GetInstance().Characters["Default Character"];
            Attacker = HeroSystemCharacterRepository.GetInstance().LoadBaseCharacter();
            Attacker.STR.MaxValue = 15;
            Dice.Dice.RandomnessState = RandomnessState.average;
        }
        [TestMethod]
        public void NormalAttackHits_CalculatesNormalDamge()
        {
            AttackResult result = Attacker.Attack("Strike", Defender);

            HitResult actualHitRestult = result.HitResult;
            Assert.AreEqual(HitResult.Hit, actualHitRestult);

            int expectedStunDamageRolled = 10;
            int expectedBodyDamageRolled = 3;
            Assert.AreEqual(expectedStunDamageRolled, result.DamageResult.STUN);
            Assert.AreEqual(expectedBodyDamageRolled, result.DamageResult.BOD);

            int expectedStunLeft = 12;
            int expectedBodyLeft = 9;
//            Assert.AreEqual(expectedStunLeft, Defender.STUN.CurrentValue);
  //          Assert.AreEqual(expectedBodyLeft, Defender.BOD.CurrentValue);

        }

        [TestMethod]
        public void TestAttack_SubstractsCorrectDamageAmountBasedOnDiceAndDefense()
        {
            HeroSystemCharacter attacker = new HeroSystemCharacter();
           Attack cm = new Attack( "Basic", attacker, DamageType.Normal, 5,
                DefenseType.PD);

            HeroSystemCharacter defender = new HeroSystemCharacter();
            defender.PD.CurrentValue = 4;
            defender.STUN.CurrentValue = 35;
            defender.BOD.CurrentValue = 10;
            cm.Defender = defender;
            cm.RollDamageAndKnockbackAndApplyDamageToDefender();

            int expectedStunLeft = 35 - (cm.Result.DamageResult.STUN - defender.PD.CurrentValue);
            int expectedBodyLeft = 10 - (cm.Result.DamageResult.BOD - defender.PD.CurrentValue);

            int actualStunLeft = defender.STUN.CurrentValue;
            int actualBodyLeft = defender.BOD.CurrentValue;

            Assert.AreEqual(expectedBodyLeft, actualBodyLeft);
            Assert.AreEqual(expectedStunLeft, actualStunLeft);
        }

        
    }

    [TestClass]
    public class ApplyKnockbackTest
    {
        [TestMethod]
        public void AttackWithKnockback_DamagesCharacter()
        {
            HeroSystemCharacter attacker = new HeroSystemCharacter();
            Attack cm = new Attack("Basic", attacker, DamageType.Normal, 5,
                DefenseType.PD);


            DamageAmount attackDamage = new DamageAmount();
            attackDamage.Type = DamageType.Normal;
            attackDamage.BOD = 13;
            attackDamage.WorksAgainstDefense = DefenseType.PD;

            HeroSystemCharacter defender = new HeroSystemCharacter();
            KnockbackResult actualKnockback = cm.KnockBackCharacter(defender, attackDamage);

            KnockbackResultType actualResult = actualKnockback.Result;
            KnockbackResultType expectedResult = KnockbackResultType.KnockBacked;

            Assert.AreEqual(actualResult, expectedResult);

            bool isProne = defender.State.ContainsKey(CharacterStateType.Prone);
            Assert.AreEqual(isProne, true);

        }

 

    }

   [TestClass]
    public class ApplyDamageToCharacterTest
    {
        HeroSystemCharacter Attacker;
        HeroSystemCharacter Defender;

        [TestInitialize]
        public void TestGiven()
        {
            Defender = HeroSystemCharacterRepository.GetInstance().Characters["Default Character"];
            Attacker = HeroSystemCharacterRepository.GetInstance().LoadBaseCharacter();
            Attacker.STR.MaxValue = 15;
            Dice.Dice.RandomnessState = RandomnessState.average;
        }

        [TestMethod]
        public void TestTakeDamage_EffectsCharacterStateBasedOnAmount()
        {
            DamageAmount attackDamage = new DamageAmount();
            attackDamage.STUN = 35;
            attackDamage.BOD = 5;
            attackDamage.WorksAgainstDefense = DefenseType.PD;
            HeroSystemCharacter defender = new HeroSystemCharacter();
            defender.PD.CurrentValue = 5;
            defender.STUN.CurrentValue = 50;
            defender.BOD.CurrentValue = 10;
            defender.BOD.MaxValue = 10;
            defender.CON.CurrentValue = 29;

            defender.TakeDamage(attackDamage);

            bool actualStunned = defender.State.ContainsKey(CharacterStateType.Stunned);
            bool expectedStunned = true;
            Assert.AreEqual(actualStunned, expectedStunned);

            attackDamage = new DamageAmount();
            attackDamage.STUN = 35;
            attackDamage.BOD = 10;
            attackDamage.WorksAgainstDefense = DefenseType.PD;

            defender.TakeDamage(attackDamage);
            bool actualUnconsious = defender.State.ContainsKey(CharacterStateType.Unconsious);
            bool expectedUnconsious = true;
            Assert.AreEqual(actualUnconsious, expectedUnconsious);

            attackDamage.BOD = 11;
            defender.TakeDamage(attackDamage);
            bool actualDying = defender.State.ContainsKey(CharacterStateType.Dying);
            bool expectedDying = true;
            Assert.AreEqual(actualDying, expectedDying);

            attackDamage.BOD = 14;
            defender.TakeDamage(attackDamage);
            bool actualDead = defender.State.ContainsKey(CharacterStateType.Dying);
            bool expectedDead = true;
            Assert.AreEqual(actualDead, expectedDead);
        }



        [TestMethod]
        public void SeriousAttack_StunsDefender()
        {
            Attacker.STR.MaxValue = 20;
            AttackResult result = Attacker.Attack("Strike", Defender);

            bool isStunned = Defender.State.ContainsKey(CharacterStateType.Stunned);
            Assert.AreEqual(true, isStunned);

            isStunned = result.Results.ContainsKey(CharacterStateType.Stunned);
            Assert.AreEqual(true, isStunned);
        }

        [TestMethod]
        public void MAssiveAttack_RendersDefenderUnconsious()
        {
            Attacker.STR.MaxValue = 35;
            AttackResult result = Attacker.Attack("Strike", Defender);

            bool isStunned = Defender.State.ContainsKey(CharacterStateType.Unconsious);
            Assert.AreEqual(true, isStunned);

            isStunned = result.Results.ContainsKey(CharacterStateType.Unconsious);
            Assert.AreEqual(true, isStunned);
        }

        [TestMethod]
        public void DevastingAttack_RendersDefenderDyingAndDead()
        {
            Attacker.STR.MaxValue = 70;
            AttackResult result = Attacker.Attack("Strike", Defender);

            bool isDying = Defender.State.ContainsKey(CharacterStateType.Dying);
            Assert.AreEqual(true, isDying);

            isDying = result.Results.ContainsKey(CharacterStateType.Dying);
            Assert.AreEqual(true, isDying);

            Attacker.STR.MaxValue = 65;

            result = Attacker.Attack("Strike", Defender);

            bool isDead = Defender.State.ContainsKey(CharacterStateType.Dead);
            Assert.AreEqual(true, isDead);

            isDead = result.Results.ContainsKey(CharacterStateType.Dead);
            Assert.AreEqual(true, isDead);

            isDying = result.Results.ContainsKey(CharacterStateType.Dying);
            Assert.AreEqual(false, isDying);
        }

        


    }

    [TestClass]
    public class CalculateKillingAttackTest
    {
        public CharacterTestObjectFactory characterFactory = new CharacterTestObjectFactory();

        [TestMethod]
        public void KillingAttackHits_CalculatesKillingDamgeIgnoringNonResistantDefense()
        {
            HeroSystemCharacter character = characterFactory.BaseCharacterWithOneKillingAttackCombatManuever;
            HeroSystemCharacter defender = characterFactory.BaseCharacter;

            AttackResult result = character.Attack("Basic Killing", defender);

            int expectedStunDamageRolled = 20;
            int expectedBodyDamageRolled = 10;

            
            Assert.AreEqual(expectedStunDamageRolled, result.DamageResult.STUN);
            Assert.AreEqual(expectedBodyDamageRolled, result.DamageResult.BOD);

            int expectedStunLeft = defender.STUN.MaxValue - result.DamageResult.STUN;
            int expectedBodyLeft = defender.BOD.MaxValue - result.DamageResult.BOD; ;
            Assert.AreEqual(expectedStunLeft, defender.STUN.CurrentValue);
            Assert.AreEqual(expectedBodyLeft, defender.BOD.CurrentValue);

        }

        [TestMethod]
        public void HandKillingAttackHits_StrengthModAddedToDamgeDice()
        {
            HeroSystemCharacter attacker= characterFactory.BaseCharacterWithOneHandKillingAttackCombatManuever;

            HandKillingAttack attack = attacker.Manuevers["Hand Killing PerformAttack"] as HandKillingAttack;
            Assert.AreEqual(attack.DamageDiceNumber, 6);
        }

        [TestMethod]
        public void HandKillingAttackHitsDefenderWithResistantDefense_DefenseSubtractedFromDamge()
        {
            HeroSystemCharacter character = characterFactory.BaseCharacterWithOneKillingAttackCombatManuever;
            HeroSystemCharacter defender = characterFactory.BaseCharacter;
            defender.RPD.CurrentValue = 10;

            AttackResult result = character.Attack("Basic Killing", defender);

            int expectedStunDamageRolled = 20;
            int expectedBodyDamageRolled = 10;


            Assert.AreEqual(expectedStunDamageRolled, result.DamageResult.STUN);
            Assert.AreEqual(expectedBodyDamageRolled, result.DamageResult.BOD);

            int expectedStunLeft = defender.STUN.MaxValue - (result.DamageResult.STUN - defender.RPD.CurrentValue);
            int expectedBodyLeft = defender.BOD.MaxValue - (result.DamageResult.BOD - defender.RPD.CurrentValue); ;
            Assert.AreEqual(expectedStunLeft, defender.STUN.CurrentValue);
            Assert.AreEqual(expectedBodyLeft, defender.BOD.CurrentValue);


        }



    }
}

﻿