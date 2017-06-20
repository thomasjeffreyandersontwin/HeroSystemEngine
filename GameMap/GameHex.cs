using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeroSystemEngine.Character;
using HeroSystemsEngine.Movement;
using HeroSystemsEngine.Perception;
using Microsoft.Xna.Framework;

namespace HeroSystemsEngine.GameMap
{
    public enum ConcealmentAmount {Full=-999, Partial=-4, Half=-2, None=0}


    public interface IGameHex
    {
        float X { get; set; }
        float Y { get; set; }
        float Z { get; set; }
        float DistanceFrom(GameHex otherHex);
        Vector3 Vector { get; set; }

        bool IsBesideOtherHex(IGameHex hex);
    }

    public class GameHex :IGameHex
    {
        public bool BesideOtherHex = false;
        public GameHex(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float DistanceFrom(GameHex otherHex)
        {
            var otherVector = otherHex.Vector;
            return Vector3.Distance(Vector, otherVector);
        }


        public Vector3 Vector
        {
            get { return new Vector3(X, Y, Z); }
            set
            {
                X = value.X;
                Y = value.Y;
                Z = value.Z;

            }

        }

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public bool IsBesideOtherHex(IGameHex hex)
        {
            return BesideOtherHex;
        }
    }

    public interface IGameMap
    {
        bool OtherCharactersBeside(GameHex Hex);
        ProtectingCover GetConcealmentForCharacterBetweenOtherCharacter(HeroSystemCharacter attacker, ITargetable defender);


        List<SightPerceptionModifiers> SightConditions { get; }
    }
    public class GameMapStub:IGameMap
    {
        public GameMapStub()
        {
            SightConditions = new List<SightPerceptionModifiers>();
        }
        public ProtectingCover ProtectingCover
        {
            get;
            set;
        }
        public bool HexBesideOtherHex;
        public bool BehindCover;
        public bool BarrierBetweenHexes;
            

        public bool OtherCharactersBeside(GameHex Hex)
        {
            return HexBesideOtherHex;
        }

        public ProtectingCover GetConcealmentForCharacterBetweenOtherCharacter(HeroSystemCharacter attacker, ITargetable defender)
        {
            return ProtectingCover;
        }

        public bool IsTargetCompletelyBlockedBehindCover(HeroSystemCharacter viewer, ITargetable target)
        {
            ProtectingCover cover = MapFactory.ActiveGameMap.GetConcealmentForCharacterBetweenOtherCharacter(viewer, target);

            if (cover != null)
            {
                ConcealmentAmount coverage = cover.BlockingCoverProvidedAgainstOtherCharacter(viewer, target);

                if (coverage == ConcealmentAmount.Partial || coverage == ConcealmentAmount.None)
                {
                    {

                        return false;
                    }
                }
                else
                {
                    {

                        return true;
                    }
                }
            }
            return false;
        }


        public List<SightPerceptionModifiers> SightConditions { get; }

        public void MoveGameObject(HeroSystemCharacter character, int velocity, YawHexFacing yaw, PitchHexFacing pitch)
        {
            
        }
    }

    public interface ProtectingCover
    {
        bool CharacterCanPeekAroundAndSeeOtherCharacter(HeroSystemCharacter attacker, HeroSystemCharacter defender);
        void UpdateAmountOfConcealmentBeingProvidedToCharacterUnderCoverFromOtherCharacter(ConcealmentAmount concealmentAmount, HeroSystemCharacter characterBehindCover, HeroSystemCharacter other);

        ConcealmentAmount BlockingCoverProvidedAgainstOtherCharacter(HeroSystemCharacter viewingCharacter,
            ITargetable coveredCharacter);
    }

    public class ProtectingCoverStub : ProtectingCover
    {
        public bool CanPeek = true;
        public ConcealmentAmount ConcealmentAmount = ConcealmentAmount.None;

        public ConcealmentAmount BlockingCoverProvidedAgainstOtherCharacter(HeroSystemCharacter viewingCharacter, ITargetable coveredCharacter)
        {
            if(coveredCharacter?.Name=="attacker")
            { return ConcealmentAmount; }
            else
            {
                return ConcealmentAmount.None;
            }
            
        }
        public bool CharacterCanPeekAroundAndSeeOtherCharacter(HeroSystemCharacter attacker,
            HeroSystemCharacter defender)
        {
            return true;
        }

        public void UpdateAmountOfConcealmentBeingProvidedToCharacterUnderCoverFromOtherCharacter(ConcealmentAmount concealmentAmount, HeroSystemCharacter characterBehindCover,
            HeroSystemCharacter other)
        {
            ConcealmentAmount = concealmentAmount;
        }
    }



    public class MapFactory
    {
        private static GameMapStub _map = new GameMapStub();
        public static GameMapStub ActiveGameMap => _map;
    }
}
