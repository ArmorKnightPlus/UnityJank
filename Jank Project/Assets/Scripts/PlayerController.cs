using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Good practice if we need to assume we will have one of these
[RequireComponent(typeof(Controller2D_SL))]
[RequireComponent(typeof(Animator2D))]
public class PlayerController: MonoBehaviour
{
    Animator2D m_Animator2D;

    PlayerCharacterState m_CharacterState;
    PlayerCharacterState.ECharacterFacing m_eFacing;
    Vector3 m_v3DefaultScale;

    StationaryCharacterState m_StationaryState;
    RunCharacterState m_RunState;
    JumpUpCharacterState m_JumpUpState;
    FallDownCharacterState m_FallDownState;

    //Quick teleport position
    public Vector3 m_v3QuickTeleportPosition = new Vector3(0,4.0f);

    //These are more intuitive ways to alter a character's jump height
    //instead of just a random velocity
    public float m_fTimeToJumpApex = 0.5f;
    public float m_fJumpHeight = 4;

    //We're solving for these, given our TimeToJumpApex and JumpHeight above

    //Solved via:   jumpHeight = (gravity*timeToJumpApex^2)/2
    //Aka:          deltaMovement = vInitial * time + (accel*time^2)/2
    float m_fGravity; //= (2.0f * m_fJumpHeight)/(m_fTimeToJumpApex*m_fTimeToJumpApex);

    //Solved via:   m_fJumpVelocity = m_fGravity * m_fTimeToJumpApex
    //Aka:          vFinal = vInitial + accel*time
    float m_fJumpVelocity; //= m_fGravity * m_fTimeToJumpApex

    float m_fAccelerationTimeAirborn = 0.0f;
    float m_fAccelerationTimeGrounded = 0.0f;

    float m_fVelocityXSmoothing;

    Controller2D_SL m_Controller2D;
    float m_fMovementSpeed = 3.0f;
    Vector3 m_v3Velocity;
    Vector2 m_v2InputMovement;



    void Start()
    {
        m_Animator2D = GetComponent<Animator2D>();
        m_Controller2D = GetComponent<Controller2D_SL>();

        m_v3DefaultScale = transform.localScale;

        m_fGravity = -(2.0f * m_fJumpHeight) / (m_fTimeToJumpApex * m_fTimeToJumpApex);
        m_fJumpVelocity = Mathf.Abs(m_fGravity) * m_fTimeToJumpApex;
        print("Gravity: " + m_fGravity + "|| JumpVel: " + m_fJumpVelocity);

        //Give the character states access this script

        m_StationaryState = new StationaryCharacterState();
        m_RunState = new RunCharacterState();
        m_JumpUpState = new JumpUpCharacterState();
        m_FallDownState = new FallDownCharacterState();

        m_StationaryState.PlayerControllerScript = this;
        m_RunState.PlayerControllerScript = this;
        m_JumpUpState.PlayerControllerScript = this;
        m_FallDownState.PlayerControllerScript = this;

        EnterCharacterState(PlayerCharacterState.ECharacterState.Stationary);
    }

    //Get movement from input every frame, before physics is applied
    void FixedUpdate()
    {

    }

    void Update()
    {
        //Quick reset for our character's position
        if (Input.GetKeyUp(KeyCode.F))
        {
            transform.position = m_v3QuickTeleportPosition;
            m_v3Velocity = Vector3.zero;
        }

        if (m_Controller2D.m_sCollisionData.m_bAbove
            || m_Controller2D.m_sCollisionData.m_bBelow)
        {
            m_v3Velocity.y = 0;
        }

        m_CharacterState.Update();
        m_CharacterState.HandleInput();

        //Handled in state
        /*
        if (Input.GetKeyDown(KeyCode.Space) && CanJump())
        {
            m_v3Velocity.y = m_fJumpVelocity;
            EnterCharacterState(ECharacterState. PreJumpUp);
        }
         */

        //m_v3Velocity.y = m_fJumpVelocity;
        float moveHoriz = Input.GetAxisRaw("Horizontal");
        float moveVert = Input.GetAxisRaw("Vertical");
        m_v2InputMovement = new Vector2(moveHoriz, moveVert);        

        //apply player input to our horizontal velocity
        //we're using a smoothing function to make direction change not so abrupt
        float fTargetVelocityX = m_v2InputMovement.x * m_fMovementSpeed;
        m_v3Velocity.x = Mathf.SmoothDamp(m_v3Velocity.x, fTargetVelocityX, ref m_fVelocityXSmoothing, 
            m_Controller2D.m_sCollisionData.m_bBelow ? m_fAccelerationTimeGrounded : m_fAccelerationTimeAirborn);

        //apply gravity to our vertical velocity
        m_v3Velocity.y += m_fGravity * Time.deltaTime;
        m_Controller2D.Move(m_v3Velocity * Time.deltaTime);


        /*
            It might be good to have two different types of states; motion and shooting
            Since shooting can happen in parallel with almost any form of motion, it would 
            make sense for it to be covered under a different type of state.

            It might be as simple as adding a flag, but would likely be more complicated 
            considering you'd hold your blaster out for a short time after a shot is fired.

            This, it might be more appropriate to name the changing function
                EnterCharacterMotionState
            Then, have motion state enums
        */

        //if (m_Controller2D.m_sCollisionData.m_bBelow)
        //{
        //    if (m_v3Velocity.x == 0)
        //    {
        //        ContinueOrEnterCharacterState(ECharacterState.Idle, m_eFacing);
        //    }

        //    else if (m_v3Velocity.x < 0)
        //    {
        //        ContinueOrEnterCharacterState(ECharacterState.Run, ECharacterFacing.Left);
        //    }

        //    else
        //    {
        //        //We should probably defer to some action here, like shooting
        //        //But for now let's just put Idle
        //        ContinueOrEnterCharacterState(ECharacterState.Run, ECharacterFacing.Right);
        //    }
        //}
    }

    public bool IsGrounded()
    {
        return m_Controller2D.m_sCollisionData.m_bBelow;
    }

    public bool IsFalling()
    {
        return (!m_Controller2D.m_sCollisionData.m_bBelow && m_v3Velocity.y < 0);
    }

    void ContinueOrEnterCharacterState(PlayerCharacterState.ECharacterState eState, PlayerCharacterState.ECharacterFacing eFacing)
    {
        if (eState != m_CharacterState.CharacterState || eFacing != m_eFacing)
        {
            EnterFacing(eFacing);
            EnterCharacterState(eState);
        }
    }

    public void EnterCharacterState(PlayerCharacterState.ECharacterState eState)
    {
        if (!m_Animator2D)
            return;
        if (m_CharacterState != null)
        {
            m_CharacterState.OnExit();
        }

        switch (eState)
        {
            case PlayerCharacterState.ECharacterState.Stationary:
            {
                m_CharacterState = m_StationaryState;
            }
            break;

            case PlayerCharacterState.ECharacterState.Run:
            {
                m_CharacterState = m_RunState;
            }
            break;

            case PlayerCharacterState.ECharacterState.JumpUp:
            {
                //WHY DOESN'T THIS WORK
                m_v3Velocity.y = m_fJumpVelocity;
                m_CharacterState = m_JumpUpState;
            }
            break;

            case PlayerCharacterState.ECharacterState.FallDown:
            {
                    m_CharacterState = m_FallDownState;
            }
            break;
        }

        m_CharacterState.OnEnter();

        //Play the current animation associated with this state
        if (!string.IsNullOrEmpty(m_CharacterState.AnimationName))
        {
            m_Animator2D.Play(m_CharacterState.AnimationName);
        }
    }

    public void EnterFacing(PlayerCharacterState.ECharacterFacing eFacing)
    {
        switch (eFacing)
        {
            case PlayerCharacterState.ECharacterFacing.Left:
            {
                m_Animator2D.FaceLeft();
                //transform.localScale = new Vector3(m_v3DefaultScale.x * -1.0f, m_v3DefaultScale.y, m_v3DefaultScale.z);
            }
            break;

            case PlayerCharacterState.ECharacterFacing.Right:
            {
                m_Animator2D.FaceRight();
                //transform.localScale = m_v3DefaultScale;
            }
            break;
        }

        m_eFacing = eFacing;
    }
}
