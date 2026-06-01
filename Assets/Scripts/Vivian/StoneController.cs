using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class StoneController : MonoBehaviour
{
   
    private Rigidbody rb;
    private Camera cam;

    private Vector2 mousePosition;

    private bool chargingUp = true; 

    private LineRenderer Ir;

    public float maxForce = 50f;
    public float chargeSpeed = 30f;

    private float currentForce = 0f;
    private bool isCharging = false;
    private PlayerTurnManager turnManager;
    [SerializeField] private bool hasBeenThrown = false;
     private bool hasBeenScored = false;
    private bool hasStartedMoving = false;

    // CURL SYSTEM
    private bool isCurling = false;
    private Vector2 lastMousePos;
    public float curlStrength = 2f;
    public float maxCurlForce = .5f;

    void Start()
    {
        turnManager = GameObject.Find("Game Manager").gameObject.GetComponent<PlayerTurnManager>();
        hasBeenThrown = false;
        rb = GetComponent<Rigidbody>();
        Ir = GetComponent<LineRenderer>();
        cam = Camera.main;
    }

    void OnPoint(InputValue value)
    {
        mousePosition = value.Get<Vector2>();
    }

    void OnClick(InputValue value)
    {
        // BEFORE THROW → charging
        if (!hasBeenThrown)
        {
            if (value.isPressed)
            {
                StartCharging();
                isCharging = true;
                chargingUp = true;
            }
            else
            {
                isCharging = false;
                ThrowStone();
            }
        }
        // AFTER THROW -> curling
        else
        {
            if (value.isPressed)
            {
                isCurling = true;
                lastMousePos = mousePosition;
            }
            else
            {
                isCurling = false;
            }
        }
    }

    void Update()
    {

        if(!hasBeenThrown )
        {
            AimWithMouse();
            
            HandleCharging();
            UpdateAimLine();
             
            // Debug.Log("THROW FORCE: " + currentForce);
        }    
        else 
        {
          
            if(rb.linearVelocity.magnitude > 3.5f)
            {
                HandleCurl();
            }
            if(rb.linearVelocity.magnitude > 0.5f)
            {
                hasStartedMoving = true;
            }
            else if(hasStartedMoving && !hasBeenScored && rb.linearVelocity.magnitude < 0.1f)
            {
                Destroy(Ir);
                turnManager.EndTurn();
                hasBeenScored = true;
            }
            
        }
    }

    void AimWithMouse()
    {
        Ray ray = cam.ScreenPointToRay(mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Vector3 target = hit.point;

            Vector3 direction = target - transform.position;
            direction.y = 0;

            if (direction != Vector3.zero)
            {
                transform.forward = direction;
            }
        }
    }

    void StartCharging()
    {
        isCharging = true;
    }

    void HandleCharging()
    {
        if (isCharging)
        {
            if (chargingUp)
            {
                currentForce += chargeSpeed * Time.deltaTime;
            
                if (currentForce >= maxForce)
                {
                    currentForce = maxForce;
                    chargingUp = false;
                }
            }
            else
            {
                currentForce -= chargeSpeed * Time.deltaTime;

                if (currentForce <= 0f)
                {
                    currentForce = 0f;
                    chargingUp = true;
                }
            }
        }
    }

    void ThrowStone()
    {
        // Debug.Log("THROW FORCE: " + currentForce);
        rb.AddForce(transform.forward * currentForce, ForceMode.Impulse);

        currentForce = 0f;
        isCharging = false;
        hasBeenThrown = true;
        
        
    }

    void UpdateAimLine()
    {
        Vector3 start = transform.position;

        float lineLength = Mathf.Lerp(1f, 4f, currentForce / maxForce);
        Vector3 end = transform.position + transform.forward * lineLength;

        Ir.SetPosition(0, start);
        Ir.SetPosition(1, end);
    }

    void HandleCurl()
    {
        if (!isCurling) return;

        // Calculate mouse movement
        Vector2 currentMouse = mousePosition;
        float deltaX = currentMouse.x - lastMousePos.x;

        // Get sideways direction relative to stone
        Vector3 sideDirection = transform.right;

        // Curl increases as velocity decreases
        float speed = rb.linearVelocity.magnitude;
        float speedFactor = Mathf.Clamp01(1f - speed / 20f); 
        // tweak "10f" later based on feel

        float curlForce = deltaX * curlStrength * speedFactor;

        curlForce = Mathf.Clamp(curlForce, -maxCurlForce, maxCurlForce);

        rb.AddForce(sideDirection * curlForce, ForceMode.Force);

        lastMousePos = currentMouse;
    }
}