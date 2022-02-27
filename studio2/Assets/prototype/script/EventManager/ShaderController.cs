using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShaderController : MonoBehaviour
{
    private EventManager eventManager;
    [SerializeField]
    private Material[] dayMaterials;
    
    private List<Collider> dayColliders;
    [SerializeField]
    private Material[] nightMaterials;
    
    private List<Collider> nightColliders;
    [SerializeField]
    private float dissolveSpeed = 0.01f;

    private bool showupStart;
    private bool dissolveStart;
    private float showupMeasure;
    private float dissolveMeasure;
    private bool isday = false;

    //how the models show up
    private void showupModel(Material[] showupMATs) 
    {
        if (showupStart)
        {
            for (int i = 0; i < showupMATs.Length; i++)
            {
                showupMeasure = Mathf.Lerp(showupMATs[i].GetFloat("_strength"), 0.1f, dissolveSpeed);
                showupMATs[i].SetFloat("_strength", showupMeasure);
            }
            if (showupMeasure>0.8f) 
            {
                showupStart = false;
            }
        }
    }


    //how the shader dissolve 
    private void dissolveModel(Material[] dissolveMATs)
    {
        if (dissolveStart) 
        {
            for (int i = 0; i < dissolveMATs.Length; i++)
            {
                dissolveMeasure = Mathf.Lerp(dissolveMATs[i].GetFloat("_strength"), 1f, dissolveSpeed);
                dissolveMATs[i].SetFloat("_strength", dissolveMeasure);
            }
            if (dissolveMeasure < 0.1) 
            {
                dissolveStart = false;
            }
        }
        
    }


    //collidrs controll, call in the call back function 
    private void dayColliderSetting(List<Collider> dayColliders, List<Collider> nightColliders) 
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

    private void nightColliderSetting(List<Collider> dayColliders, List<Collider> nightColliders)
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

    private void switchToDay() 
    {
        isday = true;
        showupStart = true;
        dissolveStart = true;
        dayColliderSetting(dayColliders,nightColliders);
    }

    private void switchToNight() 
    {
        isday = false;
        showupStart = true;
        dissolveStart = true;
        nightColliderSetting(dayColliders,nightColliders);
    }



    // Start is called before the first frame update
    void Start()
    {
        eventManager = FindObjectOfType<EventManager>();

        GameObject[] dayModels = GameObject.FindGameObjectsWithTag("dayModel");//find all day models in scene
        foreach (GameObject dayModel in dayModels) // get colliders of all day model and add them into dayColliders list
        {
            dayColliders.Add(dayModel.GetComponent<Collider>());
        }

        GameObject[] nightModels = GameObject.FindGameObjectsWithTag("nightModel");
        foreach (GameObject nightModel in nightModels) 
        {
            nightColliders.Add(nightModel.GetComponent<Collider>());
        }

        EventManager.RegisterListener("SwitchToDay", switchToDay);
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
        EventManager.UnregisterListener("SwitchToDay", switchToDay);
        EventManager.UnregisterListener("SwitchToNight", switchToNight);
    }
}
