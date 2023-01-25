using Unity.Netcode;

public struct NetworkCard : INetworkSerializable
{
    public NetworkCard(int col = -1, int val = -1)
    {
        color = col;
        value = val;
    }
    public int color;
    public int value;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref color);
        serializer.SerializeValue(ref value);
    }

    public override string ToString()
    {
        return $"{(Values)value} of {(Colors)color}";
    }
}
