using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RazMods.Hunter
{
    public class PScale
    {
        public const byte SCALEMODE_NOSCALE = 0;//No scaling enabled
        public const byte SCALEMODE_DOWN = 1;//Scale higher the lower you go
        public const byte SCALEMODE_UP = 2;//Scale higher the higher you go
        public const byte SCALEMODE_MIDOUT = 3;//Scale higher the further from the origin you go
        public const byte SCALEMODE_OUTMID = 4;//Scale higher the closer from the origin you go

        //Values in order : Branch, Turn, End, Stairs, Model, CSAvoid, Max Length

        public float bval, tval, eval, rval, sval, mval, cval, mlval;//Scaling factor for each generation float setting, per floor difference.


        public byte mode;

        public PScale(float bv, float tv, float ev, float rv, float sv, float mv, float cv, float mlv, byte m)
        {
            this.bval = bv;
            this.eval = ev;
            this.tval = tv;
            this.rval = rv;
            this.sval = sv;
            this.mval = mv;
            this.cval = cv;
            this.mlval = mlv;
            this.mode = m;
        }

        public static PScale NoScalingDefault()
        {
            return new PScale(1, 1, 1, 1, 1, 1, 1, 1, PScale.SCALEMODE_NOSCALE);
        }

        public static float Process(byte scalingMode, float toScale, float val, int originFlr, int currentFlr, int levelFloors)
        {


            switch (scalingMode)
            {

                case SCALEMODE_NOSCALE:
                    return val;

                case SCALEMODE_DOWN:
                    return (float)Math.Pow(toScale, Math.Max(originFlr - currentFlr, 0)) * val;

                case SCALEMODE_UP:
                    return (float)Math.Pow(toScale, Math.Max(currentFlr - originFlr, 0)) * val;

                case SCALEMODE_MIDOUT:
                    return (float)Math.Pow(toScale, Math.Abs(originFlr - currentFlr)) * val;

                case SCALEMODE_OUTMID:
                    return (float)Math.Pow(toScale, (levelFloors / 2) - Math.Abs(originFlr - currentFlr)) * val;

                default:
                    return val;
            }
        }

    }
}
