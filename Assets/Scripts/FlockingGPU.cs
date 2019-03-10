using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public struct Boid
{
	public Vector3 position;
	public Vector3 forward;
	public Vector3 targetDir;		
	/// <summary>
	/// x -> maxSpeed
	/// y -> maxAngular 
	/// </summary>
	public Vector2 boidOptions;

	public int neighbours;
	public int cell;
}

public class FlockingGPU : MonoBehaviour
{
	public const int THREAD_GROUP_SIZE = 512;
	
	public int boidCount = 2048;
	public ComputeShader compute;	
	
	[Header("Boid options")] 
	public float maxBoidDistance = 25;
	public float boidNeighbourDistance = 3;
	public float boidSeparationDistance = 1;
	public bool separate;

	[Space] 
	private Boid[] data;
	public ComputeBuffer boidBuffer;
	
	private int shaderKernel;

	public Vector3 stageSize
	{
		get { return new Vector3(maxBoidDistance, .5f * maxBoidDistance, maxBoidDistance); }
	}
	
	private void Start()
	{
		InitComputeShader();
		SetComputeData();
	}

	private void Update()
	{
		SetComputeData();
		RunComputeShader();
	}

	private void OnDisable()
	{
		if (boidBuffer != null) boidBuffer.Release();
	}

	private void InitComputeShader()
	{
		data = new Boid[boidCount];

		// Initialize data
		for (var i = 0; i < data.Length; i++)
		{
			data[i].position = new Vector3(
				Random.Range(-maxBoidDistance, maxBoidDistance),
				.5f * Random.Range(-maxBoidDistance, maxBoidDistance),
				Random.Range(-maxBoidDistance, maxBoidDistance)
				); 
			data[i].forward = (new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)))
				.normalized;
			data[i].targetDir = data[i].forward;
			data[i].boidOptions = new Vector2(5f, 180f);
		}

		boidBuffer = new ComputeBuffer(boidCount, Marshal.SizeOf(typeof(Boid)));
		boidBuffer.SetData(data);
		shaderKernel = compute.FindKernel("Flocking");
	}

	private void SetComputeData()
	{
		// Set shader values
		compute.SetFloat("time", Time.time);
		compute.SetFloat("dt", Time.deltaTime);
		compute.SetInt("boidCount", boidCount);
		compute.SetVector("stageSize", stageSize);
		compute.SetVector("simulationSettings", new Vector4(
			maxBoidDistance,
			boidNeighbourDistance,
			boidSeparationDistance,
			separate ? 1 : 0)
		);
		
		compute.SetBuffer(shaderKernel, "boidBuffer", boidBuffer);
	}
	
	public void RunComputeShader()
	{
		compute.Dispatch(shaderKernel, boidCount / THREAD_GROUP_SIZE, 1, 1);
		
		// boidBuffer.GetData(data);
	}
}