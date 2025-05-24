using UnityEngine;
using Unity.XR.PXR;
using Unity.XR.PICO.TOBSupport;
using System.Collections.Generic;
using System;
using Unity.XR.PXR.Input;

namespace Edia.Eye.Pico {

    [EdiaHeader("EDIA EYE", "Pico converter","Manages Pico SDK eyedata conversion to EDIA")]
    public class EyeDataConverterPico : MonoBehaviour {

        public ILslTimeAccessible LslTimer { get; private set; }
        public bool UseLslTiming = false;

        TrackingStateCode trackingState;
        EyeTrackingMode eyeTrackingMode;
        EyeTrackingStartInfo startInfo;
        EyeTrackingDataGetInfo info;
        EyeTrackingData eyeTrackingData;

        long timestamp;

        Vector3 eyeCenterPos;
        Quaternion eyeCenterRot;
        EyePupilInfo eyePupilData;
        Posef rightEyePose;
        Posef leftEyePose;
        bool success, successPos, successRot = false;
        float opennessLeft, opennessRight;
        Vector3 posWorld;
        Quaternion rotWorld;

        private Vector3 combineEyeGazeVector;

        private struct EyePose {
            public string Eye;
            public bool IsValid;
            public float Confidence;
            public Vector3 Position;
            public Quaternion Rotation;
            public float Openness;
            public float Diameter;
        }

        private void Awake() {
            SetupPicoEyeTracking();
        }

        private void Start() {

            if (UseLslTiming) {
                // check if the LslTiming component is available
                if (GetComponent<ILslTimeAccessible>() == null) {
                    Debug.LogError("To use LSL timing, the EyeDataConverterSRanipal requires a component on the same GameObject " +
                                   "which implements the ILslTimeAccessible interface (e.g., Edia.Lsl.LslTiming or " +
                                   "Edia.Lsl.EyeOutlet).");
                } else {
                    LslTimer = GetComponent<ILslTimeAccessible>();
                }
            }
        }


        void Update() {
            HandleGazeData();
        }


        void SetupPicoEyeTracking() {

            //PXR_Enterprise.InitEnterpriseService();
            //PXR_Enterprise.BindEnterpriseService((bound) => {
            //    Debug.Log("Bind success.");
            //});

            //Disable app exit confirmation popup
            PXR_Enterprise.SwitchSystemFunction(SystemFunctionSwitchEnum.SFS_BASIC_SETTING_SHOW_APP_QUIT_CONFIRM_DIALOG, SwitchEnum.S_OFF);


            // Request the eye tracking service for the current app
            trackingState = (TrackingStateCode)PXR_MotionTracking.WantEyeTrackingService();

            eyeTrackingMode = EyeTrackingMode.PXR_ETM_BOTH;

            startInfo = new EyeTrackingStartInfo();

            startInfo.needCalibration = 1;
            startInfo.mode = eyeTrackingMode;
            trackingState = (TrackingStateCode)PXR_MotionTracking.StartEyeTracking(ref startInfo);

            // Get eye tracking data
            info = new EyeTrackingDataGetInfo {
                displayTime = 0,
                flags = EyeTrackingDataGetFlags.PXR_EYE_DEFAULT
                        | EyeTrackingDataGetFlags.PXR_EYE_POSITION
                        | EyeTrackingDataGetFlags.PXR_EYE_ORIENTATION
            };

            eyeTrackingData = new EyeTrackingData();
            trackingState = (TrackingStateCode)PXR_MotionTracking.GetEyeTrackingData(ref info, ref eyeTrackingData);
            timestamp = new long();
            leftEyePose = new Posef();
            rightEyePose = new Posef();
        }


        void HandleGazeData() {

            successPos = PXR_EyeTracking.GetCombineEyeGazePoint(out eyeCenterPos);
            successRot = PXR_EyeTracking.GetCombineEyeGazeVector(out combineEyeGazeVector);

            int returnPose = PXR_MotionTracking.GetPerEyePose(ref timestamp, ref leftEyePose, ref rightEyePose); //Single eye poses are in weird format so we do not use them
            int returnOpenness = PXR_MotionTracking.GetEyeOpenness(ref opennessLeft, ref opennessRight);
            int returnPupil = PXR_MotionTracking.GetEyePupilInfo(ref eyePupilData);

            success = successPos || successRot || (returnPose == 0) || (returnOpenness == 0) || (returnPupil == 0);

            //Send no package if nothing returned
            if (!success) {
                return;
            }
            
            List<EyePose> eyePoses = new List<EyePose>();

            foreach (Constants.EyeId eye in Enum.GetValues(typeof(Constants.EyeId))) {
                EyePose eyePose = new EyePose();
                eyePose.Eye = eye.ToString().ToLower();
                switch (eye) {
                    case Constants.EyeId.LEFT:
                        eyePose.IsValid = (returnOpenness == 0) || (returnPupil == 0);
                        eyePose.Openness = (returnOpenness == 0) ? opennessLeft : -1;
                        eyePose.Diameter = (returnPupil == 0) ? eyePupilData.leftEyePupilDiameter : -1;
                        break;
                    case Constants.EyeId.RIGHT:
                        eyePose.IsValid = (returnOpenness == 0) || (returnPupil == 0);
                        eyePose.Openness = (returnOpenness == 0) ? opennessRight : -1;
                        eyePose.Diameter = (returnPupil == 0) ? eyePupilData.rightEyePupilDiameter : -1;
                        break;
                    case Constants.EyeId.CENTER:
                        eyePose.IsValid = success;
                        eyePose.Position = successPos ? eyeCenterPos : new Vector3();
                        eyePose.Rotation = successRot ? Quaternion.LookRotation(combineEyeGazeVector) : new Quaternion();
                        eyePose.Openness = (returnOpenness == 0) ? (opennessLeft + opennessRight) / 2 : -1;
                        eyePose.Diameter = (returnPupil == 0) ? (eyePupilData.leftEyePupilDiameter + eyePupilData.rightEyePupilDiameter) / 2 : -1;
                        break;
                }
                eyePoses.Add(eyePose);
            }

            foreach (var ep in eyePoses) {
                var ed = new EyeDataPackage();
                ed.eye = ep.Eye;
                ed.isValid = ep.IsValid;
                ed.timestamp_et = (double)(timestamp / 1_000_000) % (long)1e8; // Convert from nanoseconds to milliseconds (int division -> flooring) and keep last 8 digits;
                ed.timestamp_lsl = UseLslTiming ? LslTimer.GetLslTime() : 0f;  // We don't get a reliable offset between LSL and PICO clocks, so we use LSL timestamp from here
                if (ep.IsValid) {
                    ed.position_x_local = ep.Position.x;
                    ed.position_y_local = ep.Position.y;
                    ed.position_z_local = ep.Position.z;

                    ed.rotation_x_local = ep.Rotation.eulerAngles.x;
                    ed.rotation_y_local = ep.Rotation.eulerAngles.y;
                    ed.rotation_z_local = ep.Rotation.eulerAngles.z;
                    
                    Vector3 direction = ep.Rotation * Vector3.forward;
                    ed.direction_x_local = direction.x;
                    ed.direction_y_local = direction.y;
                    ed.direction_z_local = direction.z;

                    ed.openness = ep.Openness;
                }

                if (EyeDataHandler.Instance == null || !EyeDataHandler.Instance.enabled) {
                    Debug.LogError("No active EyeDataHandler (EDIA Eye) found in the scene.");
                }

                lock (EyeDataHandler.Instance.Lock) {
                    EyeDataHandler.Instance.AddEyeDataPackage(ed);
                }
            }
        }
    }
}