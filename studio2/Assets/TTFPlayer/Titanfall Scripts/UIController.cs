using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIController : MonoBehaviour
{
    #region Variables  
    public Camera FPSCam;

    [Space, Header("Ray Settings")]
    UIController uiController;
    [Space, Header("Ray Settings")]
    float rayDistance = 0f;
    float raySphereRadius = 0f;
    private LayerMask interactableLayer = ~0;

    #endregion
    void Start()
    {
        FPSCam = this.GetComponentInParent<TTFCameraController>().mainCamera;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    #region Custom methods
    
    void RayChecking()
    {
        Ray ShooterRay = new Ray(FPSCam.transform.position,FPSCam.transform.forward);
        RaycastHit hitInfo;

        bool hitSomething = Physics.SphereCast(ShooterRay, raySphereRadius, out hitInfo, rayDistance, interactableLayer);
        if (hitSomething != null)
        {

        }
    }
    #endregion
}
