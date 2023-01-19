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

    private void OnEnable()
    {
        gameObject.name = $"Player {gM.players.Count + 1}";
        GameManager.gM.AddPlayer(this);

    }

    private void OnDisable()
    {
    }

    public override void OnNetworkSpawn()
    {
        GameManager.gM.currentPlayerId.OnValueChanged += StartTurnClientRpc;
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
            GameManager.gM.SetPlayerCount();

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
            if (IsCurrentPlayer())
            {
                // move
                GameManager.gM.NextPlayerServerRpc();


            }
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

    // ----------------------- Utils -----------------------

    private void LogCards()
    {
        Debug.Log($"Player has the following {networkHand.Count} cards in Hand:");
        foreach (NetworkCard networkCard in networkHand)
        {
            Debug.Log("LocalClientId: " + NetworkManager.Singleton.LocalClientId + " " + networkCard.ToString());
            Debug.Log("of " + NetworkObject.NetworkObjectId);

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

    public bool IsDone()
    {
        if (cardsInHand.Count == 0) return true;
        return false;
    }


    public Card checkHand(int wish)
    {
        Card tmp = null;
        foreach (var card in cardsInHand)
        {
            if ((int)card.value == wish)
            {
                tmp = card;
                cardsInHand.Remove(card);
                return tmp;
            }
        }
        return tmp;
    }
}
