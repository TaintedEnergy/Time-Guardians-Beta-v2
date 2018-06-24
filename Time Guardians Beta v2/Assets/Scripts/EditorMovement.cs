using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorMovement : MonoBehaviour
{

    Rigidbody rb;
    public CapsuleCollider capsuleCollider;

    public float walkSpeed = 10;
    public float strafeSpeed = 4;
    public float jumpPower = 1;
    public float settableSprintMultiplier = 1.6f;
    public float sprintMultiplier = 1;

    public float height = 1.8f;

    public float[] settableCrouchMultiplier;
    float[] crouchMultiplier = new float[] { 1, 1 };

    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;

    public LayerMask groundLayer;

    public Animator holdMovement;

    public CursorLockMode lockedMode;
    public CursorLockMode unlockedMode;

    //get the radius of the players capsule collider, and make it a tiny bit smaller than that
    float radius;
    //get the position (assuming its right at the bottom) and move it up by almost the whole radius
    Vector3 pos;
    //returns true if the sphere touches something on that layer
    bool isGrounded;

    // #Dom (all under)

    public Vector3 moveDirection;
    int directions;
    float y;
    float maximumMagnitude = 100;

    public bool isMoving;

    int glideTime;
    int waitingForGrounded;

    int touching;
    int lastToucing;
    bool touchingAndJumped;

    string currentMaterial;
    public PhysicMaterial moveMaterial;
    public PhysicMaterial stopMaterial;

    float stepTime;

    public GameObject groundSphere;

    int fallTime;
    float lastDownwardVelocity;
    float lastHight;
    float hight;
    int fallAminTime;
    int jumpTime;

    float reticuleSpeed = 0.17f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        fallTime = 0;
        lastDownwardVelocity = 0;
        lastHight = 0;
        hight = 0;
        touching = 0;
    }

    void FixedUpdate()
    {
        jumpTime++;

        // Sprinting
        if (Input.GetButton("Fire3") && Input.GetKey("w") && !Input.GetKey("s") && !Input.GetKey("a") && !Input.GetKey("d"))
        {
            sprintMultiplier = settableSprintMultiplier;
        }
        else
        {
            sprintMultiplier = 1;
        }

        // Moving

        moveDirection = Vector3.zero;
        directions = 0;

        #region WASD
        
        if (Input.GetKey("w"))
        {
            moveDirection += transform.forward * walkSpeed;
            directions++;
        }
        if (Input.GetKey("s"))
        {
            moveDirection += -transform.forward * walkSpeed;
            directions++;
        }
        if (Input.GetKey("d"))
        {
            moveDirection += transform.right * strafeSpeed;
            directions++;
        }
        if (Input.GetKey("a"))
        {
            moveDirection += -transform.right * strafeSpeed;
            directions++;
        }
        #endregion

        // Save y velocity
        y = rb.velocity.y;

        // Slow down for diagonals
        moveDirection = new Vector3(moveDirection.x, 0, moveDirection.z);
        if (directions == 2)
        {
            moveDirection /= 1.41f;
        }

        // Add Multipliers
        if (directions >= 1)
        {
            // Move player (set velocity)
            moveDirection *= sprintMultiplier * crouchMultiplier[0];
        }

        // Set TouchingAndJumped
        touchingAndJumped = (fallAminTime > 10 && touching > 0);

        // Stop waiting for grounded
        if (waitingForGrounded > 0 && !holdMovement.GetBool("Falling"))
        {
            waitingForGrounded--;
        }

        // Set Walk or Run
        isMoving = (glideTime == 0 && waitingForGrounded == 0 && !touchingAndJumped);

        // Walk or Run
        if (isMoving)
        {
            rb.velocity = moveDirection;

            // Set correct physic material
            if (Input.GetKey("w") || Input.GetKey("a") || Input.GetKey("s") || Input.GetKey("d"))
            {
                if (currentMaterial != "move")
                {
                    GetComponent<CapsuleCollider>().material = moveMaterial;
                    currentMaterial = "move";
                }
            }
            else if (currentMaterial != "stop")
            {
                GetComponent<CapsuleCollider>().material = stopMaterial;
                currentMaterial = "stop";
            }
        }
        else // Glide
        {
            rb.velocity += moveDirection * Time.deltaTime;

            if (glideTime > 0)
            {
                glideTime--;
            }
        }

        // Exceeded max vel

        if (rb.velocity.magnitude > maximumMagnitude)
        {
            rb.velocity /= rb.velocity.magnitude / maximumMagnitude;
        }

        // Insuring Y value is untouched
        rb.velocity = new Vector3(rb.velocity.x, y, rb.velocity.z);

        //Jump Physics
        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
        else if (rb.velocity.y > 0 && !Input.GetButton("Jump"))
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
        }

        //Debug.Log(Input.GetAxis("Jump"));
        if (Input.GetButton("Jump") && Grounded())
        {
            //rb.AddForce(Vector3.up * , ForceMode.Impulse);
            rb.velocity = new Vector3(rb.velocity.x, (jumpPower * crouchMultiplier[1]), rb.velocity.z);
            // Debug.Log("Jumping?");
            //gameObject.transform.position += new Vector3(0, jumpPower, 0);
        }

        // Falling
        if (holdMovement != null && groundSphere != null)
        {
            RaycastHit hit;
            Ray ray = new Ray(groundSphere.transform.position, -Vector3.up);
            bool result = Physics.Raycast(ray, out hit, 100);

            float slopeAngleDistance = 0.6f;
            if ((hit.distance < slopeAngleDistance && rb.velocity.y > 0) || hit.distance > slopeAngleDistance || hit.distance == 0)
            {
                if (!Grounded() && !holdMovement.GetBool("Falling") && Input.GetButton("Jump"))
                {
                    holdMovement.Rebind();
                }
                holdMovement.SetBool("Falling", !Grounded());
            }
            else
            {
                holdMovement.SetBool("Falling", false);
            }
        }

        // Untouching
        if (touching > 0)
        {
            touching = 0;
        }
    }

    bool Grounded()
    {
        // New Way
        if (groundSphere != null)
        {
            isGrounded = Physics.CheckSphere(groundSphere.transform.position, groundSphere.GetComponent<SphereCollider>().radius, groundLayer);
        }
        else // Old Way
        {
            //get the radius of the players capsule collider, and make it a tiny bit smaller than that
            radius = capsuleCollider.radius * 0.9f;
            //get the position and move it to the required position
            pos = transform.position - Vector3.up * (radius * 0.9f) * 3;
            //returns true if the sphere touches something on that layer
            isGrounded = Physics.CheckSphere(pos, radius, groundLayer);
        }
        return isGrounded;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(pos, radius);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Minus))
        {
            GetComponent<Rigidbody>().AddExplosionForce(20, transform.position + new Vector3(0, 0, 0), 8, 3, ForceMode.VelocityChange);
        }

        if (Input.GetKeyDown("i"))
        {
            print(isMoving + " " + glideTime + " " + waitingForGrounded + " " + touchingAndJumped + " " + fallAminTime + " " + touching);
        }
        if (Input.GetKeyDown(KeyCode.Space) && Grounded())
        {
            jumpTime = 0;
        }

        // print((Mathf.Round(rb.velocity.magnitude * 100))/100);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.transform.root.transform.GetComponent<Rigidbody2D>() == null)
        {
            touching++;
        }
    }
}