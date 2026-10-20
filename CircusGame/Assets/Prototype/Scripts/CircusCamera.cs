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
        bool activeView,introCamera;
        Vector3 introStartPosition;Quaternion introStartRotation;float introStartFov;
        public bool IntroCameraActive=>introCamera;
        CircusBossEntrance Intro=>session && session.Boss?session.Boss.GetComponent<CircusBossEntrance>():null;
        float shakeUntil, shakeStrength;
        public void Shake(float strength=.035f,float seconds=.12f)
        { shakeStrength=Mathf.Clamp(strength,0,.05f);shakeUntil=Time.time+seconds; }
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
            if (!activeView || session.State==ShowState.BossIntro) return;
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
            player.SetFirstPersonBodyHidden(activeView && FirstPerson && !(Intro && !Intro.Complete));
            if (firstPersonBell) firstPersonBell.gameObject.SetActive(activeView && FirstPerson && !(Intro && !Intro.Complete));
        }
        void LateUpdate()
        {
            if (!target || !session) return;
            ApplyPresentation();
            view.orthographic = session.State == ShowState.Menu;
            if (session.State == ShowState.Menu)
            {introCamera=false;
                transform.position = new Vector3(0, 17, -14);
                transform.LookAt(Vector3.zero);
                return;
            }
            view.fieldOfView = FirstPerson ? 75 : 65;
            var entry=Intro;
            if(entry && !entry.Complete) { ApplyIntro(entry);return; }
            if(introCamera){introCamera=false;}
            var rotation = Quaternion.Euler(pitch, yaw, 0);
            var eye = target.position + Vector3.up * eyeHeight;
            var desired = FirstPerson ? eye : eye - rotation * Vector3.forward * thirdPersonDistance;
            if (!FirstPerson && Physics.SphereCast(eye, .2f, (desired - eye).normalized,
                out var hit, thirdPersonDistance, obstructionMask, QueryTriggerInteraction.Ignore))
                desired = eye + (desired - eye).normalized * Mathf.Max(.25f, hit.distance - .1f);
            transform.SetPositionAndRotation(desired, rotation);
            ApplyShake(rotation);
            if (firstPersonBell) firstPersonBell.localRotation = Quaternion.Euler(0, 180, player.AttackSwing * -45);
        }
        void ApplyIntro(CircusBossEntrance entry) {
            if(!introCamera){introCamera=true;introStartPosition=transform.position;introStartRotation=transform.rotation;introStartFov=view.fieldOfView;}
            var bossPosition=session.Boss.transform.position;
            var toward=bossPosition-target.position;toward.y=0;var direction=toward.sqrMagnitude>.01f?toward.normalized:Vector3.forward;
            var look=bossPosition+Vector3.up*entry.LookHeight;
            var wide=bossPosition-direction*entry.WideDistance+Vector3.up*entry.WideHeight;
            var close=bossPosition-direction*entry.CloseDistance+Vector3.up*entry.CloseHeight;
            float lead=Mathf.SmoothStep(0,1,entry.Elapsed/Mathf.Max(.1f,entry.QuietSeconds));
            var position=Vector3.Lerp(introStartPosition,wide,lead);
            var rotation=Quaternion.Slerp(introStartRotation,Quaternion.LookRotation(look-wide),lead);
            position=Vector3.Lerp(position,close,entry.CloseBlend);rotation=Quaternion.Slerp(rotation,Quaternion.LookRotation(look-close),entry.CloseBlend);
            yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;pitch=FirstPerson?0:8;
            var fightRotation=Quaternion.Euler(pitch,yaw,0);var eye=target.position+Vector3.up*eyeHeight;
            var fightPosition=FirstPerson?eye:eye-fightRotation*Vector3.forward*thirdPersonDistance;
            if(!FirstPerson && Physics.SphereCast(eye,.2f,(fightPosition-eye).normalized,out var hit,thirdPersonDistance,obstructionMask,QueryTriggerInteraction.Ignore))
                fightPosition=eye+(fightPosition-eye).normalized*Mathf.Max(.25f,hit.distance-.1f);
            position=Vector3.Lerp(position,fightPosition,entry.ReturnBlend);rotation=Quaternion.Slerp(rotation,fightRotation,entry.ReturnBlend);
            transform.SetPositionAndRotation(position,rotation);
            view.fieldOfView=Mathf.Lerp(Mathf.Lerp(introStartFov,entry.Fov,lead),FirstPerson?75:65,entry.ReturnBlend);
            ApplyShake(rotation);
        }
        void ApplyShake(Quaternion rotation) {
            if((session.IsRunning || introCamera) && Time.time<shakeUntil)
                transform.position+=rotation*new Vector3(Mathf.Sin(Time.time*83),Mathf.Sin(Time.time*97),0)*shakeStrength;
        }
        void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (player) player.SetFirstPersonBodyHidden(false);
        }
    }
}
