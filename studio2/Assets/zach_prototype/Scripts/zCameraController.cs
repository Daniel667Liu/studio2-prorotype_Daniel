using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class zCameraController : MonoBehaviour
{

    public Camera mainCamera;
    //public Camera weaponCamera;
    public float sensX = 1f;
    public float sensY = 1f;
    float baseFov = 90f;
    float maxFov = 140f;
    float wallRunTilt = 15f;

    float wishTilt = 0;
    float curTilt = 0;
    Vector2 currentLook;
    Vector2 sway = Vector3.zero;
    float fov;
    Rigidbody rb;
    #region CameraSway
    public float swayAmount = 0f;
    public float swaySpeed = 0f;
    public float returnSpeed = 0f;
    public float changeDirectionMultiplier = 0f;

    public AnimationCurve swayCurve = new AnimationCurve();

    private float _scrollSpeed;

    private float m_xAmountThisFrame;
    private float m_xAmountPreviousFrame;

    private bool m_diffrentDirection;

    #endregion

    void Start()
    {
        rb = GetComponentInParent<Rigidbody>();
        curTilt = transform.localEulerAngles.z;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        RotateMainCamera();
    }

    void FixedUpdate()
    {
        float addedFov = rb.velocity.magnitude - 3.44f;
        fov = Mathf.Lerp(fov, baseFov + addedFov, 0.5f);
        fov = Mathf.Clamp(fov, baseFov, maxFov);
        mainCamera.fieldOfView = fov;
        //weaponCamera.fieldOfView = fov;

        currentLook = Vector2.Lerp(currentLook, currentLook + sway, 0.8f);
        curTilt = Mathf.LerpAngle(curTilt, wishTilt * wallRunTilt, 0.05f);

        sway = Vector2.Lerp(sway, Vector2.zero, 0.2f);
    }

    void RotateMainCamera()
    {
        Vector2 mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        mouseInput.x *= sensX;
        mouseInput.y *= sensY;

        currentLook.x += mouseInput.x;
        //currentLook.y += mouseInput.y;
        currentLook.y = Mathf.Clamp(currentLook.y += mouseInput.y, -90, 90);

        transform.localRotation = Quaternion.AngleAxis(-currentLook.y, Vector3.right);
        transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, curTilt);
        transform.root.transform.localRotation = Quaternion.Euler(0, currentLook.x, 0);
        HandleCameraSway(this.GetComponentInParent<TitanfallMovement>().dir);
        

    }

    public void Punch(Vector2 dir)
    {
        sway += dir;
    }
    void HandleCameraSway(Vector3 dir)
    {
        float _xAmount = Input.GetAxisRaw("Horizontal");
        Debug.Log(dir.x);
        m_xAmountThisFrame = Input.GetAxisRaw("Horizontal");

        if (Input.GetAxisRaw("Horizontal") != 0f) // if we have some input
        {
            //Debug.Log("haveInput");
            if (m_xAmountThisFrame != m_xAmountPreviousFrame && m_xAmountPreviousFrame != 0) // if our previous dir is not equal to current one and the previous one was not idle
                m_diffrentDirection = true;

            // then we multiplier our scroll so when changing direction it will sway to the other direction faster
            float _speedMultiplier = m_diffrentDirection ? changeDirectionMultiplier : 1f;
            _scrollSpeed += (_xAmount * swaySpeed * Time.deltaTime * _speedMultiplier);
            
        }
        else // if we are not moving so there is no input
        {
            if (m_xAmountThisFrame == m_xAmountPreviousFrame) // check if our previous dir equals current dir
                m_diffrentDirection = false; // if yes we want to reset this bool so basically it can be used correctly once we move again

            _scrollSpeed = Mathf.Lerp(_scrollSpeed, 0f, Time.deltaTime * returnSpeed);
        }

        _scrollSpeed = Mathf.Clamp(_scrollSpeed, -1f, 1f);

        float _swayFinalAmount;

        if (_scrollSpeed < 0f)
            _swayFinalAmount = -swayCurve.Evaluate(_scrollSpeed) * -swayAmount;
        else
            _swayFinalAmount = swayCurve.Evaluate(_scrollSpeed) * -swayAmount;
        Vector3 _swayVector;

        _swayVector.z = _swayFinalAmount;

        transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, curTilt+_swayVector.z);
        m_xAmountPreviousFrame = m_xAmountThisFrame;
        

    }

    // void ChangeCursorState()
    // {
    //     Cursor.lockState = CursorLockMode.Locked;
    //     Cursor.visible = false;
    // }

    #region Setters
    public void SetTilt(float newVal)
    {
        wishTilt = newVal;
    }

    public void SetXSens(float newVal)
    {
        sensX = newVal;
    }

    public void SetYSens(float newVal)
    {
        sensY = newVal;
    }

    public void SetFov(float newVal)
    {
        baseFov = newVal;
    }
    #endregion
}
