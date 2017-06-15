using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeroSystemEngine.Character;
namespace HeroSystemsEngine.Focus
{
    public class Focus
    {

        public Focus(FocusType focustype)
        {
            FocusType = focustype;
        }

        public void Drop()
        {
            Holder.HeldFoci.Remove(this);
            Holder = null;

        }

        public FocusType FocusType { get; set; }
        public HeroSystemCharacter Holder { get; set; }
        public HeroSystemCharacter Owner { get; set; }
        public HandsRequired HandsRequired { get; set; }
    }

    public enum FocusType
    {
        OIF, IIF, OAF, IAF
    }
    public enum HandsRequired { TwoHanded, OneHanded, Nohands }

    public class Weapon : Focus
    {
        public Weapon(FocusType focustype) : base(focustype)
        {
        }
    }
}
