using UnityEngine;
using UnityEngine.InputSystem;

namespace CanopyKin
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerAnt : MonoBehaviour
    {
        CharacterController body; Transform cam; float yaw, pitch=18, vertical; public float Health { get; private set; }=100; public float Stamina { get; private set; }=100;
        public Transform CameraTransform => cam;
        void Start()
        {
            body=GetComponent<CharacterController>();
            cam=Camera.main.transform;
#if !UNITY_WEBGL
            Cursor.lockState=CursorLockMode.Locked;
#endif
        }
        void Update()
        {
            var k=Keyboard.current; var m=Mouse.current; if(k==null) return;
            if(Application.platform==RuntimePlatform.WebGLPlayer && Cursor.lockState!=CursorLockMode.Locked && m!=null && m.leftButton.wasPressedThisFrame) Cursor.lockState=CursorLockMode.Locked;
            if(k.escapeKey.wasPressedThisFrame) Cursor.lockState=Cursor.lockState==CursorLockMode.Locked?CursorLockMode.None:CursorLockMode.Locked;
            if(Cursor.lockState==CursorLockMode.Locked && m!=null) { var d=m.delta.ReadValue()*GameSettings.Sensitivity; yaw+=d.x; pitch=Mathf.Clamp(pitch-d.y,-10,55); }
            var input=new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0)); input=Vector2.ClampMagnitude(input,1);
            var forward=Quaternion.Euler(0,yaw,0)*Vector3.forward; var right=Quaternion.Euler(0,yaw,0)*Vector3.right; bool sprint=k.leftShiftKey.isPressed&&Stamina>1&&input.sqrMagnitude>.1f; float speed=sprint?7.2f:4.5f;
            Stamina=Mathf.Clamp(Stamina+(sprint?-24:16)*Time.deltaTime,0,100); var move=(forward*input.y+right*input.x)*speed;
            if(body.isGrounded) vertical=-1; else vertical-=18*Time.deltaTime; if(k.spaceKey.wasPressedThisFrame&&body.isGrounded) vertical=5;
            body.Move((move+Vector3.up*vertical)*Time.deltaTime); if(move.sqrMagnitude>.1f) transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(move),12*Time.deltaTime);
            var target=transform.position+Vector3.up*1.2f; var wanted=target+Quaternion.Euler(pitch,yaw,0)*new Vector3(0,0,-5.2f); if(Physics.Linecast(target,wanted,out var hit)) wanted=hit.point+(target-wanted).normalized*.25f; cam.position=Vector3.Lerp(cam.position,wanted,12*Time.deltaTime); cam.rotation=Quaternion.LookRotation(target-cam.position);
            if(k.eKey.wasPressedThisFrame) Interact(); if(Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame) Attack();
        }
        void Interact() { var hits=Physics.OverlapSphere(transform.position,2.2f); foreach(var h in hits) if(h.TryGetComponent<IInteractableHost>(out var i)) { i.Use(this); return; } }
        void Attack() { var p=transform.position+transform.forward*1.2f; foreach(var h in Physics.OverlapSphere(p,1.2f)) if(h.TryGetComponent<Creature>(out var c)) c.Damage(22); }
        public void Damage(float value) { Health=Mathf.Max(0,Health-value); if(Health<=0){ Health=100; transform.position=WorldBootstrap.NestPoint; } }
    }
    public interface IInteractable { string Prompt {get;} void Interact(PlayerAnt player); }
    public sealed class IInteractableHost : MonoBehaviour { public IInteractable Target; public void Use(PlayerAnt p)=>Target?.Interact(p); }
}
