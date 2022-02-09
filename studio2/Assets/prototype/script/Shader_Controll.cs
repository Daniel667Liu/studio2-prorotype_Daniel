using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shader_Controll : MonoBehaviour
{
    public Material[] DissolveMAT;//store the dissolve shaders, including the changing one.
    public float Dissolve_Speed = 0.01f;//Difine the speed of shader lerping , 0.1f - 1f.
    public bool Dissolve_Start;
    public bool Dissolve_Reverse;//begin to dissolve?
    public bool isDay;
    public Animator anim;
    
    
    
    void Start()
    {
        
    }

    public void Dissolve() 
    {
        if (Dissolve_Start) 
        {
            for (int i = 0; i< DissolveMAT.Length; i++) 
            {
                float a = Mathf.Lerp(DissolveMAT[i].GetFloat("_strength"), 1f, Dissolve_Speed);
                DissolveMAT[i].SetFloat("_strength", a);

                if (a > 0.8) //dissolve end.
                {
                    Dissolve_Start = false;
                    
                }

                Debug.Log(a);// for test
            }
        }
        
    }

    public void DissolveReverse()
    {
        if (Dissolve_Reverse)
        {
            for (int i = 0; i < DissolveMAT.Length; i++)
            {
                float a = Mathf.Lerp(DissolveMAT[i].GetFloat("_strength"), 0f, Dissolve_Speed);
                DissolveMAT[i].SetFloat("_strength", a);

                if (a < 0.1) //dissolve end.
                {
                    Dissolve_Reverse = false;
                    
                }

                Debug.Log(a);// for test
            }
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G)) //for test, key down
        {
            if (isDay)
            {
                isDay = false;//begin to dissolve
                Dissolve_Start = true;
                
            }
            else 
            {
                isDay = true;
                Dissolve_Reverse = true;
                
            }
            
            
        }

        anim.SetBool("isDay", isDay);

        Dissolve();
        DissolveReverse();
    }
}
