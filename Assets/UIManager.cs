using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{

    public static UIManager Instance;

    public TextMeshProUGUI hostClientText;
    public TextMeshProUGUI playerTxt;
    public GameObject _parent;

    public Button endTurnBtn;

    public TextMeshProUGUI cardTOBeatText;
    public TextMeshProUGUI amountToBeat;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        
    }

    private void OnEnable()
    {
        
    }
    public void ClickHost()
    {
      
        NetworkManager.Singleton.StartHost();
        _parent.SetActive(false);
        hostClientText.text = "Host";

    }

    public void ClickClient()
    {
        
        NetworkManager.Singleton.StartClient();
        _parent.SetActive(false);
        hostClientText.text = "Client";

    }

    public void ChangeTextForPlayerValue(Values val)
    {
        cardTOBeatText.text = " of " + val.ToString();
    }
    public void ChangeTextForPlayerInt( int amount)
    {
        amountToBeat.text = amount.ToString();
    }

}
