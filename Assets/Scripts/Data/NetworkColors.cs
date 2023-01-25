using Unity.Netcode;

public struct NetworkColors : INetworkSerializable
{

    public NetworkColors(bool cl, bool sp, bool he, bool di)
    {
        club = cl;
        spade = sp;
        heart = he;
        diamond = di;
    }

    public bool club;
    public bool spade;
    public bool heart;
    public bool diamond;



    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref club);
        serializer.SerializeValue(ref spade);
        serializer.SerializeValue(ref heart);
        serializer.SerializeValue(ref diamond);
    }

}
