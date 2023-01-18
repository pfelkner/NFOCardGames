using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameManager : NetworkBehaviour
{
    //----------------------- config start //-----------------------
    public static GameManager gM;

    [Header("SetUp")]
    public GameObject cardEmptyPrefab;
    public GameObject deckGO;

    public List<Player> players;
    public Player currentPlayer;
    int index;

    public NetworkVariable<ulong> currentPlayerId = new NetworkVariable<ulong>(1000, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkClient currentPlayerNetworkClient;


    [Header("DeckInfo")]
    public List<Colors> colorsAvaliable;
    public List<Values> valuesAvaliable;

    public TMPro.TextMeshProUGUI text;

    [Header("Rules")]
    public int maximumCardsInHand;

    //[HideInInspector]
    public static List<Card> createdCardsList = new List<Card>();


    public List<Card> playedCardList = new List<Card>();
    public List<Card> lastCardPlayed = new List<Card>();


    public NetworkVariable<int> lastCardPlayedValue = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    // 
    public NetworkVariable<bool> lastCardIsHeart = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> lastCardIsClub = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> lastCardIsSpades = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> lastCardIsDiamond = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<int> lastCardPlayedAmount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<NetworkCard> netWorkCard = new NetworkVariable<NetworkCard>();
    public NetworkVariable<int> rnd = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField]
    public List<NetworkCard> networkDeck = new List<NetworkCard>();
    



    public struct NetworkCard : INetworkSerializable
    {
        public NetworkCard(int col, int val) {
            color = col;
            value = val;
            Debug.Log($"Created NetworkCard {(Values)value} of {(Colors)color}");
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

    private void Awake()
    {
        if (gM == null) gM = this;
    }

    public override void OnNetworkSpawn()
    {
        lastCardPlayedValue.OnValueChanged += (int prevVal, int newVal) =>
        {
            Debug.Log(OwnerClientId + "Previous Value " + prevVal + "New value " + newVal);
        };

        

        rnd.OnValueChanged += ShuffleWithRandomClientRpc;
        currentPlayerId.Value = 69420;
        index = 0;
    }

    public override void OnNetworkDespawn()
    {
        rnd.OnValueChanged -= ShuffleWithRandomClientRpc;
    }

    //----------------------- Set Up ------------------------


    // All possible cards in a deck get created
    [ClientRpc]
    public void InitDeckClientRpc()
    {
        Debug.Log("Init deck called");
        for (int i = 0; i < colorsAvaliable.Count; i++)
        {
            for (int j = 1; j < valuesAvaliable.Count+1; j++)
            {
                networkDeck.Add(new NetworkCard(i, j));
            }
        }
    }



    // starts the shuffling process
    public void InitShuffle()
    {
        for (int i = 0; i < 100; i++)
        {
            SetRandom();
        }
    }



    // Is attached to networkvariable random (of type int); on change the first card in the deck is stwiched with the card at position of the new radnom value
    [ClientRpc]
    private void ShuffleWithRandomClientRpc(int previousValue, int newValue)
    {
        Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} is shuffling");
        NetworkCard temp = networkDeck[newValue];
        networkDeck[newValue] = networkDeck[0];
        networkDeck[0] = temp;
    }


    // Upon being called the networkvariable (is updated across the network) random is being set to a ranadom value;
    // whenever this value changes the attacked listener triggers a method
    public void SetRandom()
    {
        rnd.Value = Random.Range(1, networkDeck.Count);
        Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} randomized. New value: {rnd}");
    }



    //----------------------- Handling player order ------------------------


    [ServerRpc(RequireOwnership =false)]
    public void SetFirstPlayerServerRpc()
    {
        currentPlayerNetworkClient = NetworkManager.Singleton.ConnectedClientsList[0];
        currentPlayerId.Value = currentPlayerNetworkClient.ClientId;
        Debug.Log($"Current plaxer initially set to Player {currentPlayerId.Value}");
    }


    [ServerRpc(RequireOwnership = false)]
    public void NextPlayerServerRpc()
    {
        Debug.Log("Called NextPlayerTestServerRpc");
        if (index + 1 < NetworkManager.Singleton.ConnectedClientsList.Count)
        {
            index++;
            Debug.Log("Incrementing index to "+index);
        } else
        {
            index = 0;
            Debug.Log("Resetting index to 0");
        }
        currentPlayerId.Value = NetworkManager.Singleton.ConnectedClientsList[index].ClientId;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetLastCardServerRpc(int value)
    {
        lastCardPlayedValue.Value = value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetLastAmountServerRpc(int value)
    {
        lastCardPlayedAmount.Value = value;
    }



}


