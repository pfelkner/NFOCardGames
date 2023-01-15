using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEditor;
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
        int color;
        int value;

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

    

    private void Awake()
    {
        if (gM == null) gM = this;
    }

    private void Start()
    {
        // basic
        //CreateDeck();
        //ShuffleDeck();

    }

    private void Update()
    {
        // testing
        if (Input.GetKeyDown(KeyCode.T) && IsServer)
        {
            //DealCards();
            //DealCard();
            
        }
    }











    //-------------------- Code before Test --------------------
    private void CreateDeck()
    {
        for (int i = 0; i < colorsAvaliable.Count ; i++)
        {
            for (int j = 0; j < valuesAvaliable.Count ; j++)
            {
                CreateCard(i,j);
            }
        }
    }

    private void CreateCard(int i,int j)
    {
        Colors newColor = colorsAvaliable[i];
        Values newVal = valuesAvaliable[j];

        GameObject gO = Instantiate(cardEmptyPrefab, transform.position, Quaternion.identity);
        gO.name = newVal + " of "+ newColor;
        gO.transform.SetParent(deckGO.transform);
        Card card = gO.GetComponent<Card>();
        card.SetCard(newColor, newVal);

        createdCardsList.Add(card);
    }

    public Card CreateCardClient(int i, int j)
    {
        Colors newColor = colorsAvaliable[i];
        Values newVal = valuesAvaliable[j];

        GameObject gO = Instantiate(cardEmptyPrefab, transform.position, Quaternion.identity);
        gO.name = newVal + " of " + newColor;
        gO.transform.SetParent(deckGO.transform);
        Card card = gO.GetComponent<Card>();
        card.SetCard(newColor, newVal);

        return card;
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

    private void DealCards()
    {


        if (IsOwner && !IsServer)
        {
            DealcardsServerRpc();
        }
        else if (IsOwner && IsServer)
        {
            foreach (Player player in players)
            {
                while (player.cardsInHand.Count < maximumCardsInHand)
                {
                    Debug.Log(player.OwnerClientId + " has: " + player.cardsInHand + " cards");
                    Debug.Log("Amount of cards left in deck: " + createdCardsList.Count);
                    player.cardsInHand.Add(createdCardsList[0]);
                    createdCardsList.RemoveAt(0);
                }
                player.SetPlayer(player.cardsInHand);
                //UpdatePlayersClientRpc(players);
            }
        }

        Debug.Log("Deal cards called for "+players.Count+" players");
        //foreach (Player player in players)
        //{
        //    while (player.cardsInHand.Count < maximumCardsInHand)
        //    {
        //        Debug.Log(player.OwnerClientId + " has: " + player.cardsInHand + " cards");
        //        Debug.Log("Amount of cards left in deck: " + createdCardsList.Count);
        //        player.cardsInHand.Add(createdCardsList[0]);
        //        createdCardsList.RemoveAt(0);
        //    }
        //    player.SetPlayer(player.cardsInHand);
        //}
    }

    [ServerRpc]
    private void DealcardsServerRpc()
    {
        List<int> t1 = new List<int>();
        foreach (Player player in players)
        {
            while (player.cardsInHand.Count < maximumCardsInHand)
            {
                Debug.Log(player.OwnerClientId + " has: " + player.cardsInHand + " cards");
                Debug.Log("Amount of cards left in deck: " + createdCardsList.Count);
                player.cardsInHand.Add(createdCardsList[0]);
                //UpdatePlayersClientRpc((int)createdCardsList[0].value);
                player.UpdatePlayersClientRpc((int)createdCardsList[0].value);
                createdCardsList.RemoveAt(0);
            }
            player.SetPlayer(player.cardsInHand);
            player.CreateCardsInHandClientRpc();
        }
        
        
    }



    //private void AddHandToPlayer(Player player, List<Card> cards)
    //{

    //    while (player.cardsInHand.Count > maximumCardsInHand)
    //    {
    //        player.cardsInHand.Add(createdCardsList[0]);
    //        createdCardsList.RemoveAt(0);
    //    }
    //    player.SetPlayer(cards);
        
    //}



    //private void CheckForAce()
    //{
    //    if ((int)lastCardPlayed[0].value ==(int)Values.ass)
    //    {
    //        lastCardPlayed.Clear();
    //        lastCardPlayed.Add(playedCardList[0]);
    //    }
    //}

    //public void SetValueOfLastCard(int value)
    //{
    //    lastCardPlayedValue.Value = value;
    //}


    // Logging changes of lates card played
    public override void OnNetworkSpawn()
    {
        lastCardPlayedValue.OnValueChanged += (int prevVal, int newVal) =>
        {
            Debug.Log(OwnerClientId + "Previous Value " + prevVal + "New value " + newVal);
        };

    }
} //


