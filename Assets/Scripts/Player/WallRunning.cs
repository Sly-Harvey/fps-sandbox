using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallRunning : MonoBehaviour
{
    [Header("Wallrunning")]
    public LayerMask wallMask;
    public LayerMask groundMask;
    public float wallRunForce;
    public float maxWallRunTime;
    private float wallRunTimer;

    [Header("Input")]
    private float horizontalInput;
    private float verticalInput;

    [Header("Detection")]
    public float wallCheckDistance;
    public float minJumpHeight;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    private bool leftWall;
    private bool rightWall;

    [Header("References")]
    private Rigidbody rb;
    public Transform player;
    private PlayerMovement pm;

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

        if((leftWall || rightWall) && verticalInput > 0 && aboveGround())
        {
            if (!pm.wallRunning)
                startWallRun();
        }
        else
        {
            if (pm.wallRunning)
                stopWallRun();
        }
    }

    private void wallRunMovement()
    {
        rb.useGravity = false;
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        Vector3 wallNormal = rightWall ? rightWallHit.normal : leftWallHit.normal;

        Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up);

        if ((player.forward - wallForward).magnitude > (player.forward - -wallForward).magnitude)
            wallForward = -wallForward;

        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);

        if(!(leftWall && horizontalInput > 0) && !(rightWall && horizontalInput < 0))
            rb.AddForce(-wallNormal * 100, ForceMode.Force);
    }
    private void startWallRun()
    {
        pm.wallRunning = true;
    }

    private void stopWallRun()
    {
        pm.wallRunning = false;
    }
}
