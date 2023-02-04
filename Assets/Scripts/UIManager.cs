using System.Globalization;
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

    [Header("Turn Indicator")]
    public TextMeshProUGUI isCurrentPlayerText;
    public Image frameImage;
    public Color frameColorTurn;
    public Color frameColorNoTurn;

    [Header("ExchangerObject")]
    public GameObject exchangerGo;

    public GameObject cardsHolder;

    public GameObject joinCodeInput;

    public TextMeshProUGUI codeText;
    public GameObject codePanel;

    public GameObject bubbleImageLeft;
    public TextMeshProUGUI bubbleTextLeft;

    public GameObject bubbleImageRight;
    public TextMeshProUGUI bubbleTextRight;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) && joinCodeInput)
        {
            SubmitInputCode();
        }
    }

    public void ClickHost()
    {
        codePanel.SetActive(true);
        btnPanel.SetActive(false);
        hostClientText.text = "Host";
    }

    public void ClickClient()
    {
        //NetworkManager.Singleton.StartClient();
        btnPanel.SetActive(false);
        hostClientText.text = "Client";
        joinCodeInput.SetActive(true);
    }

    public void SubmitInputCode()
    {
        string code;
        code = joinCodeInput.GetComponent<TMP_InputField>().text;
        if (code != "")
        {
            Relay.Instance.JoinRelay(code);
            joinCodeInput.SetActive(false);
        }
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

    public void SetCodeText(string _code)
    {
        codeText.text = _code;
    }

    public void SetBubbleLeft(bool _flag)
    {
        bubbleImageLeft.SetActive(_flag);
    }
    public void SetBubbleRight(bool _flag)
    {
        bubbleImageRight.SetActive(_flag);
    }


}
