#if !(PLATFORM_LUMIN && !UNITY_EDITOR)

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.ObjdetectModule;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using DlibFaceLandmarkDetector;

namespace ARFoundationWithOpenCVForUnityExample
{
    public class ARFoundationCameraToMatExample : MonoBehaviour
    {
        public ARFaceManager faceManager;
        [SerializeField, TooltipAttribute("The ARCameraManager which will produce frame events.")]
        public ARCameraManager cameraManager = default;

        [SerializeField, TooltipAttribute("The ARCamera.")]
        public Camera arCamera;

        [SerializeField]


        Mat rgbaMat;

        Mat rotatedFrameMat;

        Texture2D texture;

        bool hasInitDone = false;

        bool isPlaying = true;

        ScreenOrientation screenOrientation;

        int displayRotationAngle = 0;
        bool displayFlipVertical = false;
        bool displayFlipHorizontal = false;

        string dlibShapePredictorFileName = "sp_human_face_68_for_mobile.dat";
        FaceLandmarkDetector faceLandmarkDetector;

        void Start()
        {
           faceLandmarkDetector = new FaceLandmarkDetector(Utils.getFilePath(dlibShapePredictorFileName));
        }

        void OnEnable()
        {
            if (cameraManager != null)
            {
                cameraManager.frameReceived += OnCameraFrameReceived;
            }
        }

        void OnDisable()
        {
            if (cameraManager != null)
            {
                cameraManager.frameReceived -= OnCameraFrameReceived;
            }
        }

        protected void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
        {
            if ((cameraManager == null) || (cameraManager.subsystem == null) || !cameraManager.subsystem.running)
                return;
            // Attempt to get the latest camera image. If this method succeeds,
            // it acquires a native resource that must be disposed (see below).
            if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {
                return;
            }

            int width = image.width;
            int height = image.height;
            Matrix4x4 m_DisplayRotationMatrix = Matrix4x4.identity;
            if (!hasInitDone || rgbaMat == null || rgbaMat.cols() != width || rgbaMat.rows() != height || screenOrientation != Screen.orientation)
            {
                Dispose();

                screenOrientation = Screen.orientation;

                XRCameraConfiguration config = (XRCameraConfiguration)cameraManager.currentConfiguration;
                int framerate = config.framerate.HasValue ? config.framerate.Value : -1;

                // Remove scaling and offset factors from the camera display matrix while maintaining orientation.
                // Decompose that matrix to extract the rotation and flipping factors.
                
                if (eventArgs.displayMatrix.HasValue)
                {
                    // Copy the display rotation matrix from the camera.
                    Matrix4x4 cameraMatrix = eventArgs.displayMatrix ?? Matrix4x4.identity;

                    Vector2 affineBasisX = new Vector2(1.0f, 0.0f);
                    Vector2 affineBasisY = new Vector2(0.0f, 1.0f);
                    Vector2 affineTranslation = new Vector2(0.0f, 0.0f);

#if UNITY_IOS
                    affineBasisX = new Vector2(cameraMatrix[0, 0], cameraMatrix[1, 0]);
                    affineBasisY = new Vector2(cameraMatrix[0, 1], cameraMatrix[1, 1]);
                    affineTranslation = new Vector2(cameraMatrix[2, 0], cameraMatrix[2, 1]);
#endif // UNITY_IOS
#if UNITY_ANDROID
                    affineBasisX = new Vector2(cameraMatrix[0, 0], cameraMatrix[0, 1]);
                    affineBasisY = new Vector2(cameraMatrix[1, 0], cameraMatrix[1, 1]);
                    affineTranslation = new Vector2(cameraMatrix[0, 2], cameraMatrix[1, 2]);
#endif // UNITY_ANDROID

                    affineBasisX = affineBasisX.normalized;
                    affineBasisY = affineBasisY.normalized;

                    m_DisplayRotationMatrix = Matrix4x4.identity;
                    m_DisplayRotationMatrix[0, 0] = affineBasisX.x;
                    m_DisplayRotationMatrix[0, 1] = affineBasisY.x;
                    m_DisplayRotationMatrix[1, 0] = affineBasisX.y;
                    m_DisplayRotationMatrix[1, 1] = affineBasisY.y;

#if UNITY_IOS
                    Matrix4x4 FlipYMatrix = Matrix4x4.Scale(new Vector3(1, -1, 1));
                    m_DisplayRotationMatrix = FlipYMatrix.inverse * m_DisplayRotationMatrix;
#endif // UNITY_IOS

                    displayRotationAngle = (int)ARUtils.ExtractRotationFromMatrix(ref m_DisplayRotationMatrix).eulerAngles.z;
                    Vector3 localScale = ARUtils.ExtractScaleFromMatrix(ref m_DisplayRotationMatrix);
                    displayFlipVertical = Mathf.Sign(localScale.y) == -1;
                    displayFlipHorizontal = Mathf.Sign(localScale.x) == -1;

                }

                rgbaMat = new Mat(height, width, CvType.CV_8UC4);

                if (displayRotationAngle == 90 || displayRotationAngle == 270)
                {
                    width = image.height;
                    height = image.width;

                    rotatedFrameMat = new Mat(height, width, CvType.CV_8UC4);
                }

                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);


                gameObject.GetComponent<Renderer>().material.mainTexture = texture;

                //gameObject.transform.localScale = new Vector3(width, height, 1);

                hasInitDone = true;


             

            }

            if (hasInitDone && isPlaying)
            {
                XRCpuImage.ConversionParams conversionParams = new XRCpuImage.ConversionParams(image, TextureFormat.RGBA32, XRCpuImage.Transformation.None);
                image.Convert(conversionParams, (IntPtr)rgbaMat.dataAddr(), (int)rgbaMat.total() * (int)rgbaMat.elemSize());

                if (displayFlipVertical && displayFlipHorizontal)
                {
                    Core.flip(rgbaMat, rgbaMat, -1);
                }
                else if (displayFlipVertical)
                {
                    Core.flip(rgbaMat, rgbaMat, 0);
                }
                else if (displayFlipHorizontal)
                {
                    Core.flip(rgbaMat, rgbaMat, 1);
                }

                if (rotatedFrameMat != null)
                {
                    if (displayRotationAngle == 90)
                    {
                        Core.rotate(rgbaMat, rotatedFrameMat, Core.ROTATE_90_CLOCKWISE);
                    }
                    else if (displayRotationAngle == 270)
                    {
                        Core.rotate(rgbaMat, rotatedFrameMat, Core.ROTATE_90_COUNTERCLOCKWISE);
                    }
                     

                    Doprocess(rotatedFrameMat);
                    Utils.fastMatToTexture2D(rotatedFrameMat, texture);
                }
                else
                {
                    if (displayRotationAngle == 180)
                    {
                        Core.rotate(rgbaMat, rgbaMat, Core.ROTATE_180);
                    }
                    Doprocess(rgbaMat);
                    Utils.fastMatToTexture2D(rgbaMat, texture);
                }


            }

            image.Dispose();
        }
        private void Doprocess(Mat mat)
        {
            if (faceManager.enabled && faceManager.trackables.count > 0)
            {
                
                //mat to texture
                Texture2D texture2D = new Texture2D(mat.cols(), mat.rows(), TextureFormat.RGBA32, false);
                Utils.fastMatToTexture2D(mat, texture);

                
                faceLandmarkDetector.SetImage(texture2D);

                //detect face rects
                List<UnityEngine.Rect> detectResult = faceLandmarkDetector.Detect();


                foreach (var rect in detectResult)
                {
                    Debug.Log("face : " + rect);

                    //detect landmark points
                    List<Vector2> points = faceLandmarkDetector.DetectLandmark(rect);

                    Debug.Log("face points count : " + points.Count);
                    foreach (var point in points)
                    {
                        Debug.Log("face point : x " + point.x + " y " + point.y);
                    }

                    //draw landmark points
                    faceLandmarkDetector.DrawDetectLandmarkResult(texture2D, 0, 255, 0, 255);

                }

                //draw face rect
                faceLandmarkDetector.DrawDetectResult(texture2D, 255, 0, 0, 255, 2);

                faceLandmarkDetector.Dispose();

                //texture to mat
                Utils.fastTexture2DToMat(texture2D, mat);
                // destroy texture
                Destroy(texture2D);

            }
        }
        /// <summary>
        /// Releases all resource.
        /// </summary>
        private void Dispose()
        {
            hasInitDone = false;

            if (rgbaMat != null)
            {
                rgbaMat.Dispose();
                rgbaMat = null;
            }
            if (rotatedFrameMat != null)
            {
                rotatedFrameMat.Dispose();
                rotatedFrameMat = null;
            }
            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            Dispose();
        }
    }
}

#endif