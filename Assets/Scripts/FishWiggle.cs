using UnityEngine;

/// <summary>
/// Тип движения рыбы
/// </summary>
public enum FishSwimProfile
{
    Simple,     // простой, как у первой версии
    Realistic,  // более "живая" волна по телу
    Shark       // стиль акулы: жёсткий нос, активный хвост
}
public enum TailSwingPlane
{
    Horizontal, // хвост влево-вправо (рыбы)
    Vertical    // хвост вверх-вниз (дельфины, киты и т.п.)
}
public class FishWiggle : MonoBehaviour
{
    [Header("Body wiggle (общие настройки амплитуды)")]
    public Transform[] spineBones;
    public float amplitude = 15f;   // базовая амплитуда изгиба (модифицируется профилями)

    [Header("Swim style")]
    public FishSwimProfile swimProfile = FishSwimProfile.Simple;

    // ---------- Realistic (обычные рыбы) ----------
    [Header("Realistic fish settings")]
    public float maxRealisticSpeed = 4f;
    public float minWiggleFrequency = 1f;
    public float maxWiggleFrequency = 4f;
    public float waveLength = 2f;
    public AnimationCurve bendAlongBody =
        AnimationCurve.Linear(0f, 0.2f, 1f, 1f); // голова почти не гнётся, хвост сильно

    // ---------- Shark (акулы) ----------
    [Header("Shark settings")]

    [Header("Tail settings")]
    public TailSwingPlane tailSwing = TailSwingPlane.Horizontal;

    [Tooltip("Макс. скорость, при которой акула плывёт «на полную»")]
    public float sharkMaxSpeed = 6f;
    [Tooltip("Мин. частота хвоста у акулы")]
    public float sharkMinFrequency = 0.8f;
    [Tooltip("Макс. частота хвоста у акулы")]
    public float sharkMaxFrequency = 2.2f;
    [Tooltip("Длина волны вдоль тела (больше = более плавная S-форма)")]
    public float sharkWaveLength = 1.3f;
    [Tooltip("Кривая, где 0 = голова, 1 = хвост. Задаёт, как сильно гнётся тело акулы.")]
    public AnimationCurve sharkBendCurve =
        new AnimationCurve(
            new Keyframe(0f, 0f),    // голова почти жёсткая
            new Keyframe(0.4f, 0.1f),
            new Keyframe(0.7f, 0.6f),
            new Keyframe(1f, 1f)     // хвост гнётся максимально
        );
    [Tooltip("Насколько акула покачивается роллом (наклон вбок)")]
    public float sharkRollAmount = 5f;
    [Tooltip("Сдвиг фазы ролла относительно изгиба хвоста")]
    public float sharkRollPhaseOffset = 0.5f;

    [Header("Simple mode (для старых спавнеров)")]
    public float frequency = 2f;      // раньше simpleFrequency
    public float phaseOffset = 0.5f;  // раньше simplePhaseOffset

    [Header("Swim motion (для всех профилей)")]
    public float forwardSpeed = 2f;
    public float turnSmooth = 2f;
    public float wanderAnglePerSec = 10f;

    [Header("Target tracking")]
    public Transform submarine;
    public float desiredDistance = 8f;
    public float approachStrength = 1f;

    [Header("Lifetime")]
    public float maxLifetime = 20f;
    private float lifeTimer = 0f;

    private Quaternion[] baseRot;
    private Vector3 swimDirWorld = Vector3.forward;

    void Start()
    {
        baseRot = new Quaternion[spineBones.Length];
        for (int i = 0; i < spineBones.Length; i++)
            baseRot[i] = spineBones[i].localRotation;

        if (submarine == null)
        {
            swimDirWorld = Random.onUnitSphere;
            swimDirWorld.y *= 0.3f;
            swimDirWorld.Normalize();
        }
    }

    void Update()
    {
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        AnimateBody();
        ThinkAndSwim();
    }

    void AnimateBody()
    {
        if (spineBones == null || spineBones.Length == 0)
            return;

        switch (swimProfile)
        {
            case FishSwimProfile.Simple:
                AnimateSimple();
                break;
            case FishSwimProfile.Realistic:
                AnimateRealisticFish();
                break;
            case FishSwimProfile.Shark:
                AnimateShark();
                break;
        }
    }

    // --- старый простой режим ---
    void AnimateSimple()
    {
        float t = Time.time * frequency;

        for (int i = 0; i < spineBones.Length; i++)
        {
            float along = (float)i / (spineBones.Length - 1);
            float phase = t + i * phaseOffset;
            float angle = Mathf.Sin(phase) * amplitude * Mathf.Lerp(0.1f, 1f, along);

            spineBones[i].localRotation = baseRot[i] * GetTailRotationDelta(angle);

        }
    }

    // --- реалистичная обычная рыба ---
    void AnimateRealisticFish()
    {
        float speedFactor = Mathf.Clamp01(forwardSpeed / Mathf.Max(0.01f, maxRealisticSpeed));
        float currFreq = Mathf.Lerp(minWiggleFrequency, maxWiggleFrequency, speedFactor);
        float time = Time.time * currFreq;

        for (int i = 0; i < spineBones.Length; i++)
        {
            float along = (float)i / (spineBones.Length - 1);
            float phase = time - along * waveLength;
            float wave = Mathf.Sin(phase * Mathf.PI * 2f);

            float localAmp = amplitude * bendAlongBody.Evaluate(along) * (0.5f + speedFactor);
            float angle = wave * localAmp;

            spineBones[i].localRotation = baseRot[i] * GetTailRotationDelta(angle);

        }
    }

    void AnimateShark()
    {
        // Акула – более "жёсткая": частота ниже, изгиб в основном сзади
        float speedFactor = Mathf.Clamp01(forwardSpeed / Mathf.Max(0.01f, sharkMaxSpeed));
        float freq = Mathf.Lerp(sharkMinFrequency, sharkMaxFrequency, speedFactor);
        float time = Time.time * freq;

        for (int i = 0; i < spineBones.Length; i++)
        {
            float along = (float)i / (spineBones.Length - 1); // 0 (голова) -> 1 (хвост)

            // волна, бегущая от середины тела к хвосту
            float phase = time - along * sharkWaveLength;
            float wave = Mathf.Sin(phase * Mathf.PI * 2f);

            // насколько эта часть тела вообще гнётся
            float bend = sharkBendCurve.Evaluate(along);

            float localAmp = amplitude * bend * (0.6f + 0.7f * speedFactor);
            float yaw = wave * localAmp; // "угол" изгиба

            // лёгкий ролл корпуса, чтобы акула «качалась» боком
            float roll = Mathf.Sin((phase + sharkRollPhaseOffset) * Mathf.PI * 2f)
                         * sharkRollAmount * bend * 0.5f;

            Quaternion bendQ = GetTailRotationDelta(yaw);
            Quaternion rollQ = Quaternion.Euler(0f, 0f, roll);

            spineBones[i].localRotation = baseRot[i] * bendQ * rollQ;
        }
    }


    Quaternion GetTailRotationDelta(float angle)
    {
        // вращаем вокруг нужной локальной оси
        switch (tailSwing)
        {
            case TailSwingPlane.Vertical:
                // вверх-вниз: вращение по локальной X (pitch)
                return Quaternion.Euler(angle, 0f, 0f);

            case TailSwingPlane.Horizontal:
            default:
                // влево-вправо: вращение по локальной Y (yaw)
                return Quaternion.Euler(0f, angle, 0f);
        }
    }

    void ThinkAndSwim()
    {
        Vector3 targetDir = swimDirWorld;

        if (submarine != null)
        {
            Vector3 toSub = submarine.position - transform.position;
            float dist = toSub.magnitude;

            if (dist > desiredDistance)
            {
                targetDir = Vector3.Lerp(swimDirWorld, toSub.normalized, approachStrength);
            }
            else
            {
                Vector3 perp = Vector3.Cross(toSub.normalized, Vector3.up);
                if (perp == Vector3.zero) perp = Random.onUnitSphere;
                targetDir = Vector3.Lerp(swimDirWorld, perp.normalized, 0.5f);
            }
        }

        Quaternion wanderRot = Quaternion.Euler(
            0f,
            Random.Range(-wanderAnglePerSec, wanderAnglePerSec) * Time.deltaTime,
            0f
        );
        targetDir = wanderRot * targetDir;
        targetDir.Normalize();

        swimDirWorld = targetDir;

        if (swimDirWorld.sqrMagnitude > 0.0001f)
        {
            Quaternion wantRot = Quaternion.LookRotation(swimDirWorld, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                wantRot,
                turnSmooth * Time.deltaTime
            );
        }

        transform.position += transform.forward * forwardSpeed * Time.deltaTime;
    }
}
