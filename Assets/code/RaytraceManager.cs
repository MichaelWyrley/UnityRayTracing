using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;

[ExecuteAlways, ImageEffectAllowedInSceneView]
public class RaytraceManager : MonoBehaviour
{

    [Header("Ray Tracing Settings")]
	[SerializeField, Range(0, 32)] int maxBounceCount = 4;
	[SerializeField, Range(0, 64)] int numRaysPerPixel = 2;
	[SerializeField, Min(0)] float defocusStrength = 0;
	[SerializeField, Min(0)] float divergeStrength = 0.3f;
	[SerializeField, Min(0)] float focusDistance = 1.0f;
    [SerializeField] Transform sun; 

    [SerializeField] bool debug;
    [SerializeField] bool environment_enabled;
    [SerializeField] Color environment_groundColour;
    [SerializeField] Color environment_skyColourHorizon;
    [SerializeField] Color environment_skyColourZenith;
    [SerializeField] float environment_sunFocus;
    [SerializeField] float environment_sunIntensity;
    [SerializeField] float ambient;
    

    [Header("View Settings")]
	[SerializeField] bool useShaderInSceneView;
    [SerializeField] bool isRendering;

	[Header("References")]
	[SerializeField] ComputeShader shader;
	[SerializeField] Shader accumulateShader;

    [Header("Info")]
	[SerializeField] int numRenderedFrames;
	[SerializeField] int numShapes;
	[SerializeField] int totalnumTriangles;
    bool render;

	// Buffers
    RenderTexture outputTexture;
    Material accumulateMaterial;
    RenderTexture accumulateTexture;
    ComputeBuffer shapeBuffer = null;
    ComputeBuffer triangleBuffer = null;

	List<Triangles> allTriangles;
	List<ShapeData> allMeshInfo;
    ShapeData[] shapeData;
    // List<ComputeBuffer> buffersToDispose;
    int kernel;


    
    Camera cam;

    bool reload = true;


    void Start()
	{
        cam = Camera.current;
		numRenderedFrames = 0;
        InitParameters();
	}

    void Update() 
    {
        if (Input.GetKeyDown(KeyCode.R)){

            InitParameters();
        }
    }


    void OnRenderImage(RenderTexture src, RenderTexture destination) {
        cam = Camera.current;
        bool isSceneCam = cam.name == "SceneCamera";
        kernel = shader.FindKernel("CSMain");
        shader.SetInt("_Frames", numRenderedFrames);
        

		if (isSceneCam)
		{
			if (useShaderInSceneView)
			{
                InitRenderTexture (ref outputTexture);
                UpdateCameraParams(cam);
                // InitParameters();

                shader.SetTexture(kernel, "Result", outputTexture);

                int threadGroupsX = Mathf.CeilToInt (cam.pixelWidth / 8.0f);
                int threadGroupsY = Mathf.CeilToInt (cam.pixelHeight / 8.0f);
                shader.Dispatch (kernel, threadGroupsX, threadGroupsY, 1);

                Graphics.Blit (outputTexture, destination);
                // DisposeBuffer();
                numRenderedFrames += Application.isPlaying ? 1 : 0;

			}
			else
			{
				Graphics.Blit(src, destination); // Draw the unaltered camera render to the screen
			}
		}
		else
		{    
            InitRenderTexture (ref outputTexture);
            UpdateCameraParams(cam);

            if (render){

                InitMaterial(accumulateShader, ref accumulateMaterial);
                InitRenderTexture(ref accumulateTexture);
                

                // Create copy of prev frame
                RenderTexture prevFrameCopy = RenderTexture.GetTemporary(src.width, src.height, 0);
                Graphics.Blit(outputTexture, prevFrameCopy);

                // Run the ray tracing shader and draw the result to a temp texture

                shader.SetTexture(kernel, "Result", accumulateTexture);

                int threadGroupsX = Mathf.CeilToInt (cam.pixelWidth / 8.0f);
                int threadGroupsY = Mathf.CeilToInt (cam.pixelHeight / 8.0f);
                shader.Dispatch (0, threadGroupsX, threadGroupsY, 1);
        
                // Accumulate
                accumulateMaterial.SetInt("_Frames", numRenderedFrames);
                accumulateMaterial.SetTexture("_PrevFrame", prevFrameCopy);
                Graphics.Blit(accumulateTexture, outputTexture, accumulateMaterial);

                // Draw result to screen
                Graphics.Blit(outputTexture, destination);
                // Graphics.Blit(outputTexture, accumulateTexture);

                // Release temps
                RenderTexture.ReleaseTemporary(prevFrameCopy);

                numRenderedFrames += Application.isPlaying ? 1 : 0;
            } else {
                shader.SetTexture(kernel, "Result", outputTexture);

                int threadGroupsX = Mathf.CeilToInt (cam.pixelWidth / 8.0f);
                int threadGroupsY = Mathf.CeilToInt (cam.pixelHeight / 8.0f);
                shader.Dispatch (0, threadGroupsX, threadGroupsY, 1);
                Graphics.Blit(outputTexture, destination);
            }

            // DisposeBuffer();
		}
		
    }

    public void Render(){
        render = !render;
        numRenderedFrames = 0;

    }

    private void InitParameters() {
        // buffersToDispose = new List<ComputeBuffer> ();
        SendShapes();

        shader.SetInt("_maxBounceCount", maxBounceCount);
        shader.SetInt("_numRaysPerPixel", numRaysPerPixel);
        shader.SetFloat("_focusDistance", focusDistance);
        shader.SetFloat("_defocusStrength", defocusStrength);
        shader.SetFloat("_divergeStrength", divergeStrength);
        

        Vector3 env_c = new Vector3(environment_groundColour.r, environment_groundColour.b,environment_groundColour.g);
        Vector3 sk_c_h = new Vector3(environment_skyColourHorizon.r, environment_skyColourHorizon.b,environment_skyColourHorizon.g);
        Vector3 sk_c_v = new Vector3(environment_skyColourZenith.r, environment_skyColourZenith.b,environment_skyColourZenith.g);

        shader.SetInt("_Debug",  debug ? 1 : 0);
        shader.SetInt("_environmentEnabled", environment_enabled ? 1 : 0);
		shader.SetVector("_groundColour", env_c);
		shader.SetVector("_skyColourHorizon", sk_c_h);
		shader.SetVector("_skyColourZenith", sk_c_v);
		shader.SetFloat("_sunFocus", environment_sunFocus);
		shader.SetFloat("_sunIntensity", environment_sunIntensity);
        shader.SetVector("_sunPos", sun.forward);
        shader.SetFloat("_ambient", ambient);
    }

    // private void DisposeBuffer (){
    //     foreach (var buffer in buffersToDispose) {
    //         buffer.Dispose ();
    //     }
    // }

    void InitRenderTexture (ref RenderTexture tex) {
        if (tex == null || tex.width != cam.pixelWidth || tex.height != cam.pixelHeight) {
            if (tex != null) {
                tex.Release ();
            }
            tex = new RenderTexture (cam.pixelWidth, cam.pixelHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            tex.enableRandomWrite = true;
            tex.Create ();
        }
    }


    void UpdateCameraParams(Camera camera)
	{
		float planeHeight = focusDistance * Tan(camera.fieldOfView * 0.5f * Deg2Rad) * 2;
		float planeWidth = planeHeight * camera.aspect;
		// Send data to shader
		shader.SetVector("_CamPlane", new Vector3(planeWidth, planeHeight, focusDistance));
		shader.SetMatrix("_CamToWorldMatrix", camera.transform.localToWorldMatrix);
	}

    private void SendShapes() {
        Shape[] allShapes = FindObjectsByType<Shape>(FindObjectsSortMode.None);


        shapeData = new ShapeData[allShapes.Length];
        allTriangles ??= new List<Triangles>();
        allTriangles.Clear();

        int triangle_begining = 0;
        int triangle_end = 0;
        for (int i = 0; i < allShapes.Length; i++) {
            var s = allShapes[i];
            Mesh m = s.gameObject.GetComponent<MeshFilter>().sharedMesh;

            triangle_begining = triangle_end;
            triangle_end += m.triangles.Length /3;
            var bounds = s.GetComponent<Renderer>().bounds;

            var T = s.gameObject.transform;
            var M = T.localToWorldMatrix;


            shapeData[i] = new ShapeData () {
                colour = s.colour,
                emission = s.emission,
                specularColour = s.specularColour,
                transmitance = s.transmitance,
                smoothness = s.smoothness,
                triangle_begin = triangle_begining,
                triangle_count = m.triangles.Length /3,
                bounds_min = bounds.min,
                bounds_max = bounds.max,
            };

            for (int j = 0; j < m.triangles.Length; j+=3){
                var a = M.MultiplyPoint3x4(m.vertices[m.triangles[j]]);
                var b = M.MultiplyPoint3x4(m.vertices[m.triangles[j+1]]);
                var c = M.MultiplyPoint3x4(m.vertices[m.triangles[j+2]]);

                var na = T.TransformDirection(m.normals[m.triangles[j]]).normalized;
                var nb = T.TransformDirection(m.normals[m.triangles[j+1]]).normalized;
                var nc = T.TransformDirection(m.normals[m.triangles[j+2]]).normalized;

                Triangles tri = new Triangles () {
                    posA = a,
                    posB = b,
                    posC = c,
                    normalA = na,
                    normalB = nb,
                    normalC = nc
                };

                allTriangles.Add(tri);
            }


        }
        

        int numTriangles = allTriangles.Count;
        if (numTriangles > 0) {

            if (shapeBuffer != null) shapeBuffer.Dispose();
            if (triangleBuffer != null) triangleBuffer.Dispose();

            shapeBuffer = new ComputeBuffer (shapeData.Length, ShapeData.GetSize ());
            triangleBuffer = new ComputeBuffer(numTriangles, Triangles.GetSize());
            shapeBuffer.SetData (shapeData);
            triangleBuffer.SetData(allTriangles);

            shader.SetBuffer (kernel, "shapes", shapeBuffer);
            shader.SetBuffer (kernel, "triangles", triangleBuffer);
            shader.SetInt ("_noShapes", shapeData.Length);
            shader.SetInt ("_noTriangles", numTriangles);

            // buffersToDispose.Add (shapeBuffer);
            // buffersToDispose.Add (triangleBuffer);

            numShapes = shapeData.Length;
            totalnumTriangles = numTriangles;

        }
        
    }

    public static void InitMaterial(Shader shader, ref Material mat)
    {
        if (mat == null || (mat.shader != shader && shader != null))
        {
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Texture");
            }

            mat = new Material(shader);
        }
    }
    



	

}
