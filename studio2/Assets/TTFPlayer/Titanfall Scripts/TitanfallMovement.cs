using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitanfallMovement : MonoBehaviour
{
    //Ground
    public float crouchSpeed = 2f;
    public float groundSpeed = 4f;
    public float runSpeed = 6f;
    public float grAccel = 20f;


    //Air
    public float airSpeed = 3f;
    public float airAccel = 20f;

    //Jump
    public float jumpUpSpeed = 9.2f;
    public float dashSpeed = 6f;

    //Wall
    public float wallSpeed = 10f;
    public float wallClimbSpeed = 4f;
    public float wallAccel = 20f;
    public float wallRunTime = 3f;

    //I'm making wallStickiness not-public because IDK what it does yet
    float wallStickiness = 20f;

    //distance from wall before wallrunning gets triggered
    public float wallStickDistance = 1f;

    //this is the minimum angle at which a wall becomes runnable, probably should stay around 40f
    public float wallFloorBarrier = 40f;

    public float wallBanTime = 4f;
    Vector3 bannedGroundNormal;

    //Cooldowns
    bool canJump = true;
    bool canDJump = true;
    //wallban is used in fixedUpdate to check and see if player is already wallrunning, do not modify
    float wallBan = 0f;
    //wallrunTimer is used to apply inverse gravity and keep player off the wall, do not modify
    float wrTimer = 0f;
    //wallsticktimer is also used in fixedUpdate to check and see if player is already wallrunning, do not modify
    float wallStickTimer = 0f;
    
    //States
    bool running;
    bool jump;
    bool crouched;

    //the grounded boolean is used to check if the player is currently in contact with *A WALL* or *A FLOOR*, it is not specific to just the "ground"
    //this lets us ensure we don't double-wallrun or wallrun-on-the-ground or any other silly things later on
    public bool grounded;

    Collider ground;

    Vector3 groundNormal = Vector3.up;

    CapsuleCollider col;
    #region HeadBod
    public float idleBodSpeed = 10f;
    public float idleBodAmount = 0.02f;
    public float walkBobSpeed = 14f;
    public float walkBobAmount = 0.05f;
    public float sprintBobSpeed = 18f;
    public float sprintBobAmount = 0.1f;
    public float crouchBobSpeed = 8f;
    public float crouchBobAmount = 0.025f;

    public float defaultCamXpos = 0;
    public float defaultCamYpos = 0;
    float timer;
    float offset_y
        , offset_x;

    #endregion


    public enum Mode
    //this controlled uses modes to manage which inputs are valid at any point in time and to change vector from wall to ground to air
    {
        Walking,
        Flying,
        Wallruning
    }
    public Mode mode = Mode.Flying;

    TTFCameraController camCon;
    Rigidbody rb;
    public Vector3 dir = Vector3.zero;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        camCon = GetComponentInChildren<TTFCameraController>();
        col = GetComponent<CapsuleCollider>();
        defaultCamXpos = camCon.mainCamera.transform.localPosition.x;
        defaultCamYpos = camCon.mainCamera.transform.localPosition.y;

    }
    void Start()
    {
        //on start pull the RB, CapsuleCollider, and the special cameraController script

    }

    void OnGUI()
    {
        //for debug purposes, would love to see an accelerometer in final game too tho
        GUILayout.Label("Spid: " + new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude);
        GUILayout.Label("SpidUp: " + rb.velocity.y);
    }

    void Update()
    {
        //dynamicFriction is only mentioned once here - is this a built in Unity thing? if we modify what does it do? (0f by default)
        col.material.dynamicFriction = 0f;
        //set dir == to current player direction on each update frame
        dir = Direction();
        //Debug.Log("dir" + dir);
        //in update we only check for run, crouch, and jump - these 3 inputs work in all modes, independent of mode
        running = (Input.GetKey(KeyCode.LeftShift) && Input.GetAxisRaw("Vertical") > 0.9);
        //crouched = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C));
        if(Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKey(KeyCode.C))
        {
            if(!crouched)
            {
                crouched = true;
            }
            else
            {
                crouched = false;
            }
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jump = true;
        }

        //Special use
        //if (Input.GetKeyDown(KeyCode.T)) transform.position = new Vector3(0f, 30f, 0f);
        //if (Input.GetKeyDown(KeyCode.X)) rb.velocity = new Vector3(rb.velocity.x, 40f, rb.velocity.z);
        //if (Input.GetKeyDown(KeyCode.V)) rb.AddForce(dir * 20f, ForceMode.VelocityChange);
    }

    void FixedUpdate()
    {
        HandleHeadbob(dir);
        //set collider height lower when crouched
        if (crouched)
        {
            col.height = Mathf.Max(0.6f, col.height - Time.deltaTime * 10f);
        }
        else
        {
            col.height = Mathf.Min(1.8f, col.height + Time.deltaTime * 10f);
        }

        //this checks to see if the player has recently wall run and sets the ground vector as non-wall-runnable
        if (wallStickTimer == 0f && wallBan > 0f)
        {
            bannedGroundNormal = groundNormal;
        }
        else
        {
            bannedGroundNormal = Vector3.zero;
        }

        //this checks to see if the player has been on a wall in the past FixedUpdate, if true
        //the player cannot re-stick to a new wall. This avoids double-wall-running
        wallStickTimer = Mathf.Max(wallStickTimer - Time.deltaTime, 0f);
        wallBan = Mathf.Max(wallBan - Time.deltaTime, 0f);

        //when mode switches, run the function for the given mode
        switch (mode)
        {
            //wallrunning applies a camera tilt. we also need to set up wrTimer here so we can track and apply inverse gravity
            //inside the Wallrun function
            case Mode.Wallruning:
                camCon.SetTilt(WallrunCameraAngle());
                Wallrun(dir, wallSpeed, wallClimbSpeed, wallAccel);
                if (ground.tag != "InfiniteWallrun") wrTimer = Mathf.Max(wrTimer - Time.deltaTime, 0f);
                break;

            //walking is boring
            case Mode.Walking:
                camCon.SetTilt(0);
                Walk(dir, crouched ? crouchSpeed : running ? runSpeed : groundSpeed, grAccel);
                break;

            //flying is slightly less boring but still boring
            case Mode.Flying:
                camCon.SetTilt(0);
                AirMove(dir, airSpeed, airAccel);
                break;
        }

        jump = false;
    }



    private Vector3 Direction()
    //default Unity controls and direction get pulled here
    {
        float hAxis = Input.GetAxisRaw("Horizontal");
        float vAxis = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3(hAxis, 0, vAxis);
        return rb.transform.TransformDirection(direction);

    }



    #region Collisions
    void OnCollisionStay(Collision collision)
    {
        if (collision.contactCount > 0)
        {
            float angle;
            foreach (ContactPoint contact in collision.contacts)
            //on collision we find the angle of the surface relative to the Y axis
            //then compare to our wallFloorBarrier, if it's below we get rejected and enter walk state
            //if it's above, we enter WallRun state
            {
                //angle to compare against the wall-to-floor angle minimum is the angle perpendicular and up from the contact point's normal angle 
                //unity can give us the contact.normal (this is perpendicular to the wall-run-surface), we can use Vector3.up to get the perpendicular & up angle from this.
                angle = Vector3.Angle(contact.normal, Vector3.up);

                //if our up angle is less  than the wallfloorbarrier, we get kicked back into walk mode
                if (angle < wallFloorBarrier)
                {
                    EnterWalking();
                    grounded = true;
                    groundNormal = contact.normal;
                    ground = contact.otherCollider;
                    return;
                }
            }

            //before checking wallFloorBarrier we first check distance to ground. 
            //if player is basically on the ground, no wallrun for you
            if (VectorToGround().magnitude > 0.2f)
            {
                grounded = false;
            }

            //if our surface of contact is sufficiently perpendicular to the ground AND we are not grounded (not yet attached to a wall or on the ground)
            //then we can finally trigger wallrun
            if (grounded == false)
            {
                foreach (ContactPoint contact in collision.contacts)
                {
                    if (contact.otherCollider.tag != "NoWallrun" && contact.otherCollider.tag != "Player" && mode != Mode.Walking)
                    {
                        //if player successfully hits a wall between our barrier angle and 120degrees (nearly a ceiling)
                        //then they can enter WallRun state
                        angle = Vector3.Angle(contact.normal, Vector3.up);
                        if (angle > wallFloorBarrier && angle < 120f)

                        {
                            //set grounded to true and enter wallrun so player cannot enter wallrun on a second wall and then immediately enters
                            //wallrun on the contact wall
                            grounded = true;
                            groundNormal = contact.normal;
                            ground = contact.otherCollider;
                            EnterWallrun();
                            return;
                        }
                    }
                }
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        //when exiting a wall/zero collisions, send player into Flying mode
        if (collision.contactCount == 0)
        {
            EnterFlying();
        }
    }
    #endregion



    #region Entering States

    //this section is *technically* just to help the player transition into a walking mode
    //BUT what it's really doing is checking to see if we can "reward" the player with a successful b-Hop
    //if the player is using b-Hop or counterstrike surfing techniques, they'll get rewarded with a slight speed buff when their mode changes from flying to walking
    //if they continue walking the friction will slow them down quickly but if they enter flying right away this will simulate the slow speed build up from counterstrike surfing style movement
    void EnterWalking()
    {
        //if the player is not walking *and* can jump (this means the player is in flying mode and either has a double jump or just collided with a surface, which gifts them back their jumps)
        //then we can check to see if the conditions are correct for a bunny hop when the player touches the ground
        if (mode != Mode.Walking && canJump)
        {
            //to b-Hop, the player must be coming *from* a flying mode and must have the crouch button pressed on contact with the ground
            if (mode == Mode.Flying && crouched)
            {
                //add a bit of force on EnterWalking if Mode.Flying is enable and crouch is being held
                rb.AddForce(-rb.velocity.normalized, ForceMode.VelocityChange);
            }
            //this is for a camera effect if we want to visually reward players for b-Hopping, not currently used yet
            if (rb.velocity.y < -1.2f)
            {
                //camCon.Punch(new Vector2(0, -3f));
            }
            //bHopCoroutine with Leniency is probably a way to add a "forgiveness" period to bunny hopping (surfing)
            //we should DEFINITELY add a leniency period to bunny hopping, current controll is like frame-perfect and hard
            //StartCoroutine(bHopCoroutine(bhopLeniency));
            gameObject.SendMessage("OnStartWalking");
            mode = Mode.Walking;
        }
    }

    void EnterFlying(bool wishFly = false)
    {
        //for obvious reasons, if the player is entering flying, they are not grounded
        grounded = false;

        //check to ensure player is not wallrunning
        if (mode == Mode.Wallruning && VectorToWall().magnitude < wallStickDistance && !wishFly)
        {
            return;
        }
        else if (mode != Mode.Flying)
        {

            //if in air, reset wallBan, turn double jump back on, set mode to flying
            wallBan = wallBanTime;
            canDJump = true;
            mode = Mode.Flying;
        }
    }

    void EnterWallrun()
    {
        if (mode != Mode.Wallruning)
        {
            //check to ensure the player is actually on a wall and not on the ground - no wall running on the floor!
            if (VectorToGround().magnitude > 0.2f && CanRunOnThisWall(bannedGroundNormal) && wallStickTimer == 0f)
            {
                //on a true wallrun, start timer (for anti-gravity function), reset the double jump bool, enter wallrun
                gameObject.SendMessage("OnStartWallrunning");
                wrTimer = wallRunTime;
                canDJump = true;
                mode = Mode.Wallruning;
            }
            else
            {
                //if we're not wallrunning we're flying
                EnterFlying(true);
            }
        }
    }
    #endregion



    #region Movement Types
    //walking is boring but unfortunately we still need to code it
    void Walk(Vector3 wishDir, float maxSpeed, float acceleration)
    {
        //just a lil jump debug and function
        if (jump && canJump)
        {
            gameObject.SendMessage("OnJump");
            Jump();
        }
        //if we're not calling the jump function, we'll continue walking
        else
        {
            //if (crouched) acceleration = 0.5f;

            //here we're going to clamp player speed while walking to our walk max. if players continue to walk without using jump or b-Hop strategies to maintain speed, they'll slow down
            //***NOTE FOR ZACH*** - if we want to make the mech feel "heavy" and "slow" I think we should increase the rate at which the player slows down during walk or even run modes 
            //this should give the sense that when not using jumpjets or rollerblades the player is heavy/weighted
            wishDir = wishDir.normalized;
            Vector3 spid = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            //here's the fun but where we slow down the player if they're going too fast for our walk mode
            if (spid.magnitude > maxSpeed) acceleration *= spid.magnitude / maxSpeed;
            Vector3 direction = wishDir * maxSpeed - spid;

            //not entirely sure what this section does, I think it is to smooth out the deceleration of the player once they stop hitting any directional input keys.
            //direction is pulled directly from default Unity input keys, so it will only have this low of a magnitude when the player is not using any directional input keys
            //using the magnitude and accceleration is probably done to give the player a smooth decceleration curve after they stop inputting direction keys.
            if (direction.magnitude < 0.5f)
            {
                acceleration *= direction.magnitude / 0.5f;
            }

            direction = direction.normalized * acceleration;
            float magn = direction.magnitude;
            direction = direction.normalized;
            direction *= magn;

            //slope correction here is specifically targeting the y-axis decceleration of the player.
            //this allows us to give the falling/floaty feel a different decceleration curve than the directional movement, I think?
            //**NOTE TO ZACH*** - here's another spot we could play with to give the mech a "heavier" feel - we can tweak the slope correction curve to make the player deccelerate on the Y axis faster than they do on the Z or X?
            Vector3 slopeCorrection = groundNormal * Physics.gravity.y / groundNormal.y;
            slopeCorrection.y = 0f;
            //if(!crouched)
            direction += slopeCorrection;

            //at the end we AddForce based off current input keys! 
            rb.AddForce(direction, ForceMode.Acceleration);
        }
    }

    //here's a fun one finally, moving in the air!
    void AirMove(Vector3 wishDir, float maxSpeed, float acceleration)
    {
        if (jump && !crouched)
        {
            gameObject.SendMessage("OnDoubleJump");
            DoubleJump(wishDir);
        }

        //**IMPORTANT FEATURE REQUEST** we should make this inputKey public, this is a unique airJump functionv- we should standardize the jump key across all modes into a public KeyCode
        //**BROADLY WE SHOULD MAKE FULLY CUSTOMIZABLE AND PUBLIC KEYCODES FOR EVERYTHING**
        //if player is 1) in the air, 2) crouched, ANDDD 3) has a y-axis velocity greater than 10, and they hit [SPACE], then we'll let them jump
        //I'm a little confused by this one, reminde me to playtest if crouching breaks double jump and lets me triple jump, or if crouching exhibits any weird jump behaviour when falling fast. I'll climb something tall
        if (crouched && rb.velocity.y > -10 && Input.GetKey(KeyCode.Space))
        {
            rb.AddForce(Vector3.down * 20f, ForceMode.Acceleration);
        }


        float projVel = Vector3.Dot(new Vector3(rb.velocity.x, 0f, rb.velocity.z), wishDir); // Vector projection of Current velocity onto accelDir.
        float accelVel = acceleration * Time.deltaTime; // Accelerated velocity in direction of movment

        // If necessary, truncate the accelerated velocity so the vector projection does not exceed max_velocity
        if (projVel + accelVel > maxSpeed)
            accelVel = Mathf.Max(0f, maxSpeed - projVel);

        //basically, these let you strafe in midair more like the source engine, I think. The faster you're going the harder it will be to turn quickly, because we're using your speed and acceleration to constrain 
        //your change in direction when in air mode
        //I *could* be wrong, we should probably not mess with this code though, I think we can get Zach's gravity other ways
        rb.AddForce(wishDir.normalized * accelVel, ForceMode.VelocityChange);
    }

    //another fun one - wallrunning!
    void Wallrun(Vector3 wishDir, float maxSpeed, float climbSpeed, float acceleration)
    {
        //if we jump while wallrunning, we'll need to exit the wallrun
        if (jump)
        {
            //Vertical velocity change on exiting a wallrun - different from jumping in midair, since we need to push the player up slightly but more off the wall
            float upForce = Mathf.Clamp(jumpUpSpeed - rb.velocity.y, 0, Mathf.Infinity);
            rb.AddForce(new Vector3(0, upForce, 0), ForceMode.VelocityChange);

            //Horizontal velocity change on exiting a wallrun - different from jumping in midair, we will push the player further sideways than a normal jump, and sort of 
            //ignore the player camera and direction inputs a little - pushing off the wall is more important here
            Vector3 jumpOffWall = groundNormal.normalized;
            jumpOffWall *= dashSpeed;
            jumpOffWall.y = 0;
            rb.AddForce(jumpOffWall, ForceMode.VelocityChange);
            wrTimer = 0f;
            //don't forget to enter flying mode! we're not wallrunning anymore
            EnterFlying(true);
        }
        //if wallrunning timer runs out, it means we've reset the timer becaues the player has lost contact with a wall - the wall has run out or the player fell off for some reason
        //crouching mid-wall-run disconnects the player from the wall in titanfall, this is something we *could* change if we decide it's a layer of unnecessary difficulty
        else if (wrTimer == 0f || crouched)
        {
            //add force to kick the player off the ground when we enter flying
            rb.AddForce(groundNormal * 3f, ForceMode.VelocityChange);
            EnterFlying(true);
        }
        else
        {
            //Horizontal velocity if we continue wallrunning!
            Vector3 distance = VectorToWall();
            wishDir = RotateToPlane(wishDir, -distance.normalized);
            wishDir *= maxSpeed;
            wishDir.y = Mathf.Clamp(wishDir.y, -climbSpeed, climbSpeed);
            Vector3 wallrunForce = wishDir - rb.velocity;
            if (wallrunForce.magnitude > 0.2f) wallrunForce = wallrunForce.normalized * acceleration;

            //Vertical velocity if we continue wallrunning!
            if (rb.velocity.y < 0f && wishDir.y > 0f) wallrunForce.y = 2f * acceleration;

            //Anti-gravity force
            //this counteracts gravity based on how long the player is on the wall - each tick the player is wallrunning
            //the game adds an inverse gravity piece to keep the player on the wall
            Vector3 antiGravityForce = -Physics.gravity;
            //wrTimer is a variable set in wallrunning - it's wall-run-timer, length of time on a wall
            //it'll increase each frame the player is on the wall
            if (wrTimer < 0.33 * wallRunTime)
            {
                antiGravityForce *= wrTimer / wallRunTime;
                wallrunForce += (Physics.gravity + antiGravityForce);
            }

            //Forces - wallrun to stick player to the wall
            rb.AddForce(wallrunForce, ForceMode.Acceleration);
            //antigravity to keep the player from sliding down the wall
            rb.AddForce(antiGravityForce, ForceMode.Acceleration);

            //not sure exactly what this does, it either keeps the player stuck to the wall or it stops the player from clipping through the wall
            if (distance.magnitude > wallStickDistance) distance = Vector3.zero;
            rb.AddForce(distance * wallStickiness, ForceMode.Acceleration);
        }

        //if player loses the grounding state, force them back into flying. add a slight timer to stop player from entering a new wallrun for about half a second
        if (!grounded)
        {
            wallStickTimer = 0.2f;
            EnterFlying();
        }
    }

    //what does jump do??? here we go:
    void Jump()
    {
        //if we're walking, we can jump. flying off a wall we only receive a double jump back, the jump to exit the wall counts as our first jump and is calculated inside the wallrun functions, not using the jump function
        if (mode == Mode.Walking && canJump)
        {
            //add a little jumpy jump math right here
            float upForce = Mathf.Clamp(jumpUpSpeed - rb.velocity.y, 0, Mathf.Infinity);
            rb.AddForce(new Vector3(0, upForce, 0), ForceMode.VelocityChange);
            //start a coroutine to stop the player from jumping again for a sec and enter flying state!
            StartCoroutine(jumpCooldownCoroutine(0.2f));
            EnterFlying(true);
        }
    }
    //jump, but for a second time!
    void DoubleJump(Vector3 wishDir)
    {
        //make sure we haven't used our double jump already first
        if (canDJump)
        {
            //Vertical force on double jump - because we should *always* be flying when in double jump, we want a slightly different feeling here
            float upForce = Mathf.Clamp(jumpUpSpeed - rb.velocity.y, 0, Mathf.Infinity);

            rb.AddForce(new Vector3(0, upForce, 0), ForceMode.VelocityChange);

            //Horizontal force on a double jump - because we should *always* be flying, we want a diff feeling vs. the vertical (y axis) above
            //we're also checking to see if wishDir is non-zero. If wishDir is zero, then the player has not input any direction keys so we'll just add vertical force, no X/Z force
            if (wishDir != Vector3.zero)
            {
                Vector3 horSpid = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                Vector3 newSpid = wishDir.normalized;
                float newSpidMagnitude = dashSpeed;

                //I think this is to make sure we clamp new acceleration from double jumping to avoid overly speeding up here. double jump shouldn't add more speed, only b-Hopping or first jump adds speed.
                //technically we could change this in the future but it will probably make the mech feel lighter not heavier
                if (horSpid.magnitude > dashSpeed)
                {
                    float dot = Vector3.Dot(wishDir.normalized, horSpid.normalized);
                    if (dot > 0)
                    {
                        newSpidMagnitude = dashSpeed + (horSpid.magnitude - dashSpeed) * dot;
                    }
                    else
                    {
                        newSpidMagnitude = Mathf.Clamp(dashSpeed * (1 + dot), dashSpeed * (dashSpeed / horSpid.magnitude), dashSpeed);
                    }
                }

                newSpid *= newSpidMagnitude;

                rb.AddForce(newSpid - horSpid, ForceMode.VelocityChange);
            }
            //set double jump to false so we can't do it until after a new surface contact event
            canDJump = false;
        }
    }

    void HandleHeadbob(Vector3 dir)
    {
        Debug.Log(mode);
        if (Mathf.Abs(dir.magnitude)>0)
        {
            timer += Time.deltaTime * (crouched? crouchBobSpeed:running||mode==Mode.Wallruning? sprintBobSpeed:walkBobSpeed) ;
            camCon.mainCamera.transform.localPosition = new Vector3
                (defaultCamXpos + Mathf.Cos(timer/2) *1.5f* (crouched ? crouchBobAmount : running || mode == Mode.Wallruning ? sprintBobAmount : walkBobAmount),
                 defaultCamYpos + Mathf.Sin(timer) * (crouched ? crouchBobAmount : running || mode == Mode.Wallruning ? sprintBobAmount : walkBobAmount),
                 camCon.mainCamera.transform.localPosition.z);
        }
        else
        {
            offset_y = camCon.mainCamera.transform.localPosition.y - defaultCamYpos;
            offset_x = camCon.mainCamera.transform.localPosition.x - defaultCamXpos;


            timer += Time.deltaTime * idleBodSpeed;
            #region removing offsetY

            if (offset_y > 0)
            {
                offset_y -= Time.deltaTime;
                offset_y = offset_y < 0 ? 0 : offset_y;
            }
            else
            {
                offset_y += Time.deltaTime;
                offset_y = offset_y > 0 ? 0 : offset_y;
            }

            #endregion
            #region removing offsetX
            if (offset_x > 0)
            {
                offset_x -= Time.deltaTime ;
                offset_x = offset_x < 0 ? 0 : offset_x;
            }
            else
            {
                offset_x += Time.deltaTime;
                offset_x = offset_x > 0 ? 0 : offset_x;
            }
            #endregion
            //Debug.Log("offset X" + offset_x);
            //Debug.Log("offset Y" + offset_y);

            if (Mathf.Abs(offset_y) > 0&& Mathf.Abs(offset_x)>0)
            {
                camCon.mainCamera.transform.localPosition = new Vector3
                    (defaultCamYpos + Mathf.Cos(timer/2) * 1.5f*idleBodAmount + offset_x,
                    defaultCamYpos + Mathf.Sin(timer) * idleBodAmount + offset_y,
                    camCon.mainCamera.transform.localPosition.z);
            }
            else
            {
                camCon.mainCamera.transform.localPosition = new Vector3
                    (defaultCamXpos + Mathf.Cos(timer / 2) * 1.5f * idleBodAmount,
                    defaultCamYpos + Mathf.Cos(timer) * idleBodAmount,
                    camCon.mainCamera.transform.localPosition.z);
            }

        }
    }
    

    #endregion



    //the below functions are all fancy mathy bits to support the above modes and functions
    //I don't think we should ever ever mess with these. We should be able to modify this controller as much as we want 
    //just with the above mode and player centric functions
    #region MathGenious
    Vector2 ClampedAdditionVector(Vector2 a, Vector2 b)
    {
        float k, x, y;
        k = Mathf.Sqrt(Mathf.Pow(a.x, 2) + Mathf.Pow(a.y, 2)) / Mathf.Sqrt(Mathf.Pow(a.x + b.x, 2) + Mathf.Pow(a.y + b.y, 2));
        x = k * (a.x + b.x) - a.x;
        y = k * (a.y + b.y) - a.y;
        return new Vector2(x, y);
    }

    Vector3 RotateToPlane(Vector3 vect, Vector3 normal)
    {
        Vector3 rotDir = Vector3.ProjectOnPlane(normal, Vector3.up);
        Quaternion rotation = Quaternion.AngleAxis(-90f, Vector3.up);
        rotDir = rotation * rotDir;
        float angle = -Vector3.Angle(Vector3.up, normal);
        rotation = Quaternion.AngleAxis(angle, rotDir);
        vect = rotation * vect;
        return vect;
    }

    float WallrunCameraAngle()
    {
        Vector3 rotDir = Vector3.ProjectOnPlane(groundNormal, Vector3.up);
        Quaternion rotation = Quaternion.AngleAxis(-90f, Vector3.up);
        rotDir = rotation * rotDir;
        float angle = Vector3.SignedAngle(Vector3.up, groundNormal, Quaternion.AngleAxis(90f, rotDir) * groundNormal);
        angle -= 90;
        angle /= 180;
        Vector3 playerDir = transform.forward;
        Vector3 normal = new Vector3(groundNormal.x, 0, groundNormal.z);

        return Vector3.Cross(playerDir, normal).y * angle;
    }

    bool CanRunOnThisWall(Vector3 normal)
    {
        if (Vector3.Angle(normal, groundNormal) > 10 || wallBan == 0f)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    Vector3 VectorToWall()
    {
        Vector3 direction;
        Vector3 position = transform.position + Vector3.up * col.height / 2f;
        RaycastHit hit;
        if (Physics.Raycast(position, -groundNormal, out hit, wallStickDistance) && Vector3.Angle(groundNormal, hit.normal) < 70)
        {
            groundNormal = hit.normal;
            direction = hit.point - position;
            return direction;
        }
        else
        {
            return Vector3.positiveInfinity;
        }
    }

    Vector3 VectorToGround()
    {
        Vector3 position = transform.position;
        RaycastHit hit;
        if (Physics.Raycast(position, Vector3.down, out hit, wallStickDistance))
        {
            return hit.point - position;
        }
        else
        {
            return Vector3.positiveInfinity;
        }
    }
    #endregion



    #region Coroutines
    IEnumerator jumpCooldownCoroutine(float time)
    {
        canJump = false;
        yield return new WaitForSeconds(time);
        canJump = true;
    }
    #endregion
}
