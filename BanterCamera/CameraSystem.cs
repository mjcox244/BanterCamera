﻿using MelonLoader;
using UnityEngine;
using System.Reflection;

namespace BanterCamera
{
    public class CameraMod : MelonMod
    {
        public bool IsQuest;
        static CameraSystem cameraStuff = new CameraSystem();

        private MelonPreferences_Category ConfigCategory;
        private MelonPreferences_Entry<bool> Enabled;
        private MelonPreferences_Entry<float> SmoothingSpeed;
        private MelonPreferences_Entry<bool> Smoothpos;
        private MelonPreferences_Entry<bool> SmoothRot;
        private MelonPreferences_Entry<int> CullMask;
        

        private MelonPreferences_Category ConfigDebugCategory;
        private MelonPreferences_Entry<bool> EnableInDesktop;
        private MelonPreferences_Entry<bool> AllowQuestSupport;

        public override void OnInitializeMelon()
        {

            if (UnityEngine.XR.XRSettings.enabled)
            {
                LoggerInstance.Msg("VR is enabled");
            }
            else
            {
                LoggerInstance.Msg("VR is not enabled");
            }

            //Add all the values to the config file if not allready present
            ConfigCategory = MelonPreferences.CreateCategory("BanterCamera");
            ConfigCategory.SetFilePath("UserData/BanterCamera.cfg");
            Enabled = ConfigCategory.CreateEntry<bool>("Enabled", true);
            SmoothingSpeed = ConfigCategory.CreateEntry<float>("Smoothing Speed", 9f);
            Smoothpos = ConfigCategory.CreateEntry<bool>("Smooth Position", false);
            SmoothRot = ConfigCategory.CreateEntry<bool>("Smooth Rotation", true);
            CullMask = ConfigCategory.CreateEntry<int>("Culling Mask", 0);
            ConfigCategory.SaveToFile();

            //Apply the values ither just generated or stored in config to the main cameraStuff object
            cameraStuff.smoothSpeed = SmoothingSpeed.Value;
            cameraStuff.SmoothPosition = Smoothpos.Value;
            cameraStuff.SmoothRotation = SmoothRot.Value;
            cameraStuff.cullingMask = CullMask.Value;

            //Create the Debug Catigory for enableing experimental/ debug tools
            //This includes Flatscreen for testing and Quest support
            ConfigDebugCategory = MelonPreferences.CreateCategory("BanterCameraDebug");
            ConfigDebugCategory.SetFilePath("UserData/BanterCamera.cfg");
            EnableInDesktop = ConfigDebugCategory.CreateEntry<bool>("Enable in Desktop", false);
            AllowQuestSupport = ConfigDebugCategory.CreateEntry<bool>("Allow Quest Support", true);

            IsQuest = MelonUtils.CurrentPlatform == (MelonPlatformAttribute.CompatiblePlatforms)3; //This will be true if using LemonLoader / Android
            if (IsQuest && AllowQuestSupport.Value)
            {
                cameraStuff.OutPutEyes = StereoTargetEyeMask.Left; //Output to the left eye as this is what meta uses to record
            }
            if (IsQuest && !AllowQuestSupport.Value)
            {
                Enabled.Value = false;
            }
        }

        public override void OnApplicationQuit()
        {
            ConfigCategory.SaveToFile();
        }
        public override void OnUpdate()
        {
            if ((UnityEngine.XR.XRSettings.enabled || EnableInDesktop.Value) && Enabled.Value)
            {
                cameraStuff.SmoothMoveCamera(Time.deltaTime);
            }
        }
        public override void OnSceneWasInitialized(int buildIndex, string sceneName) 
        {
            if ((UnityEngine.XR.XRSettings.enabled || EnableInDesktop.Value) && Enabled.Value)
            {
                GameObject PlayerHead = Camera.main.gameObject; //Find the gameobject of the main camera
                cameraStuff.PlayerHead = PlayerHead.transform; //Get the transform of the target
                cameraStuff.VRMainCam = Camera.main; //send in the main camera for refrence for things like culling mask
                cameraStuff.CreateCamera(); //Make the new camera
            }
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
                try{ Object.Destroy(SpectatorCam); } catch { } //Try and Destroy the Camera if its allready in the scene.
            }
            CameraPosition = PlayerHead.position; CameraRot = PlayerHead.rotation; //Reset Positions and rotations.


            // Create a new GameObject
            SpectatorCam = new GameObject("SpectatorCam");

            // Add a Camera component to the GameObject
            Camera cameraComponent = SpectatorCam.AddComponent<Camera>();

            //Change the setting of the camera conponant
            cameraComponent.farClipPlane = VRMainCam.farClipPlane;
            cameraComponent.nearClipPlane = VRMainCam.nearClipPlane;
            cameraComponent.depth = 2;
            cameraComponent.stereoTargetEye = OutPutEyes;
            cameraComponent.fieldOfView = 90;
            cameraComponent.allowDynamicResolution = true;
            cameraComponent.ResetAspect();

            if (cullingMask == 0) //If culling mask is 0, inherit it from the main camera
            {
                cameraComponent.cullingMask = VRMainCam.cullingMask;
            }
            else if (cullingMask < 0) //If the culling mask is negative, subtract from it, this should really be an xor
            {
                cameraComponent.cullingMask = VRMainCam.cullingMask + cullingMask;
            }
            else //If culling mask is grater than 0, use the one given
            {
                cameraComponent.cullingMask = cullingMask;
            }
           
            
            

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
