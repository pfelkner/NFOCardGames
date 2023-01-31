using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{

    public static UIManager Instance;

    [Header("HostClientText")]
    public TextMeshProUGUI hostClientText;
    public TextMeshProUGUI playerTxt;

    [Header("Btn Panel")]
    public GameObject btnPanel;

    [Header("Beat Text")]
    public TextMeshProUGUI cardTOBeatText;
    public TextMeshProUGUI amountToBeat;

    [Header("Turn Indicator")]
    public TextMeshProUGUI isCurrentPlayerText;
    public Image frameImage;
    public Color frameColorTurn;
    public Color frameColorNoTurn;

    [Header("ExchangerObject")]
    public GameObject exchangerGo;

    public GameObject cardsHolder;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }
    public void ClickHost()
    {
        NetworkManager.Singleton.StartHost();
        btnPanel.SetActive(false);
        hostClientText.text = "Host";
    }

    public void ClickClient()
    {
        NetworkManager.Singleton.StartClient();
        btnPanel.SetActive(false);
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
    public void StealMode()
    {
        exchangerGo.SetActive(true);
        cardsHolder.SetActive(true);
    }

    public void ReturnMode()
    {
        //exchangerGo.SetActive(true);
        cardsHolder.SetActive(false);
    }

    public void ResetSelection()
    {
        ExChangeCards.Instance.ResetSelection();
        //exchangerGo.SetActive(false);
    }


}
