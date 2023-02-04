using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static GameManager;
using System.Linq;
using System;
using System.Runtime.InteropServices;

public class Player : NetworkBehaviour
{
    public List<Card> cardsInHand;
    [SerializeField]
    public List<NetworkCard> networkHand = new List<NetworkCard>();
    public GameObject cardPrefab;

    public List<Card> selectedCards = new List<Card>();

    public override void OnNetworkSpawn()
    {
        gameObject.name = $"Player {gM.players.Count + 1}";
        GameManager.gM.players.Add(this);
        if (IsLocalPlayer)
            GameManager.gM.SetLocalPlayer(this);
    }

    public override void OnNetworkDespawn()
    {
        GameManager.gM.players.Remove(this);
    }


    private void Update()
    {
        UIManager.Instance.SetIsCurrentPlayerText(IsCurrentPlayer());
        
        // testing
        if (Input.GetKeyDown(KeyCode.T) && IsOwner && IsServer)
        {
            GameManager.gM.playerIds = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
            GameManager.gM.InitDeckClientRpc();
            GameManager.gM.InitShuffleServerRpc();
            DealCards();
            GameManager.gM.SetFirstPlayerServerRpc();
            //GameManager.gM.state = GameManager.gM.ChangeState(GameManager.gM.state, true);
            GameManager.gM.ChangeStateServerRpc();
            UIManager.Instance.codePanel.SetActive(false);
        }
        if (Input.GetKeyDown(KeyCode.U) && IsOwner)
        {
            LogCards();
        }
        if (Input.GetKeyDown(KeyCode.I) && IsOwner)
        {
            LogDeck();
        }
        if (Input.GetKeyDown(KeyCode.L) && IsOwner)
        {
            GameManager.gM.LogPlacements();
        }

        if (Input.GetKeyDown(KeyCode.Space) && IsClient && IsOwner)
        {
            if (GameManager.gM.state == State.Playing)
                TakeTurn();
            //else if ()
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            GameManager.gM.ResetPlacementsServerRpc();
        }

        if (!IsExchanging())
        {
            ExChangeCards.Instance.gameObject.SetActive(false);
            ExChangeCards.Instance.SetPosition2();
        } else
        {
            ExChangeCards.Instance.SetPosition();
        }

    }

    private bool IsExchanging()
    {
        return GameManager.gM.state == State.Stealing
            || GameManager.gM.state == State.Returning
            || GameManager.gM.state == State.StealingVize
            || GameManager.gM.state == State.ReturningVize;
    }


    [ClientRpc]
    private void SpawnCardsClientRpc()
    {
        float spacing = 0f;
        List<NetworkCard> newHand = SortNetworkCards(networkHand);
        // crate cards locally
        foreach (NetworkCard networkCard in newHand)
        {
            CreateCardInHand(networkCard);
            spacing++;
        }
        SpriteHolder.sh.SetSpritesPosition(this);
    }
    private List<NetworkCard> SortNetworkCards(List<NetworkCard> _netHand)
    {
       return _netHand.OrderBy(card => card.value).ToList();
    }
    private void CreateCardInHand(NetworkCard _netCard)
    {
        GameObject go = Instantiate(cardPrefab, new Vector2(-10f, -10f), Quaternion.identity);
        Card currentCard = go.GetComponent<Card>();

        currentCard.value = (Values)_netCard.value;
        currentCard.color = (Colors)_netCard.color;
        go.name = currentCard.value + " of " + currentCard.color;

        currentCard.cardOwner = this;

        cardsInHand.Add(currentCard);
    }

    internal void DealCards()
    {
        UpdateHandClientRpc();
        foreach (ulong uid in NetworkManager.Singleton.ConnectedClientsIds)
        {
            GameManager.GetPlayerById(uid).SpawnCardsClientRpc();
           
        }        
    }

    [ClientRpc]
    private void UpdateHandClientRpc()
    {
        int deckIndex = 0;
        foreach (Player player in GameManager.gM.players)
        {
            player.networkHand.Clear();

            for (int i = 0; i < GameManager.gM.maximumCardsInHand; i++)
            {
                player.networkHand.Add(GameManager.gM.networkDeck[deckIndex]);
                deckIndex++;
            }
        }
    }

    [ClientRpc]
    public void StealCardsClientRpc(int _valOne, int _valTwo, ulong _senderId, ClientRpcParams  _targetId)
    {
        bool cardOne = false, cardTwo = false;
        // sind im arschloch
        NetworkCard newValOne_ = new NetworkCard();
        NetworkCard newValTwo_ = new NetworkCard();

        // if no match -> error
        List<int> available = new List<int>();

        networkHand.ForEach(c => available.Add(c.value));

        if (available.Contains(_valOne))
        {
            newValOne_ = networkHand.First(c => c.value == _valOne);
            networkHand.Remove(newValOne_);
            available.Remove(_valOne);
            LogCards();
            cardOne = true;
        }
        // seven has value 2
        if (available.Contains(_valTwo))
        {
            newValTwo_ = networkHand.First(c => c.value == _valTwo);
            networkHand.Remove(newValTwo_);
            available.Remove(_valTwo);
            LogCards();
            cardTwo = true;
        }
        if (!cardOne && !cardTwo) HandleStolenCardsServerRpc();
        else if (cardOne && cardTwo)
        {
            cardsInHand.ForEach(c => c.gameObject.SetActive(false));
            cardsInHand.Clear();
            SpawnCardsClientRpc();
            HandleStolenCardsServerRpc(newValOne_, newValTwo_, _senderId);
        }
        else if (cardOne && !cardTwo)
        {
            cardsInHand.ForEach(c => c.gameObject.SetActive(false));
            cardsInHand.Clear();
            SpawnCardsClientRpc();
            HandleStolenCardsServerRpc(newValOne_, _senderId);
        }
        else if (!cardOne && cardTwo)
        {
            cardsInHand.ForEach(c => c.gameObject.SetActive(false));
            cardsInHand.Clear();
            SpawnCardsClientRpc();
            HandleStolenCardsServerRpc(newValTwo_, _senderId);
        }
    }

    [ServerRpc]
    private void HandleStolenCardsServerRpc()
    {
        GameManager.gM.cardsExchanged.Value = 0;
        GameManager.gM.ChangeStateServerRpc();
        GameManager.gM.ChangeStateServerRpc();
        UIManager.Instance.ReturnMode();
        GameManager.gM.DisplayBubbleRightServerRpc(0);
        GameManager.gM.PreGame();
    }

    [ServerRpc(RequireOwnership =false)]
    public void HandleStolenCardsServerRpc(NetworkCard _newCardOne,NetworkCard _newCardTwo,ulong _senderId)
    {
        GameManager.gM.cardsExchanged.Value = 2;
        GetPlayerById(_senderId).AddToHandClientRpc(_newCardOne, _newCardTwo, GameManager.gM.TargetId(_senderId));
        GameManager.gM.ChangeStateServerRpc();
        GameManager.gM.DisplayBubbleRightServerRpc(2);
        UIManager.Instance.ReturnMode();

    }
    [ServerRpc(RequireOwnership = false)]
    public void HandleStolenCardsServerRpc(NetworkCard _newCardOne, ulong _senderId)
    {
        GameManager.gM.cardsExchanged.Value = 1;
        GetPlayerById(_senderId).AddToHandClientRpc(_newCardOne, GameManager.gM.TargetId(_senderId));
        GameManager.gM.ChangeStateServerRpc();
        GameManager.gM.DisplayBubbleRightServerRpc(1);
        UIManager.Instance.ReturnMode();
    }


    //[ServerRpc]
    //private void HandleReturnCardsServerRpc()
    //{
    //    GameManager.gM.cardsExchanged.Value = 0;
    //    UIManager.Instance.ResetSelection();
    //    EdgeCaseClientRpc();
    //    //GameManager.gM.PreGame();
    //}

    [ServerRpc(RequireOwnership = false)]
    public void HandleReturnCardsServerRpc(NetworkCard _newCardOne, NetworkCard _newCardTwo, ulong _senderId)
    {
        Debug.Log("HandleReturncArdsServer2" + GameManager.gM.state);
        GameManager.gM.cardsExchanged.Value = 2;
        GetPlayerById(_senderId).AddToHandClientRpc(_newCardOne, _newCardTwo, GameManager.gM.TargetId(_senderId));
        GameManager.gM.ChangeStateServerRpc();
        if (GameManager.gM.state == State.PreGame)
        {
            GameManager.gM.PreGame();
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void HandleReturnCardsServerRpc(NetworkCard _newCardOne, ulong _senderId)
    {
        Debug.Log("HandleReturncArdsServer1" + GameManager.gM.state);
        GameManager.gM.cardsExchanged.Value = 1;
        GetPlayerById(_senderId).AddToHandClientRpc(_newCardOne, GameManager.gM.TargetId(_senderId));

        GameManager.gM.ChangeStateServerRpc();
        if (GameManager.gM.state == State.PreGame)
        {
            GameManager.gM.PreGame();
        }
    }



    [ClientRpc]
    public void AddToHandClientRpc(NetworkCard _newValOne, NetworkCard _newValTwo, ClientRpcParams _PraesiId)
    {
        networkHand.Add(_newValOne);
        networkHand.Add(_newValTwo);
        cardsInHand.ForEach(c => Destroy(c.gameObject));
        cardsInHand.Clear();
        SpawnCardsClientRpc();
    }

    [ClientRpc]
    public void AddToHandClientRpc(NetworkCard _newValOne, ClientRpcParams _PraesiId)
    {
        networkHand.Add(_newValOne);
        cardsInHand.ForEach(c => Destroy(c.gameObject));
        cardsInHand.Clear();
        SpawnCardsClientRpc();
    }

    [ClientRpc]
    internal void ReturnCardsClientRpc(int _valOne, int _valTwo, ulong _senderId, ClientRpcParams clientRpcParams)
    {
        if (!IsOwner) return;

        // in case presi didn't get any cards, leave returning state and move on
        // TODO maybe remove to properly handle states
        if (GameManager.gM.cardsExchanged.Value == 0)
        {
            GameManager.gM.ChangeStateServerRpc();
            GameManager.gM.PreGame();
        }
        bool cardOne = false, cardTwo = false;
        // sind im arschloch
        NetworkCard newValOne_ = new NetworkCard();
        NetworkCard newValTwo_ = new NetworkCard();

        // if no match -> error
        List<int> available = new List<int>();
        networkHand.ForEach(c => available.Add(c.value));

        // TODO refactor so only newvalone is being used if only one card can be found in hand
        if (available.Contains(_valOne) && _valOne > 0)
        {
            newValOne_ = networkHand.First(c => c.value == _valOne);
            networkHand.Remove(newValOne_);
            LogCards();
            cardOne = true;
        }
        // seven has value 2
        if (available.Contains(_valTwo) && _valTwo > 0)
        {
            newValTwo_ = networkHand.First(c => c.value == _valTwo);
            networkHand.Remove(newValTwo_);
            LogCards();
            cardTwo = true;
        }

        Debug.Log(cardOne + ":" + cardTwo);
        if (!cardOne && !cardTwo) return;

        else if (cardOne && cardTwo)
        {
            cardsInHand.ForEach(c => c.gameObject.SetActive(false));
            cardsInHand.Clear();
            SpawnCardsClientRpc();
            HandleReturnCardsServerRpc(newValOne_, newValTwo_, _senderId);
        }
        else if (cardOne && !cardTwo)
        {
            cardsInHand.ForEach(c => c.gameObject.SetActive(false));
            cardsInHand.Clear();
            SpawnCardsClientRpc();
            HandleReturnCardsServerRpc(newValOne_, _senderId);
        }
        else if (!cardOne && cardTwo)
        {
            cardsInHand.ForEach(c => c.gameObject.SetActive(false));
            cardsInHand.Clear();
            SpawnCardsClientRpc();
            HandleReturnCardsServerRpc(newValTwo_, _senderId);
        }
        Debug.Log(cardOne + ":" + cardTwo);
    }
    #region Gamelogic

    public bool IsValidCard(Card card)
    {
        return (int)card.value > GameManager.gM.lastCardPlayedValue.Value || HansBomb();
    }
    public bool HansBomb()
    {
        return cardsInHand.GroupBy(x => x.value).Any(g => g.Count() > 3);
    }

    public void TakeTurn()
    {
        if (!IsClient || !IsOwner || !IsCurrentPlayer()) return;

        if (selectedCards.Count == 0 || IsDone()) // pass
        {
            PassTurn();
            return;
        }
        // alredy checked for higher only need to check if equal
        if ((!AreEqualValue() || !AreEqualCount())&& !HansBomb())
        {
            CancelMove();
            return;
        }   
        else
            PlayCards();
        
        if (IsDone())
            GameManager.gM.HandlePlayerDone();

        EndTurn();
    }

    private void EndTurn()
    {
        GameManager.gM.NextPlayerServerRpc();
        GameManager.gM.CheckGameOverServerRpc(); 
    }

    private void PlayCards()
    {
        GameManager.gM.SetLastCardServerRpc((int)selectedCards[0].value);
        GameManager.gM.SetLastAmountServerRpc(selectedCards.Count);
        GameManager.gM.HandleCardsToSpwawnServerRpc(GetSelectedColors(selectedCards));
        SoundManager.Instance.PlaySound();

        RemovePlayedcards();
    }

    private void RemovePlayedcards()
    {
        selectedCards.ForEach(card => HandlePlayerCard(card));
        selectedCards.Clear();
    }
    [ClientRpc]
    public void RemoveSelectedCardsClientRpc()
    {
        selectedCards.ForEach(card => HandlePlayerCard(card));
        selectedCards.Clear();
    }

    private void PassTurn()
    {
        GameManager.gM.SetLastAmountServerRpc(0);
        GameManager.gM.SetLastCardServerRpc(0);
        GameManager.gM.NextPlayerServerRpc();
        SoundManager.Instance.CheckSound();
    }

    private void CancelMove()
    {
        selectedCards.ForEach(card => card.Deselect()); ;
        selectedCards.Clear();
        SoundManager.Instance.CancelSound();
    }

    // ----------------------- Utils -----------------------

    public void HandlePlayerCard(Card _card)
    {
        cardsInHand.Remove(_card);
        Destroy(_card.gameObject);
    }

    private NetworkColors GetSelectedColors(List<Card> _cards)
    {
        NetworkColors cols = new NetworkColors();
        foreach (Card card  in _cards)
        {
            switch (card.color)
            {
                case Colors.club:
                    cols.club = true;
                    break;
                case Colors.spade:
                    cols.spade = true;
                    break;
                case Colors.heart:
                    cols.heart = true;
                    break;
                case Colors.diamond:
                    cols.diamond = true;
                    break;
            }
        }
        return cols;
    }
   
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

    public bool IsCurrentPlayer()
    {
        return GameManager.gM.currentPlayerId.Value == NetworkManager.Singleton.LocalClientId;
    }

    public bool IsDone()
    {
        if (cardsInHand.Count <= 0) return true;
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
    #endregion

    #region Utils
    private void LogCards()
    {
        Debug.Log($"Player has the following {networkHand.Count} cards in Hand:");
        foreach (NetworkCard networkCard in networkHand)
        {
            Debug.Log("LocalClientId: "+NetworkManager.Singleton.LocalClientId+" "+networkCard.ToString());
            
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
    #endregion


    [ClientRpc]
    public void ExchangeCardsClientRpc(ClientRpcParams clientRpcParams)
    {
        UIManager.Instance.StealMode();
    }

    public List<Card> GetSelectedCards()
    {
        if (IsOwner)
            return selectedCards;
        else
            return new List<Card>();
    }


 
}
