namespace HaptiOS.Src.Serialization
{
    public class GameObjectDeserializer : IDeserializer<GameObject>
    {
        public GameObject Deserialize(byte[] data)
        {
            return GameObject.Parser.ParseFrom(data);
        }
    }
}