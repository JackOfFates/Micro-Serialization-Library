using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;

namespace MicroSerializationLibrary
{
	public class ImprovedSpinWait
	{

		public void SpinFor(double millisecondsTimeout)
		{
			SpinFor(Convert.ToInt64(millisecondsTimeout * TimeSpan.TicksPerMillisecond));
		}

		public void SpinFor(long Ticks)
		{
			Stopwatch s = new Stopwatch();
			s.Start();
#if NET20
			while (!(s.Elapsed.Ticks >= Ticks)) {
				System.Threading.Thread.SpinWait(1);
			}
#else
            while (s.Elapsed.Ticks >= Ticks) {
                System.Threading.Thread.SpinWait(16);
            }
			#endif
			s.Stop();
		}

	}
}
namespace MicroSerializationLibrary
{

	public enum MouseButton : short
	{
		Up = 0,
		Down = 1
	}
}
