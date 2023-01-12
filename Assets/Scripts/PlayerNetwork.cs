using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    public struct Card : INetworkSerializable
    {
        public int color;
        public int value;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref color);
            serializer.SerializeValue(ref value);
        }
    }

    private NetworkVariable<Card> test = new NetworkVariable<Card>(
        new Card
        {
            color = 0,
            value = 7
        }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void Update()
    {
        Debug.Log("Id: " + OwnerClientId + "; value: " + test.Value);
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.T))
        {
            test.Value = new Card { color = 1, value = 7 };
            TestServerRPC();
        }
    }

    public override void OnNetworkSpawn()
    {
        test.OnValueChanged += (Card prevVal, Card newVal) =>
        {
            Debug.Log(OwnerClientId + "; color " + newVal.color + "value "+ newVal.value);
        };
    }

    [ServerRpc]
    private void TestServerRPC ()
    {
        Debug.Log("ServerRPC from " + OwnerClientId);
    }


    [ServerRpc]
    private void TestServerParamsRPC(ServerRpcParams serverRpcParams)
    {
        Debug.Log("ServerRPC from " + OwnerClientId + "; " + serverRpcParams.Receive.ToString());
    }
}
