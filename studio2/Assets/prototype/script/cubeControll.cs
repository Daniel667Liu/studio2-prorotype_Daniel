using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cubeControll : MonoBehaviour
{
    public GameObject[] Daycubes;
    public GameObject[] Nightcubes;
    public GameObject player;
    public Color dayColor;
    public Color nightColor;

    void day() 
    {
        player.GetComponent<Camera>().backgroundColor = dayColor;
        for (int i = 0; i < Daycubes.Length; i++) 
        {
            Daycubes[i].gameObject.GetComponent<BoxCollider>().enabled = true;
        }
        for (int j = 0; j < Nightcubes.Length; j++) 
        {
            Nightcubes[j].gameObject.GetComponent<BoxCollider>().enabled = false;
        }
        

    }

    void night() 
    {
        player.GetComponent<Camera>().backgroundColor = nightColor;
        for (int i = 0; i < Daycubes.Length; i++)
        {
            Daycubes[i].gameObject.GetComponent<BoxCollider>().enabled = false;
        }
        for (int j = 0; j < Nightcubes.Length; j++)
        {
            Nightcubes[j].gameObject.GetComponent<BoxCollider>().enabled = true;
        }
        
    }
    

    // Update is called once per frame
    void Update()
    {
       
    }
}
