using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameLogicServer : MonoBehaviour
{
    //public GameObject cardGo;
    List<GameObject> deckGo;

    //public GameObject cardPrefab;
    public List<Card> deck = new List<Card>();

    //private static GamelogicServer _singleton;
    //public static GamelogicServer Singleton
    //{
    //    get => _singleton;
    //    private set
    //    {
    //        if (_singleton == null)
    //            _singleton = value;
    //        else if (_singleton != value)
    //        {
    //            Debug.Log($"{nameof(GamelogicServer)} instance already exists, destroying duplicate!");
    //            return;
    //        }
    //    }
    //}
    public static GameLogicServer Instance { get; private set; }
    List<int> playerIds = new List<int>;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Starting Game with " + NetworkManager.Singleton.ConnectedClientsIds.Count + " players");
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Updating Game with " + NetworkManager.Singleton.ConnectedClientsIds.Count + " players");
    }

    internal void StartGame()
    {
        playerIds = (List<int>)NetworkManager.Singleton.ConnectedClientsIds;
        int cardsPerPlayer = 36 / playerIds.Count;

    }

    private void CreateCard(Colors color, Values value)
    {
        Card card = new Card(color, value);
        //card.color = color;
        //card.value = value;
        deck.Add(card);

    }

    private void CreateDeck()
    {
        var test = Enum.GetValues(typeof(Colors));
        var colors = EnumUtil.GetValues<Colors>();
        var values = EnumUtil.GetValues<Values>();

        foreach (Colors color in colors)
        {
            foreach (Values value in values)
            {
                CreateCard(color, value);
            }
        }
    }
}