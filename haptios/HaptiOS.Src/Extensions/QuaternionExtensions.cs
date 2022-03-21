using System;
using System.Numerics;

namespace HaptiOS.Src
{
    public static class QuaternionExtensions
    {
        public static Quaternion ToNumerics(this VinteR.Model.Gen.MocapFrame.Types.Body.Types.Quaternion q)
        {
            return new Quaternion(q.X, q.Y, q.Z, q.W);
        }

        public static Quaternion ToNumerics(this HaptiOS.GameObject.Types.Quaternion q)
        {
            return new Quaternion(q.X, q.Y, q.Z, q.W);
        }

        public static Vector3 ToEulers(this VinteR.Model.Gen.MocapFrame.Types.Body.Types.Quaternion q)
        {
            return ToEulers(q.X, q.Y, q.Z, q.W);
        }

        public static Vector3 ToEulers(this HaptiOS.GameObject.Types.Quaternion q)
        {
            return ToEulers(q.X, q.Y, q.Z, q.W);
        }

        public static Vector3 ToEulers(this System.Numerics.Quaternion q)
        {
            return ToEulers(q.X, q.Y, q.Z, q.W);
        }

        private static Vector3 ToEulers(float x, float y, float z, float w)
        {
            var ysqr = y * y;

            var t0 = +2.0 * (w * x + y * z);
            var t1 = +1.0 - 2.0 * (x * x + ysqr);
            var X = Math.Atan2(t0, t1).ToDegrees();

            var t2 = +2.0 * (w * y - z * x);
            t2 = t2 > +1.0 ? 1.0 : t2;
            t2 = t2 < -1.0 ? -1.0 : t2;
            var Y = Math.Asin(t2).ToDegrees();

            var t3 = +2.0 * (w * z + x * y);
            var t4 = +1.0 - 2.0 * (ysqr * z * z);
            var Z = Math.Atan2(t3, t4).ToDegrees();

            return new Vector3(Convert.ToSingle(X), Convert.ToSingle(Y), Convert.ToSingle(Z));
        }

    }
}