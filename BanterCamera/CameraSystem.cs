using MelonLoader;
using UnityEngine;
using System.Reflection;

namespace BanterCamera
{
    public class CameraMod : MelonMod
    {
        public bool IsQuest;
        static CameraSystem cameraStuff = new CameraSystem();

        private MelonPreferences_Category ConfigCategory;
        private MelonPreferences_Entry<float> SmoothingSpeed;
        private MelonPreferences_Entry<bool> Smoothpos;
        private MelonPreferences_Entry<bool> SmoothRot;

        private MelonPreferences_Category ConfigDebugCategory;
        private MelonPreferences_Entry<bool> EnableInDesktop;

        public override void OnInitializeMelon()
        {
            base.OnLateInitializeMelon();
            if (UnityEngine.XR.XRSettings.enabled)
            {
                LoggerInstance.Msg("VR is enabled, enableing smooth cam.");
            }
            else
            {
                LoggerInstance.Msg("VR is not enabled");
            }
            LoggerInstance.Msg("Screen Width: " + Screen.width + " Screen Hight:" +  Screen.height);

            ConfigCategory = MelonPreferences.CreateCategory("BanterCamera");
            ConfigCategory.SetFilePath("UserData/BanterCamera.cfg");
            SmoothingSpeed = ConfigCategory.CreateEntry<float>("Smoothing Speed", 9f);
            Smoothpos = ConfigCategory.CreateEntry<bool>("Smooth Position", false);
            SmoothRot = ConfigCategory.CreateEntry<bool>("Smooth Rotation", true);
            ConfigCategory.SaveToFile();

            cameraStuff.smoothSpeed = SmoothingSpeed.Value;
            cameraStuff.SmoothPosition = Smoothpos.Value;
            cameraStuff.SmoothRotation = SmoothRot.Value;

            ConfigDebugCategory = MelonPreferences.CreateCategory("BanterCameraDebug");
            EnableInDesktop = ConfigDebugCategory.CreateEntry<bool>("Enable in Desktop", false);

            IsQuest = MelonUtils.CurrentPlatform == (MelonPlatformAttribute.CompatiblePlatforms)3; //This will be true if using LemonLoader / Android
            if (IsQuest)
            {
                cameraStuff.OutPutEyes = StereoTargetEyeMask.Left; //Output to the left eye as this is what meta uses to record
            }
        }

        public override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            ConfigCategory.SaveToFile();
        }
        public override void OnUpdate()
        {
            if (UnityEngine.XR.XRSettings.enabled || EnableInDesktop.Value)
            {
                cameraStuff.SmoothMoveCamera(Time.deltaTime);
            }
        }
        public override void OnSceneWasInitialized(int buildIndex, string sceneName) 
        {
            if (UnityEngine.XR.XRSettings.enabled || EnableInDesktop.Value)
            {
                GameObject PlayerHead = Camera.main.gameObject;
                cameraStuff.PlayerHead = PlayerHead.transform;
                cameraStuff.VRMainCam = Camera.main;
                cameraStuff.CreateCamera();
            }
        }

        public override void OnGUI()
        {
            base.OnGUI();
        }

    }
    public class CameraSystem
    {
        private Vector3 CameraPosition = Vector3.zero;
        private Quaternion CameraRot = new Quaternion(0,0,0,0);
        public Transform PlayerHead;
        public Camera VRMainCam;
        public float smoothSpeed = 9;
        public GameObject SpectatorCam;
        public int cullingMask = 0;
        public bool SmoothPosition;
        public bool SmoothRotation;
        public StereoTargetEyeMask OutPutEyes = StereoTargetEyeMask.None;

        public void CreateCamera()
        {
            if (SpectatorCam != null)
            {
                try
                {
                    Object.Destroy(SpectatorCam);
                }
                catch { }
            }
            CameraPosition = PlayerHead.position; CameraRot = PlayerHead.rotation; //Reset Positions and rotations.


            // Create a new GameObject
            SpectatorCam = new GameObject("SpectatorCam");

            // Add a Camera component to the GameObject
            Camera cameraComponent = SpectatorCam.AddComponent<Camera>();

            //Change the setting of the camera conponant
            cameraComponent.farClipPlane = VRMainCam.farClipPlane;
            cameraComponent.nearClipPlane = VRMainCam.nearClipPlane;
            if (cullingMask == 0)
            {
                cameraComponent.cullingMask = VRMainCam.cullingMask;
            }
            else if (cullingMask < 0)
            {
                cameraComponent.cullingMask = VRMainCam.cullingMask + cullingMask;
            }
            else
            {
                cameraComponent.cullingMask = cullingMask;
            }
           
            cameraComponent.depth = 2;
            cameraComponent.stereoTargetEye = OutPutEyes;
            cameraComponent.fieldOfView = 90;
            cameraComponent.allowDynamicResolution = true;
            cameraComponent.ResetAspect();
            

        }

        public void SmoothMoveCamera(float deltaTime)
        {
            if (SpectatorCam == null)
            {
                return;
            }

            //Establish vars
            Vector3 TargetPos = PlayerHead.transform.position;
            Quaternion TargetRot = PlayerHead.transform.rotation;
            float lerpPoint = deltaTime * smoothSpeed; //yep, this is a smoothed time method of smooth cam instad of the proper method.

            if (lerpPoint > 1)
            {
                lerpPoint = 1;
            }
            else if (lerpPoint < 0)
            {
                lerpPoint = 0;
            }

            if(SmoothPosition)
            {
                CameraPosition = Vector3.Lerp(CameraPosition, TargetPos, lerpPoint);
            }
            else
            {
                CameraPosition = TargetPos;
            }
            
            if(SmoothRotation)
            {
                CameraRot = Quaternion.Lerp(CameraRot, TargetRot, lerpPoint);
            }
            else
            {
                CameraRot = TargetRot;
            }
            

            SpectatorCam.transform.position = CameraPosition;
            SpectatorCam.transform.rotation = CameraRot;
        }
    }
}
