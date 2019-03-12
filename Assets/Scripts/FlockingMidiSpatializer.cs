using System.Runtime.InteropServices;
using Midi;
using UnityEngine;

public class FlockingMidiSpatializer : MonoBehaviour
{
	private const int NOTE_COUNT = 25;
	
	[Header("Midi config")] 
	public bool disableMidi;
	public int midiDevice = 1;
	public Channel channel = Channel.Channel1;
	
	private OutputDevice outputDevice;
	
	[Space]
	public Pitch[] pitches;	
	
	[Header("Detector")]
	public float highThreshold = .9f;
	public float lowThreshold = .1f;
    
	[Header("GPU")]
	public ComputeShader computeShader;
	public int[] countData;
	public bool[] midiInBuffer;
	public bool[] midiCurrentBuffer;
    
	private ComputeBuffer countBuffer;
	private int shaderKernel;
    
	#region Properties
    
	private FlockingGPU _manager;

	private FlockingGPU manager
	{
		get
		{
			if (_manager == null) _manager = FindObjectOfType<FlockingGPU>();
			return _manager;
		}
	}
    
	#endregion

	private void Start()
	{
		countData = new int[NOTE_COUNT];
		midiInBuffer = new bool[NOTE_COUNT];
		midiCurrentBuffer = new bool[NOTE_COUNT];
		
		InitComputeShader();
		InitMidiDevice();
	}

	private void Update()
	{
		SetComputeData();
		RunComputeShader();

		UpdateMidiDevice();
	}

	private void OnDestroy()
	{
		DeinitComputeShader();
		DeinitMidiDevice();
	}

	private void OnDrawGizmosSelected()
	{
		if (countData.Length < NOTE_COUNT || midiCurrentBuffer.Length < NOTE_COUNT) return;

		var size = manager.stageSize * 2;
		float w = size.x / 5;
		float h = size.y / 5;
		float prof = size.z;

		Vector3 startPos = -manager.stageSize + new Vector3(w / 2, h / 2, 0);
		startPos.z = 0;
		
		for (var x = 0; x < 5; x++)
		{
			for (var y = 0; y < 5; y++)
			{
				Gizmos.color = midiCurrentBuffer[x + 5 * y] ? new Color(1,0,0,.25f) : new Color(.2f, .2f, .2f,.25f);
				Gizmos.DrawCube(startPos + new Vector3(x * w,y * h,0), new Vector3(w,h,prof));
			}
		}
	}

	private void InitComputeShader()
	{
		countBuffer = new ComputeBuffer(countData.Length, Marshal.SizeOf(typeof(int)));       
		countBuffer.SetData(countData);   
        
		shaderKernel = computeShader.FindKernel("CSMain");
	}

	private void SetComputeData()
	{
		for (var i = 0; i < countData.Length; i++)
		{
			countData[i] = 0;
		}
        
		countBuffer.SetData(countData);               
		computeShader.SetInt("boidCount", manager.boidCount);               
		computeShader.SetVector("stageSize", manager.stageSize);
		computeShader.SetBuffer(shaderKernel, "boidBuffer", manager.boidBuffer);
		computeShader.SetBuffer(shaderKernel, "countBuffer", countBuffer); 
	}
    
	private void RunComputeShader()
	{
		computeShader.Dispatch(shaderKernel, countData.Length / 64 + 1, 1, 1);
        
		countBuffer.GetData(countData);
	}

	private void DeinitComputeShader()
	{
		if (countBuffer != null) countBuffer.Release();
	}

	private void InitMidiDevice()
	{
		if (disableMidi) return;
		
		outputDevice = OutputDevice.InstalledDevices[midiDevice];
		if (!outputDevice.IsOpen)
		{
			outputDevice.Open();
		}         
	}

	private void UpdateMidiDevice()
	{		
		// Calculate density
		int max = Mathf.Max(countData);
		for (var i = 0; i < NOTE_COUNT; i++)
		{
			midiInBuffer[i] = 
				((countData[i] / (float)max) > highThreshold) ||
				((countData[i] / (float)max) < lowThreshold);
		}
		
		for (var i = 0; i < NOTE_COUNT; i++)
		{
			if (midiInBuffer[i]) SendNoteOn(i);
			else SendNoteOff(i);
			
			// Clear input buffer
			midiInBuffer[i] = false;
		}
	}

	private void DeinitMidiDevice()
	{
		SilenceCurrent();
		
		if (outputDevice != null && outputDevice.IsOpen)
		{            
			outputDevice.Close();
		} 
	}

	/// <summary>
	/// C Pentatonic scale 
	/// </summary>
	[ContextMenu("Setup notes")]
	public void SetupNotes()
	{
		pitches = new Pitch[NOTE_COUNT];
        
		pitches[0] = Pitch.C2;
		pitches[1] = Pitch.D2;
		pitches[2] = Pitch.E2;
		pitches[3] = Pitch.G2;
		pitches[4] = Pitch.A2;
        
		pitches[5] = Pitch.C3;
		pitches[6] = Pitch.D3;
		pitches[7] = Pitch.E3;
		pitches[8] = Pitch.G3;
		pitches[9] = Pitch.A3;
        
		pitches[10] = Pitch.C4;
		pitches[11] = Pitch.D4;
		pitches[12] = Pitch.E4;
		pitches[13] = Pitch.G4;
		pitches[14] = Pitch.A4;
        
		pitches[15] = Pitch.C5;
		pitches[16] = Pitch.D5;
		pitches[17] = Pitch.E5;
		pitches[18] = Pitch.G5;
		pitches[19] = Pitch.A5;
        
		pitches[20] = Pitch.C6;
		pitches[21] = Pitch.D6;
		pitches[22] = Pitch.E6;
		pitches[23] = Pitch.G6;
		pitches[24] = Pitch.A6;
	}

	[ContextMenu("Up tone")]
	public void SetToneUp()
	{
		SilenceCurrent();			
		
		for (var i = 0; i < pitches.Length; i++)
		{
			pitches[i] = (Pitch)((int) pitches[i] + 2);
		}
	}
	
	[ContextMenu("Down tone")]
	public void SetToneDown()
	{
		SilenceCurrent();		
		
		for (var i = 0; i < pitches.Length; i++)
		{
			pitches[i] = (Pitch)((int) pitches[i] - 2);
		}
	}

	private void SilenceCurrent()
	{
		if (!Application.isPlaying) return;
		
		for (var i = 0; i < midiCurrentBuffer.Length; i++)
		{
			SendNoteOff(i);
		}
	}

	private void SendNoteOn(int index)
	{
		if (midiCurrentBuffer[index]) return;
		midiCurrentBuffer[index] = true;
				
		if (disableMidi || outputDevice == null) return;
		outputDevice.SendNoteOn(channel, pitches[index], 127);		
	}

	private void SendNoteOff(int index)
	{
		if (!midiCurrentBuffer[index]) return;
		midiCurrentBuffer[index] = false;

		if (disableMidi || outputDevice == null) return;
		outputDevice.SendNoteOff(channel, pitches[index], 127);		
	}
}