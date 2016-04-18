using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Kinect;

namespace ComperPlayer
{
    public struct SmoothParameters
	{
		public float smoothing;
		public float correction;
		public float prediction;
		public float jitterRadius;
		public float maxDeviationRadius;
	}
/// <summary>
/// Implementation of a Holt Double Exponential Smoothing filter. The double exponential
/// smooths the curve and predicts.  There is also noise jitter removal. And maximum
/// prediction bounds.  The parameters are commented in the Init function.
/// </summary>
    public class JointPositionsFilter
    {
   
    }
}