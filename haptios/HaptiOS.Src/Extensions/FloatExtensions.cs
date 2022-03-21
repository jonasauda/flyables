using System;

namespace HaptiOS.Src
{
    public static class FloatExtensions
    {
        public static double ToRadians(this double degrees)
        {
            return Math.PI * degrees / 180;
        }
        public static float ToRadians(this float degrees)
        {
            return MathF.PI * degrees / 180f;
        }

        public static double ToDegrees(this double radians)
        {
            return radians * 180 / Math.PI;
        }

        public static float ToDegrees(this float radians)
        {
            return radians * 180f / MathF.PI;
        }

        /// <summary>
        /// Translates this float value from the range leftMin <= value <= leftMax
        /// to the range rightMin <= value <= rightMax. Value must be between
        /// leftMin and leftMax.
        /// 
        ///      |---x------|
        ///         /
        ///  |-----x--------------|
        /// </summary>
        /// <param name="value">Value to translate</param>
        /// <param name="leftMin">Minimum source range value</param>
        /// <param name="leftMax">Maximum source range value</param>
        /// <param name="rightMin">Minimum target range value</param>
        /// <param name="rightMax">Maximum target range value</param>
        /// <returns></returns>
        public static float Translate(this float value, float leftMin, float leftMax, float rightMin, float rightMax)
        {
            // Figure out how 'wide'each range is
            var leftSpan = leftMax - leftMin;
            var rightSpan = rightMax - rightMin;

            // Convert the left range into a 0 - 1 range(float)
            var valueScaled = (value - leftMin) / leftSpan;

            // Convert the 0 - 1 range into a value in the right range.
            return rightMin + (valueScaled * rightSpan);
        }
    }
}