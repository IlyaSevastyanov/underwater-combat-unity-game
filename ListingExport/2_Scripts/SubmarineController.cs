using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SubmarineController : MonoBehaviour
{
    [Header("References")]
    public SubmarineAudio audioRef;
    public PropellerRotate[] propellers;

    private Rigidbody rb;

    [Header("Movement")]
    public float forwardThrust = 15f;
    public float verticalThrust = 10f;

    [Header("Rotation Rates (deg/sec)")]
    public float yawRate = 60f;     // скорость поворота влево/вправо
    public float pitchRate = 50f;   // скорость наклона вверх/вниз

    [Tooltip("Ограничение угла тангажа (вверх/вниз).")]
    public float maxPitchAngle = 60f;

    [Header("Smoothing")]
    [Tooltip("Сглаживание линейной скорости.")]
    public float movementSmoothing = 0.1f;

    [Tooltip("Сглаживание поворота (чем больше, тем быстрее догоняет цель).")]
    public float rotationLerpSpeed = 8f;

    private Vector3 smoothVelocity;

    [Header("Speed Limits")]
    public float maxForwardSpeed = 15f;
    public float maxVerticalSpeed = 8f;

    [Header("Desktop Input Settings")]
    public string verticalAxis = "Vertical";       // W/S
    public string horizontalAxis = "Horizontal";   // A/D
    public KeyCode ascendKey = KeyCode.Space;
    public KeyCode descendKey = KeyCode.R;
    public KeyCode pitchUpKey = KeyCode.I;
    public KeyCode pitchDownKey = KeyCode.K;

    [Header("Mobile Input")]
    public bool forceMobileInEditor = false;
    public MobileMoveJoystick mobileMoveJoystick;
    public MobileVerticalThruster mobileVerticalThruster;

    private bool isUsingMobileInput;

    [Header("Stabilization")]
    [Tooltip("Жёстко запретить крен (самый стабильный вариант).")]
    public bool lockRollWithConstraints = false;

    [Tooltip("Если roll не заблокирован, то насколько активно возвращать крен к нулю.")]
    public float rollAutoLevelStrength = 6f;

    [Tooltip("Максимальная угловая скорость Rigidbody (защита от резких ударов).")]
    public float maxAngularVelocity = 2f;

    [Tooltip("Сопротивление воды.")]
    public float waterDrag = 0.5f;

    [Tooltip("Угловое сопротивление.")]
    public float angularDrag = 2f;

    [Header("Debug")]
    public bool showDebugInfo = true;

    [Header("Sonar Scan")]
    public float sonarWorldRadius = 50f;
    public float sonarHighlightDuration = 2f;
    public float sonarRadarDuration = 2f;
    public bool sonarOnlyUncaughtFish = true;

    // Внутренние переменные ввода
    private float moveInput;
    private float yawInput;
    private float upInput;
    private float pitchInput;

    private float throttleForward;

    // Целевые углы
    private float desiredYaw;
    private float desiredPitch;

    // Кэш списка для сонара (чтобы меньше мусора)
    private readonly List<FishSonarHighlight> sonarInRange = new List<FishSonarHighlight>(64);

    // Геттеры для туториала
    public float MoveAxis => moveInput;
    public float YawAxis => yawInput;
    public float UpAxis => upInput;
    public float PitchAxis => pitchInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!rb)
        {
            enabled = false;
            return;
        }

        if (!audioRef)
            audioRef = GetComponent<SubmarineAudio>();

        rb.drag = waterDrag;
        rb.angularDrag = angularDrag;
        rb.maxAngularVelocity = maxAngularVelocity;

        // --- решаем, мобильное ли управление ---
        bool isMobileRuntime = false;

#if UNITY_WEBGL && !UNITY_EDITOR
        isMobileRuntime = WebGLBrowserCheck.IsMobileBrowser();
#elif UNITY_ANDROID || UNITY_IOS
        isMobileRuntime = true;
#else
        isMobileRuntime = false;
#endif
        isUsingMobileInput = isMobileRuntime || (Application.isEditor && forceMobileInEditor);

        // Инициализируем целевые углы из текущего поворота
        Vector3 e = rb.rotation.eulerAngles;
        desiredYaw = e.y;

        desiredPitch = e.x;
        if (desiredPitch > 180f) desiredPitch -= 360f;
        desiredPitch = Mathf.Clamp(desiredPitch, -maxPitchAngle, maxPitchAngle);

        // Опционально жёстко запрещаем крен
        if (lockRollWithConstraints)
        {
            rb.constraints |= RigidbodyConstraints.FreezeRotationZ;
        }

        Debug.Log($"[SubmarineController] platform={Application.platform}, " +
                  $"isMobileRuntime={isMobileRuntime}, " +
                  $"forceMobileInEditor={forceMobileInEditor}, " +
                  $"isUsingMobileInput={isUsingMobileInput}");
    }

    void Update()
    {
        ReadInput();

        // накопление целевых углов по вводу
        desiredYaw += yawInput * yawRate * Time.deltaTime;
        desiredPitch += pitchInput * pitchRate * Time.deltaTime;
        desiredPitch = Mathf.Clamp(desiredPitch, -maxPitchAngle, maxPitchAngle);

        // sonar по E (на мобилке вызывается кнопкой)
        if (Input.GetKeyDown(KeyCode.E))
        {
            OnSonarButtonPressed();
        }
    }

    void FixedUpdate()
    {
        if (!rb) return;

        rb.drag = waterDrag;
        rb.angularDrag = angularDrag;

        ApplyMovementWithSmoothing();
        ApplyRotationStable();
        LimitLinearSpeeds();

        UpdatePropellersAndAudio();
    }

    void OnDrawGizmos()
    {
        if (!showDebugInfo) return;

        if (!rb)
            rb = GetComponent<Rigidbody>();
        if (!rb) return;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, rb.velocity);

        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * 3f);
    }

    // ---------- Input ----------

    void ReadInput()
    {
        if (isUsingMobileInput && mobileMoveJoystick != null)
            ReadMobileInput();
        else
            ReadDesktopInput();

        throttleForward = Mathf.Clamp01(Mathf.Abs(moveInput));
    }

    void ReadDesktopInput()
    {
        moveInput = Input.GetAxis(verticalAxis);
        yawInput = Input.GetAxis(horizontalAxis);

        upInput = 0f;
        if (Input.GetKey(ascendKey)) upInput += 1f;
        if (Input.GetKey(descendKey)) upInput -= 1f;

        pitchInput = 0f;
        if (Input.GetKey(pitchUpKey)) pitchInput += 1f;
        if (Input.GetKey(pitchDownKey)) pitchInput -= 1f;
    }

    void ReadMobileInput()
    {
        if (mobileMoveJoystick != null)
        {
            moveInput = mobileMoveJoystick.Vertical;
            yawInput = mobileMoveJoystick.Horizontal;
        }
        else
        {
            moveInput = 0f;
            yawInput = 0f;
        }

        if (mobileVerticalThruster != null)
            upInput = mobileVerticalThruster.Value;
        else
            upInput = 0f;

        pitchInput = 0f; // если на мобилке нет отдельного pitch-контрола
    }

    // ---------- Movement ----------

    void ApplyMovementWithSmoothing()
    {
        Vector3 targetVelocity =
            transform.forward * (moveInput * forwardThrust) +
            transform.up * (upInput * verticalThrust);

        rb.velocity = Vector3.SmoothDamp(
            rb.velocity,
            targetVelocity,
            ref smoothVelocity,
            movementSmoothing
        );
    }

    void LimitLinearSpeeds()
    {
        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);

        localVelocity.z = Mathf.Clamp(localVelocity.z, -maxForwardSpeed, maxForwardSpeed);
        localVelocity.y = Mathf.Clamp(localVelocity.y, -maxVerticalSpeed, maxVerticalSpeed);

        rb.velocity = transform.TransformDirection(localVelocity);
    }

    // ---------- Rotation (новая логика) ----------

    void ApplyRotationStable()
    {
        // Базовая целевая ротация без крена
        Quaternion target = Quaternion.Euler(desiredPitch, desiredYaw, 0f);

        // Если крен жёстко запрещён — просто тянемся к target
        if (lockRollWithConstraints)
        {
            Quaternion newRot = Quaternion.Slerp(
                rb.rotation,
                target,
                rotationLerpSpeed * Time.fixedDeltaTime
            );

            rb.MoveRotation(newRot);
            return;
        }

        // Если крен не запрещён — мягко возвращаем roll к 0,
        // сохраняя yaw/pitch целью
        Quaternion current = rb.rotation;

        // Сначала тянемся к target
        Quaternion intermediate = Quaternion.Slerp(
            current,
            target,
            rotationLerpSpeed * Time.fixedDeltaTime
        );

        // Потом отдельно подправим roll в сторону нуля
        Vector3 e = intermediate.eulerAngles;
        float roll = e.z;
        if (roll > 180f) roll -= 360f;

        float newRoll = Mathf.Lerp(roll, 0f, rollAutoLevelStrength * Time.fixedDeltaTime);

        Quaternion final = Quaternion.Euler(
            NormalizeAngleSigned(e.x),
            NormalizeAngleUnsigned(e.y),
            newRoll
        );

        rb.MoveRotation(final);
    }

    float NormalizeAngleSigned(float angle)
    {
        if (angle > 180f) angle -= 360f;
        return angle;
    }

    float NormalizeAngleUnsigned(float angle)
    {
        while (angle < 0f) angle += 360f;
        while (angle >= 360f) angle -= 360f;
        return angle;
    }

    // ---------- Props / Audio ----------

    void UpdatePropellersAndAudio()
    {
        if (propellers != null)
        {
            foreach (var propeller in propellers)
            {
                if (propeller != null)
                    propeller.SetThrottle(throttleForward);
            }
        }

        if (audioRef != null)
        {
            float speed = rb.velocity.magnitude;
            audioRef.SetEngineLoad(throttleForward, speed);
        }
    }

    // ---------- Sonar ----------

    public void OnSonarButtonPressed()
    {
        if (audioRef != null)
        {
            audioRef.TriggerSonar();
        }

        HighlightFishBySonar();
    }

    void HighlightFishBySonar()
    {
        var allHighlights = FindObjectsOfType<FishSonarHighlight>();

        sonarInRange.Clear();

        foreach (var h in allHighlights)
        {
            if (h == null) continue;

            float dist = Vector3.Distance(transform.position, h.transform.position);
            if (dist > sonarWorldRadius) continue;

            if (sonarOnlyUncaughtFish)
            {
                var data = h.GetComponent<FishData>();
                if (data != null && data.caught)
                    continue;
            }

            sonarInRange.Add(h);
            h.Ping(sonarHighlightDuration);
        }

        if (SonarRadarUI.I != null)
        {
            SonarRadarUI.I.ShowPing(transform, sonarInRange, sonarRadarDuration);
        }
    }
}
