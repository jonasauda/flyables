using System;

namespace HaptiOS.Src.PID
{
    /// <inheritdoc/>
    public class PIDController : IPIDController
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public int SampleTimeMillis { get; set; }
        public double WindupGuard { get; set; }
        public double KP { get; set; }
        public double KI { get; set; }
        public double KD { get; set; }
        public double OutputMin { get; set; }
        public double OutputMax { get; set; }
        public double PTerm { get => _pTerm; }
        public double ITerm { get => _iTerm; }
        public double DTerm { get => _dTerm; }

        private double _pTerm;
        private double _iTerm;
        private double _dTerm;
        private DateTime _lastTime;
        private double _lastError;
        private double _output;

        public void Clear()
        {
            _pTerm = 0;
            _iTerm = 0;
            _dTerm = 0;

            OutputMin = 0;
            OutputMax = 0;

            SampleTimeMillis = 100;
        }

        public void Init(double kP, double kI, double kD,
            double oMin, double oMax, int sampleTime)
        {
            Clear();

            KP = kP;
            KI = kI;
            KD = kD;

            OutputMin = oMin;
            OutputMax = oMax;

            _lastTime = DateTime.Now;

            SampleTimeMillis = sampleTime;
        }

        public double Update(double pv, double sp)
        {
            var error = sp - pv;
            var deltaError = error - _lastError;
            var deltaTime = (DateTime.Now - _lastTime).TotalSeconds;

            if ((DateTime.Now - _lastTime).TotalMilliseconds < SampleTimeMillis)
            {
                return _output;
            }

            /*
             * The overall control function can be expressed mathematically as
             * u(t) = K_p e(t) + K_i \int_{0}^{t} e(t)dt + K_d {de}/{dt}
             */

            // error = e(t)
            _pTerm = error;
            // iTerm = \int_{0}^{t} e(t')dt'
            _iTerm += error * deltaTime;
            _iTerm = Clamp(_iTerm, -WindupGuard, WindupGuard);

            _dTerm = 0;
            if (deltaTime > 0)
            {
                // _dTerm = {de}/{dt}
                _dTerm = deltaError / deltaTime;
            }

            _lastTime = DateTime.Now;
            _lastError = error;

            const string fmt = "{0:0.00} * {1} + {2:0.000} * {3} + {4:0.00} * {5}";
            Logger.Debug(fmt, KP, _pTerm, KI, _iTerm, KD, _dTerm);
            _output = KP * _pTerm + KI * _iTerm + KD * _dTerm;

            _output = Clamp(_output, -1, 1);
            _output = ScaleValue(_output, -1, 1, OutputMin, OutputMax);
            return _output;
        }

        private static double ScaleValue(double value, double valueMin,
            double valueMax, double scaleMin, double scaleMax)
        {
            // break down to -1.0 <= x <= 1.0
            double vPerc = (value - valueMin) / (valueMax - valueMin);
            double scaled = vPerc * (scaleMax - scaleMin) + scaleMin;
            return scaled;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value > max)
                return max;
            if (value < min)
                return min;
            return value;
        }
    }
}