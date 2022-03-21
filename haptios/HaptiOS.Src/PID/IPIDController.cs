namespace HaptiOS.Src.PID
{
    /// <summary>
    /// The <code>IPID</code> is meant to calculate output values of a
    /// proprotional-integral-derivative controller. An implementation
    /// relies on the data provided to this instance.
    /// </summary>
    /// <example>
    /// <code>
    /// var pid = new PIDController();
    /// var processValue = 0;
    /// var setPoint = 50;
    /// pid.Init(2.0, 0.2, 0.5, 100, 0, 10);
    /// pid.OnOutputCalculated += (sender, output) => Console.WriteLine("{0:0.00}", output);
    /// while (true)
    /// {
    ///     var currentOutput = pid.Update(processValue, setPoint);
    ///     Thread.Sleep(50);
    ///     processValue = GetProcessValue();
    ///     setPoint = GetSetPoint();
    /// }
    /// </code>
    /// </example>
    public interface IPIDController
    {
        /// <summary>
        /// Sample time in milli seconds that this controller should use to
        /// calculate the current output. If Update() is called to early the
        /// last calculated output value is returned.
        /// </summary>
        int SampleTimeMillis { get; set; }

        /// <summary>
        /// Integral windup, also known as integrator windup or reset windup,
        /// refers to the situation in a PID feedback controller where
        /// a large change in setpoint occurs(say a positive change)
        /// and the integral terms accumulates a significant error
        /// during the rise(windup), thus overshooting and continuing
        /// to increase as this accumulated error is unwound
        /// (offset by errors in the other direction).
        /// The specific problem is the excess overshooting.
        /// </summary>
        double WindupGuard { get; set; }

        /// <summary>
        /// Determines how aggressively the PID reacts to the current 
        /// error with setting Proportional Gain
        /// </summary>
        double KP { get; set; }

        /// <summary>
        /// Determines how aggressively the PID reacts to the current 
        /// error with setting Integral Gain
        /// </summary>
        double KI { get; set; }

        /// <summary>
        /// Determines how aggressively the PID reacts to the current 
        /// error with setting Derivative Gain
        /// </summary>
        double KD { get; set; }

        /// <summary>
        /// Last calculated proportional term. This value is not multiplied
        /// by kP.
        /// </summary>
        double PTerm { get; }

        /// <summary>
        /// Last calculated integral term. This value is not multiplied
        /// by kI.
        /// </summary>
        double ITerm { get; }
        
        /// <summary>
        /// Last calculated derivative term. This value is not multiplied
        /// by kD.
        /// </summary>
        double DTerm { get; }

        /// <summary>
        /// Minimum value that the output value can get.
        /// </summary>
        double OutputMin { get; set; }

        /// <summary>
        /// Maximum value that the output value can get.
        /// </summary>
        double OutputMax { get; set; }

        /// <summary>
        /// Initialize the calculation of an output value. This method must
        /// be called before any update, otherwise exceptions will be thrown.
        /// A method is used instead of the constructor, so the controller
        /// can be accessed with dependency injection.
        /// </summary>
        /// <param name="kP">proportional gain</param>
        /// <param name="kI">integral gain</param>
        /// <param name="kD">derivative gain</param>
        /// <param name="oMin">min output value</param>
        /// <param name="oMax">max output value</param>
        /// <param name="sampleTime">sample time in milli seconds</param>
        /// <param name="pv">Delegate that returns the current process value</param>
        /// <param name="sp">Delegate that returns the current set point</param>
        void Init(double kP, double kI, double kD,
            double oMin, double oMax, int sampleTime);

        /// <summary>
        /// Tries to update the current output value in regard of the sample
        /// time. Can be called any time after init.
        /// </summary>
        /// <returns>The current calculated output value</returns>
        /// <param name="processValue"></param>
        /// <param name="setPoint"></param>
        double Update(double processValue, double setPoint);

        /// <summary>
        /// Resets the current terms that are used for calculation, all
        /// defined properties.
        /// </summary>
        void Clear();
    }
}