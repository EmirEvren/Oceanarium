using UnityEngine;
using UnityEngine.InputSystem;
namespace OceanariumFishing
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class LocalPlayerController : MonoBehaviour
    {
        public Transform visual, cameraTarget;
        public Camera view;
        public float walkSpeed=3.4f, runSpeed=5.8f, acceleration=13, sensitivity=.14f;
        public bool InputEnabled {get;set;}=true;
        public bool FishingLocked {get;set;}
        public float Speed {get;private set;}
        public Vector3 SafeSpawn {get;private set;}
        private CharacterController motor;
        private Vector3 velocity;
        private float gravitySpeed,yaw,pitch=18;
        private void Awake() {motor=GetComponent<CharacterController>();SafeSpawn=transform.position;yaw=transform.eulerAngles.y;}
        private void Update()
        {
            var keys=Keyboard.current; var mouse=Mouse.current;
            Vector2 input=Vector2.zero;
            if(InputEnabled && keys!=null)
            {
                if(!FishingLocked)input=new Vector2((keys.dKey.isPressed?1:0)-(keys.aKey.isPressed?1:0),(keys.wKey.isPressed?1:0)-(keys.sKey.isPressed?1:0));
                if(mouse!=null && mouse.rightButton.isPressed)
                {var delta=mouse.delta.ReadValue();yaw+=delta.x*sensitivity;pitch=Mathf.Clamp(pitch-delta.y*sensitivity,8,65);}
            }
            cameraTarget.position=transform.position+Vector3.up*1.55f;
            cameraTarget.rotation=Quaternion.Euler(pitch,yaw,0);
            float speed=InputEnabled && keys!=null && keys.leftShiftKey.isPressed?runSpeed:walkSpeed;
            var desired=Quaternion.Euler(0,yaw,0)*new Vector3(input.x,0,input.y).normalized*speed;
            velocity=Vector3.MoveTowards(velocity,desired,acceleration*Time.deltaTime);
            gravitySpeed=motor.isGrounded?-2:Mathf.Max(-30,gravitySpeed-22*Time.deltaTime);
            motor.Move((velocity+Vector3.up*gravitySpeed)*Time.deltaTime);
            Speed=new Vector2(motor.velocity.x,motor.velocity.z).magnitude;
            if(velocity.sqrMagnitude>.05f) visual.rotation=Quaternion.Slerp(visual.rotation,Quaternion.LookRotation(velocity),1-Mathf.Exp(-12*Time.deltaTime));
            if(transform.position.y < -2) Teleport(SafeSpawn);
        }
        public void SetViewAngles(float heading,float elevation){yaw=heading;pitch=Mathf.Clamp(elevation,8,65);cameraTarget.rotation=Quaternion.Euler(pitch,yaw,0);}
        public void Teleport(Vector3 position) {motor.enabled=false;transform.position=position;motor.enabled=true;velocity=Vector3.zero;gravitySpeed=0;}
        private void OnDisable() {Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
    }
}

