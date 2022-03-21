using VinteR.Model.Gen;

namespace HaptiOS.Src.Serialization
{
    public class MocapFrameDeserializer : IDeserializer<MocapFrame>
    {
        public MocapFrame Deserialize(byte[] data)
        {
            return MocapFrame.Parser.ParseFrom(data);
        }
    }
}