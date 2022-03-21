using System;
using Microsoft.Extensions.Configuration;

namespace HaptiOS.Src.Config
{
    /// <summary>
    /// The HaptiosConfig is a singleton that can be used inside classes
    /// that are not created by this applications dependency injection
    /// container. It is based on the <code>IConfiguration</code> of this
    /// application and must be loaded <seealso cref="Load(IConfiguration)"/>
    /// before any value can be read. It can be used in the same way the
    /// <code>IConfiguration</code> is used.
    /// 
    /// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-2.2#getvalue
    /// </summary>
    public sealed class HaptiosConfig
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly HaptiosConfig _instance = new HaptiosConfig();
        private IConfiguration _config;

        public static HaptiosConfig Instance { get => _instance; }

        private HaptiosConfig()
        {
            Logger.Info("HaptiosConfig instance created");
        }

        /// <summary>
        /// Before any value of this <seealso cref="_config"> can be retrieved,
        /// it must be loaded into this instance.
        /// </summary>
        /// <param name="config">Configuration of this singleton</param>
        public void Load(IConfiguration config)
        {
            _config = config;
            Logger.Info("Configuration loaded");
        }

        /// <summary>
        /// Extracts the value with the specified key and converts it to type T.
        /// </summary>
        /// <param name="key">The key of the configuration section's value to convert.</param>
        /// <typeparam name="T">The type to convert the value to.</typeparam>
        /// <returns>The converted value</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if this config is not loaded</exception>
        public T GetValue<T>(string key)
        {
            CheckLoad();
            return _config.GetValue<T>(key);
        }

        /// <summary>
        /// Extracts the value with the specified key and converts it to type T.
        /// </summary>
        /// <param name="key">The key of the configuration section's value to convert</param>
        /// <param name="defaultValue">The default value to use if no value is found.</param>
        /// <typeparam name="T">The type to convert the value to.</typeparam>
        /// <returns>The converted value</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if this config is not loaded</exception>
        public T GetValue<T>(string key, T defaultValue)
        {
            CheckLoad();
            return _config.GetValue<T>(key, defaultValue);
        }

        private void CheckLoad()
        {
            if (_config == null)
            {
                throw new InvalidOperationException("Configuration not loaded");
            }
        }
    }
}