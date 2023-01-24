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

    public TextMeshProUGUI isCurrentPlayerText;
    public TextMeshProUGUI endText;

    public Image frameImage;
    public Color frameColorTurn;
    public Color frameColorNoTurn;

    public GameObject exchanger;

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


    public void SetIsCurrentPlayerText(bool _flag)
    {
        if (_flag)
        {
            isCurrentPlayerText.text = "Your Turn";
            frameImage.color = frameColorTurn;
        }

        else
        {
            isCurrentPlayerText.text = "";
            frameImage.color = frameColorNoTurn;
        }
            


    }
    public void SetEndText(string _txt)
    {
        endText.text = _txt;
    }

    public void TurnOnExchanger()
    {
        exchanger.SetActive(true);
    }


}
