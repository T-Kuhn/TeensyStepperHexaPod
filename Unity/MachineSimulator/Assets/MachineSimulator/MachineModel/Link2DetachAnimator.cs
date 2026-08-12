using UnityEngine;

namespace MachineSimulator.MachineModel
{
    // NOTE: Demo helper for the IK explanation video. While active, Joint4/Joint5 stop
    //       driving Link3 (it freezes at its current pose) and instead swing Link2 around
    //       the wrist universal joint, so Link2's free end (the Joint2/Joint3 end) travels
    //       along the IK sphere surface or along the plane-sphere intersection circle.
    //       Exit() restores the original hierarchy and joint rotations bit-exactly.
    public sealed class Link2DetachAnimator
    {
        public enum Mode
        {
            Sphere,
            Circle,
        }

        private const float NoiseSeedA = 12.9898f;
        private const float NoiseSeedB = 78.233f;
        private const float ValidReconstructionError = 1e-4f;

        private readonly Transform _armRoot;
        private readonly Transform _joint3;
        private readonly Transform _joint4;
        private readonly Transform _joint5;

        private Transform _link2;
        private Transform _link3;

        private Vector3 _savedLink2LocalPosition;
        private Quaternion _savedLink2LocalRotation;
        private int _savedLink2SiblingIndex;
        private Vector3 _savedLink3LocalPosition;
        private Quaternion _savedLink3LocalRotation;
        private int _savedLink3SiblingIndex;
        private Quaternion _savedJoint4LocalRotation;
        private Quaternion _savedJoint5LocalRotation;

        private float _animTime;
        private bool _warnedAboutUnsolvableDirection;

        // NOTE: Joint4/Joint5 angles at enter time (the joints hold pure Y / pure X eulers)
        private float _alpha0;
        private float _beta0;
        private float _alphaPrev;
        private float _betaPrev;

        // NOTE: Link2's free-end direction in Joint5-local space, captured at enter time.
        //       Analytically u = (-sin a0, cos a0 sin b0, cos a0 cos b0).
        private Vector3 _link2LocalDir;

        // NOTE: Sphere mode: noise samples at enter time, so the wander starts jump-free
        private float _noiseStartA;
        private float _noiseStartB;

        // NOTE: Circle mode: intersection circle in arm-root local space (z = 0 plane)
        private Vector3 _sphereCenter;
        private Vector2 _circleCenter;
        private float _circleRadius;
        private float _theta0;

        public bool IsActive { get; private set; }
        public Mode CurrentMode { get; private set; }

        public Link2DetachAnimator(Transform armRoot, Transform joint3, Transform joint4, Transform joint5)
        {
            _armRoot = armRoot;
            _joint3 = joint3;
            _joint4 = joint4;
            _joint5 = joint5;
        }

        // NOTE: Captures the current pose, reparents Link3 out of Joint5 (freezing it in
        //       place) and Link2 under Joint5, so that Joint4/Joint5 now rotate Link2.
        //       Returns false without changing anything if the current pose is unusable.
        public bool TryEnter(Mode mode, out string failReason)
        {
            failReason = null;

            if (IsActive)
            {
                failReason = "an animation is already active";
                return false;
            }

            if (_link2 == null) _link2 = _joint3.Find("Link2:1");
            if (_link3 == null) _link3 = _joint5.Find("Link3:1");
            if (_link2 == null || _link3 == null)
            {
                failReason = "could not find 'Link2:1' under Joint3 / 'Link3:1' under Joint5";
                return false;
            }

            var alpha0 = Vector3.SignedAngle(Vector3.forward, _joint4.localRotation * Vector3.forward, Vector3.up);
            var beta0 = Vector3.SignedAngle(Vector3.up, _joint5.localRotation * Vector3.up, Vector3.right);
            var link2LocalDir = _joint5.InverseTransformDirection((_joint3.position - _joint5.position).normalized);
            var sphereCenter = _armRoot.InverseTransformPoint(_joint4.position);
            var sphereRadius = Mathf.Abs(_joint4.localPosition.z);
            var freeEnd = _armRoot.InverseTransformPoint(_joint3.position);

            // NOTE: The IK solver can produce NaN poses while reporting success (division
            //       by zero when sphereCenter.y == circleCenter.y), so check explicitly.
            if (!IsFinite(link2LocalDir) || !IsFinite(sphereCenter) || !IsFinite(freeEnd)
                || float.IsNaN(alpha0) || float.IsNaN(beta0))
            {
                failReason = "the current pose contains non-finite values";
                return false;
            }

            if (mode == Mode.Circle)
            {
                // NOTE: Same construction as in IkDebugVisualizer: the sphere cut by the
                //       link1 circle plane (arm-local z = 0) forms the intersection circle.
                var circleRadiusSquared = sphereRadius * sphereRadius - sphereCenter.z * sphereCenter.z;
                if (circleRadiusSquared < 1e-6f)
                {
                    failReason = "the link1 circle plane does not cut the link2 sphere at the current pose";
                    return false;
                }

                _circleCenter = new Vector2(sphereCenter.x, sphereCenter.y);
                _circleRadius = Mathf.Sqrt(circleRadiusSquared);

                // NOTE: The free end currently sits on the intersection circle (it is one of
                //       the two IK solutions), so starting theta there makes the start jump-free.
                _theta0 = Mathf.Atan2(freeEnd.y - _circleCenter.y, freeEnd.x - _circleCenter.x);
            }
            else
            {
                _noiseStartA = SignedNoise(0f, NoiseSeedA);
                _noiseStartB = SignedNoise(0f, NoiseSeedB);
            }

            _savedLink2LocalPosition = _link2.localPosition;
            _savedLink2LocalRotation = _link2.localRotation;
            _savedLink2SiblingIndex = _link2.GetSiblingIndex();
            _savedLink3LocalPosition = _link3.localPosition;
            _savedLink3LocalRotation = _link3.localRotation;
            _savedLink3SiblingIndex = _link3.GetSiblingIndex();
            _savedJoint4LocalRotation = _joint4.localRotation;
            _savedJoint5LocalRotation = _joint5.localRotation;

            _alpha0 = alpha0;
            _beta0 = beta0;
            _alphaPrev = alpha0;
            _betaPrev = beta0;
            _link2LocalDir = link2LocalDir;
            _sphereCenter = sphereCenter;
            _animTime = 0f;
            _warnedAboutUnsolvableDirection = false;

            _link3.SetParent(_armRoot, worldPositionStays: true);
            _link2.SetParent(_joint5, worldPositionStays: true);

            CurrentMode = mode;
            IsActive = true;

            // NOTE: Self-check: at enter time the wrist -> free-end direction is exactly +z
            //       in Joint3's frame (Joint4 sits at (0, 0, -0.202) under Joint3), so the
            //       solver must reproduce the captured joint angles. If this assert fires,
            //       one of the two sign-marked lines in the solver below is wrong.
            if (mode == Mode.Circle)
            {
                SolveYx(Vector3.forward, out var alphaCheck, out var betaCheck);
                Debug.Assert(
                    Mathf.Abs(Mathf.DeltaAngle(alphaCheck, alpha0)) < 0.1f
                    && Mathf.Abs(Mathf.DeltaAngle(betaCheck, beta0)) < 0.1f,
                    $"Link2DetachAnimator self-check failed: solver returned ({alphaCheck:F2}, {betaCheck:F2}), expected ({alpha0:F2}, {beta0:F2})");
            }

            return true;
        }

        // NOTE: Restores the original hierarchy and rotations. Assigning the saved local
        //       values (instead of relying on worldPositionStays math) makes the restore
        //       bit-exact regardless of float drift accumulated while animating.
        public void Exit()
        {
            if (!IsActive) return;
            IsActive = false;

            if (_link2 != null && _joint3 != null)
            {
                _link2.SetParent(_joint3, worldPositionStays: false);
                _link2.localPosition = _savedLink2LocalPosition;
                _link2.localRotation = _savedLink2LocalRotation;
                _link2.SetSiblingIndex(_savedLink2SiblingIndex);
            }

            if (_link3 != null && _joint5 != null)
            {
                _link3.SetParent(_joint5, worldPositionStays: false);
                _link3.localPosition = _savedLink3LocalPosition;
                _link3.localRotation = _savedLink3LocalRotation;
                _link3.SetSiblingIndex(_savedLink3SiblingIndex);
            }

            if (_joint4 != null) _joint4.localRotation = _savedJoint4LocalRotation;
            if (_joint5 != null) _joint5.localRotation = _savedJoint5LocalRotation;
        }

        public void Tick(float deltaTime, float wanderSpeed, float wanderAmplitudeDeg, float circleSpeedDegPerSec)
        {
            if (!IsActive) return;

            _animTime += deltaTime;

            if (CurrentMode == Mode.Sphere)
            {
                TickSphere(wanderSpeed, wanderAmplitudeDeg);
            }
            else
            {
                TickCircle(circleSpeedDegPerSec);
            }
        }

        // NOTE: Link2's free end sits at a constant distance from the wrist (= the sphere
        //       center), so ANY Joint4/Joint5 rotation keeps it on the sphere surface.
        //       Two decorrelated Perlin channels around the enter pose give a smooth random
        //       sweep; subtracting the enter-time samples makes the start jump-free.
        private void TickSphere(float wanderSpeed, float wanderAmplitudeDeg)
        {
            var t = _animTime * wanderSpeed;
            var alpha = _alpha0 + wanderAmplitudeDeg * (SignedNoise(t, NoiseSeedA) - _noiseStartA);
            var beta = _beta0 + wanderAmplitudeDeg * (SignedNoise(t, NoiseSeedB) - _noiseStartB);
            ApplyJointRotations(alpha, beta);
        }

        private void TickCircle(float circleSpeedDegPerSec)
        {
            var theta = _theta0 + _animTime * circleSpeedDegPerSec * Mathf.Deg2Rad;
            var pointOnCircle = new Vector3(
                _circleCenter.x + _circleRadius * Mathf.Cos(theta),
                _circleCenter.y + _circleRadius * Mathf.Sin(theta),
                0f);

            // NOTE: Joint3 is frozen while the animation runs, so its frame is stable.
            var worldDir = _armRoot.TransformDirection((pointOnCircle - _sphereCenter).normalized);
            var dirInJoint3Frame = _joint3.InverseTransformDirection(worldDir);

            var solvedExactly = SolveYx(dirInJoint3Frame, out var alpha, out var beta);
            if (!solvedExactly && !_warnedAboutUnsolvableDirection)
            {
                _warnedAboutUnsolvableDirection = true;
                Debug.LogWarning("Link2DetachAnimator: could not exactly reach a point on the intersection circle, using the closest reachable pose.");
            }

            ApplyJointRotations(alpha, beta);
        }

        private void ApplyJointRotations(float alphaDeg, float betaDeg)
        {
            _joint4.localRotation = Quaternion.Euler(0f, alphaDeg, 0f);
            _joint5.localRotation = Quaternion.Euler(betaDeg, 0f, 0f);
            _alphaPrev = alphaDeg;
            _betaPrev = betaDeg;
        }

        // NOTE: Solves R_y(alpha) * R_x(beta) * u = dir for the Joint4 (Y) and Joint5 (X)
        //       angles, u being Link2's free-end direction in Joint5-local space and dir the
        //       wanted free-end direction in Joint3's frame. A Y-rotation cannot change the
        //       y-component, so beta follows from u.y*cos(beta) - u.z*sin(beta) = dir.y alone
        //       (two branches); alpha then aligns the xz-projections. Both candidates are
        //       validated by reconstructing the direction with Unity's own quaternion math,
        //       so a sign mistake can never produce a silently wrong orbit. Returns false
        //       when only a clamped best-effort pose exists.
        private bool SolveYx(Vector3 dir, out float alphaDeg, out float betaDeg)
        {
            alphaDeg = _alphaPrev;
            betaDeg = _betaPrev;

            var a = _link2LocalDir.y;
            var b = -_link2LocalDir.z; // NOTE: flip this sign if the enter-time self-check assert fires
            var c = dir.y;
            var k = Mathf.Sqrt(a * a + b * b);

            // NOTE: Only possible when link2 lies on Joint4's yaw axis; keep the previous pose.
            if (k < 1e-6f) return false;

            var phi = Mathf.Atan2(b, a);
            var delta = Mathf.Acos(Mathf.Clamp(c / k, -1f, 1f));
            var betaPlus = (phi + delta) * Mathf.Rad2Deg;
            var betaMinus = (phi - delta) * Mathf.Rad2Deg;

            EvaluateBetaCandidate(betaPlus, dir, out var alphaPlus, out var errorPlus);
            EvaluateBetaCandidate(betaMinus, dir, out var alphaMinus, out var errorMinus);

            var plusIsValid = errorPlus < ValidReconstructionError;
            var minusIsValid = errorMinus < ValidReconstructionError;

            bool pickPlus;
            if (plusIsValid && minusIsValid)
            {
                // NOTE: Both branches reach the direction exactly; stay on the branch closest
                //       to the previous frame so the motion never flips mid-orbit.
                pickPlus = ContinuityCost(betaPlus) <= ContinuityCost(betaMinus);
            }
            else if (plusIsValid || minusIsValid)
            {
                pickPlus = plusIsValid;
            }
            else
            {
                pickPlus = errorPlus <= errorMinus;
            }

            alphaDeg = pickPlus ? alphaPlus : alphaMinus;
            betaDeg = pickPlus ? betaPlus : betaMinus;
            return pickPlus ? plusIsValid : minusIsValid;
        }

        private void EvaluateBetaCandidate(float betaDeg, Vector3 dir, out float alphaDeg, out float error)
        {
            var w = Quaternion.Euler(betaDeg, 0f, 0f) * _link2LocalDir;
            var wFlat = new Vector3(w.x, 0f, w.z);
            var dirFlat = new Vector3(dir.x, 0f, dir.z);

            alphaDeg = wFlat.sqrMagnitude < 1e-8f
                ? _alphaPrev
                : Vector3.SignedAngle(wFlat, dirFlat, Vector3.up); // NOTE: flip this axis if the enter-time self-check assert fires

            var reconstructed = Quaternion.Euler(0f, alphaDeg, 0f) * w;
            error = (reconstructed - dir).sqrMagnitude;
        }

        // NOTE: Beta distance ONLY. The two solution branches are beta = phi +/- delta, so
        //       the branch identity lives purely in beta; mixing alpha into the cost can
        //       flip branches near the fast-swing latitudes where the correct branch has a
        //       high alpha velocity.
        private float ContinuityCost(float betaDeg)
        {
            return Mathf.Abs(Mathf.DeltaAngle(_betaPrev, betaDeg));
        }

        private static float SignedNoise(float t, float seed)
        {
            return (Mathf.PerlinNoise(t + seed, seed * 0.7317f) - 0.5f) * 2f;
        }

        private static bool IsFinite(Vector3 v)
        {
            return !float.IsNaN(v.x) && !float.IsInfinity(v.x)
                   && !float.IsNaN(v.y) && !float.IsInfinity(v.y)
                   && !float.IsNaN(v.z) && !float.IsInfinity(v.z);
        }
    }
}
