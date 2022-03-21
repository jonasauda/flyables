namespace HaptiOS.Src.Serialization
{
    /// <summary>
    /// A deserializer is used to transform raw bytes of data into
    /// a object specified by the generic given in the deserializer.
    /// The type is given inside the interface definition to provide better
    /// usage for dependency injection.
    /// </summary>
    /// <typeparam name="T">Type this deserializer operates on</typeparam>
    public interface IDeserializer<out T>
    {
        /// <summary>
        /// Deserialize given bytes into the desired type
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        T Deserialize(byte[] data);
    }
}