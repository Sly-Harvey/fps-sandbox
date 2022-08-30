using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallRunning : MonoBehaviour
{
    [Header("Wallrunning")]
    public float wallRunFov = 90f;
    public float wallRunTilt = 5f;

    public LayerMask wallMask;
    public LayerMask groundMask;
    public float wallRunForce;
    public float wallJumpUpForce;
    public float wallJumpSideForce;
    public float maxWallRunTime;
    private float wallRunTimer;

    [Header("Input")]
    public KeyCode jumpKey = KeyCode.Space;
    private float horizontalInput;
    private float verticalInput;

    [Header("Exiting Wall")]
    private bool exitingWall;
    private float exitWallTimer;
    public float exitWallTime;

    [Header("Detection")]
    public float wallCheckDistance;
    public float minJumpHeight;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    private bool leftWall;
    private bool rightWall;

    [Header("Gravity")]
    public PlayerCam cam;
    public bool useGravity;
    public float gravityCounterForce;

    [Header("References")]
    public Transform player;
    private PlayerMovement pm;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        wallCheck();
        stateMachine();
    }

    private void FixedUpdate()
    {
        if (pm.wallRunning)
            wallRunMovement();
    }

    void wallCheck()
    {
        leftWall = Physics.Raycast(transform.position, -transform.right, out leftWallHit, wallCheckDistance, wallMask);
        rightWall = Physics.Raycast(transform.position, transform.right, out rightWallHit, wallCheckDistance, wallMask);
    }

    private bool aboveGround()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, groundMask);
    }

    private void stateMachine()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if((leftWall || rightWall) && verticalInput > 0 && aboveGround() && !exitingWall)
        {
            if (!pm.wallRunning)
                startWallRun();

            if (wallRunTimer > 0)
                wallRunTimer -= Time.deltaTime;

            if(wallRunTimer <= 0 && pm.wallRunning)
            {
                exitingWall = true;
                exitWallTimer = exitWallTime;
            }

            if (Input.GetKeyDown(jumpKey))
                wallJump();
        }

        else if(exitingWall)
        {
            if (pm.wallRunning)
                stopWallRun();

            if (exitWallTimer > 0)
                exitWallTimer -= Time.deltaTime;

            if (exitWallTimer <= 0)
                exitingWall = false;
        }

        else
        {
            if (pm.wallRunning)
                stopWallRun();
        }
    }

    private void wallRunMovement()
    {
        rb.useGravity = useGravity;

        Vector3 wallNormal = rightWall ? rightWallHit.normal : leftWallHit.normal;

        Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up);

        if ((player.forward - wallForward).magnitude > (player.forward - -wallForward).magnitude)
            wallForward = -wallForward;

        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);

        if(!(leftWall && horizontalInput > 0) && !(rightWall && horizontalInput < 0))
            rb.AddForce(-wallNormal * 100, ForceMode.Force);

        if (useGravity)
            rb.AddForce(transform.up * gravityCounterForce, ForceMode.Force);
    }
    private void startWallRun()
    {
        pm.wallRunning = true;

        wallRunTimer = maxWallRunTime;

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        cam.doFov(wallRunFov);
        if(leftWall)cam.doTilt(-wallRunTilt);
        if (rightWall) cam.doTilt(wallRunTilt);
    }

    private void stopWallRun()
    {
        pm.wallRunning = false;

        cam.doTilt(0f);
        cam.doFov(80f);
    }

    private void wallJump()
    {
        exitingWall = true;
        exitWallTimer = exitWallTime;

        Vector3 wallNormal = rightWall ? rightWallHit.normal : leftWallHit.normal;

        Vector3 forceToApply = transform.up * wallJumpUpForce + wallNormal * wallJumpSideForce;

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(forceToApply, ForceMode.Impulse);
    }
}
