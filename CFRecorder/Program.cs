using System;
using System.Windows.Forms;
using QUT;

namespace CFRecorder
{
    static class Program
    {
        [MTAThread]
        static void Main()
        {
			try
			{
				// Attempt to ensure any faults will be recovered from by re-running the application
				Utilities.QueueNextAppRun(DateTime.Now.AddMinutes(5));

				if (!Settings.DebugMode)
					PDA.Video.PowerOffScreen();

				DeviceManager.Start();

				PDA.Video.Standby();
			}
			catch (Exception e)
			{
				Utilities.Log(e, "Caught at top-level handler");
			}
        }
    }
}