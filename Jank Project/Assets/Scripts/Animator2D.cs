using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//using UnityEngine.Events

/*
    This is meant to be a simple 2D animation class, for animating 2D sprites
    The base of this code largely comes from Joe Strout's gamasutra article here:
    http://www.gamasutra.com/blogs/JoeStrout/20150807/250646/2D_Animation_Methods_in_Unity.php
*/

[System.Serializable]
public class Animation2D 
{
    public Sprite[] m_aFrames;
    public string m_szName;
    public float m_fFPS;
    public bool m_bLoop;
    public int m_nEndFrame;

    public float fDuration
    {
        get { return m_aFrames.Length * m_fFPS; }
        set { m_fFPS = value / m_aFrames.Length; }
    }
}

public class Animator2D : MonoBehaviour
{
    public List<Animation2D> m_Animations = new List<Animation2D>();
    private Animation2D m_CurrentAnimation;
    private SpriteRenderer m_srSpriteRenderer;
    private int m_nCurrentFrame;
    private bool m_bPlaying;

    private float m_fSecondsPerFrame;
    private float m_fNextFrameTime;

    public

    void Start()
    {
        m_srSpriteRenderer = GetComponent<SpriteRenderer>();
        if (m_Animations.Count > 0)
        {
            Play(0);
        }
    }

    void Update()
    {
        if (!m_bPlaying || Time.time < m_fNextFrameTime || m_srSpriteRenderer == null)
        {
            return;
        }

        ++m_nCurrentFrame;

        if (m_nCurrentFrame >= m_CurrentAnimation.m_aFrames.Length)
        {
            if (!m_CurrentAnimation.m_bLoop)
            {
                m_bPlaying = false;
                return;
            }
            m_nCurrentFrame = m_CurrentAnimation.m_nEndFrame;
        }

        m_srSpriteRenderer.sprite = m_CurrentAnimation.m_aFrames[m_nCurrentFrame];
        m_fNextFrameTime += m_fSecondsPerFrame;
    }

    public void PlayFromCurrentFrame(string szName)
    {
        Play(szName, m_nCurrentFrame);
    }

    public void Play(string szName, int nOverrideStartFrame = -1)
    {
        //Using a C# lambda here, as a predicate to FindIndex
        int nIndex = m_Animations.FindIndex(a => a.m_szName == szName);
        if (nIndex < 0)
        {
            //Didn't find any animation by that name
        }
        else
        {
            Play(nIndex, nOverrideStartFrame);
        }
    }

    public void Play(int nIndex, int nOverrideStartFrame = -1)
    {
        if (nIndex < 0)
        {
            return;
        }

        //DEBUGGING, to catch replaying animations
        if (m_CurrentAnimation == m_Animations[nIndex])
        {
            print("WARNING: Trying to play animation that is already playing");
        }

        //This setup is used so that once we hit the update loop, we will
        //immediately advance to the first frame of animation
        m_CurrentAnimation = m_Animations[nIndex];
        m_fSecondsPerFrame = 1.0f / m_CurrentAnimation.m_fFPS;
        m_bPlaying = true;
        m_fNextFrameTime = Time.time;

        m_nCurrentFrame = nOverrideStartFrame < m_CurrentAnimation.m_aFrames.Length 
            ? nOverrideStartFrame
            : -1;
    }

    public void Stop()
    {
        m_bPlaying = false;
    }

    public void Resume()
    {
        m_bPlaying = true;
    }

    public void FaceLeft()
    {
        m_srSpriteRenderer.flipX = true;
    }

    public void FaceRight()
    {
        m_srSpriteRenderer.flipX = false;
    }
}
