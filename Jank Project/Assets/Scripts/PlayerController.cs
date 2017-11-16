using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Good practice if we need to assume we will have one of these
[RequireComponent(typeof(Controller2D_SL))]
[RequireComponent(typeof(Animator2D))]
public class PlayerController: MonoBehaviour
{
    Animator2D                                      m_Animator2D;

    PlayerCharacterState                            m_CharacterState;
    PlayerCharacterState.ECharacterFacing           m_eFacing;

    PlayerCharacterState.ECharacterWeaponState      m_eWeaponState;
    bool                                            m_bWeaponStateDirty;
    System.Timers.Timer                             m_tmWeaponStateTimer;
    Vector3                                         m_v3DefaultScale;

    StationaryCharacterState                        m_StationaryState;
    RunCharacterState                               m_RunState;
    JumpUpCharacterState                            m_JumpUpState;
    FallDownCharacterState                          m_FallDownState;

    //Quick teleport position
    public Vector3                                  m_v3QuickTeleportPosition = new Vector3(0,4.0f);

    //These are more intuitive ways to alter a character's jump height
    //instead of just a random velocity
    public float                                    m_fTimeToJumpApex = 0.5f;
    public float                                    m_fJumpHeight = 4;

    //We're solving for these, given our TimeToJumpApex and JumpHeight above

    //Solved via:   jumpHeight = (gravity*timeToJumpApex^2)/2
    //Aka:          deltaMovement = vInitial * time + (accel*time^2)/2
    //= (2.0f * m_fJumpHeight)/(m_fTimeToJumpApex*m_fTimeToJumpApex);
    float                                           m_fGravity; 

    //Solved via:   m_fJumpVelocity = m_fGravity * m_fTimeToJumpApex
    //Aka:          vFinal = vInitial + accel*time
    //= m_fGravity * m_fTimeToJumpApex
    float                                           m_fJumpVelocity; 

    float                                           m_fAccelerationTimeAirborn = 0.0f;
    float                                           m_fAccelerationTimeGrounded = 0.0f;

    float                                           m_fVelocityXSmoothing;

    Controller2D_SL                                 m_Controller2D;
    float                                           m_fMovementSpeed = 3.0f;
    Vector3                                         m_v3Velocity;
    Vector2                                         m_v2InputMovement;



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
        m_eWeaponState = PlayerCharacterState.ECharacterWeaponState.BlasterInactive;
        m_tmWeaponStateTimer = new System.Timers.Timer(333);
        m_tmWeaponStateTimer.AutoReset = false;
        m_tmWeaponStateTimer.Elapsed +=
            (object sender, System.Timers.ElapsedEventArgs e) =>
            {
                m_bWeaponStateDirty = true;
                m_eWeaponState = PlayerCharacterState.ECharacterWeaponState.BlasterInactive;
            };
            
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

        if (Input.GetKeyUp(KeyCode.Z))
        {
            m_bWeaponStateDirty = (m_eWeaponState != PlayerCharacterState.ECharacterWeaponState.BlasterActive);
            m_eWeaponState = PlayerCharacterState.ECharacterWeaponState.BlasterActive;
            m_tmWeaponStateTimer.Stop();
            m_tmWeaponStateTimer.Start();
        }

        if (m_bWeaponStateDirty)
        {
            m_CharacterState.CorrectWeaponState();
            CorrectWeaponStateAnimation();
            m_bWeaponStateDirty = false;
        }

        if (m_Controller2D.m_sCollisionData.m_bAbove
        || m_Controller2D.m_sCollisionData.m_bBelow)
        {
            m_v3Velocity.y = 0;
        }

        m_CharacterState.Update();
        m_CharacterState.HandleInput();

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
    }

    public bool IsGrounded()
    {
        return m_Controller2D.m_sCollisionData.m_bBelow;
    }

    public PlayerCharacterState.ECharacterWeaponState GetWeaponState()
    {
        return m_eWeaponState;
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

    void CorrectWeaponStateAnimation()
    {
        //Play the current animation associated with this state
        //BUT, we want to make sure we play from the frame of the animation 
        // we were just on, to preserve state (for example, if you pull out a 
        //blaster while running, we don't want the run animation to suddenly restart)
        if (m_Animator2D && !string.IsNullOrEmpty(m_CharacterState.AnimationName))
        {
            m_Animator2D.PlayFromCurrentFrame(m_CharacterState.AnimationName);
        }
    }

    public void EnterCharacterState(PlayerCharacterState.ECharacterState eState)
    {
        if (!m_Animator2D)
        {
            return;
        }

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
