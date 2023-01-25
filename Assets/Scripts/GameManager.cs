using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Newtonsoft.Json.Linq;
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
    public Player localPlayer;
    int index;
    int playerCount;
    public List<ulong> playerIds = new List<ulong>();

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

    public NetworkVariable<int> lastCardPlayedValue = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> lastCardPlayedAmount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
    public NetworkVariable<NetworkCard> netWorkCard = new NetworkVariable<NetworkCard>();
    public NetworkVariable<int> rnd = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField]
    public List<NetworkCard> networkDeck = new List<NetworkCard>();

    Dictionary<int, ulong> placements = new Dictionary<int, ulong>();

    public List<Player> playersFinished = new List<Player>();


    private void Awake()
    {
        if (gM == null) gM = this;
    }

    public override void OnNetworkSpawn()
    {
        InitHandlers();
        InitVariables();
        
    }

    public override void OnNetworkDespawn()
    {
        lastCardPlayedValue.OnValueChanged -= OnLastCardPlayedValueChanged;
        lastCardPlayedAmount.OnValueChanged -= OnLastCardPlayedAmountChanged;
        rnd.OnValueChanged -= ShuffleWithRandomClientRpc;
    }

    private void InitVariables()
    {
        currentPlayerId.Value = 69420;
        lastCardPlayedValue.Value = 0;
        index = 0;
    }

    public void InitHandlers()
    {
        lastCardPlayedValue.OnValueChanged += OnLastCardPlayedValueChanged;
        lastCardPlayedAmount.OnValueChanged += OnLastCardPlayedAmountChanged;

        rnd.OnValueChanged += ShuffleWithRandomClientRpc;
    }

    private void OnLastCardPlayedValueChanged(int prevVal, int newVal)
    {
        UIManager.Instance.ChangeTextForPlayerValue((Values) newVal);
        SpriteHolder.sP.cardsValue = newVal;
    }

    private void OnLastCardPlayedAmountChanged(int prevVal, int newVal)
    {
        UIManager.Instance.ChangeTextForPlayerInt(newVal);
        SpriteHolder.sP.cardsAmount = newVal;
    }


    //----------------------- Set Up ------------------------


    // All possible cards in a deck get created
    [ClientRpc]
    public void InitDeckClientRpc()
    {
        colorsAvaliable.ForEach(col => valuesAvaliable.ForEach( val => networkDeck.Add(new NetworkCard((int)col,(int)val))));
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
        Debug.Log("shuffle");
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


    [ServerRpc(RequireOwnership =false)]
    public void SetFirstPlayerServerRpc()
    {
        if (placements.Count == 0)
        {
            // TODO first round: lowest heart, not host
            currentPlayerNetworkClient = NetworkManager.Singleton.ConnectedClientsList[0];
            currentPlayerId.Value = currentPlayerNetworkClient.ClientId;
        } else
        {
            // whoever came in last place takes first turn
            currentPlayerId.Value = placements[placements.Count];
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void NextPlayerServerRpc()
    {
        // index has to be decreased instead of increased to avoid overflowing palyerids count
        //if (index - 1 >= 0)
        //    index--;
        //else
        //    index = playerIds.Count;
        if (index + 1 < playerIds.Count)
            index++;
        else
            index = 0;

        currentPlayerId.Value = playerIds[index];

        Player player = GetPlayerById(currentPlayerId.Value);
        if (player.IsDone())
        {
            NextPlayerServerRpc();
        }
        ResetCardsInMiddle();
    }

    //
    private void ResetCardsInMiddle()
    {
        if (SpriteHolder.sP.goS.Count <= 0) return;
        
        if (SpriteHolder.sP.goS[0].GetComponent<Card>().ownerId == currentPlayerId.Value)
        {
            SpriteHolder.sP.ResetCardsInMiddleClientRpc();
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

    [ServerRpc(RequireOwnership =false)]
    public void HandleCardsToSpwawnServerRpc(NetworkColors cols)
    {
        SpriteHolder.sP.ResetCardsInMiddleClientRpc();
        SpriteHolder.sP.SetCardInMiddleClientRpc(lastCardPlayedAmount.Value, lastCardPlayedValue.Value, cols);
    }

    [ServerRpc(RequireOwnership = false)]
    public void CheckGameOverServerRpc()
    {
        if (playerIds.Count > 1) return;
        SetPlacementServerRpc();
        Player player_ = GetPlayerById(currentPlayerId.Value);
        //player_.cardsInHand.ForEach(c => c.gameObject.SetActive(false));
        //player_.cardsInHand.Clear();
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
        SpriteHolder.sP.ResetCardsInMiddleClientRpc();
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
        Debug.Log($"PrepareNextGameServerRpc on {NetworkManager.Singleton.LocalClientId}; Amount of clients: {NetworkManager.Singleton.ConnectedClientsIds.Count}");

        playerIds = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
        Debug.Log($"playerIds lenght after reset: {playerIds.Count}");
        SetPlayerCount();
        lastCardPlayedValue.Value = 0;
        lastCardPlayedAmount.Value = 0;

        InitShuffle();
        //arschloch mischt
        Debug.Log($"CurrentPlayerId {currentPlayerId.Value}");
        GetPlayerById(currentPlayerId.Value).DealCards();
        //SetFirstPlayerServerRpc();
        //placements.Clear();
        UIManager.Instance.TurnOnExchanger();
    }

    public void RequestCard(List<Values> _vals, bool _flag)
    {
        // erste mal is true, der pr�si will karten , das zweite mal false , der pr�si gibt karten
        // der unterschied ist nur dass die empf�nger sender getauscht werden
        ulong targetId_;
        ulong senderId_;
        if (_flag)
        {
            targetId_ = placements[placements.Count];
            senderId_ = placements[1];
        }
        else
        {
            targetId_ = placements[1];
            senderId_ = placements[placements.Count];
        }
       
        int valOne_ = (int)_vals[0];
        int valTwo_ = (int)_vals[1];



        Debug.Log("Target ID: " + targetId_);

        if(IsOwner)
            GetPlayerById(targetId_).StealCardsFromPlayerToSenderClientRpc(valOne_, valTwo_, senderId_, TargetId(targetId_));

        // finde arsch
        // values entpacken
        // arsch karten entnehmen 
    }
 

    //Utils

    public static Player GetPlayerById(ulong id)
    {
        Debug.Log($" GetPlayerById called by {NetworkManager.Singleton.LocalClientId}");
        return NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(id).GetComponent<Player>();
    }

    [ServerRpc(RequireOwnership =false)]
    public void SetPlacementServerRpc()
    {
        int placement = placements.Count +1;
        placements.Add(placement, currentPlayerId.Value);
        LogPlacements();
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
        Debug.LogWarning($"RemovePlayerServerRpc: count after remove = {NetworkManager.Singleton.ConnectedClientsIds.Count}");
    }

    public void HandlePlayerDone()
    {
        //SpriteHolder.sP.SetWinLooseImageClientRpc();
        SetPlacementServerRpc();
        RemovePlayerIdServerRpc(currentPlayerId.Value);
    }

    public void EndTurn()
    {
        GameManager.gM.NextPlayerServerRpc();
        GameManager.gM.CheckGameOverServerRpc();
    }
}


