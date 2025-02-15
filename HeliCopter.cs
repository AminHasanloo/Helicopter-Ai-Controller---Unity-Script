using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

public class HelicopterController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float hoverForce = 150f;
    public float forwardSpeed = 50f;
    public float turnSpeed = 2f;
    public float liftSpeed = 25f;
    public float maxHeight = 80f; // حداکثر ارتفاع مجاز
    
    [Header("Waypoint Settings")]
    public Transform[] waypoints;
    public bool randomMovement = false;
    public float waypointRadius = 5f;
    
    [Header("Permissions")]
    public bool canTakeOff = false;
    public bool canLand = false;
    
    [Header("Rotor Settings")]
    public Transform mainRotor;
    public Transform tailRotor;
    public float mainRotorSpeed = 500f;
    public float tailRotorSpeed = 700f;
    
    [Header("State Events")]
    public UnityEvent onTakeOff;
    public UnityEvent onHover;
    public UnityEvent onLanding;
    public UnityEvent onMoving;
    
    [Header("Audio")]
    public AudioSource engineSound;
    public AudioSource bladeSound;
    public float maxEngineVolume = 1f;
    
    private Rigidbody rb;
    private int currentWaypoint = 0;
    private HelicopterState currentState;
    private bool isAutomatic = true;
    private float rotorSpeed = 0f;
    
    private enum HelicopterState
    {
        Grounded,
        TakingOff,
        Hovering,
        Moving,
        Landing
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentState = HelicopterState.Grounded;
        
        if (engineSound) engineSound.volume = 0;
        if (bladeSound) bladeSound.volume = 0;
        
        StartCoroutine(AutomaticBehavior());
    }

    private IEnumerator AutomaticBehavior()
    {
        while (true)
        {
            if (isAutomatic)
            {
                if (currentState == HelicopterState.Grounded && canTakeOff)
                {
                    StartTakeOff();
                }
                else if (currentState == HelicopterState.Hovering && transform.position.y >= maxHeight * 0.95f)
                {
                    StartMovement();
                }
                else if (currentState == HelicopterState.Moving && 
                         currentWaypoint == waypoints.Length - 1 && 
                         Vector3.Distance(transform.position, waypoints[currentWaypoint].position) < waypointRadius &&
                         canLand)
                {
                    StartLanding();
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void FixedUpdate()
    {
        // محدود کردن ارتفاع در تمام حالت‌ها
        if (transform.position.y > maxHeight)
        {
            Vector3 pos = transform.position;
            pos.y = maxHeight;
            transform.position = pos;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, Mathf.Min(0, rb.linearVelocity.y), rb.linearVelocity.z);
        }

        UpdateRotors();
        
        switch (currentState)
        {
            case HelicopterState.TakingOff:
                HandleTakeOff();
                break;
            case HelicopterState.Hovering:
                HandleHovering();
                break;
            case HelicopterState.Moving:
                HandleMovement();
                break;
            case HelicopterState.Landing:
                HandleLanding();
                break;
        }
        
        UpdateSounds();
    }

    private void HandleTakeOff()
    {
        float targetHeight = maxHeight;
        float currentHeight = transform.position.y;
        
        float heightDifference = targetHeight - currentHeight;
        float upwardForce = Mathf.Clamp(heightDifference * liftSpeed, 0, liftSpeed);
        
        rb.AddForce(Vector3.up * upwardForce, ForceMode.Acceleration);
        
        if (rb.linearVelocity.y > 5f)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 5f, rb.linearVelocity.z);
        }
        
        if (currentHeight >= maxHeight * 0.95f)
        {
            currentState = HelicopterState.Hovering;
            onHover.Invoke();
        }
    }

    private void HandleHovering()
    {
        float targetHeight = transform.position.y;
        float currentHeight = transform.position.y;
        
        float heightError = targetHeight - currentHeight;
        float hoverPower = hoverForce + (heightError * 10f);
        
        rb.AddForce(Vector3.up * hoverPower, ForceMode.Acceleration);
        
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        rb.AddForce(-horizontalVelocity * 2f, ForceMode.Acceleration);
    }

private void HandleMovement()
{
    if (waypoints.Length == 0) return;
    
    Vector3 targetPoint = waypoints[currentWaypoint].position;
    targetPoint.y = maxHeight;
    
    Vector3 toTarget = targetPoint - transform.position;
    Vector3 horizontalToTarget = new Vector3(toTarget.x, 0, toTarget.z);
    
    float distanceToTarget = horizontalToTarget.magnitude;
    
    if (distanceToTarget > waypointRadius)
    {
        // محاسبه سرعت نرمال شده برای تیلت
        float normalizedSpeed = rb.linearVelocity.magnitude / forwardSpeed;
        
        // زاویه تیلت به جلو - حداکثر 20 درجه
        float forwardTilt = Mathf.Lerp(0, 20f, normalizedSpeed);
        
        // محاسبه جهت هدف با در نظر گرفتن تیلت
        Vector3 targetDirection = horizontalToTarget.normalized;
        
        // محاسبه زاویه چرخش نسبت به هدف
        float angleToTarget = Vector3.SignedAngle(transform.forward, targetDirection, Vector3.up);
        
        // زاویه بنک در چرخش - حداکثر 30 درجه
        float bankAngle = Mathf.Clamp(angleToTarget * -0.5f, -30f, 30f);
        
        // ترکیب همه چرخش‌ها
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        Quaternion tiltRotation = Quaternion.Euler(forwardTilt, 0, bankAngle);
        Quaternion finalRotation = targetRotation * tiltRotation;
        
        // اعمال چرخش با نرمی
        transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, turnSpeed * Time.deltaTime);
        
        // محاسبه و اعمال نیروی حرکت
        float speedMultiplier = Mathf.Clamp01(distanceToTarget / 10f);
        Vector3 moveForce = transform.forward * forwardSpeed * speedMultiplier;
        rb.AddForce(moveForce, ForceMode.Acceleration);
        
        // محدود کردن سرعت افقی
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        if (horizontalVelocity.magnitude > forwardSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * forwardSpeed;
            rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
        }
        
        // کاهش سرعت در چرخش‌های تند
        float turnFactor = Mathf.Abs(angleToTarget) / 180f;
        rb.linearVelocity *= (1f - turnFactor * 0.5f);
        
        HandleHovering();
    }
    else
    {
        // رسیدن به وی‌پوینت - برگشت به حالت عادی
        Quaternion normalRotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, normalRotation, turnSpeed * Time.deltaTime);
        
        if (randomMovement)
            currentWaypoint = Random.Range(0, waypoints.Length);
        else
            currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
        
        if (currentWaypoint == 0 && canLand)
        {
            StartLanding();
        }
    }
}
    private void HandleLanding()
    {
        Vector3 landingPoint = new Vector3(transform.position.x, 0, transform.position.z);
        float distanceToGround = transform.position.y;
        
        Quaternion landingRotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, landingRotation, turnSpeed * Time.deltaTime);
        
        if (distanceToGround > 0.1f)
        {
            float landingForce = -liftSpeed * (distanceToGround / 10f);
            rb.AddForce(Vector3.up * landingForce, ForceMode.Acceleration);
            
            if (rb.linearVelocity.y < -2f)
            {
                float stabilizingForce = -rb.linearVelocity.y * liftSpeed;
                rb.AddForce(Vector3.up * stabilizingForce, ForceMode.Acceleration);
            }
            
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            rb.AddForce(-horizontalVelocity * 2f, ForceMode.Acceleration);
        }
        else
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.position = new Vector3(transform.position.x, 0, transform.position.z);
            currentState = HelicopterState.Grounded;
        }
    }

    private void UpdateRotors()
    {
        float targetSpeed = currentState == HelicopterState.Grounded ? 0f : 1f;
        rotorSpeed = Mathf.Lerp(rotorSpeed, targetSpeed, Time.deltaTime * 2f);

        if (mainRotor)
            mainRotor.Rotate(Vector3.up, mainRotorSpeed * rotorSpeed * Time.deltaTime);
        if (tailRotor)
            tailRotor.Rotate(Vector3.right, tailRotorSpeed * rotorSpeed * Time.deltaTime);
    }

    private void UpdateSounds()
    {
        if (!engineSound || !bladeSound) return;
        
        float targetVolume = 0f;
        switch (currentState)
        {
            case HelicopterState.Grounded:
                targetVolume = 0f;
                break;
            case HelicopterState.TakingOff:
                targetVolume = Mathf.Lerp(0f, maxEngineVolume, rb.linearVelocity.magnitude / liftSpeed);
                break;
            default:
                targetVolume = maxEngineVolume;
                break;
        }
        
        engineSound.volume = Mathf.Lerp(engineSound.volume, targetVolume, Time.deltaTime * 2f);
        bladeSound.volume = engineSound.volume;
        
        engineSound.pitch = Mathf.Lerp(1f, 1.5f, rb.linearVelocity.magnitude / forwardSpeed);
        bladeSound.pitch = engineSound.pitch;
    }

    public void StartTakeOff()
    {
        if (currentState == HelicopterState.Grounded && canTakeOff)
        {
            currentState = HelicopterState.TakingOff;
            onTakeOff.Invoke();
        }
    }

    public void StartLanding()
    {
        if (currentState != HelicopterState.Grounded && canLand)
        {
            currentState = HelicopterState.Landing;
            onLanding.Invoke();
        }
    }

    public void StartMovement()
    {
        if (currentState == HelicopterState.Hovering)
        {
            currentState = HelicopterState.Moving;
            onMoving.Invoke();
        }
    }

    public void SetAutomaticMode(bool automatic)
    {
        isAutomatic = automatic;
    }

    public void ResetToStart()
    {
        if (waypoints.Length > 0)
        {
            transform.position = waypoints[0].position;
            currentWaypoint = 0;
            currentState = HelicopterState.Grounded;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rotorSpeed = 0f;
        }
    }
}