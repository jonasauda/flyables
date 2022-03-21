using System;
using System.Numerics;

namespace HaptiOS.Src
{
    public static class Vector2Extensions
    {
        /// <summary>
        /// Returns the angle from -pi to pi between this and the target
        /// vector.
        /// 
        /// http://www.euclideanspace.com/maths/algebra/vectors/angleBetween/
        /// https://stackoverflow.com/questions/21483999/using-atan2-to-find-angle-between-two-vectors
        /// </summary>
        /// <param name="src"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static float AngleBetween(this Vector2 src, Vector2 target)
        {
            // normalize target
            src = Vector2.Normalize(src);
            target = Vector2.Normalize(target);

            var angle = MathF.Atan2(target.Y, target.X) - MathF.Atan2(src.Y, src.X);
            if (angle <= -MathF.PI)
            {
                angle += MathF.PI * 2;
            }
            else if (angle > MathF.PI)
            {
                angle -= MathF.PI * 2;
            }
            return angle;
        }
    }
}