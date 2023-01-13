using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public void ClickHost()
    {
        NetworkManager.Singleton.StartHost();
    }

    public void ClickClient()
    {
        NetworkManager.Singleton.StartClient();
    }
}
