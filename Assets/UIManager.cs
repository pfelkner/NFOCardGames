using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class UIManager : MonoBehaviour
{

    public static UIManager Instance;

    public TextMeshProUGUI hostClientText;
    public TextMeshProUGUI playerTxt;
    public GameObject _parent;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        
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
}
