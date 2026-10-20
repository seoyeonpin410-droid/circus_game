using UnityEngine;
using UnityEngine.InputSystem;

namespace CircusPrototype
{
    [DefaultExecutionOrder(-50)]
    public sealed class CircusCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Transform firstPersonBell;
        [SerializeField] float sensitivity = .12f;
        [SerializeField] float thirdPersonDistance = 5;
        [SerializeField] float eyeHeight = 1.55f;
        [SerializeField] LayerMask obstructionMask = 1;
        CircusSession session;
        CircusPlayer player;
        Camera view;
        float yaw;
        float pitch = 15;
        bool activeView;
        public bool FirstPerson { get; private set; }
        public Vector3 AimForward => Quaternion.Euler(0, yaw, 0) * Vector3.forward;
        void Awake()
        {
            view = GetComponent<Camera>();
            session = FindFirstObjectByType<CircusSession>();
            player = target.GetComponent<CircusPlayer>();
        }
        void Update()
        {
            activeView = session && session.CanPause;
            bool capture = activeView && Application.isFocused;
            Cursor.lockState = capture ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !capture;
            if (!activeView) return;
            if (Keyboard.current?.vKey.wasPressedThisFrame == true) SetFirstPerson(!FirstPerson);
            if (capture && Mouse.current != null)
            {
                var delta = Mouse.current.delta.ReadValue();
                yaw += delta.x * sensitivity;
                pitch = Mathf.Clamp(pitch - delta.y * sensitivity, -60, 70);
            }
        }
        public void SetFirstPerson(bool value) { FirstPerson = value; ApplyPresentation(); }
        void ApplyPresentation()
        {
            player.SetFirstPersonBodyHidden(activeView && FirstPerson);
            if (firstPersonBell) firstPersonBell.gameObject.SetActive(activeView && FirstPerson);
        }
        void LateUpdate()
        {
            if (!target || !session) return;
            ApplyPresentation();
            view.orthographic = session.State == ShowState.Menu;
            if (session.State == ShowState.Menu)
            {
                transform.position = new Vector3(0, 17, -14);
                transform.LookAt(Vector3.zero);
                return;
            }
            view.fieldOfView = FirstPerson ? 75 : 65;
            var rotation = Quaternion.Euler(pitch, yaw, 0);
            var eye = target.position + Vector3.up * eyeHeight;
            var desired = FirstPerson ? eye : eye - rotation * Vector3.forward * thirdPersonDistance;
            if (!FirstPerson && Physics.SphereCast(eye, .2f, (desired - eye).normalized,
                out var hit, thirdPersonDistance, obstructionMask, QueryTriggerInteraction.Ignore))
                desired = eye + (desired - eye).normalized * Mathf.Max(.25f, hit.distance - .1f);
            transform.SetPositionAndRotation(desired, rotation);
            if (firstPersonBell) firstPersonBell.localRotation = Quaternion.Euler(0, 180, player.AttackSwing * -45);
        }
        void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (player) player.SetFirstPersonBodyHidden(false);
        }
    }
}
