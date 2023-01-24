using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class ExChangeCards : MonoBehaviour
{
   
    public List<Values> valuesToChoose;
    public List<Values> valuesToRemove;

    public GameObject addBtn;
    public GameObject removeBtn;

    [SerializeField]
    CardUI cardUI;
    public TextMeshProUGUI text;

    private void Update()
    {
        string newText = string.Empty;
        foreach (Values item in valuesToChoose)
        {
            newText += item.ToString() +" ";
        }
        text.text = newText;
    }

    public void SelectCards(CardUI _cardUI)
    {
        SetButtons(true);
        cardUI = _cardUI;
    }



    public void SetButtons(bool _flag)
    {
        addBtn.SetActive(_flag);
        removeBtn.SetActive(_flag);
    }

    public void OnClickAdd()
    {
        if (valuesToChoose.Count < 2)
        {
            valuesToChoose.Add(cardUI.value);
        }
    }

    public void OnClickRemove()
    {
         valuesToChoose.Remove(valuesToChoose.First(c => c == cardUI.value));
    }

    public void SentCards()
    {
        GameManager.gM.RequestCard(valuesToChoose);
        gameObject.SetActive(false);
    }

    


}
