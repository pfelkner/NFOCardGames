using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using TMPro;
using UnityEngine;
using static UnityEngine.CullingGroup;
using static GameManager;
using UnityEditor.PackageManager;
using Unity.VisualScripting;
using System.Linq;

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

    public List<Card> selectedCards = new List<Card>();

    private void OnEnable()
    {
        gameObject.name = $"Player {gM.players.Count+1}";
        GameManager.gM.players.Add(this);
        UIManager.Instance.endTurnBtn.onClick.AddListener(OnTurnEnd);
    }

    private void OnDisable()
    {
       // UIManager.Instance.endTurnBtn.onClick.RemoveListener(OnTurnEnd);
    }

    public override void OnNetworkSpawn()
    {
      //  GameManager.gM.currentPlayerId.OnValueChanged += StartTurnClientRpc;
    }

    public override void OnNetworkDespawn()
    {
        GameManager.gM.currentPlayerId.OnValueChanged -= StartTurnClientRpc;
     
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
        if (Input.GetKeyDown(KeyCode.U) && IsOwner)
        {
            LogCards();
        }
        if (Input.GetKeyDown(KeyCode.I) && IsOwner)
        {
            LogDeck();
        }

        if (Input.GetKeyDown(KeyCode.V) && IsClient && IsOwner)
        {
            OnTurnEnd();
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


    // this will contain clicking cards and checking wchich cards are viable to play
    [ClientRpc]
    private void StartTurnClientRpc(ulong previousValue, ulong newValue)
    {
        //Debug.Log($"SetCurrentPlayerClientRpc: previous: {previousValue}, new: {newValue}");
        
        if (newValue == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log($"Player {newValue} is now current Player");
        }
        else
            Debug.Log($"Not {NetworkManager.Singleton.LocalClientId}'s turn");
    }

    public bool IsValidCard(Card card)
    {
        return (int)card.value > GameManager.gM.lastCardPlayedValue.Value;
        
    }

    // btn press
    public void OnTurnEnd()
    {
        // alredy checked for higher only need to check if equal // Paul edit: moved this to top to avoid
        // logical error (if two different cards were selected on first turn, lastCardAMount was being set)
        if (!AreEqualValue() || !AreEqualCount())
        {
            Debug.Log($"First if with {!AreEqualValue()} or { !AreEqualCount()}");
            selectedCards.ForEach(card => card.Deselect()); ;
            selectedCards.Clear();
            return;
        }

        // turn is valid; set the amount of last cards played
        if (GameManager.gM.lastCardPlayedAmount.Value == 0) GameManager.gM.SetLastAmountServerRpc(selectedCards.Count);

        //player cant make a move; reset the amount of cards in middle
        if (selectedCards.Count == 0) GameManager.gM.SetLastAmountServerRpc(0);

        // change network variables on server gm
        if (IsClient && IsOwner && IsCurrentPlayer())
        {
            // setting last card
            if (selectedCards.Count == 0) // pass
            {
                GameManager.gM.SetLastAmountServerRpc(0);
                GameManager.gM.SetLastCardServerRpc(0); 
            }
            else
            {
                GameManager.gM.SetLastCardServerRpc((int)selectedCards[0].value);
                GameManager.gM.SetLastAmountServerRpc(selectedCards.Count);
            }
            // setting new player
            GameManager.gM.NextPlayerServerRpc();
        }
        // destroy cards locally
        selectedCards.ForEach(card => card.gameObject.SetActive(false));
        selectedCards.Clear();
    }


    // ----------------------- Utils -----------------------

   
    private bool AreEqualValue()
    {
        return selectedCards.All(c => c.value == selectedCards[0].value);
    }

    // checks if the count is either equal to middle; also returns true if middle is empty
    private bool AreEqualCount()
    {
        return selectedCards.Count == GameManager.gM.lastCardPlayedAmount.Value
            || GameManager.gM.lastCardPlayedAmount.Value == 0;
    }

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

    public bool IsCurrentPlayer()
    {
        return GameManager.gM.currentPlayerId.Value == NetworkManager.Singleton.LocalClientId;
    }
}
