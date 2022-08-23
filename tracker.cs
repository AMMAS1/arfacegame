using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using OpenCVForUnity;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.ObjdetectModule;

public class tracker : MonoBehaviour
{
    public GameObject target;
    public GameObject arCamera;
    public GameObject textureplane;
    private ARFaceManager faceManager;

    // Start is called before the first frame update
    void Start()
    {
        faceManager = GetComponent<ARFaceManager>();
        arCamera.GetComponent<ARCameraManager>().frameReceived += OnCameraFrameReceived;
    }

    // Update is called once per frame
    void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        if  (faceManager.enabled && faceManager.trackables.count > 0)
        {
            ARFace face = new ARFace();
            foreach (ARFace f in faceManager.trackables)
            {
                face = f;
            }
            //move the target to the forward
            target.transform.position = face.transform.position; //target.transform.position * 1.001f;
            int[] indices = {57, 186, 92, 165, 167, 164, 393, 391, 322, 410, 287, 273, 335, 406, 313, 18, 83, 182, 106, 43};
            // native array of vertices 

            
            ARCameraBackground arCameraBackground = arCamera.GetComponent<ARCameraBackground>();
            // create new render texture
            RenderTexture renderTexture = new RenderTexture(Screen.width, Screen.height, 0);
            // create a new texture 2D
            Texture2D texture2D = null;
            // create render texture
            Graphics.Blit(null, renderTexture, arCameraBackground.material);
            // Copy the RenderTexture from GPU to CPU
            var activeRenderTexture = RenderTexture.active;
            RenderTexture.active = renderTexture;
            if (texture2D == null)
                texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, true);
            texture2D.ReadPixels(new UnityEngine.Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture2D.Apply();
            RenderTexture.active = activeRenderTexture;
            Destroy(renderTexture);

            // convert texture to mat
            Mat mat = new Mat(texture2D.height, texture2D.width, CvType.CV_8UC3);
            Utils.texture2DToMat(texture2D, mat);
            // destroy texture2D
            Destroy(texture2D);
            // resize the mat to /3 to speed up the processing
            //Imgproc.resize(mat, mat, new Size(mat.cols() / 3, mat.rows() / 3));
            // get screen position
            Vector3 screenPosition = arCamera.GetComponent<Camera>().WorldToScreenPoint(face.transform.position + face.vertices[0]);
            // draw a point at fact transform.position
            Imgproc.circle(mat, new Point(screenPosition.x, mat.rows() - screenPosition.y), 5, new Scalar(255, 0, 0), -1);
            // write text saying face transford position at the top left corner with size 5 in the center of the mat
            Imgproc.putText(mat, "face position: " + face.transform.position.ToString(), new Point(mat.cols() / 2 - 400,  mat.rows() / 2-100), Imgproc.FONT_HERSHEY_SIMPLEX, 1, new Scalar(255, 255, 255), 2);
            Imgproc.putText(mat, "texture size: " + mat.cols() + "x" + mat.rows(), new Point(mat.cols() / 2 - 400,  mat.rows() / 2 - 50), Imgproc.FONT_HERSHEY_SIMPLEX, 1, new Scalar(255, 255, 255), 2);
            Imgproc.putText(mat, "target position: " + target.transform.position.ToString(), new Point(mat.cols() / 2 - 400,  mat.rows() / 2), Imgproc.FONT_HERSHEY_SIMPLEX, 1, new Scalar(255, 255, 255), 2);
            Imgproc.putText(mat, "camera position: " + arCamera.transform.position.ToString(), new Point(mat.cols() / 2 - 400,  mat.rows() / 2 + 50), Imgproc.FONT_HERSHEY_SIMPLEX, 1, new Scalar(255, 255, 255), 2);
            Imgproc.putText(mat, "transform position: " + transform.position.ToString(), new Point(mat.cols() / 2 - 400,  mat.rows() / 2 + 100), Imgproc.FONT_HERSHEY_SIMPLEX, 1, new Scalar(255, 255, 255), 2);
            Imgproc.putText(mat, "screen position: " + screenPosition.ToString(), new Point(mat.cols() / 2 - 400,  mat.rows() / 2 + 150), Imgproc.FONT_HERSHEY_SIMPLEX, 1, new Scalar(255, 255, 255), 2);
            // rotate the mat to put on the texture plane
            Core.flip(mat, mat, 1);
            // create a texture2D with mat size
            Texture2D texture2DResized = new Texture2D(mat.cols(), mat.rows(), TextureFormat.RGBA32, false);
            // convert mat to texture2D
            Utils.matToTexture2D(mat, texture2DResized);
            // update target texture
            Destroy(textureplane.GetComponent<MeshRenderer>().material.mainTexture);
            textureplane.GetComponent<Renderer>().material.mainTexture = texture2DResized;
        }
        else{
            // return target to original position
            target.transform.position = new Vector3(0, 0, 28);
        }
    }
}
