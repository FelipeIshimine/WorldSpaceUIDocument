using System;
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
	public UIDocument Document => document; 
	[SerializeField] internal PanelSettings panelSettingsAsset;
	[SerializeField] private WorldSpaceUIDocument parentDocument;
	[SerializeField] private Vector2 normalizedPosition;
	[SerializeField] private Vector3 parentPositionOffset;

	public Vector2 NormalizedPosition
	{
		get => normalizedPosition;
		set
		{ 
            normalizedPosition = value; 
            ApplyNormalizedPosition();
        }
	}

	public Vector2 Pivot
	{
		get => pivot;
		set
		{
			pivot = value;
			Init();
		}
	}

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
	private MeshCollider col;

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
		if (Application.isPlaying)
		{
			name = $"[{gameObject.GetInstanceID()}]{name}";
		}
		document = GetComponent<UIDocument>();
	}
	
	private Vector2 NormalizedPositionToLocalPosition(Vector2 nPosition)
	{
		if(panelSettings)
		{
			var offset = Vector2.Scale(pivot, -panelSettings.referenceResolution) / pixelsPerUnit;
			//var pivotPos = Vector2.Scale(panelSettings.referenceResolution, offset);
			return offset + Vector2.Scale(panelSettings.referenceResolution, nPosition) / pixelsPerUnit;
		}
		Debug.LogWarning("Panel settings not found");
		return Vector2.zero;
	}

	private void Start()
	{
		Init();

	
	    
	
	}

	private Vector3 NormalizedPositionToWorldPosition(Vector2 normalizedPosition) =>
		transform.TransformPoint(NormalizedPositionToLocalPosition(normalizedPosition));

	private void Init()
	{
//		Debug.Log("Init");
		if (!panelSettingsAsset)
		{
			Debug.LogWarning("PanelSettingsAsset is Null. InitializationCancelled");
			return;
		}
			
		if ((!panelSettings || panelSettingsAsset.referenceResolution != panelSettings.referenceResolution))
		{
			if (Application.isPlaying && panelSettings)
			{
				Destroy(panelSettings);
			}
			panelSettings = Instantiate(panelSettingsAsset);
			panelSettings.name =  $"[{gameObject.GetInstanceID()}]{panelSettings.name.Replace($"(Clone)", $"({name})")}";
			document.panelSettings = panelSettings;
			panelSettings.targetTexture = renderTexture;
			col = gameObject.GetComponent<MeshCollider>();
			panelSettings.SetScreenToPanelSpaceFunction(screenPosition =>
			{
				screenPosition.y = Screen.height - screenPosition.y;

				var invalidPosition = new Vector2(float.NaN, float.NaN);

				var mainCamera = GetCamera();
				var cameraRay = mainCamera.ScreenPointToRay(screenPosition);

				if (!col.Raycast(cameraRay, out hit, 100f))
				{
					Debug.DrawRay(cameraRay.origin, cameraRay.direction * 100, Color.magenta);
					return invalidPosition;
				}	

				//Debug.Log(mainCamera.name);
				Debug.DrawLine(cameraRay.origin, hit.point, Color.green);
				Vector2 pixelUV = hit.textureCoord;

				pixelUV.y = 1 - pixelUV.y;
				pixelUV.x *= this.document.panelSettings.targetTexture.width;
				pixelUV.y *= this.document.panelSettings.targetTexture.height;

				return pixelUV;
			});
		}
			
		if (!renderTexture)
		{
			renderTexture = new RenderTexture(
				width:panelSettings.referenceResolution.x,
				height:panelSettings.referenceResolution.y, 
				32);
				
			panelSettings.targetTexture = renderTexture;
		}
			
		ApplyNormalizedPosition();
		
		CreateMesh((Vector2)panelSettings.referenceResolution / pixelsPerUnit, pivot);
	}

	private void ApplyNormalizedPosition()
	{
		if (parentDocument)
		{
			transform.transform.position = parentDocument.NormalizedPositionToWorldPosition(normalizedPosition) + parentPositionOffset;
		}
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
			col = meshRenderer.gameObject.AddComponent<MeshCollider>();
		}
		col.sharedMesh = mesh;/*
		col.center = (Vector3)size / 2f + offset;
		col.size = size;*/
	}

	private Camera GetCamera()
	{
		if (cam == null)
		{
			cam = Camera.main;
		}
		return cam;
	}

	public Vector3 GetWorldPosition() => hit.point;/*
	{
		return Camera.main.WorldToScreenPoint(hit.point);
		/*
		screenPoint.y = Screen.height - screenPoint.y;
		
		//RuntimePanelUtils.ScreenToPanel(screenPoint)

		//var pos = RuntimePanelUtils.CameraTransformWorldToPanel(document.rootVisualElement.panel, hit.point, cam);
        
		return pos;
	#1#
	}*/

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
		if (!Application.isPlaying && document)
		{
			document.panelSettings = panelSettingsAsset;
		}
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