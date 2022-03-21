using System;
using System.Numerics;

namespace HaptiOS.Src
{
    public static class Vector3Extensions
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Calculates the shortest distance between this vector and the line
        /// that contains point p and directed towards given direction.
        /// </summary>
        /// <param name="p">Point of interest</param>
        /// <param name="lineAnchor">Anchor point of the line</param>
        /// <param name="direction">Vector the line is pointing to</param>
        /// <returns>The shortest distance between this vector and the line 
        /// or a negative number in case the direction is zero</returns>
        public static float DistanceToLine(this Vector3 p, Vector3 lineAnchor, Vector3 direction)
        {
            /*
            In theorie we have to take three steps
            1. calculate connection vector
            2. connection vector * direction = 0 (calculate coefficient s)
            3. d = | AF |

            (lineAnchor + s * direction - p) * direction = 0
            <=>
            s = ( p * direction - lineAnchor * direction ) / ( direction * direction )
            */
            var nom = Vector3.Dot(p, direction) - Vector3.Dot(lineAnchor, direction);
            var denom = Vector3.Dot(direction, direction);

            if (denom == 0) return -1;

            var s = nom / denom;

            var f = lineAnchor + s * direction;

            var pf = f - p;
            return pf.Length();
        }

        /// <summary>
        /// Returns the angle between this and target vector on the x-z plane.
        /// The y axis is used as plane normal.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static float AngleBetween(this Vector3 u, Vector3 v)
        {
            var n = Vector3.UnitY;

            /*
            To calculate the angle of two three dimensional vectors on a
            specific plane the vectors are projected onto that plane. In this
            case the angle around the y axis is preserved. Both vectors
            are parallel with the y axis (y = 0). This information can be used
            to calculate the angle as if we calculate the angle on a 2d plane.
             */
            var projU = u.Project(n);
            var projV = v.Project(n);

            /*
            Remove the y component of the vector to perform a two dimensional
            calculation of the angle.
             */
            var projU2D = new Vector2(projU.X, projU.Z);
            var projV2D = new Vector2(projV.X, projV.Z);

            var angle = projU2D.AngleBetween(projV2D);
            return angle;
        }

        public static Vector3 Project(this Vector3 u, Vector3 normal)
        {
            // https://www.maplesoft.com/support/help/maple/view.aspx?path=MathApps%2FProjectionOfVectorOntoPlane
            return u - Vector3.Dot(u, normal) / (MathF.Pow(normal.Length(), 2)) * normal;
        }
    }
}