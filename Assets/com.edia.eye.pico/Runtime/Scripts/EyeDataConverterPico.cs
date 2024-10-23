using UnityEngine;
using Unity.XR.PXR;
using Unity.XR.PICO.TOBSupport;

namespace Edia.Eye.Pico {

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

        private Vector3 combineEyeGazeVector;


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

            PXR_MotionTracking.GetPerEyePose(ref timestamp, ref leftEyePose, ref rightEyePose); // the timestamp does not seem to work

            bool successPos = PXR_EyeTracking.GetCombineEyeGazePoint(out eyeCenterPos);
            bool successRot = PXR_EyeTracking.GetCombineEyeGazeVector(out combineEyeGazeVector);

            eyeCenterRot = Quaternion.LookRotation(combineEyeGazeVector);

            bool success = successPos && successRot;

            var eyeData = new EyeDataPackage();
            eyeData.eye = "center";
            eyeData.isValid = success;
            eyeData.timestamp_et = (float)timestamp;
            eyeData.timestamp_lsl = UseLslTiming ? LslTimer.GetLslTime() : 0f;  // We don't get offset so we use LSL timestamp from here
            eyeData.direction_x_local = combineEyeGazeVector.x;
            eyeData.direction_y_local = combineEyeGazeVector.y;
            eyeData.direction_z_local = combineEyeGazeVector.z;
            eyeData.position_x_local = eyeCenterPos.x;
            eyeData.position_y_local = eyeCenterPos.y;
            eyeData.position_z_local = eyeCenterPos.z;
            eyeData.rotation_x_local = eyeCenterRot.eulerAngles.x;
            eyeData.rotation_y_local = eyeCenterRot.eulerAngles.y;
            eyeData.rotation_z_local = eyeCenterRot.eulerAngles.z;

            if (EyeDataHandler.Instance == null || !EyeDataHandler.Instance.enabled) {
                Debug.LogError("No active EyeDataHandler (EDIA Eye) found in the scene.");
            }

            lock (EyeDataHandler.Instance.Lock) {
                EyeDataHandler.Instance.AddEyeDataPackage(eyeData);
            }
        }
    }
}