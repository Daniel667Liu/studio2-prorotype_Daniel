using System.Collections;
using System.Collections.Generic;//using for create dictionary
using UnityEngine;
using UnityEngine.Events;//using for call events

public class EventManager :MonoBehaviour
{
    private Dictionary<string, UnityEvent> eventDictionary;//create a dictionary to store the evnets

    private static EventManager eventManager;// create the instance for get outside

    public static EventManager instance
    {
        get
        {// getting from other scripts, if there is no eventManger, find one in the scene
            if (!eventManager)
            {
                eventManager = FindObjectOfType<EventManager>();
                if (!eventManager)
                {
                    Debug.LogError("no event manager in the scene");
                }
                else
                {
                    //initailize the event manager
                    eventManager.Init();
                }
            }
            return eventManager;
        }
    }

    
    void Init()
    {//if there is no event dictionary, create a new one
        if (eventDictionary == null)
        {
            eventDictionary = new Dictionary<string, UnityEvent>();
        }
    }

    public static void RegisterListener(string eventName, UnityAction listener) 
    {
        UnityEvent thisEvent = null;
        // create an event pointer for output
        if (instance.eventDictionary.TryGetValue(eventName, out thisEvent))
        {//if the event exsists in the dictionary, add listener
            thisEvent.AddListener(listener);
        }
        else 
        {//if the event doesnt exsist in the dictionary, create a new one and add into the dictionary
            thisEvent = new UnityEvent();
            thisEvent.AddListener(listener);
            instance.eventDictionary.Add(eventName, thisEvent);
        }
    }

    public static void UnregisterListener(string eventName, UnityAction listener) 
    {
        if (eventManager == null) return;
        
        UnityEvent thisEvent = null;
        
        if (instance.eventDictionary.TryGetValue(eventName, out thisEvent)) 
        {
            thisEvent.RemoveListener(listener);
        }
    }

    public static void TiggerEvent(string eventName) 
    {
        UnityEvent thisEvent = null;
        if (instance.eventDictionary.TryGetValue(eventName, out thisEvent)) 
        {
            thisEvent.Invoke();//if the event was found in the event dictionary, invoke the event
        }
    }
}
