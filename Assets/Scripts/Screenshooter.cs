using System;
using UnityEngine;

public class Screenshooter : MonoBehaviour
{
	public int superSize = 1;

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.F8))
		{
			TakeScreenshot();
		}
	}

	[ContextMenu("Take screenshot")]
	private void TakeScreenshot()
	{
		ScreenCapture.CaptureScreenshot("Screenshot_" + DateTime.Now.ToString("MMddyyyy_hhmmss"), superSize);
	}
}