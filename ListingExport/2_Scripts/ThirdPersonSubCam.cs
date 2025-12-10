using UnityEngine;

public class ThirdPersonSubCam : MonoBehaviour
{
    [Header("Follow target")]
    public Transform followTarget;

    // Offset в ЛОКАЛЬНЫХ осях субмарины: X = вправо(-влево), Y = вверх, Z = вперёд(-назад)
    public Vector3 localOffset = new Vector3(-1f, 2f, -6f);

    [Header("Smoothing")]
    public float posSmoothTime = 0.15f;   // SmoothDamp-время к целевой позиции
    public float dirSmoothSpeed = 10f;    // сглаживание смены направления (чем больше — тем резче)
    public float lookLerpSpeed = 12f;     // скорость поворота камеры к цели

    [Header("Collision")]
    public LayerMask collisionMask;
    public float camRadius = 0.3f;
    public float minDistance = 0.5f;
    public float wallPadding = 0.05f;

    Vector3 posVel;         // velocity для SmoothDamp позиции
    Vector3 smoothedDir;    // сглажённое направление от цели к камере (нормализованное)

    void Start()
    {
        if (!followTarget) return;

        // стартовая идеальная позиция
        Vector3 idealPos = followTarget.TransformPoint(localOffset);
        Vector3 from = followTarget.position;
        smoothedDir = (idealPos - from).normalized;

        transform.position = idealPos;
        transform.rotation = Quaternion.LookRotation(from - transform.position, Vector3.up);
    }

    void LateUpdate()
    {
        if (!followTarget) return;

        // 1) Идеальная точка без коллизий в МИРЕ из локального смещения
        Vector3 idealPos = followTarget.TransformPoint(localOffset);
        Vector3 from = followTarget.position;

        Vector3 desiredDir = (idealPos - from).normalized;
        float desiredDist = Vector3.Distance(from, idealPos);

        // 2) Сгладить смену направления (даёт плавное «боковое» следование)
        float t = 1f - Mathf.Exp(-dirSmoothSpeed * Time.deltaTime); // плавный коэффициент
        smoothedDir = Vector3.Slerp(smoothedDir, desiredDir, t);
        if (smoothedDir.sqrMagnitude < 1e-6f) smoothedDir = desiredDir; // защита
        smoothedDir.Normalize();

        // 3) Коллизии по сглаженному направлению
        float targetDist = desiredDist;
        if (Physics.SphereCast(from, camRadius, smoothedDir, out var hit, desiredDist, collisionMask, QueryTriggerInteraction.Ignore))
            targetDist = Mathf.Max(minDistance, hit.distance - wallPadding);

        Vector3 targetPos = from + smoothedDir * targetDist;

        // 4) Плавное смещение камеры
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref posVel, posSmoothTime);

        // 5) Плавный взгляд на субмарину
        var desiredRot = Quaternion.LookRotation(from - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Time.deltaTime * lookLerpSpeed);
    }
}
