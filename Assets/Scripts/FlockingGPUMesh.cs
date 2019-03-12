using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FlockingGPUMesh : MonoBehaviour
{
	public const int TRIANGLES_PER_BOID = 6;
	public const int VERTEX_PER_BOID = TRIANGLES_PER_BOID * 3;

	public ComputeShader generator;

	[Space] public ComputeBuffer vertexBuffer;

	public ComputeBuffer normalBuffer;
//    public ComputeBuffer colorBuffer;

	private Vector3[] vertex;

	private Vector3[] normals;

//    public Color[] colors;
	private int[] indices;

	private int shaderKernel;

	private FlockingGPU _manager;

	private FlockingGPU manager
	{
		get
		{
			if (_manager == null) _manager = FindObjectOfType<FlockingGPU>();
			return _manager;
		}
	}

	private Mesh _mesh;

	private Mesh mesh
	{
		get
		{
			if (_mesh == null) _mesh = new Mesh();
			return _mesh;
		}
	}

	private MeshFilter _meshFilter;

	private MeshFilter meshFilter
	{
		get
		{
			if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
			return _meshFilter;
		}
	}

	private void Start()
	{
		vertex = new Vector3[manager.boidCount * VERTEX_PER_BOID];
		normals = new Vector3[manager.boidCount * VERTEX_PER_BOID];
//        colors = new Color[manager.boidCount * VERTEX_PER_BOID];
		indices = new int[vertex.Length];

		for (int i = 0; i < vertex.Length; i++)
		{
			indices[i] = i;
		}

		InitComputeShader();
	}

	private void Update()
	{
		ComputeMesh();
		UpdateMesh();
	}

	private void OnDestroy()
	{
		DeinitComputeShader();
	}

	private void InitComputeShader()
	{
		vertexBuffer = new ComputeBuffer(manager.boidCount * VERTEX_PER_BOID, Marshal.SizeOf(typeof(Vector3)));
		normalBuffer = new ComputeBuffer(manager.boidCount * VERTEX_PER_BOID, Marshal.SizeOf(typeof(Vector3)));
//        colorBuffer = new ComputeBuffer(manager.boidCount * VERTEX_PER_BOID, Marshal.SizeOf(typeof(Color)));

		shaderKernel = generator.FindKernel("CSMain");
	}

	private void ComputeMesh()
	{
		generator.SetBuffer(shaderKernel, "boidBuffer", manager.boidBuffer);
		generator.SetBuffer(shaderKernel, "vertexBuffer", vertexBuffer);
		generator.SetBuffer(shaderKernel, "normalBuffer", normalBuffer);
//        generator.SetBuffer(shaderKernel, "colorBuffer", colorBuffer);

		generator.Dispatch(shaderKernel, manager.boidCount / FlockingGPU.THREAD_GROUP_SIZE, 1, 1);

		vertexBuffer.GetData(vertex);
		normalBuffer.GetData(normals);		
	}

	private void DeinitComputeShader()
	{
		if (vertexBuffer != null) vertexBuffer.Release();
		if (normalBuffer != null) normalBuffer.Release();
//        if (colorBuffer != null) colorBuffer.Release();
	}

	private void UpdateMesh()
	{
		if (vertex.Length > 0)
		{
			mesh.vertices = vertex;
			mesh.normals = normals;

//            mesh.colors = colors;
			mesh.SetIndices(indices, MeshTopology.Triangles, 0);
			meshFilter.sharedMesh = mesh;
		}
		else
		{
			meshFilter.sharedMesh = null;
		}
	}
}