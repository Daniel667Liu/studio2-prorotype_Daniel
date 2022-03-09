using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchEventTrigger : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        EventManager.TriggerEvent("SwitchToDay");//game scene is day time by default
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G)) 
        {
            EventManager.TriggerEvent("SwitchToDay");
        }

        if (Input.GetKeyDown(KeyCode.H)) 
        {
            EventManager.TriggerEvent("SwitchToNight");
        }
    }
}
