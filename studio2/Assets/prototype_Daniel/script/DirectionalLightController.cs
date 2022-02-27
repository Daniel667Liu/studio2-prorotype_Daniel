using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectionalLightController : MonoBehaviour
{
    private bool isDay = true;
    private Animator animator;
    
    // Start is called before the first frame update
    void Start()
    {
        animator = gameObject.GetComponent<Animator>();
        EventManager.RegisterListener("SwitchToDay", SwitchToDay);
        EventManager.RegisterListener("SwitchToNight", SwitchToNight);
    }

    

    
    private void SwitchToNight()
    {
        isDay = false;
      
    }

    private void SwitchToDay()
    {
       
        isDay = true;
    }

    // Update is called once per frame
    void Update()
    {
        animator.SetBool("isDay", isDay);
    }

    private void OnDestroy()
    {
        EventManager.UnregisterListener("SwitchToDay", SwitchToDay);
        EventManager.UnregisterListener("SwitchToNight", SwitchToNight);
    }
}
