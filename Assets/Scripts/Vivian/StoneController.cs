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
    private GameManager gameManager;
    [SerializeField] private bool hasBeenThrown = false;
    [SerializeField] private bool hasEndedTurn = false;
    [SerializeField] private bool isBeingThrownByEnemy = false;
    [SerializeField]  private bool hasStartedMoving = false;

    [Header("Audio - Movement")]
    [SerializeField] private AudioSource movementAudioSource;
    [SerializeField] private float movementMinSpeed = 0.2f;
    [SerializeField] private float movementMaxSpeedForVolume = 14f;
    [SerializeField] private float movementMaxVolume = 0.8f;
    [SerializeField] private float movementVolumeLerpSpeed = 10f;

    [Header("Audio - Turning")]
    [SerializeField] private AudioSource turningAudioSource;
    [SerializeField] private float turningInputToMaxVolume = 40f;
    [SerializeField] private float turningMaxVolume = 0.7f;
    [SerializeField] private float turningVolumeLerpSpeed = 14f;

    // CURL SYSTEM
    private bool isCurling = false;
    private Vector2 lastMousePos;
    private float turnAudioIntensity;
    public float curlStrength = 2f;
    public float maxCurlForce = .5f;

    void Start()
    {
        gameManager = GameObject.Find("Game Manager").gameObject.GetComponent<GameManager>();
        hasBeenThrown = false;
        rb = GetComponent<Rigidbody>();
        Ir = GetComponent<LineRenderer>();
        cam = Camera.main;

        if (Ir != null)
        {
            Ir.enabled = true;
        }

        if (movementAudioSource != null)
        {
            movementAudioSource.loop = true;
            movementAudioSource.playOnAwake = false;
            movementAudioSource.volume = 0f;
        }

        if (turningAudioSource != null)
        {
            turningAudioSource.loop = true;
            turningAudioSource.playOnAwake = false;
            turningAudioSource.volume = 0f;
        }
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
                gameObject.GetComponent<Player>().shotsTaken++;
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
            turnAudioIntensity = 0f;

       
            if(!hasBeenThrown)
            {
                AimWithMouse();
                
                HandleCharging();
                UpdateAimLine();
                    
                // Debug.Log("THROW FORCE: " + currentForce);
            }    
            else 
            {
                if (isBeingThrownByEnemy)
                {
                    UpdateMovementAudio();
                    UpdateTurningAudio();
                    return;
                }
                if(rb.linearVelocity.magnitude > 3.5f)
                {
                    HandleCurl();
                }
                if(rb.linearVelocity.magnitude > 0.5f)
                {
                    hasStartedMoving = true;
                }
                else if(hasStartedMoving && !hasEndedTurn && rb.linearVelocity.magnitude < 0.1f )
                {
                    Debug.Log("Running EndTurn function");
                    // Destroy(Ir);
                    hasEndedTurn = true;
                    gameManager.EndTurn();
                    
                   
                }
                
            }

            UpdateMovementAudio();
            UpdateTurningAudio();
    }
    public void reEnableTurn()
    {
        Debug.Log("Reenabling current player turn");
        hasBeenThrown = false;
        hasEndedTurn = false;
        isBeingThrownByEnemy = false;
        hasStartedMoving = false;

        if (Ir != null)
        {
            Ir.enabled = true;
        }

    }
    public void disableTurn()
    {
        hasBeenThrown = true;

        if (Ir != null)
        {
            Ir.enabled = false;
        }
    }

    public void SetBeingThrownByEnemy(bool isThrownByEnemy)
    {
        isBeingThrownByEnemy = isThrownByEnemy;

        if (isThrownByEnemy)
        {
            isCurling = false;
            turnAudioIntensity = 0f;
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

        if (Ir != null)
        {
            Ir.enabled = false;
        }
        
        
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
        turnAudioIntensity = Mathf.Clamp01(Mathf.Abs(deltaX) / Mathf.Max(1f, turningInputToMaxVolume));

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

    private void UpdateMovementAudio()
    {
        if (movementAudioSource == null)
        {
            return;
        }

        float speed = rb != null ? rb.linearVelocity.magnitude : 0f;
        float targetVolume = 0f;
        if (speed > movementMinSpeed)
        {
            float speed01 = Mathf.Clamp01((speed - movementMinSpeed) / Mathf.Max(0.01f, movementMaxSpeedForVolume - movementMinSpeed));
            targetVolume = speed01 * movementMaxVolume;
        }

        movementAudioSource.volume = Mathf.Lerp(movementAudioSource.volume, targetVolume, movementVolumeLerpSpeed * Time.deltaTime);

        if (targetVolume > 0.001f)
        {
            if (!movementAudioSource.isPlaying)
            {
                movementAudioSource.Play();
            }
        }
        else if (movementAudioSource.isPlaying && movementAudioSource.volume <= 0.01f)
        {
            movementAudioSource.Stop();
        }
    }

    private void UpdateTurningAudio()
    {
        if (turningAudioSource == null)
        {
            return;
        }

        float targetVolume = turnAudioIntensity * turningMaxVolume;
        turningAudioSource.volume = Mathf.Lerp(turningAudioSource.volume, targetVolume, turningVolumeLerpSpeed * Time.deltaTime);

        if (targetVolume > 0.001f)
        {
            if (!turningAudioSource.isPlaying)
            {
                turningAudioSource.Play();
            }
        }
        else if (turningAudioSource.isPlaying && turningAudioSource.volume <= 0.01f)
        {
            turningAudioSource.Stop();
        }
    }
}