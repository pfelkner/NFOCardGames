using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameManager : NetworkBehaviour
{
    // config
    public static GameManager gM;

    [Header("SetUp")]
    public GameObject cardEmptyPrefab;
    public GameObject deckGO;

    public List<Player> players;
    public Player currentPlayer;
   

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
    public NetworkVariable<int> lastCardPlayedAmount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<NetworkCard> netWorkCard = new NetworkVariable<NetworkCard>();
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


    [ClientRpc]
    public void InitDeckClientRpc()
    {
        Debug.Log("Init deck called");
        for (int i = 0; i < colorsAvaliable.Count; i++)
        {
            for (int j = 0; j < valuesAvaliable.Count; j++)
            {
                networkDeck.Add(new NetworkCard(i, j));
            }
        }
    }


    // Logging changes of lates card played
    public override void OnNetworkSpawn()
    {
        lastCardPlayedValue.OnValueChanged += (int prevVal, int newVal) =>
        {
            Debug.Log(OwnerClientId + "Previous Value " + prevVal + "New value " + newVal);
        };

    }


    private void Awake()
    {
        if (gM == null) gM = this;
    }

    
    private void ShuffleDeck()
    {
        for (int i = 0; i < createdCardsList.Count; i++)
        {
            int rnd = Random.Range(1, createdCardsList.Count);
            Card temp = createdCardsList[rnd];
            createdCardsList[rnd] = createdCardsList[0];
            createdCardsList[0] = temp;
        }
    }
}


