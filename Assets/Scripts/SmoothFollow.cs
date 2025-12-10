using UnityEngine;

public class SmoothFollow : MonoBehaviour
{
    public Transform target;                // субмарина
    public Vector3 offset = new Vector3(0, 2, -6);
    public float followSmooth = 3f;         // чем ниже, тем более вялое следование

    private Vector3 velocity;

    void LateUpdate()
    {
        if (!target) return;

        // желаемая позиция за субмариной
        Vector3 desiredPos = target.position
                           + target.transform.TransformDirection(offset);

        // плавно догоняем
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPos,
            ref velocity,
            1f / followSmooth
        );

        // плавно поворачиваемся лицом к лодке/направлению
        Quaternion desiredRot = Quaternion.LookRotation(
            target.position - transform.position,
            Vector3.up
        );
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRot,
            Time.deltaTime * followSmooth * 0.5f
        );
    }
}
