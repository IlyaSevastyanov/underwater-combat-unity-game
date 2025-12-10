using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FishFreezeController : MonoBehaviour
{
    [Header("Behaviour disabling")]
    [Tooltip("Отключать все MonoBehaviour на рыбе, кроме белого списка.")]
    public bool forceDisableAllBehaviours = true;

    [Tooltip("Типы, которые НЕ будем отключать.")]
    public string[] whitelistTypeNames = new[]
    {
        "FishFreezeController",
        "FishHighlighter",
        "ButcherSequencer",
        "CutFxPreset"
    };

    [Header("Transform lock")]
    [Tooltip("Жёстко держать трансформ корня, чтобы ничто не сдвинуло рыбу.")]
    public bool lockRootTransform = true;

    [Header("Physics freeze")]
    [Tooltip("Замораживать ли Rigidbody (true по умолчанию). Можно выключить на конкретной рыбе.")]
    public bool freezeRigidbodies = true;

    // saved states
    struct MBState { public MonoBehaviour mb; public bool enabled; }
    struct RBState
    {
        public Rigidbody rb; public bool isKinematic; public RigidbodyConstraints constraints;
        public Vector3 vel; public Vector3 angVel;
    }
    struct AgentState
    {
        public NavMeshAgent agent; public bool wasEnabled; public bool wasStopped; public bool updPos; public bool updRot;
    }
    struct AnimatorState { public Animator anim; public float speed; public bool applyRootMotion; }

    readonly List<MBState> _behaviourStates = new();
    readonly List<RBState> _rbStates = new();
    readonly List<AgentState> _agentStates = new();
    readonly List<AnimatorState> _animStates = new();

    Vector3 _savedPos;
    Quaternion _savedRot;
    Vector3 _savedScale;

    bool _frozen;

    bool IsWhitelisted(MonoBehaviour mb)
    {
        var t = mb.GetType().Name;
        for (int i = 0; i < whitelistTypeNames.Length; i++)
        {
            if (t == whitelistTypeNames[i]) return true;
        }
        return false;
    }

    public void Freeze()
    {
        if (_frozen) return;

        // --- save root transform
        if (lockRootTransform)
        {
            _savedPos = transform.position;
            _savedRot = transform.rotation;
            _savedScale = transform.localScale;
        }

        // --- freeze ALL rigidbodies
        _rbStates.Clear();
        if (freezeRigidbodies)
        {
            var rbs = GetComponentsInChildren<Rigidbody>(true);
            foreach (var rb in rbs)
            {
                var st = new RBState
                {
                    rb = rb,
                    isKinematic = rb.isKinematic,
                    constraints = rb.constraints,
                    vel = rb.velocity,
                    angVel = rb.angularVelocity
                };
                _rbStates.Add(st);

                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                rb.constraints = RigidbodyConstraints.FreezeAll;
            }
        }

        // --- stop ALL animators
        _animStates.Clear();
        var anims = GetComponentsInChildren<Animator>(true);
        foreach (var a in anims)
        {
            var st = new AnimatorState { anim = a, speed = a.speed, applyRootMotion = a.applyRootMotion };
            _animStates.Add(st);
            a.speed = 0f;
            a.applyRootMotion = false;
        }

        // --- stop ALL navmesh agents
        _agentStates.Clear();
        var agents = GetComponentsInChildren<NavMeshAgent>(true);
        foreach (var ag in agents)
        {
            var st = new AgentState
            {
                agent = ag,
                wasEnabled = ag.enabled,
                wasStopped = ag.isStopped,
                updPos = ag.updatePosition,
                updRot = ag.updateRotation
            };
            _agentStates.Add(st);
            if (ag.enabled)
            {
                ag.isStopped = true;
                ag.updatePosition = false;
                ag.updateRotation = false;
            }
        }

        // --- disable behaviours (except whitelist)
        _behaviourStates.Clear();
        var mbs = GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var mb in mbs)
        {
            if (!mb) continue;
            if (mb == this) continue; // себя не трогаем
            if (forceDisableAllBehaviours && IsWhitelisted(mb)) continue;

            // если forceDisableAllBehaviours=false — отключим только явные «движки»
            bool isMovementLike = false;
            if (!forceDisableAllBehaviours)
            {
                var n = mb.GetType().Name.ToLowerInvariant();
                isMovementLike = n.Contains("move") || n.Contains("swim") || n.Contains("wander") ||
                                 n.Contains("steer") || n.Contains("ai") || n.Contains("path") ||
                                 n.Contains("follow") || n.Contains("seek") || n.Contains("avoid") ||
                                 n.Contains("controller");
            }

            if (forceDisableAllBehaviours || isMovementLike)
            {
                _behaviourStates.Add(new MBState { mb = mb, enabled = mb.enabled });
                mb.enabled = false;
            }
        }

        _frozen = true;
    }

    public void Unfreeze()
    {
        if (!_frozen) return;

        // restore behaviours
        foreach (var st in _behaviourStates)
            if (st.mb) st.mb.enabled = st.enabled;
        _behaviourStates.Clear();

        // restore agents
        foreach (var st in _agentStates)
        {
            var ag = st.agent;
            if (!ag) continue;
            if (st.wasEnabled)
            {
                ag.updatePosition = st.updPos;
                ag.updateRotation = st.updRot;
                ag.isStopped = st.wasStopped;
            }
        }
        _agentStates.Clear();

        // restore animators
        foreach (var st in _animStates)
        {
            var a = st.anim;
            if (!a) continue;
            a.speed = st.speed;
            a.applyRootMotion = st.applyRootMotion;
        }
        _animStates.Clear();

        // restore rigidbodies
        if (freezeRigidbodies)
        {
            foreach (var st in _rbStates)
            {
                var rb = st.rb;
                if (!rb) continue;
                rb.isKinematic = st.isKinematic;
                rb.constraints = st.constraints;
                rb.velocity = st.vel;
                rb.angularVelocity = st.angVel;
            }
        }
        _rbStates.Clear();

        _frozen = false;
    }

    void LateUpdate()
    {
        if (_frozen && lockRootTransform)
        {
            // Жёстко удерживаем позу корня
            transform.position = _savedPos;
            transform.rotation = _savedRot;
            transform.localScale = _savedScale;
        }
    }
}
