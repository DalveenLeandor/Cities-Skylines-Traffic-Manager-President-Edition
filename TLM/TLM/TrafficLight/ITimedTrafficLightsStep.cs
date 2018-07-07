﻿using System.Collections.Generic;
using TrafficManager.Geometry.Impl;
using TrafficManager.Manager;
using TrafficManager.Traffic;
using TrafficManager.Traffic.Enums;

namespace TrafficManager.TrafficLight {
	public interface ITimedTrafficLightsStep : ICustomSegmentLightsManager {
		// TODO documentation
		IDictionary<ushort, ICustomSegmentLights> CustomSegmentLights { get; }
		LinkedList<ICustomSegmentLights> InvalidSegmentLights { get; }
		int PreviousStepRefIndex { get; set; }
		int NextStepRefIndex { get; set; }
		int MinTime { get; set; }
		int MaxTime { get; set; }
		StepChangeMetric ChangeMetric { get; set; }
		float WaitFlowBalance { get; set; }
		float CurrentWait { get; }
		float CurrentFlow { get; }

		void CalcWaitFlow(bool countOnlyMovingIfGreen, int stepRefIndex, out float wait, out float flow);
		RoadBaseAI.TrafficLightState GetLightState(ushort segmentId, ExtVehicleType vehicleType, int lightType);
		float GetMetric(float flow, float wait);
		ICustomSegmentLights GetSegmentLights(ushort segmentId);
		long MaxTimeRemaining();
		long MinTimeRemaining();
		bool IsInStartTransition();
		bool IsInEndTransition();
		bool IsEndTransitionDone();
		bool RelocateSegmentLights(ushort sourceSegmentId, ushort targetSegmentId);
		ICustomSegmentLights RemoveSegmentLights(ushort segmentId);
		bool SetSegmentLights(ushort segmentId, ICustomSegmentLights lights);
		void SetStepDone();
		bool ShouldGoToNextStep(out float metric);
		bool ShouldGoToNextStep(float flow, float wait, out float metric);
		void Start(int previousStepRefIndex = -1);
		bool StepDone(bool updateValues);
		string ToString();
		void UpdateLights();
		void UpdateLiveLights();
		void UpdateLiveLights(bool noTransition);
	}
}