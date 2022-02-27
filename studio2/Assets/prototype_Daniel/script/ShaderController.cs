using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShaderController : MonoBehaviour
{
    
    [SerializeField]
    private Material[] dayMaterials;// store all materials that show in the day
    [SerializeField]
    private Material[] nightMaterials;//store all materials that show in the night
    [SerializeField]
    private float dissolveSpeed = 0.01f;//controll how fast materials can show and dissolve
    

    private List<Collider> dayColliders = new List<Collider>();
    private List<Collider> nightColliders = new List<Collider>();
    private bool showupStart = false;
    private bool dissolveStart = false;
    private float showupMeasure;
    private float dissolveMeasure;
    private bool isday = true;

    //how the models show up
    private void showupModel(Material[] showupMATs) //call this to show materials, by cnortrolling some parameters in materials
    {
        if (showupStart)
        {
            for (int i = 0; i < showupMATs.Length; i++)
            {
                showupMeasure = Mathf.Lerp(showupMATs[i].GetFloat("_strength"), 0.1f, dissolveSpeed);
                showupMATs[i].SetFloat("_strength", showupMeasure);
            }
            if (showupMeasure < 0.1f) 
            {
                showupStart = false;
            }
        }
    }


    //how the shader dissolve 
    private void dissolveModel(Material[] dissolveMATs)//call this to dissolve materials, by cnortrolling some parameters in materials
    {
        if (dissolveStart) 
        {
            for (int i = 0; i < dissolveMATs.Length; i++)
            {
                dissolveMeasure = Mathf.Lerp(dissolveMATs[i].GetFloat("_strength"), 1f, dissolveSpeed);
                dissolveMATs[i].SetFloat("_strength", dissolveMeasure);
            }
            if (dissolveMeasure > 0.8f) 
            {
                dissolveStart = false;
            }
        }
        
    }


    //collidrs controll, call in the call back function 
    private void dayColliderSetting(List<Collider> dayColliders, List<Collider> nightColliders) //controll which colliders can be active in the day
    {
        foreach (Collider dayCollider in dayColliders)  
        {
            dayCollider.enabled = true;
        }

        foreach (Collider nightCollider in nightColliders) 
        {
            nightCollider.enabled = false;
        }
    }

    private void nightColliderSetting(List<Collider> dayColliders, List<Collider> nightColliders) // controll which colliders can be active in the night
    {
        foreach (Collider dayCollider in dayColliders)
        {
            dayCollider.enabled = false;
        }

        foreach (Collider nightCollider in nightColliders)
        {
            nightCollider.enabled = true;
        }
    }

    private void switchToDay() // register for "SwitchToDay" event in EventManager, call back funcion.
    {
        isday = true;
        showupStart = true;
        dissolveStart = true;
        dayColliderSetting(dayColliders,nightColliders);
    }

    private void switchToNight() //register for "SwitchToNight" event in EventManager, call back function.
    {
        isday = false;
        showupStart = true;
        dissolveStart = true;
        nightColliderSetting(dayColliders,nightColliders);
    }



    // Start is called before the first frame update
    void Start()
    {
        

        GameObject[] dayModels = GameObject.FindGameObjectsWithTag("dayModel");//find all day models in scene
        //Debug.Log(dayModels.Length);
        foreach (GameObject dayModel in dayModels) // get colliders of all day models and add them into dayColliders list
         {
             dayColliders.Add(dayModel.gameObject.GetComponent<BoxCollider>());
         }

        

        GameObject[] nightModels = GameObject.FindGameObjectsWithTag("nightModel");//find all night models in scene
        foreach (GameObject nightModel in nightModels) 
        {
            nightColliders.Add(nightModel.gameObject.GetComponent<BoxCollider>());// get colliders of all night models and add them into dayColliders list
        }

        EventManager.RegisterListener("SwitchToDay", switchToDay);//register events listener in EventManager, so that they can be triggered by any "TriggerEvent" call
        EventManager.RegisterListener("SwitchToNight", switchToNight);

        
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isday)
        {
            showupModel(dayMaterials);
            dissolveModel(nightMaterials);
        }
        else 
        {
            showupModel(nightMaterials);
            dissolveModel(dayMaterials);
        }
    }
    private void OnDestroy()

    {
        EventManager.UnregisterListener("SwitchToDay", switchToDay);//when this script destroyed, unregister all listener related with it
        EventManager.UnregisterListener("SwitchToNight", switchToNight);

        foreach (Material material in dayMaterials) //reset all materials
        {
            material.SetFloat("_strenght", 0f);
        }

        foreach (Material material in nightMaterials)
        {
            material.SetFloat("_strenght", 0f);
        }
    }

    
}
