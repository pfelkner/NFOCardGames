using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameManager : NetworkBehaviour
{
    public static GameManager gM;

    [Header("Network")]
    public NetworkVariable<ulong> currentPlayerId = new NetworkVariable<ulong>(1000, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> lastCardPlayedValue = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> lastCardPlayedAmount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> rnd = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> cardsExchanged = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public List<NetworkCard> networkDeck = new List<NetworkCard>();

    [Header("Setup")]
    public GameObject cardEmptyPrefab;
    public GameObject deckGO;

    [Header("Control")]
    public List<Player> players;
    public List<ulong> playerIds = new List<ulong>();
    [SerializeField] private Player localPlayer;

    [Header("DeckInfo")]
    [SerializeField] private List<Colors> colorsAvaliable;
    [SerializeField] private List<Values> valuesAvaliable;

    [Header("Rules")]
    public int maximumCardsInHand;

    private Dictionary<int, ulong> placements = new Dictionary<int, ulong>();
    private int currenPlayerIndex;
    private int playerCount;
    public State state;
    

    private void Awake()
    {
        if (gM == null) gM = this;
    }

    public override void OnNetworkSpawn()
    {
        InitHandlers();
        InitVariables();
        state = Transition(State.PreGame);
    }

    public override void OnNetworkDespawn()
    {
        lastCardPlayedValue.OnValueChanged -= OnLastCardPlayedValueChanged;
        lastCardPlayedAmount.OnValueChanged -= OnLastCardPlayedAmountChanged;
        rnd.OnValueChanged -= ShuffleWithRandomClientRpc;
    }

    public State ChangeState(State _state) =>
       state switch
       {
           State.PreGame => Transition(State.Playing),
           State.Playing => Transition(State.PostGame),
           State.PostGame => Transition(State.Stealing),
           State.Stealing => Transition(State.Returning),
           State.Returning => Transition(State.StealingVize),
           State.StealingVize => Transition(State.ReturningVize),
           State.ReturningVize => Transition(State.PreGame),
           _ => throw new ArgumentException("Invalid enum value for state", nameof(state)),
       };

    // pregame -> playing
    // playing -> postgame
    // postgame -> stealing
    // stealing -> returning
    // returning -> pregame
    //           -> stealing

    public State Transition(State _newState)
    {
        if (placements.Count != 4 && (_newState == State.StealingVize || _newState == State.ReturningVize))
        {
            return State.PreGame;
        } 
 
        return _newState;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeStateServerRpc()
    {
        ChangeStateClientRpc();
        if (state == State.StealingVize)
        {
            currentPlayerId.Value = placements[2];
            GetPlayerById(placements[2]).ExchangeCardsClientRpc(TargetId(placements[2]));
        }
    }

    [ClientRpc]
    private void ChangeStateClientRpc()
    {
        state = ChangeState(state);
    }

    private void InitVariables()
    {
        currentPlayerId.Value = 69420;
        lastCardPlayedValue.Value = 0;
        currenPlayerIndex = 0;
    }

    public void InitHandlers()
    {
        lastCardPlayedValue.OnValueChanged += OnLastCardPlayedValueChanged;
        lastCardPlayedAmount.OnValueChanged += OnLastCardPlayedAmountChanged;

        rnd.OnValueChanged += ShuffleWithRandomClientRpc;
    }

    private void OnLastCardPlayedValueChanged(int prevVal, int newVal)
    {
        SpriteHolder.sh.cardsValue = newVal;
    }

    private void OnLastCardPlayedAmountChanged(int prevVal, int newVal)
    {
        SpriteHolder.sh.cardsAmount = newVal;
    }


    //----------------------- Set Up ------------------------


    // All possible cards in a deck get created
    [ClientRpc]
    public void InitDeckClientRpc()
    {
        colorsAvaliable.ForEach(col => valuesAvaliable.ForEach(val => networkDeck.Add(new NetworkCard((int)col, (int)val))));
    }

    // starts the shuffling process
    [ServerRpc]
    public void InitShuffleServerRpc()
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
        
        NetworkCard temp = networkDeck[newValue];
        networkDeck[newValue] = networkDeck[0];
        networkDeck[0] = temp;
    }

    // Upon being called the networkvariable (is updated across the network) random is being set to a ranadom value;
    // whenever this value changes the attacked listener triggers a method
    public void SetRandom()
    {
        rnd.Value = Random.Range(1, networkDeck.Count);
    }

    //----------------------- Handling player order ------------------------


    [ServerRpc(RequireOwnership = false)]
    public void SetFirstPlayerServerRpc()
    {
       
        NetworkClient currentPlayerNetworkClient;
        if (placements.Count == 0)
        {
            currentPlayerNetworkClient = NetworkManager.Singleton.ConnectedClientsList[0];
            currentPlayerId.Value = currentPlayerNetworkClient.ClientId;
        }
        else
        {
            currentPlayerId.Value = placements[placements.Count];
            currenPlayerIndex = playerIds.FindIndex(id => id == currentPlayerId.Value);
        }

    }

    [ServerRpc(RequireOwnership = false)]
    public void NextPlayerServerRpc()
    {
        currentPlayerId.Value = playerIds[GetNextplayerId()];

        Player player = GetPlayerById(currentPlayerId.Value);
        if (player.IsDone())
        {
            NextPlayerServerRpc();
        }
        ResetCardsInMiddle();
    }

    private int GetNextplayerId()
    {
        currenPlayerIndex--;
        if (currenPlayerIndex < 0)
            currenPlayerIndex = playerIds.Count - 1;
        return currenPlayerIndex;
    }

    private void ResetCardsInMiddle()
    {
        if (SpriteHolder.sh.cardGos.Count <= 0) return;

        if (SpriteHolder.sh.cardGos[0].GetComponent<Card>().ownerId == currentPlayerId.Value)
        {
            SpriteHolder.sh.ResetCardsInMiddleClientRpc();
        }
    }

    //----------------------- Update round state ------------------------

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

    //----------------------- Game/Round End  ------------------------

    [ServerRpc(RequireOwnership = false)]
    public void HandleCardsToSpwawnServerRpc(NetworkColors cols)
    {
        SpriteHolder.sh.ResetCardsInMiddleClientRpc();
        SpriteHolder.sh.SetCardInMiddleClientRpc(lastCardPlayedAmount.Value, lastCardPlayedValue.Value, cols);
    }

    [ServerRpc(RequireOwnership = false)]
    public void CheckGameOverServerRpc()
    {
        if (playerIds.Count > 1) return;
        SetPlacementServerRpc();

        foreach (KeyValuePair<int, ulong> placement in placements)
        {
            UpdatePlacementClientRpc(placement.Key, placement.Value);
        }

        //state = ChangeState(state, true);
        ChangeStateServerRpc();
        EndRoundClientRpc();
        PrepareNextGameServerRpc();
    }

    [ClientRpc]
    private void EndRoundClientRpc()
    {
        foreach (var player_ in players)
        {
            player_.cardsInHand.ForEach(_c => Destroy(_c.gameObject));
            player_.cardsInHand.Clear();
        }
        SpriteHolder.sh.ResetCardsInMiddleClientRpc();
    }

    public void SetPlayerCount()
    {
        playerCount = players.Count;
    }

    public ClientRpcParams TargetId(ulong id)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { id } }
        };
    }


    [ServerRpc]
    public void PrepareNextGameServerRpc()
    {
        playerIds = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
        ResetLastPlayed();
        InitShuffleServerRpc();
        GetPlayerById(currentPlayerId.Value).DealCards();
        //SetFirstPlayerServerRpc();
        ChangeStateServerRpc();
        if (state == State.Stealing)
        {
            currentPlayerId.Value = placements[1];
            GetPlayerById(placements[1]).ExchangeCardsClientRpc(TargetId(placements[1]));
        }
     


    }

    private void ResetLastPlayed()
    {
        lastCardPlayedValue.Value = 0;
        lastCardPlayedAmount.Value = 0;
    }
   

    public void GetCards(List<Values> _vals)
    {
        int wishesAmount = GetWishesAmount();
        // erste mal is true, der pr�si will karten , das zweite mal false , der pr�si gibt karten
        // der unterschied ist nur dass die empf�nger sender getauscht werden
        ulong  targetId_;
        ulong senderId_;

        if (state == State.Stealing)
        {
            targetId_ = placements[placements.Count];
            senderId_ = placements[1];

        }
        else if (state == State.StealingVize)
        {
            targetId_ = placements[placements.Count - 1];
            senderId_ = placements[2];
        }
        else throw new Exception("GetCard is called from illegal state");

        int valOne_ = -1;
        int valTwo_ = -1;

        if (_vals.Count > 0)
            valOne_ = (int)_vals[0];

        if (_vals.Count == 2 && wishesAmount == 2)
            valTwo_ = (int)_vals[1];

        RequestCardsServerRpc(valOne_, valTwo_, senderId_, targetId_);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestCardsServerRpc(int _valOne, int _valTwo, ulong _senderId, ulong _targetId)
    {
        GetPlayerById(_targetId).StealCardsClientRpc(_valOne, _valTwo, _senderId, TargetId(_targetId));
    }



    public void ReturnCards(List<Values> _vals)
    {
        ulong targetId_;
        ulong senderId_;

        if (state == State.Returning)
        {
            targetId_ = placements[1];
            senderId_ = placements[placements.Count];
        } else if( state == State.ReturningVize)
        {
            targetId_ = placements[2];
            senderId_ = placements[placements.Count - 1];
        } else
            throw new Exception("Return cards frome illegal state");

        int valOne_ = -1;
        int valTwo_ = -1;

        if (cardsExchanged.Value == 0)
        {
            //return; TODO take care of this case
        }
        if (cardsExchanged.Value == 1)
        {
            valOne_ = (int)_vals[0];
        } else if(cardsExchanged.Value == 2)
        {
            valOne_ = (int)_vals[0];
            valTwo_ = (int)_vals[1];
        }
        ReturnCardsServerRpc(valOne_, valTwo_, senderId_, targetId_);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReturnCardsServerRpc(int _valOne, int _valTwo, ulong _senderId, ulong _targetId)
    {
        GetPlayerById(_targetId).ReturnCardsClientRpc(_valOne, _valTwo, _senderId, TargetId(_targetId));
        cardsExchanged.Value = -1;

    }


    
    //Utils

    public Dictionary<int,ulong> GetPlacements()
    {
        return placements;
    }

    public static Player GetPlayerById(ulong id)
    {
        return NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(id).GetComponent<Player>();
    }

    [ServerRpc(RequireOwnership =false)]
    public void SetPlacementServerRpc()
    {
        int placement = placements.Count +1;
        //placements.Add(placement, currentPlayerId.Value);
        ulong test = GetPlayerById(currentPlayerId.Value).OwnerClientId;
        placements.Add(placement, test);
        LogPlacements();
    }


    [ClientRpc]
    public void UpdatePlacementClientRpc(int _placement, ulong _playerId)
    {
        if (!IsServer)
        {
            placements.Add(_placement, _playerId);
        }
    }

    public void LogPlacements()
    {
        foreach(KeyValuePair<int, ulong> entry in placements)
        {
            Debug.Log($"Placement {entry.Key} : ID {entry.Value}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    internal void RemovePlayerIdServerRpc(ulong _id)
    {
        playerIds.Remove(_id);
    }

    public void HandlePlayerDone()
    {
        SetPlacementServerRpc();
        RemovePlayerIdServerRpc(currentPlayerId.Value);
    }

    [ServerRpc(RequireOwnership =false)]
    internal void ResetPlacementsServerRpc()
    {
        placements.Clear();
        ResetPlacementsClientRpc();
    }

    [ClientRpc]
    private void ResetPlacementsClientRpc()
    {
        if (!IsServer)
            placements.Clear();
    }

    public int GetPlayerPlacement()
    {
        foreach (KeyValuePair<int, ulong> item in placements)
        {
            if (item.Value == NetworkManager.Singleton.LocalClientId)
            {
                return item.Key;
            }
        } return -1;
    }

    public int GetWishesAmount()
    {
        int num_ = GetPlayerPlacement();
        // präsi
        if (num_ > 0 && num_ == 1)
        {
            return 2;
        }
        // vize präse
        else if (num_ > 0 && num_ == 2)
        {
            return 1;
        }
        else return 0;
    }

    public Player GetLocalPlayer()
    {
        return localPlayer;
    }

    internal void SetLocalPlayer(Player _player)
    {
        localPlayer = _player;
    }

    public void PreGame()
    {
        foreach(Player player in players)
        {
            player.RemoveSelectedCardsClientRpc();
        }
        SetFirstPlayerServerRpc();
        ResetPlacementsServerRpc();
        cardsExchanged.Value = -1;
        UIManager.Instance.SetBubbleRight(false);
        UIManager.Instance.SetBubbleLeft(false);
        ChangeStateServerRpc();

    }
    [ServerRpc(RequireOwnership = false)]
    public void DisplayRequestedCardsServerRpc(int _x, int _y)
    {
        DisplayBubbleLeftClientRpc(_x,_y);
    }
    [ServerRpc(RequireOwnership = false)]
    public void DisplayBubbleRightServerRpc(int _x)
    {
       
        DisplayBubbleRightClientRpc(_x);
    }

    [ClientRpc]
    public void DisplayBubbleLeftClientRpc(int _x, int _y)
    {
        string cardText_ = string.Empty;
        if (_x == -1  && _y == -1)
        {
            cardText_ = "Ich will keine Karte du Arschloch!";
        } 
        else if(_x != -1 && _y == -1)
        {
            cardText_ = "Gib "+(Values)_x;
        }
        else if (_x != -1 && _y != -1)
        {
            cardText_ = "Gib " + (Values)_x+ " , " + (Values)_y; 
        }
        UIManager.Instance.SetBubbleLeft(true);
        UIManager.Instance.bubbleTextLeft.text = cardText_;

    }
    [ClientRpc]
    public void DisplayBubbleRightClientRpc(int _x)
    {
        string cardText_ = string.Empty;
        cardText_ = _x + " Karten gegeben";
        UIManager.Instance.SetBubbleRight(true);
        UIManager.Instance.bubbleTextRight.text = cardText_;
    }
}


