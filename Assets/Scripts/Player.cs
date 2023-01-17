using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using TMPro;
using UnityEngine;
using static UnityEngine.CullingGroup;
using static GameManager;
using UnityEditor.PackageManager;

public class Player : NetworkBehaviour
{
    public List<Card> cardsInHand;
    [SerializeField]
    private NetworkVariable<Values> value = new NetworkVariable<Values>();
    [SerializeField]
    private NetworkVariable<Colors> color = new NetworkVariable<Colors>();
    [SerializeField]
    public List<NetworkCard> networkHand = new List<NetworkCard>();
    public GameObject cardPrefab;


    public bool isCurrentPlayer = false;

    private void OnEnable()
    {
        gameObject.name = $"Player {gM.players.Count+1}";
        GameManager.gM.players.Add(this);
        value.OnValueChanged += ShowCardDesc;
        GameManager.gM.currentPlayerId.OnValueChanged += SetCurrentPlayerClient;
    }

    private void OnDisable()
    {
        value.OnValueChanged -= ShowCardDesc;
        GameManager.gM.currentPlayerId.OnValueChanged -= SetCurrentPlayerClient;
    }
    private void ShowCardDesc(Values prevVal, Values newVal)
    {
        if (IsOwner)
            GameManager.gM.text.text = $"Card color: {color.Value}; Card value: {newVal}";
    }

    private void Update()
    {
        // testing
        if (Input.GetKeyDown(KeyCode.T) && IsOwner)
        {
            GameManager.gM.InitDeckClientRpc();
            Debug.Log(networkHand.Count + " cards have been created.");
            GameManager.gM.InitShuffle();
            DealCard();

            GameManager.gM.SetFirstPlayerServerRpc();




        }
        if (Input.GetKeyDown(KeyCode.Z) && IsOwner)
        {
            DealCard();
        }
        if (Input.GetKeyDown(KeyCode.U) && IsOwner)
        {
            LogCards();
        }
        if (Input.GetKeyDown(KeyCode.O) && IsOwner)
        {
            GameManager.gM.InitShuffle();
        }
        if (Input.GetKeyDown(KeyCode.I) && IsOwner)
        {
            LogDeck();
        }

        if (Input.GetKeyDown(KeyCode.E) && IsOwner)
        {
            //EndTurn();
            Debug.Log("##############");
            foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
                Debug.Log(id);
            Debug.Log("##############");
        }
        if (Input.GetKeyDown(KeyCode.N) && IsOwner)
        {
            Debug.Log("N pressed");
            if (GameManager.gM.currentPlayerId.Value == NetworkManager.Singleton.LocalClientId)
                GameManager.gM.T1(0);
        }
        if (Input.GetKeyDown(KeyCode.M) && IsOwner)
        {
            Debug.Log("M pressed");
            if (GameManager.gM.currentPlayerId.Value == NetworkManager.Singleton.LocalClientId)
                GameManager.gM.T1(1);
        }
    }


    [ClientRpc]
    private void SpawnCardsClientRpc()
    {
        float spacing = 0f;

        // crate cards locally
        foreach (NetworkCard networkCard in networkHand)
        {
            GameObject go = Instantiate(cardPrefab, new Vector2(-10f, -10f), Quaternion.identity);
            Card currentCard = go.GetComponent<Card>();

            currentCard.value = (Values)networkCard.value;
            currentCard.color = (Colors)networkCard.color;
            go.name = currentCard.value + " of " + currentCard.color;

            currentCard.cardOwner = this;

            cardsInHand.Add(currentCard);
            spacing++;
        }
        SpriteHolder.sP.SetSpritesPosition(this);
    }

    private void DealCard()
    {
        if (IsOwner)
        {
            UpdateHandClientRpc();
        }
        foreach (ulong uid in NetworkManager.Singleton.ConnectedClientsIds)
        {
            NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(uid).GetComponent<Player>().SpawnCardsClientRpc();
           
        }        
    }

    [ClientRpc]
    private void UpdateHandClientRpc()
    {
        int deckIndex = 0;
        //foreach (var clientid in NetworkManager.Singleton.ConnectedClientsIds)
        foreach (Player player in GameManager.gM.players)
        {
            Debug.Log($"Player with Id {player} is dealt cards");

            for (int i = 0; i < GameManager.gM.maximumCardsInHand; i++)
            {
                Debug.Log($"Player with Id {player.NetworkObjectId} received a {GameManager.gM.networkDeck[deckIndex].ToString()}");
                player.networkHand.Add(GameManager.gM.networkDeck[deckIndex]);
                deckIndex++;

            }
            Debug.Log($"Player with Id {player} has now {networkHand.Count} cards in hand");
          
        }

    }

    public void StartTurn()
    {
        //check cards in middle, can I pay?
        //either paly or pass
        // remove played cards from networkhand / destroy from scene
    }

    private void EvaluateTurn()
    {
        // how many crds palyed
        // what value played
        // cards in  hand left ? HasWon() : EndTurn
    }

    private void EndTurn()
    {
        if (isCurrentPlayer)
        {
            isCurrentPlayer = false;
            GameManager.gM.SetCurrentPlayerServerRpc();
        }

    }

    public void HasWon()
    {
        // set as winner
        // end turn
        // call EndGame()
    }

    private void EndGame()
    {
        // destroy cards from scene
        // empty networkHand
    }


    [ClientRpc]
    private void SetCurrentPlayerClientRpc(ulong previousValue, ulong newValue)
    {
        Debug.Log($"SetCurrentPlayerClientRpc: previous: {previousValue}, new: {newValue}");
        if (newValue == NetworkManager.Singleton.LocalClientId)
        {
            isCurrentPlayer = true;
            Debug.Log($"Player {newValue} is now current Player");
        }
        else 
            isCurrentPlayer = false;
    }

    private void SetCurrentPlayerClient(ulong previousValue, ulong newValue)
    {
        Debug.Log($"SetCurrentPlayerClient: previous: {previousValue}, new: {newValue}");
        if (newValue == NetworkManager.Singleton.LocalClientId)
        {
            isCurrentPlayer = true;
            Debug.Log($"Player {newValue} is now current Player");
        }
        else
            isCurrentPlayer = false;
    }



    // ----------------------- Utils -----------------------

    private void LogCards()
    {
        Debug.Log($"Player has the following {networkHand.Count} cards in Hand:");
        foreach (NetworkCard networkCard in networkHand)
        {
            Debug.Log("LocalClientId: "+NetworkManager.Singleton.LocalClientId+" "+networkCard.ToString());
            Debug.Log("of "+NetworkObject.NetworkObjectId);
            
        }
    }

    private void LogDeck()
    {
        Debug.Log($"Deck consists of the following {networkHand.Count} cards:");
        foreach (NetworkCard networkCard in GameManager.gM.networkDeck)
        {
            Debug.Log("LocalClientId: " + NetworkManager.Singleton.LocalClientId + " " + networkCard.ToString());
        }
    }
}
