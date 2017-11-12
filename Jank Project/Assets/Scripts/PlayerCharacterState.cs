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
    public abstract void HandleInput();
    public abstract void OnExit();
    public abstract ECharacterState CharacterState { get; }
    public string AnimationName { get { return m_sAnimationName; } }
    public PlayerController PlayerControllerScript{set { m_PlayerController = value; } }

    protected string m_sAnimationName;
    protected PlayerController m_PlayerController;

    public PlayerCharacterState()
    {
        //TODO: Make this happen
        //m_PlayerController = GetComponent
    }
}

abstract class GroundedCharacterState : PlayerCharacterState
{
    public override abstract ECharacterState CharacterState { get; }

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
        m_sAnimationName = "Idle";
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
        m_sAnimationName = "Run";
    }

    public override void Update()
    {
        base.Update();
    }

    public override void HandleInput()
    {
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
        m_sAnimationName = "JumpUp";
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
        m_sAnimationName = "FallDown";
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

    }

    public override void OnExit()
    {

    }
}
