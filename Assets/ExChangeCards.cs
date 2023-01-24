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

    public int counter;

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
        if (counter == 0)
        {
            if (valuesToChoose.Count < 2)
            {
                valuesToChoose.Add(cardUI.value);
            }
        }
        else
        {
            if (valuesToRemove.Count < 2)
            {
                valuesToRemove.Add(cardUI.value);
            }
        }
    
    }

    public void OnClickRemove()
    {

        if (counter == 0)
        {
            valuesToChoose.Remove(valuesToChoose.First(c => c == cardUI.value));
        }
        else
        {
            valuesToChoose.Remove(valuesToRemove.First(c => c == cardUI.value));
        }

      
    }

    public void SentCards()
    {
        if (counter == 0)
        {
            CardsToSteal();
        } else
        {
            CardsToGe();
        }
    }

    public void CardsToSteal()
    {
        GameManager.gM.RequestCard(valuesToChoose,true);
        counter++;
        text.text = string.Empty;
        gameObject.SetActive(false);
     
    }
    public void CardsToGe()
    {
        GameManager.gM.RequestCard(valuesToRemove,false);
        gameObject.SetActive(false);
        counter = 0;
    }

    


}
