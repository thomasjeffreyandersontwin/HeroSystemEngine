using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HeroSystemEngine.Character;
using HeroSystemEngine.Dice;
using HeroSystemEngine.Manuevers;
using HeroSystemsEngine.GameMap;
using Ploeh.AutoFixture;

namespace HeroSystemsEngine.Manuevers
{
    [TestClass]
    class StandardAttackTest
    {
        public CharacterTestObjectFactory characterFactory = new CharacterTestObjectFactory();
        HeroSystemCharacter Attacker;
        HeroSystemCharacter Defender;

        [TestInitialize]
        public void TestGiven()
        {
            Defender = HeroSystemCharacterRepository.GetInstance().Characters["Default Character"];
            Attacker = HeroSystemCharacterRepository.GetInstance().LoadBaseCharacter();
        }

        [TestMethod]
        public void ClumsyAttacker_MissesDefender()
        {
            Attacker.OCV.MaxValue = 1;
            AttackResult result = Attacker.Attack("Strike", Defender);

            bool isSuccessful = result.HitResult == HitResult.Hit;
            Assert.AreEqual(false, isSuccessful);


        }
       
        [TestMethod]
        public void TestAttack_HitsOrMissesBasedOnCharacterCV()
        {
            HeroSystemCharacter attacker = new HeroSystemCharacter();
            attacker.OCV.CurrentValue = 5;
            Attack cm = new Attack( "Basic", attacker, DamageType.Normal, 0,
                DefenseType.PD);

            HeroSystemCharacter defender = new HeroSystemCharacter();
            defender.DCV.CurrentValue = 3;
            cm.Defender = defender;

            bool actualSuccess = cm.ToHitRollIsSuccessful(14);
            bool expectedSuccess = false;
            Assert.AreEqual(expectedSuccess, actualSuccess);

            actualSuccess = cm.ToHitRollIsSuccessful(13);
            expectedSuccess = true;
            Assert.AreEqual(expectedSuccess, actualSuccess);
        }


       

        [TestMethod]
        public void TestActivatingAttack_AttackIsUsedCalculatesChanceToHitOnDefender()
        {
            HeroSystemCharacter character = characterFactory.BaseCharacterWithOneCombatManuever;
            HeroSystemCharacter defender = characterFactory.BaseCharacter;

            character.OCV.CurrentValue = 5;
            defender.DCV.CurrentValue = 3;


            var cm = character.Manuevers["Basic"] as Attack;
            int tohit = cm.RollRequiredToHitWithoutModifiers(defender);

            Assert.AreEqual(13, tohit);


        }



        /**
        public void TakeAttack_PlaysAttackCycleOnTableTopCharacters()
        {
         [TestMethod]
            TestHelperFactory factory;
            HeroSystemCharacter character;
        
            //arrange
            factory = TestHelperFactory.Instance;
            factory.CreateADefaultCharacterAndAssociateItWithMockTableTopCharacter("Attacker");
            character = factory.CharacterRepositoryWithMockTableTopRepo.Characters["Attacker"];
            character.STR.MaxValue = 109;

            AnimatedAttack attack = factory.AddMockTabletopAttackToMockTabletopCharacter("Strike");

            factory.CreateADefaultCharacterAndAssociateItWithMockTableTopCharacter("Defender");
            HeroSystemCharacter defender = factory.CharacterRepositoryWithMockTableTopRepo.Characters["Defender"];
            AttackInstructions ins = factory.CreateMockAttackInstructions();
            
            //act
            CombatManuever strike = (CombatManuever)character.Manuevers[ManueverType.Strike];
            strike.Defender= defender;
            strike.RollDamageAndKnockbackAndApplyDamageToDefender();

            //assert
            factory.MockTableTopCharacterContext.Verify(t => t.AddState(AnimatableCharacterStateType.Stunned, false), Times.Once());
            factory.MockTableTopCharacterContext.Verify(t => t.AddState(AnimatableCharacterStateType.Unconsious, false), Times.Once());
            factory.MockTableTopCharacterContext.Verify(t => t.AddState(AnimatableCharacterStateType.Dead, false), Times.Once());
            Assert.AreEqual(true, ins.AttackHit); 
            factory.MockAttackContext.Verify(t => t.PlayCompleteAttackCycle(ins));
            factory.MockTableTopCharacterContext.Verify(t => t.MarkAllAnimatableStatesAsRendered());


        }
    **/


    }

    [TestClass]
    public class HTHAttackTest
    {
        public CharacterTestObjectFactory characterFactory = new CharacterTestObjectFactory();

        [TestMethod]
        public void WhenNoDefendersWithinOneHex_HTHAttackAbilityIsDisabled()
        {
            //arrange
            HeroSystemCharacter character = characterFactory.BaseCharacterWithOneCombatManuever;
            HeroSystemCharacter defender = characterFactory.BaseCharacter;
            MapFactory.ActiveGameMap.HexBesideOtherHex = false;

            //act
            character.Hex = new GameHex(1, 1, 1);
            defender.Hex = new GameHex(1, 1, 3);

            //assert
            var strike = character.Manuevers["Basic"];
            Assert.AreEqual(strike.CanPerform, false);

        }

        [TestMethod]
        public void AttackingDefenderGreaterThanOneHex_Fails()
        {
            //arrange
            HeroSystemCharacter character = characterFactory.BaseCharacterWithOneCombatManuever;
            HeroSystemCharacter defender = characterFactory.BaseCharacter;

            //act
            character.Hex = new GameHex(1, 1, 1);
            defender.Hex = new GameHex(1, 1, 3);
            var strike = character.Manuevers["Basic"] as Attack;
            strike.Defender = defender;

            //assert
            bool success = strike.Perform();
            Assert.AreEqual(success, false);
        }

    }

    [TestClass]
    public class RangeAttackTest
    {
        public CharacterTestObjectFactory characterFactory = new CharacterTestObjectFactory();

        [TestMethod]
        public void WhenDefendersIsGreaterThanOneHexAway_TheRangeCombatModiferWillBeUsedToReduceChancetoHit()
            {
            //arrange
            HeroSystemCharacter character = characterFactory.BaseCharacterWithOneRangedCombatManuever;
            HeroSystemCharacter defender = characterFactory.BaseCharacter;
            MapFactory.ActiveGameMap.HexBesideOtherHex = false;

            //act
            character.Hex = new GameHex(1, 1, 1);
            defender.Hex = new GameHex(1, 1, 12);
            var ranged = character.Manuevers["Basic Ranged"] as Attack;
            ranged.Defender = defender;

            //assert
            character.OCV.CurrentValue = 5;
            defender.DCV.CurrentValue = 3;


            int tohit = ranged.RollRequiredToHitWithoutModifiers(defender);

            Assert.AreEqual(10, tohit);

        }


       


    }

}
