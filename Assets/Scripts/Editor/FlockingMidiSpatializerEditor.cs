using Midi;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FlockingMidiSpatializer))]
public class FlockingMidiSpatializerEditor : Editor
{
	private FlockingMidiSpatializer dat;

	private void OnEnable()
	{
		dat = target as FlockingMidiSpatializer;
	}

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		GUILayout.Space(20);
		DrawMidiDevices();
		
		GUILayout.Space(20);
		DrawNoteButtons();
	}

	private void DrawMidiDevices()
	{
		GUILayout.BeginVertical(EditorStyles.helpBox);

		GUILayout.Label("Devices", EditorStyles.boldLabel);
		for (var i = 0; i < OutputDevice.InstalledDevices.Count; i++)
		{
			if (GUILayout.Button(i + " - " + OutputDevice.InstalledDevices[i].Name))
			{
				dat.midiDevice = i;
			}
		}

		GUILayout.EndVertical();
	}

	private void DrawNoteButtons()
	{
		GUILayout.BeginVertical(EditorStyles.helpBox);
		
		GUILayout.Label("Config", EditorStyles.boldLabel);
		
		if (GUILayout.Button("Setup notes"))
		{
			dat.SetupNotes();
		}
		
		if (GUILayout.Button("Tone up"))
		{
			dat.SetToneUp();
		}
		
		if (GUILayout.Button("Tone down"))
		{
			dat.SetToneDown();
		}
		
		GUILayout.EndVertical();
	}
}