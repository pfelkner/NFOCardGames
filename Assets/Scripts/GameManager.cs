using System;
using System.Collections;
using System.Collections.Generic;
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

    public List<PlayerOwn> players;
    public PlayerOwn currentPlayer;
   

    [Header("DeckInfo")]
    public List<Colors> colorsAvaliable;
    public List<Values> valuesAvaliable;


    [Header("Rules")]
    public int maximumCardsInHand;

    [HideInInspector]
    public List<Card> createdCardsList = new List<Card>();

    // controls
    private List<Card> playerOneList = new List<Card>();
    private List<Card> playerTwoList = new List<Card>();
    private List<Card> playerThreeList = new List<Card>();
    private List<Card> playerFourList = new List<Card>();


    public List<Card> playedCardList = new List<Card>();
    public List<Card> lastCardPlayed = new List<Card>();


    public NetworkVariable<int> lastCardPlayedValue = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void Awake()
    {
        if (gM == null) gM = this;
    }

    private void Start()
    {
        // basic
        CreateDeck();
        ShuffleDeck();

        // arschloch 
        DealCards();

        // sync
       
    }

    private void Update()
    {
        // testing
        if (Input.GetKeyDown(KeyCode.T))
        {
            foreach (var item in players)
            {
                AddHandToPlayer(item, playerOneList);
            }
        }
    
    }

    private void CreateDeck()
    {
        var test = Enum.GetValues(typeof(Colors));

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
        // Intervall 0-9 P1, 10-18 P2, ... for all cards
        for (int i = 0; i < createdCardsList.Count; i++)
        {
            if (i >= 0 && i < maximumCardsInHand)
                playerOneList.Add(createdCardsList[i]);
            
            else if( i >= maximumCardsInHand && i < maximumCardsInHand*2)
                playerTwoList.Add(createdCardsList[i]);
           
            else if (i >= maximumCardsInHand*2 && i < maximumCardsInHand * 3)
                playerThreeList.Add(createdCardsList[i]);
           
            else if (i >= maximumCardsInHand*3 && i < maximumCardsInHand * 4)
                playerFourList.Add(createdCardsList[i]);
        }
    }

    private void AddHandToPlayer(PlayerOwn player, List<Card> cards)
    {
        player.SetPlayer(cards);
    }



    private void CheckForAce()
    {
        if ((int)lastCardPlayed[0].value ==(int)Values.ass)
        {
            lastCardPlayed.Clear();
            lastCardPlayed.Add(playedCardList[0]);
        }
    }

    public void SetValueOfLastCard(int value)
    {
        lastCardPlayedValue.Value = value;
    }


} //


