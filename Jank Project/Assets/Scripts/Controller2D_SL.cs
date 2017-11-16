using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
    This works in tandem with the PlayerController to create 2D platformer physics
    for the player. The majority of this code has been taken from the 2D platformer 
    tutorial videos by Sebastian Lague, though I've made a few alterations.

    The first video in his series can be found here:
    https://www.youtube.com/watch?v=MbWK8bCAU2w
*/


[RequireComponent(typeof(BoxCollider2D))]
public class Controller2D_SL : MonoBehaviour
{
    struct SRaycastOrigins
    {
        public Vector2 m_v2TopLeft;
        public Vector2 m_v2TopRight;
        public Vector2 m_v2BottomLeft;
        public Vector2 m_v2BottomRight;
    }

    public struct SCollisionData
    {
        public bool m_bAbove, m_bBelow, m_bLeft, m_bRight;
        public bool m_bClimbingSlope;
        public bool m_bDescendingSlope;
        public float m_fSlopeAngle;
        public float m_fSlopeAngleOld;

        public Vector3 m_v3VelocityOld;

        public void ResetData()
        {
            m_bAbove = m_bBelow = m_bLeft = m_bRight = false;
            m_bClimbingSlope = false;
            m_bDescendingSlope = false;
            m_fSlopeAngleOld = m_fSlopeAngle;
            m_fSlopeAngle = 0.0f;
        }
    };

    const float fSkinWidth = 0.015f;
    public int nHorizontalRayCount = 4;
    public int nVerticalRayCount = 4;

    float m_fHorizontalRaySpacing;
    float m_fVerticalRaySpacing;

    float m_fMaxClimbAngle = 80.0f;
    float m_fMaxDescendAngle = 75.0f;

    BoxCollider2D m_bBoxCollider2D;
    SRaycastOrigins m_sRaycastOrigins;

    public LayerMask m_lmCollisionMask;

    public SCollisionData m_sCollisionData;

    void Start()
    {
        m_bBoxCollider2D = GetComponent<BoxCollider2D>();
        CalculateRaySpacing();
        UpdateRaycastOrigins();
    }

    void UpdateRaycastOrigins()
    {
        //We're shrinking our collider bounds to be inside our "skin"
        //This is so we have a bit of buffer space before casting rays
        Bounds bounds = m_bBoxCollider2D.bounds;
        bounds.Expand(fSkinWidth * -2.0f);

        //These are the corners from which our raycast is going out
        m_sRaycastOrigins.m_v2BottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        m_sRaycastOrigins.m_v2BottomRight = new Vector2(bounds.max.x, bounds.min.y);
        m_sRaycastOrigins.m_v2TopLeft = new Vector2(bounds.min.x, bounds.max.y);
        m_sRaycastOrigins.m_v2TopRight = new Vector2(bounds.max.x, bounds.max.y);
    }

    //Should only ever need to call this at Start, or when we're changing
    //the number of vertical or horizontal rays we're casting
    void CalculateRaySpacing()
    {
        Bounds bounds = m_bBoxCollider2D.bounds;
        bounds.Expand(fSkinWidth * -2.0f);

        nHorizontalRayCount = Mathf.Clamp(nHorizontalRayCount, 2, int.MaxValue);
        nVerticalRayCount = Mathf.Clamp(nVerticalRayCount, 2, int.MaxValue);

        //Space between each ray
        m_fHorizontalRaySpacing = bounds.size.y / (nHorizontalRayCount - 1);
        m_fVerticalRaySpacing = bounds.size.x / (nVerticalRayCount - 1);
    }

    void Update()
    {

    }

    public void Move(Vector3 v3Velocity)
    {
        UpdateRaycastOrigins();
        m_sCollisionData.ResetData();

        m_sCollisionData.m_v3VelocityOld = v3Velocity;

        if (v3Velocity.y < 0)
        {
            DescendSlope(ref v3Velocity);
        }
        if (v3Velocity.x != 0)
        {
            HorizontalCollisions(ref v3Velocity);
        }
        if (v3Velocity.y != 0)
        {
            VerticalCollisions(ref v3Velocity);
        }

        transform.Translate(v3Velocity);
    }

    void HorizontalCollisions(ref Vector3 v3Velocity)
    {
        float fDirectionX = Mathf.Sign(v3Velocity.x);
        float fRayLength = Mathf.Abs(v3Velocity.x) + fSkinWidth;

        for (int i = 0; i < nHorizontalRayCount; ++i)
        {
            //Cast rays left if we're moving left, right if we're moving right
            Vector2 v2RayOrigin = fDirectionX == -1
                ? m_sRaycastOrigins.m_v2BottomLeft
                : m_sRaycastOrigins.m_v2BottomRight;

            //Shift our ray along our bound/skin
            v2RayOrigin += Vector2.up * (m_fHorizontalRaySpacing * i);

            RaycastHit2D rhHit = Physics2D.Raycast(v2RayOrigin, Vector2.right * fDirectionX, fRayLength, m_lmCollisionMask);

            //if we collided, we alter our velocity so we hit the object instead of going through
            if (rhHit)
            {
                //Get the angle of the surface we've hit, to deal with slopes
                //The raycast hit stores the normal of the collision surface,
                //which allows us to find the angle
                float fSlopeAngle = Vector2.Angle(rhHit.normal, Vector2.up);
                if (i == 0 && fSlopeAngle <= m_fMaxClimbAngle)
                {
                    //This addresses the issue of climbing a slope from a descending slope
                    //(basically a valley where we're doing both at the same time)
                    if (m_sCollisionData.m_bDescendingSlope)
                    {
                        m_sCollisionData.m_bDescendingSlope = false;
                        v3Velocity = m_sCollisionData.m_v3VelocityOld;
                    }


                    //This addresses the issue of starting to climb the slope before we've
                    //reached it, leaving space between us and the ground. This is happening 
                    //because once our ray intersects the slope we will start to climb the wall
                    //instead of moving TO the wall, then climbing it.

                    //Basically, we're subtracting the velocity that puts us AT the slope.
                    //Then, we're climbing the slope (using only the X velocity we'd have
                    //once we reach it). Finally, we then give back the old velocity that 
                    //will take us to the slope.

                    float fDistanceToSlopeStart = 0.0f;
                    if (fSlopeAngle != m_sCollisionData.m_fSlopeAngleOld)
                    {

                        fDistanceToSlopeStart = rhHit.distance - fSkinWidth;
                        v3Velocity.x -= fDistanceToSlopeStart * fDirectionX;
                    }

                    ClimbSlope(ref v3Velocity, fSlopeAngle);
                    v3Velocity.x += fDistanceToSlopeStart * fDirectionX;
                }

                //If we're slope-climbing, we don't want other rays to interfere with this
                //So skip over this part i
                if (!m_sCollisionData.m_bClimbingSlope || fSlopeAngle > m_fMaxClimbAngle)
                {
                    v3Velocity.x = (rhHit.distance - fSkinWidth) * fDirectionX;

                    //changing raylength so we won't hit things further away than THIS collision
                    //but we will still hit things closer than this collision
                    fRayLength = rhHit.distance;

                    //For obstacle on slope; this is happening when we detect a slope (from obstacle)
                    //but we're already climbing a slope. We need to alter our Y velocity to respect this 
                    //new slope angle
                    if (m_sCollisionData.m_bClimbingSlope)
                    {
                        v3Velocity.y = Mathf.Tan(m_sCollisionData.m_fSlopeAngle * Mathf.Deg2Rad) * Mathf.Abs(v3Velocity.x);
                    }

                    //recording whether or not we collided with something when moving horizontally
                    m_sCollisionData.m_bLeft = fDirectionX == -1;
                    m_sCollisionData.m_bRight = fDirectionX == 1;
                }
            }

            Debug.DrawRay(v2RayOrigin, Vector2.right * fDirectionX, rhHit ? Color.red : Color.blue);
        }
    }

    void VerticalCollisions(ref Vector3 v3Velocity)
    {
        float fDirectionY = Mathf.Sign(v3Velocity.y);
        float fRayLength = Mathf.Abs(v3Velocity.y) + fSkinWidth;

        for (int i = 0; i < nVerticalRayCount; ++i)
        {
            //Cast rays up if we're moving up, down if we're moving down
            Vector2 v2RayOrigin = fDirectionY == -1
                ? m_sRaycastOrigins.m_v2BottomLeft
                : m_sRaycastOrigins.m_v2TopLeft;

            //Shift our ray along our bound/skin
            v2RayOrigin += Vector2.right * (m_fVerticalRaySpacing * i);

            RaycastHit2D rhHit = Physics2D.Raycast(v2RayOrigin, Vector2.up * fDirectionY, fRayLength, m_lmCollisionMask);

            //if we collided, we alter our velocity so we hit the object instead of going through
            if (rhHit)
            {
                v3Velocity.y = (rhHit.distance - fSkinWidth) * fDirectionY;

                //changing raylength so we won't hit things further away than THIS collision
                //but we will still hit things closer than this collision
                fRayLength = rhHit.distance;

                if (m_sCollisionData.m_bClimbingSlope)
                {
                    v3Velocity.x = v3Velocity.y / Mathf.Tan(m_sCollisionData.m_fSlopeAngle * Mathf.Deg2Rad) * Mathf.Sign(v3Velocity.x);
                }

                //recording whether or not we collided with something when moving vertically
                m_sCollisionData.m_bBelow = fDirectionY == -1;
                m_sCollisionData.m_bAbove = fDirectionY == 1;
            }

            Debug.DrawRay(v2RayOrigin, Vector2.up * fDirectionY, rhHit ? Color.red : Color.blue);
        }

        //Checking for new slope, to avoid hitting it from an old slope
        //We're casting our ray from the left or right, at the height we're about to be at
        //If we detect that our raycast hit a NEW slope, we need to start using that instead; 
        //otherwise we could get stuck on the old slope for a frame or two
        if (m_sCollisionData.m_bClimbingSlope)
        {
            float fDirectionX = Mathf.Sign(v3Velocity.x);
            fRayLength = Mathf.Abs(v3Velocity.x) + fSkinWidth;
            Vector2 v2RayOrigin = (fDirectionX == -1 ? m_sRaycastOrigins.m_v2BottomLeft : m_sRaycastOrigins.m_v2BottomRight)
                + Vector2.up * v3Velocity.y;
            RaycastHit2D rhHit = Physics2D.Raycast(v2RayOrigin, Vector2.right * fDirectionX, fRayLength, m_lmCollisionMask);

            if (rhHit)
            {
                float fSlopeAngle = Vector2.Angle(rhHit.normal, Vector2.up);
                if (fSlopeAngle != m_sCollisionData.m_fSlopeAngle)
                {
                    //Put us at the new slope
                    v3Velocity.x = (rhHit.distance - fSkinWidth) * fDirectionX;
                    m_sCollisionData.m_fSlopeAngle = fSlopeAngle;
                }
            }
        }
    }

    //Translates our horizontal velocity into horizontal + vertical velocity
    //to climb a slope; our overall speed isn't affected
    void ClimbSlope(ref Vector3 rv3Velocity, float fSlopeAngle)
    {
        float fTotalMoveVelocity = Mathf.Abs(rv3Velocity.x);

        //We don't want to directly set Y velocity because we could be jumping, and this would 
        //negate the jump. We only set Y velocity if we're not jumping, which we check by seeing 
        //if our climbing velocity is NOT less than our current Y velocity
        float fClimbVelocityY = (Mathf.Sin(fSlopeAngle * Mathf.Deg2Rad) * fTotalMoveVelocity);
        if (rv3Velocity.y <= fClimbVelocityY)
        {

            //We're on a slope... we're colliding with the floor
            //(fixes problem where we're not jumping because we're not on the floor)
            m_sCollisionData.m_bBelow = true;
            m_sCollisionData.m_bClimbingSlope = true;

            //Cache of slope angle
            m_sCollisionData.m_fSlopeAngle = fSlopeAngle;

            rv3Velocity.y = fClimbVelocityY;
            rv3Velocity.x = Mathf.Cos(fSlopeAngle * Mathf.Deg2Rad) * fTotalMoveVelocity * Mathf.Sign(rv3Velocity.x);
        }
    }

    //Translates our horizontal velocity into horizontal + vertical velocity
    //to descend a slope; our overall speed isn't affected

    /*
        When descending, we want to raycast with the corner that is actually 
        touching the slope; if we're moving left, use the bottom right corner, 
        and vice versa.
    */

    void DescendSlope(ref Vector3 rv3Velocity)
    {
        //ETHAN ALTER
        if (rv3Velocity.x == 0)
            return;

        float fDirectionX = Mathf.Sign(rv3Velocity.x);

        Vector2 v2RayOrigin = fDirectionX == -1
            ? m_sRaycastOrigins.m_v2BottomRight
            : m_sRaycastOrigins.m_v2BottomLeft;

        //Cast a ray down from our corner, as far as we can (infinity) and see if we hit anything
        RaycastHit2D rhHit = Physics2D.Raycast(v2RayOrigin, -Vector2.up, Mathf.Infinity, m_lmCollisionMask);

        if (rhHit)
        {
            float fSlopeAngle = Vector2.Angle(rhHit.normal, Vector2.up);
            if (fSlopeAngle != 0 && fSlopeAngle <= m_fMaxDescendAngle)
            {
                //Are we moving down the slope?
                if (Mathf.Sign(rhHit.normal.x) == fDirectionX)
                {
                    //Are we close enough to the slope that we're actually moving on it? Or are we floating above it?
                    //If our distance to the slope is smaller than the distance we have to move on the Y-Axis for it to 
                    //come into effect, then we're moving on the slope.
                    if (rhHit.distance - fSkinWidth <= Mathf.Tan(fSlopeAngle * Mathf.Deg2Rad) * Mathf.Abs(rv3Velocity.x))
                    {
                        float fTotalMoveVelocity = Mathf.Abs(rv3Velocity.x);
                        float fDescendVelocityY = Mathf.Sin(fSlopeAngle * Mathf.Deg2Rad) * fTotalMoveVelocity;
                        rv3Velocity.x = Mathf.Cos(fSlopeAngle * Mathf.Deg2Rad) * fTotalMoveVelocity * Mathf.Sign(rv3Velocity.x);
                        rv3Velocity.y -= fDescendVelocityY;

                        m_sCollisionData.m_bBelow = true;
                        m_sCollisionData.m_bDescendingSlope = true;

                        //Cache of slope angle
                        m_sCollisionData.m_fSlopeAngle = fSlopeAngle;
                    }

                    //AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
                    if (m_sCollisionData.m_bBelow == false)
                    {
                        bool bob = true;
                    }
                }
            }
        }
    }
}
