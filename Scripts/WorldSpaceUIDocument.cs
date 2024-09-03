using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument), typeof(MeshRenderer), typeof(MeshFilter))]
[ExecuteAlways]
public class WorldSpaceUIDocument : MonoBehaviour, ISerializationCallbackReceiver
{
	[SerializeField] private Shader shader;

	private RenderTexture renderTexture;
	private static readonly int MainTex = Shader.PropertyToID("_MainTex");

	[SerializeField] private UIDocument document;
	[SerializeField] internal PanelSettings panelSettingsAsset;
	private RaycastHit hit;

	private float targetZ;
	private Vector3 vel;
	private Camera cam;
	
	[SerializeField] private int pixelsPerUnit = 100;

	[SerializeField] private Vector2 pivot;
	private MeshFilter meshFilter;
	private MeshRenderer meshRenderer;
	private Material material;

	private bool isDirty = false;
	private Mesh mesh;
	private BoxCollider col;

	private PanelSettings panelSettings;

	public bool isTransparent = true;
	public RenderFace renderFace = RenderFace.Front;
	private static readonly int Cull = Shader.PropertyToID("_Cull");
	private static readonly int Surface = Shader.PropertyToID("_Surface");
	private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
	private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
	private static readonly int ZWrite = Shader.PropertyToID("_ZWrite");

	public enum RenderFace
	{
		Both,
		Back,
		Front
	}

	private void Reset()
	{
		document = GetComponent<UIDocument>();
		if (!shader)
		{
			shader = Shader.Find("Universal Render Pipeline/Unlit");
		}
	}

	private void OnValidate()
	{
		isDirty = true;
		if (!shader)
		{
			shader = Shader.Find("Universal Render Pipeline/Unlit");
		}
	}

	void Awake()
	{
		document = GetComponent<UIDocument>();
	}

	private void Start()
	{
		Init();

		
	    
		col = gameObject.GetComponent<BoxCollider>();
		panelSettings.SetScreenToPanelSpaceFunction(screenPosition =>
		{
			screenPosition.y = Screen.height - screenPosition.y ;
		    
			var invalidPosition = new Vector2(float.NaN, float.NaN);

			var mainCamera = GetCamera();
			var cameraRay = mainCamera.ScreenPointToRay(screenPosition);
	        
			if (!col.Raycast(cameraRay, out hit, 100f))
			{
				Debug.DrawRay(cameraRay.origin, cameraRay.direction*100, Color.magenta);
				return invalidPosition;
			}

			Debug.DrawLine(cameraRay.origin, hit.point, Color.green);
			Vector2 pixelUV = hit.textureCoord;

			pixelUV.y = 1 - pixelUV.y;
			pixelUV.x *= this.document.panelSettings.targetTexture.width;
			pixelUV.y *= this.document.panelSettings.targetTexture.height;

			return pixelUV;
		});
	}

	private void Init()
	{
		Debug.Log("Init");
		if (!panelSettingsAsset)
		{
			Debug.LogWarning("PanelSettingsAsset is Null. InitializationCancelled");
			return;
		}
			
		if ((!panelSettings || panelSettingsAsset.referenceResolution != panelSettings.referenceResolution))
		{
				
			panelSettings = Instantiate(panelSettingsAsset);
			panelSettings.name = $"{panelSettings.name} {gameObject.GetInstanceID()}";
			document.panelSettings = panelSettings;
			panelSettings.targetTexture = renderTexture;
		}
			
		if (!renderTexture)
		{
			renderTexture = new RenderTexture(
				width:panelSettings.referenceResolution.x,
				height:panelSettings.referenceResolution.y, 
				0);
				
			panelSettings.targetTexture = renderTexture;
		}
			
		CreateMesh((Vector2)panelSettings.referenceResolution / pixelsPerUnit, pivot);
	}

	private void CreateMesh(Vector2 size, Vector3 offset)
	{
		if (!meshRenderer)
		{
			meshRenderer = gameObject.GetComponent<MeshRenderer>();
		}

		if (!material)
		{
			material = meshRenderer.sharedMaterial = new Material(shader);
		}

		material.SetFloat(Cull, (float)renderFace);
		material.mainTexture = renderTexture;
			
		SetMaterialTransparent(isTransparent);
			
		if (!meshFilter)
		{
			meshFilter = gameObject.GetComponent<MeshFilter>();
		}
		
		offset *= -size;

		if (!mesh)
		{
			mesh = new Mesh();
		}
		
		Vector3[] vertices = new Vector3[4]
		{
			offset + new Vector3(0, 0, 0),
			offset + new Vector3(size.x, 0, 0),
			offset + new Vector3(0, size.y, 0),
			offset + new Vector3(size.x, size.y, 0)
		};
		mesh.vertices = vertices;

		int[] tris = new int[6]
		{
			// lower left triangle
			0, 2, 1,
			// upper right triangle
			2, 3, 1
		};
		mesh.triangles = tris;

		Vector3[] normals = new Vector3[4]
		{
			-Vector3.forward,
			-Vector3.forward,
			-Vector3.forward,
			-Vector3.forward
		};
		mesh.normals = normals;

		Vector2[] uv = new Vector2[4]
		{
			new Vector2(0, 0),
			new Vector2(1, 0),
			new Vector2(0, 1),
			new Vector2(1, 1)
		};
		mesh.uv = uv;
		meshFilter.mesh = mesh;

		if (!col && !gameObject.TryGetComponent(out col))
		{
			col = meshRenderer.gameObject.AddComponent<BoxCollider>();
		}
		col.center = (Vector3)size / 2f + offset;
		col.size = size;
	}

	private Camera GetCamera()
	{
		if (cam == null)
		{
			cam = Camera.main;
		}
		return cam;
	}

	public Vector2 GetScreenPosition()
	{
		var screenPoint = Camera.main.WorldToScreenPoint(hit.point);
		screenPoint.y = Screen.currentResolution.height - screenPoint.y;
		return screenPoint;
	}

	private void OnDestroy()
	{
		if(Application.isPlaying)
		{
			Destroy(material);
			Destroy(renderTexture);
			Destroy(panelSettings);
		}
		else
		{
#if UNITY_EDITOR
			DestroyImmediate(material);
			DestroyImmediate(renderTexture);
			DestroyImmediate(panelSettings);
#endif
		}
	}

	public void OnBeforeSerialize()
	{
		pixelsPerUnit = Mathf.Max(1, pixelsPerUnit);
	}

	public void OnAfterDeserialize()
	{
		isDirty = true;
	}

		
	public void Update()
	{
		if (panelSettingsAsset && document.panelSettings)
		{
			isDirty |= panelSettingsAsset.referenceResolution != document.panelSettings.referenceResolution;
		}
			
		if (isDirty)
		{
			Init();
			isDirty = false;
		}
	}

	public Mesh GetMesh()
	{
		if (!mesh)
		{
			Init();
		}
		return mesh;
	}

	public void ForceRefresh()
	{
		if (material)
		{
			if (Application.isPlaying)
			{
				Destroy(material);
			}
			else
			{
				DestroyImmediate(material);
			}
		}
		panelSettings = null;
		isDirty = true;
	}
		
	private void SetMaterialTransparent(bool value)
	{
		material.SetFloat(Surface, value ? 1:0);
		material.SetShaderPassEnabled("SHADOWCASTER", !value);
		material.renderQueue = value ? 3000 : 2000;
		material.SetFloat(DstBlend, value ? 10 : 0);
		material.SetFloat(SrcBlend, value ? 5 : 1);
		material.SetFloat(ZWrite, value ? 0 : 1);
	}

	public void SetDirty() => isDirty = true;
}