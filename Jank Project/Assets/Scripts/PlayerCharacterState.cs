using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerCharacterState
{
    public enum ECharacterState
    {
        Stationary,
        Run,
        JumpUp,
        FallDown
    }

    public enum ECharacterWeaponState
    {
        BlasterActive,
        BlasterInactive
    }

    public enum ECharacterFacing
    {
        None,
        Left,
        Right,
        Center
    }

    public abstract void OnEnter();
    public abstract void Update();
    public abstract void CorrectWeaponState();
    public abstract void HandleInput();
    public abstract void OnExit();
    public abstract ECharacterState CharacterState { get; }
    public string AnimationName { get { return m_sAnimationName; } }
    public PlayerController PlayerControllerScript{set { m_PlayerController = value; } }

    protected string m_sAnimationName;
    protected PlayerController m_PlayerController;
    protected bool m_bCaresAboutFacing = true;

    public PlayerCharacterState()
    {
        //TODO: Make this happen
        //m_PlayerController = GetComponent
    }
}

abstract class GroundedCharacterState : PlayerCharacterState
{
    public override abstract ECharacterState CharacterState { get; }
    public override abstract void CorrectWeaponState();

    public override void OnEnter()
    {
    }

    public override void Update()
    {
        if (!m_PlayerController.IsGrounded())
        {
            m_PlayerController.EnterCharacterState(ECharacterState.FallDown);
        }
    }


    public override void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            m_PlayerController.EnterCharacterState(ECharacterState.JumpUp);
        }

        if (m_bCaresAboutFacing)
        {
            float fMoveHoriz = Input.GetAxisRaw("Horizontal");
            if (fMoveHoriz != 0)
            {
                if (fMoveHoriz < 0)
                {
                    m_PlayerController.EnterFacing(ECharacterFacing.Left);
                }
                else
                {
                    m_PlayerController.EnterFacing(ECharacterFacing.Right);
                }
            }
        }
    }

    public override void OnExit()
    {
    }
}

class StationaryCharacterState : GroundedCharacterState
{
    public override ECharacterState CharacterState
    {
        get { return ECharacterState.Stationary; }
    }

    public override void OnEnter()
    {
        m_sAnimationName = m_PlayerController.GetWeaponState() == 
            PlayerCharacterState.ECharacterWeaponState.BlasterInactive 
            ? "Idle" 
            : "Idle_Blaster";
    }

    public override void CorrectWeaponState()
    {
        m_sAnimationName = m_PlayerController.GetWeaponState() ==
            PlayerCharacterState.ECharacterWeaponState.BlasterInactive
            ? "Idle"
            : "Idle_Blaster";
    }

    public override void Update()
    {
        base.Update();
        //Randomly ask the animator to play blink animation
        //Could also change animation name to show heavy breathing if damaged
    }

    public override void HandleInput()
    {
        //Solutions: return possible new state from handler, and only use base
        // input handler if substate can't handle it


        //POTENTIAL FUCKUP HERE
        //How to handle state changes gracefully...
        base.HandleInput();

        float fMoveHoriz = Input.GetAxisRaw("Horizontal");
        if (fMoveHoriz != 0)
        {
            if (fMoveHoriz < 0)
            {
                m_PlayerController.EnterFacing(ECharacterFacing.Left);
            }
            else
            {
                m_PlayerController.EnterFacing(ECharacterFacing.Right);
            }

            m_PlayerController.EnterCharacterState(ECharacterState.Run);
        }
    }

    public override void OnExit()
    {

    }
}

class RunCharacterState : GroundedCharacterState
{
    public override ECharacterState CharacterState
    {
        get { return ECharacterState.Run; }
    }

    public override void OnEnter()
    {
        m_sAnimationName = m_PlayerController.GetWeaponState() ==
            PlayerCharacterState.ECharacterWeaponState.BlasterInactive
            ? "Run"
            : "Run_Blaster";
    }

    public override void CorrectWeaponState()
    {
        m_sAnimationName = m_PlayerController.GetWeaponState() ==
            PlayerCharacterState.ECharacterWeaponState.BlasterInactive
            ? "Run"
            : "Run_Blaster";
    }

    public override void Update()
    {
        base.Update();
    }

    public override void HandleInput()
    {
        base.HandleInput();

        float fMoveHoriz = Input.GetAxisRaw("Horizontal");
        if (fMoveHoriz == 0)
        {
            m_PlayerController.EnterCharacterState(ECharacterState.Stationary);
        }
    }

    public override void OnExit()
    {

    }
}

abstract class MidairCharacterState : PlayerCharacterState
{
    public override abstract ECharacterState CharacterState { get; }
    public override abstract void CorrectWeaponState();

    protected int nMidairJumps = 0;
    protected int nJumpsRemaining;

    public override void OnEnter()
    {
        nJumpsRemaining = nMidairJumps;
    }

    public override void Update()
    {
        //Change to jump up once we're out of prejump
    }

    public override void HandleInput()
    {
        //Midair jumping
        /*
        if (Input.GetKeyDown(KeyCode.Space))
        {
            m_PlayerController.EnterCharacterState(ECharacterState.JumpUp);
        }
         */

        if (m_bCaresAboutFacing)
        {
            float fMoveHoriz = Input.GetAxisRaw("Horizontal");
            if (fMoveHoriz != 0)
            {
                if (fMoveHoriz < 0)
                {
                    m_PlayerController.EnterFacing(ECharacterFacing.Left);
                }
                else
                {
                    m_PlayerController.EnterFacing(ECharacterFacing.Right);
                }
            }
        }
    }

    public override void OnExit()
    {

    }

    public void ExpendJump()
    {
        --nJumpsRemaining;
    }
}

class JumpUpCharacterState : MidairCharacterState
{
    public override ECharacterState CharacterState
    {
        get { return ECharacterState.JumpUp; }
    }

    public override void OnEnter()
    {
        m_sAnimationName = m_PlayerController.GetWeaponState() ==
            PlayerCharacterState.ECharacterWeaponState.BlasterInactive
            ? "JumpUp"
            : "JumpUp_Blaster";
    }

    public override void CorrectWeaponState()
    {
        m_sAnimationName = m_PlayerController.GetWeaponState() ==
            PlayerCharacterState.ECharacterWeaponState.BlasterInactive
            ? "JumpUp"
            : "JumpUp_Blaster";
    }

    public override void Update()
    {
        //Change to jump up once we're out of prejump

        if (m_PlayerController.IsFalling())
        {
            m_PlayerController.EnterCharacterState(ECharacterState.FallDown);
        }
    }

    public override void HandleInput()
    {
        base.HandleInput();
    }

    public override void OnExit()
    {

    }
}

class FallDownCharacterState : MidairCharacterState
{
    public override ECharacterState CharacterState
    {
        get { return ECharacterState.FallDown; }
    }

    public override void OnEnter()
    {
        m_sAnimationName = m_PlayerController.GetWeaponState() ==
            PlayerCharacterState.ECharacterWeaponState.BlasterInactive
            ? "FallDown"
            : "FallDown_Blaster";
    }
    public override void CorrectWeaponState()
    {
        m_sAnimationName = m_PlayerController.GetWeaponState() ==
            PlayerCharacterState.ECharacterWeaponState.BlasterInactive
            ? "FallDown"
            : "FallDown_Blaster";
    }

    public override void Update()
    {
        if (m_PlayerController.IsGrounded())
        {
            //Probably need to handle running here..
            m_PlayerController.EnterCharacterState(ECharacterState.Stationary);
        }
    }

    public override void HandleInput()
    {
        base.HandleInput();
    }

    public override void OnExit()
    {

    }
}
