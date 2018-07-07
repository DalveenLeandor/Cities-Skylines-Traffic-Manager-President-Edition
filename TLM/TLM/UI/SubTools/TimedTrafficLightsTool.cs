using ColossalFramework;
using ColossalFramework.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TrafficManager.Custom.AI;
using TrafficManager.State;
using TrafficManager.Geometry;
using TrafficManager.TrafficLight;
using UnityEngine;
using TrafficManager.Manager;
using TrafficManager.Traffic;
using CSUtil.Commons;
using TrafficManager.Manager.Impl;
using TrafficManager.Geometry.Impl;
using ColossalFramework.UI;
using TrafficManager.Traffic.Enums;
using TrafficManager.UI.SubTools.TimedTrafficLights;

namespace TrafficManager.UI.SubTools {
	public class TimedTrafficLightsTool : SubTool {
		public enum TimedTrafficLightsToolMode {
			None,
			SelectNode,
			ShowLights,
			ViewStep,
			EditStep,
			AddStep,
			AddNode,
			RemoveNode,
			Copy
		}

		private readonly GUIStyle _counterStyle = new GUIStyle();
		private readonly int[] _hoveredButton = new int[2];
		private bool nodeSelectionLocked = false;
		public List<ushort> SelectedNodeIds { get; private set; } = new List<ushort>();
		private bool _cursorInSecondaryPanel;
		private Rect _windowRect = TrafficManagerTool.MoveGUI(new Rect(0, 0, 480, 350));
		private Rect _windowRect2 = TrafficManagerTool.MoveGUI(new Rect(0, 0, 300, 150));
		//private bool _timedPanelAdd = false;
		//private int _timedEditStep = -1;
		private ushort _hoveredNode = 0;
		private bool _timedShowNumbers = false;
		//private int _timedViewedStep = -1;
		private int _stepMinValue = 1;
		private int _stepMaxValue = 1;
		private StepChangeMetric _stepMetric = StepChangeMetric.Default;
		private float _waitFlowBalance = GlobalConfig.Instance.TimedTrafficLights.FlowToWaitRatio;
		private string _stepMinValueStr = "1";
		private string _stepMaxValueStr = "1";
		private bool timedLightActive = false;
		private int currentStep = -1;
		private int numSteps = 0;
		private bool inTestMode = false;
		private ushort nodeIdToCopy = 0;
		private HashSet<ushort> currentTimedNodeIds;
		private UITimedTrafficLightsWindow window;

		private TimedTrafficLightsToolMode mode;
		public TimedTrafficLightsToolMode Mode {
			get {
				return mode;
			}
			private set {
				mode = value;
				if (window != null) {
					window.UpdateBindings();
				}
			}
		}

		public ITimedTrafficLights MasterTimedTrafficLights { get; private set; }
		public int SelectedStepIndex { get; private set; }

		private GUIStyle layout = new GUIStyle { normal = { textColor = new Color(1f, 1f, 1f) } };
		private GUIStyle layoutRed = new GUIStyle { normal = { textColor = new Color(1f, 0f, 0f) } };
		private GUIStyle layoutGreen = new GUIStyle { normal = { textColor = new Color(0f, 1f, 0f) } };
		private GUIStyle layoutYellow = new GUIStyle { normal = { textColor = new Color(1f, 1f, 0f) } };

		public TimedTrafficLightsTool(TrafficManagerTool mainTool) : base(mainTool) {
			currentTimedNodeIds = new HashSet<ushort>();
			Mode = TimedTrafficLightsToolMode.None;
			window = (UITimedTrafficLightsWindow)UIView.GetAView().AddUIComponent(typeof(UITimedTrafficLightsWindow));
		}

		public override bool IsCursorInPanel() {
			return base.IsCursorInPanel() || _cursorInSecondaryPanel || (window != null && window.containsMouse);
		}

		private void RefreshCurrentTimedNodeIds(ushort forceNodeId=0) {
			TrafficLightSimulationManager tlsMan = TrafficLightSimulationManager.Instance;

			if (forceNodeId == 0) {
				currentTimedNodeIds.Clear();
			} else {
				currentTimedNodeIds.Remove(forceNodeId);
			}

			for (uint nodeId = (forceNodeId == 0 ? 1u : forceNodeId); nodeId <= (forceNodeId == 0 ? NetManager.MAX_NODE_COUNT - 1 : forceNodeId); ++nodeId) {
				if (!Constants.ServiceFactory.NetService.IsNodeValid((ushort)nodeId)) {
					continue;
				}

				if (tlsMan.HasTimedSimulation((ushort)nodeId)) {
					currentTimedNodeIds.Add((ushort)nodeId);
				}
			}
		}

		public override void OnActivate() {
			TrafficLightSimulationManager tlsMan = TrafficLightSimulationManager.Instance;

			RefreshCurrentTimedNodeIds();

			nodeSelectionLocked = false;
			foreach (ushort nodeId in currentTimedNodeIds) {
				if (!Constants.ServiceFactory.NetService.IsNodeValid(nodeId)) {
					continue;
				}

				tlsMan.TrafficLightSimulations[nodeId].Housekeeping();
			}

			/*Log._Debug($"TimedTrafficLightsTool.OnActivate(): All fonts:");
			UIDynamicFont[] allFonts = Resources.FindObjectsOfTypeAll<UIDynamicFont>();
			foreach (UIDynamicFont f in allFonts) {
				Log._Debug($"TimedTrafficLightsTool.OnActivate(): Font '{f.name}'");
			}
			Log._Debug($"TimedTrafficLightsTool.OnActivate(): ===");*/
			SelectNode();
		}

		public override void OnSecondaryClickOverlay() {
			if (!IsCursorInPanel()) {
				SelectNode();
			}
		}

		public override void OnPrimaryClickOverlay() {
			if (HoveredNodeId <= 0 || nodeSelectionLocked)
				return;

			TrafficLightSimulationManager tlsMan = TrafficLightSimulationManager.Instance;

			if (Mode == TimedTrafficLightsToolMode.ShowLights) {
				SelectNode();
			}

			switch (Mode) {
				case TimedTrafficLightsToolMode.SelectNode:
					if (! tlsMan.HasTimedSimulation(HoveredNodeId)) {
						if (IsNodeSelected(HoveredNodeId)) {
							RemoveSelectedNode(HoveredNodeId);
						} else {
							AddSelectedNode(HoveredNodeId);
						}
					} else {
						if (MasterTimedTrafficLights == null) {
							//timedSim.housekeeping();
							ShowLights(HoveredNodeId);
						} else {
							MainTool.ShowTooltip(Translation.GetString("NODE_IS_TIMED_LIGHT"));
						}
					}
					break;
				case TimedTrafficLightsToolMode.AddNode:
					if (MasterTimedTrafficLights == null) {
						SelectNode();
						return;
					}

					if (MasterTimedTrafficLights.NodeGroup.Contains(HoveredNodeId)) {
						return;
					}

					//bool mayEnterBlocked = Options.mayEnterBlockedJunctions;
					//ITimedTrafficLights existingTimedLight = null;
					//foreach (var nodeId in SelectedNodeIds) {
					//	if (!tlsMan.HasTimedSimulation(nodeId)) {
					//		continue;
					//	}

					//	//mayEnterBlocked = timedNode.vehiclesMayEnterBlockedJunctions;
					//	existingTimedLight = tlsMan.TrafficLightSimulations[nodeId].TimedLight;
					//}

					/*if (timedSim2 != null)
						timedSim2.housekeeping();*/
					ITimedTrafficLights timedLight2 = null;
					if (! tlsMan.HasTimedSimulation(HoveredNodeId)) {
						var nodeGroup = new List<ushort>();
						nodeGroup.Add(HoveredNodeId);
						tlsMan.SetUpTimedTrafficLight(HoveredNodeId, nodeGroup);
					}
					timedLight2 = tlsMan.TrafficLightSimulations[HoveredNodeId].TimedLight;
					timedLight2.Join(MasterTimedTrafficLights);
					foreach (ushort nodeId in timedLight2.NodeGroup) {
						RefreshCurrentTimedNodeIds(nodeId);
					}
					MasterTimedTrafficLights = MasterTimedTrafficLights.MasterLights();
					ShowLights();
					break;
				case TimedTrafficLightsToolMode.RemoveNode:
					if (MasterTimedTrafficLights == null) {
						SelectNode();
						return;
					}

					if (MasterTimedTrafficLights.NodeGroup.Contains(HoveredNodeId)) {
						tlsMan.RemoveNodeFromSimulation(HoveredNodeId, false, false);
						RefreshCurrentTimedNodeIds(HoveredNodeId);
					}
					MasterTimedTrafficLights = MasterTimedTrafficLights.MasterLights(); // just in case the master light has changed
					ShowLights();
					break;
				case TimedTrafficLightsToolMode.Copy:
					if (nodeIdToCopy == 0 || !tlsMan.HasTimedSimulation(nodeIdToCopy)) {
						SelectNode();
						return;
					}
					
					// compare geometry
					NodeGeometry sourceNodeGeo = NodeGeometry.Get(nodeIdToCopy);
					NodeGeometry targetNodeGeo = NodeGeometry.Get(HoveredNodeId);

					if (sourceNodeGeo.NumSegmentEnds != targetNodeGeo.NumSegmentEnds) {
						MainTool.ShowTooltip(Translation.GetString("The_chosen_traffic_light_program_is_incompatible_to_this_junction"));
						return;
					}

					// check for existing simulation
					if (tlsMan.HasTimedSimulation(HoveredNodeId)) {
						MainTool.ShowTooltip(Translation.GetString("NODE_IS_TIMED_LIGHT"));
						return;
					}

					ITimedTrafficLights sourceTimedLights = tlsMan.TrafficLightSimulations[nodeIdToCopy].TimedLight;

					// copy `nodeIdToCopy` to `HoveredNodeId`
					tlsMan.SetUpTimedTrafficLight(HoveredNodeId, new List<ushort> { HoveredNodeId });

					tlsMan.TrafficLightSimulations[HoveredNodeId].TimedLight.PasteSteps(sourceTimedLights);
					RefreshCurrentTimedNodeIds(HoveredNodeId);

					//Cleanup();
					MasterTimedTrafficLights = MasterTimedTrafficLights.MasterLights();
					ShowLights();
					break;
			}
		}

		public override void OnToolGUI(Event e) {
			base.OnToolGUI(e);

			switch (Mode) {
				case TimedTrafficLightsToolMode.SelectNode:
					_guiTimedTrafficLightsNode();
					break;
				case TimedTrafficLightsToolMode.ShowLights:
				case TimedTrafficLightsToolMode.ViewStep:
				case TimedTrafficLightsToolMode.EditStep:
				case TimedTrafficLightsToolMode.AddStep:
				case TimedTrafficLightsToolMode.AddNode:
				case TimedTrafficLightsToolMode.RemoveNode:
					_guiTimedTrafficLights();
					break;
				case TimedTrafficLightsToolMode.Copy:
					_guiTimedTrafficLightsCopy();
					break;
			}
		}

		public override void RenderOverlay(RenderManager.CameraInfo cameraInfo) {
			bool onlyInGroup = Mode == TimedTrafficLightsToolMode.RemoveNode;

			//Log._Debug($"nodeSelLocked={nodeSelectionLocked} HoveredNodeId={HoveredNodeId} IsNodeSelected={IsNodeSelected(HoveredNodeId)} onlySelected={onlySelected} isinsideui={MainTool.GetToolController().IsInsideUI} cursorVis={Cursor.visible}");
            if (!nodeSelectionLocked &&
				HoveredNodeId != 0 &&
				(Mode == TimedTrafficLightsToolMode.RemoveNode ^ !IsNodeSelected(HoveredNodeId)) &&
				!IsCursorInPanel() &&
				Cursor.visible &&
				Flags.mayHaveTrafficLight(HoveredNodeId)
			) {
				MainTool.DrawNodeCircle(cameraInfo, HoveredNodeId, false, false);
			}

			if (MasterTimedTrafficLights == null) {
				return;
			}

			foreach (var nodeId in MasterTimedTrafficLights.NodeGroup) {
				MainTool.DrawNodeCircle(cameraInfo, nodeId, true, false);
			}
		}

		private void _guiTimedControlPanel(int num) {
			//Log._Debug("guiTimedControlPanel");
			try {
				TrafficLightSimulationManager tlsMan = TrafficLightSimulationManager.Instance;

				if (Mode == TimedTrafficLightsToolMode.AddNode || Mode == TimedTrafficLightsToolMode.RemoveNode) {
					GUILayout.Label(Translation.GetString("Select_junction"));
					if (GUILayout.Button(Translation.GetString("Cancel"))) {
						ShowLights();
					} else {
						DragWindow(ref _windowRect);
						return;
					}
				}

				if (this.MasterTimedTrafficLights == null) {
					SelectNode();
					//Log._Debug("nodesim or timednodemain is null");
					DragWindow(ref _windowRect);
					return;
				}

				if (Event.current.type == EventType.Layout) {
					timedLightActive = MasterTimedTrafficLights.IsStarted();
					currentStep = MasterTimedTrafficLights.CurrentStep;
					inTestMode = MasterTimedTrafficLights.IsInTestMode();
					numSteps = MasterTimedTrafficLights.NumSteps();
				}

				if (!timedLightActive && numSteps > 0 && Mode != TimedTrafficLightsToolMode.AddStep && SelectedStepIndex < 0) {
					ViewStep(0);
					foreach (var nodeId in MasterTimedTrafficLights.NodeGroup) {
						tlsMan.TrafficLightSimulations[nodeId].TimedLight?.GetStep(SelectedStepIndex).UpdateLiveLights(true);
					}
				}

				for (var i = 0; i < MasterTimedTrafficLights.NumSteps(); i++) {
					GUILayout.BeginHorizontal();

					if (Mode != TimedTrafficLightsToolMode.EditStep || SelectedStepIndex != i) {
						if (timedLightActive) {
							if (i == currentStep) {
								GUILayout.BeginVertical();
								GUILayout.Space(5);
								String labelStr = Translation.GetString("State") + " " + (i + 1) + ": (" + Translation.GetString("min/max") + ")" + MasterTimedTrafficLights.GetStep(i).MinTimeRemaining() + "/" + MasterTimedTrafficLights.GetStep(i).MaxTimeRemaining();
								float flow = Single.NaN;
								float wait = Single.NaN;
								if (inTestMode) {
									try {
										MasterTimedTrafficLights.GetStep(MasterTimedTrafficLights.CurrentStep).CalcWaitFlow(true, MasterTimedTrafficLights.CurrentStep, out wait, out flow);
									} catch (Exception e) {
										Log.Warning("calcWaitFlow in UI: This is not thread-safe: " + e.ToString());
									}
								} else {
									wait = MasterTimedTrafficLights.GetStep(i).CurrentWait;
									flow = MasterTimedTrafficLights.GetStep(i).CurrentFlow;
								}
								if (!Single.IsNaN(flow) && !Single.IsNaN(wait))
									labelStr += " " + Translation.GetString("avg._flow") + ": " + String.Format("{0:0.##}", flow) + " " + Translation.GetString("avg._wait") + ": " + String.Format("{0:0.##}", wait);
								GUIStyle labelLayout = layout;
								if (inTestMode && !Single.IsNaN(wait) && !Single.IsNaN(flow)) {
									float metric;
									if (MasterTimedTrafficLights.GetStep(i).ShouldGoToNextStep(flow, wait, out metric))
										labelLayout = layoutRed;
									else
										labelLayout = layoutGreen;
								} else {
									bool inEndTransition = false;
									try {
										inEndTransition = MasterTimedTrafficLights.GetStep(i).IsInEndTransition();
									} catch (Exception e) {
										Log.Error("Error while determining if timed traffic light is in end transition: " + e.ToString());
									}
									labelLayout = inEndTransition ? layoutYellow : layoutGreen;
								}
								GUILayout.Label(labelStr, labelLayout);
								GUILayout.Space(5);
								GUILayout.EndVertical();
								if (GUILayout.Button(Translation.GetString("Skip"), GUILayout.Width(80))) {
									foreach (var nodeId in MasterTimedTrafficLights.NodeGroup) {
										tlsMan.TrafficLightSimulations[nodeId].TimedLight?.SkipStep();
									}
								}
							} else {
								GUILayout.Label(Translation.GetString("State") + " " + (i + 1) + ": " + MasterTimedTrafficLights.GetStep(i).MinTime + " - " + MasterTimedTrafficLights.GetStep(i).MaxTime, layout);
							}
						} else {
							GUIStyle labelLayout = layout;
							if (Mode == TimedTrafficLightsToolMode.ViewStep && SelectedStepIndex == i) {
								labelLayout = layoutGreen;
							}
							GUILayout.Label(Translation.GetString("State") + " " + (i + 1) + ": " + MasterTimedTrafficLights.GetStep(i).MinTime + " - " + MasterTimedTrafficLights.GetStep(i).MaxTime, labelLayout);

							if (Mode != TimedTrafficLightsToolMode.EditStep) {
								GUILayout.BeginHorizontal(GUILayout.Width(100));

								if (i > 0) {
									if (GUILayout.Button(Translation.GetString("up"), GUILayout.Width(48))) {
										MoveStepUp(i);
									}
								} else {
									GUILayout.Space(50);
								}

								if (i < numSteps - 1) {
									if (GUILayout.Button(Translation.GetString("down"), GUILayout.Width(48))) {
										MoveStepDown(i);
									}
								} else {
									GUILayout.Space(50);
								}

								GUILayout.EndHorizontal();

								if (GUILayout.Button(Translation.GetString("View"), GUILayout.Width(70))) {
									ViewStep(i);
								}

								if (GUILayout.Button(Translation.GetString("Edit"), GUILayout.Width(65))) {
									EditStep(i);
									_stepMinValue = MasterTimedTrafficLights.GetStep(i).MinTime;
									_stepMaxValue = MasterTimedTrafficLights.GetStep(i).MaxTime;
									_stepMetric = MasterTimedTrafficLights.GetStep(i).ChangeMetric;
									_waitFlowBalance = MasterTimedTrafficLights.GetStep(i).WaitFlowBalance;
									_stepMinValueStr = _stepMinValue.ToString();
									_stepMaxValueStr = _stepMaxValue.ToString();
									nodeSelectionLocked = true;
								}

								if (GUILayout.Button(Translation.GetString("Delete"), GUILayout.Width(70))) {
									RemoveStep(i);
								}
							}
						}
					} else {
						nodeSelectionLocked = true;
						int oldStepMinValue = _stepMinValue;
						int oldStepMaxValue = _stepMaxValue;

						// Editing step
						GUILayout.Label(Translation.GetString("Min._Time:"), GUILayout.Width(75));
						_stepMinValueStr = GUILayout.TextField(_stepMinValueStr, GUILayout.Height(20));
						if (!Int32.TryParse(_stepMinValueStr, out _stepMinValue))
							_stepMinValue = oldStepMinValue;

						GUILayout.Label(Translation.GetString("Max._Time:"), GUILayout.Width(75));
						_stepMaxValueStr = GUILayout.TextField(_stepMaxValueStr, GUILayout.Height(20));
						if (!Int32.TryParse(_stepMaxValueStr, out _stepMaxValue))
							_stepMaxValue = oldStepMaxValue;

						if (GUILayout.Button(Translation.GetString("Save"), GUILayout.Width(70))) {
							if (_stepMinValue < 0)
								_stepMinValue = 0;
							if (_stepMaxValue <= 0)
								_stepMaxValue = 1;
							if (_stepMaxValue < _stepMinValue)
								_stepMaxValue = _stepMinValue;
							if (_waitFlowBalance <= 0)
								_waitFlowBalance = GlobalConfig.Instance.TimedTrafficLights.FlowToWaitRatio;

							foreach (var nodeId in MasterTimedTrafficLights.NodeGroup) {
								var step = tlsMan.TrafficLightSimulations[nodeId].TimedLight?.GetStep(SelectedStepIndex);

								if (step != null) {
									step.MinTime = _stepMinValue;
									step.MaxTime = _stepMaxValue;
									step.ChangeMetric = _stepMetric;
									step.WaitFlowBalance = _waitFlowBalance;
									step.UpdateLights();
								}
							}

							ViewStep(SelectedStepIndex);
							nodeSelectionLocked = false;
						}

						GUILayout.EndHorizontal();

						BuildStepChangeMetricDisplay(true);
						BuildFlowPolicyDisplay(true);
						GUILayout.BeginHorizontal();
					}

					GUILayout.EndHorizontal();
				} // foreach step

				GUILayout.BeginHorizontal();

				if (Mode != TimedTrafficLightsToolMode.EditStep && !timedLightActive) {
					if (Mode == TimedTrafficLightsToolMode.AddStep) {
						nodeSelectionLocked = true;
						// new step
						int oldStepMinValue = _stepMinValue;
						int oldStepMaxValue = _stepMaxValue;

						GUILayout.Label(Translation.GetString("Min._Time:"), GUILayout.Width(65));
						_stepMinValueStr = GUILayout.TextField(_stepMinValueStr, GUILayout.Height(20));
						if (!Int32.TryParse(_stepMinValueStr, out _stepMinValue))
							_stepMinValue = oldStepMinValue;

						GUILayout.Label(Translation.GetString("Max._Time:"), GUILayout.Width(65));
						_stepMaxValueStr = GUILayout.TextField(_stepMaxValueStr, GUILayout.Height(20));
						if (!Int32.TryParse(_stepMaxValueStr, out _stepMaxValue))
							_stepMaxValue = oldStepMaxValue;

						if (GUILayout.Button(Translation.GetString("Add"), GUILayout.Width(70))) {
							TrafficManagerTool.ShowAdvisor(this.GetType().Name + "_AddStep");
							if (_stepMinValue < 0)
								_stepMinValue = 0;
							if (_stepMaxValue <= 0)
								_stepMaxValue = 1;
							if (_stepMaxValue < _stepMinValue)
								_stepMaxValue = _stepMinValue;
							if (_waitFlowBalance <= 0)
								_waitFlowBalance = 1f;

							foreach (var nodeId in MasterTimedTrafficLights.NodeGroup) {
								tlsMan.TrafficLightSimulations[nodeId].TimedLight?.AddStep(_stepMinValue, _stepMaxValue, _stepMetric, _waitFlowBalance);
							}

							ViewStep(MasterTimedTrafficLights.NumSteps() - 1);
						}
						if (GUILayout.Button("X", GUILayout.Width(22))) {
							ShowLights();
						}

						GUILayout.EndHorizontal();

						BuildStepChangeMetricDisplay(true);
						BuildFlowPolicyDisplay(true);
						GUILayout.BeginHorizontal();

					} else {
						if (Mode != TimedTrafficLightsToolMode.EditStep) {
							if (GUILayout.Button(Translation.GetString("Add_step"))) {
								TrafficManagerTool.ShowAdvisor(this.GetType().Name + "_AddStep");
								AddStep();
								nodeSelectionLocked = true;
								_stepMetric = StepChangeMetric.Default;
							}
						}
					}
				}

				GUILayout.EndHorizontal();

				GUILayout.Space(5);

				if (numSteps > 1 && Mode != TimedTrafficLightsToolMode.EditStep) {
					if (timedLightActive) {
						if (GUILayout.Button(_timedShowNumbers ? Translation.GetString("Hide_counters") : Translation.GetString("Show_counters"))) {
							_timedShowNumbers = !_timedShowNumbers;
						}

						if (GUILayout.Button(Translation.GetString("Stop"))) {
							foreach (var nodeId in MasterTimedTrafficLights.NodeGroup) {
								tlsMan.TrafficLightSimulations[nodeId].TimedLight?.Stop();
							}
						}

						/*bool isInTestMode = false;
						foreach (var sim in SelectedNodeIndexes.Select(tlsMan.GetNodeSimulation)) {
							if (sim.TimedLight.IsInTestMode()) {
								isInTestMode = true;
								break;
							}
						}*/


						var curStep = MasterTimedTrafficLights.CurrentStep;
						ITimedTrafficLightsStep currentStep = MasterTimedTrafficLights.GetStep(curStep);
						_stepMetric = currentStep.ChangeMetric;
						if (currentStep.MaxTime > currentStep.MinTime) {
							BuildStepChangeMetricDisplay(false);
						}
						
						_waitFlowBalance = MasterTimedTrafficLights.GetStep(curStep).WaitFlowBalance;
						BuildFlowPolicyDisplay(inTestMode);
						foreach (var nodeId in MasterTimedTrafficLights.NodeGroup) {
							var step = tlsMan.TrafficLightSimulations[nodeId].TimedLight?.GetStep(curStep);
							if (step != null) {
								step.WaitFlowBalance = _waitFlowBalance;
							}
						}

						//var mayEnterIfBlocked = GUILayout.Toggle(timedNodeMain.vehiclesMayEnterBlockedJunctions, Translation.GetString("Vehicles_may_enter_blocked_junctions"), new GUILayoutOption[] { });
						var testMode = GUILayout.Toggle(inTestMode, Translation.GetString("Enable_test_mode_(stay_in_current_step)"), new GUILayoutOption[] { });
						foreach (var nodeId in MasterTimedTrafficLights.NodeGroup) {
							tlsMan.TrafficLightSimulations[nodeId].TimedLight?.SetTestMode(testMode);
						}
					} else {
						if (Mode != TimedTrafficLightsToolMode.EditStep && Mode != TimedTrafficLightsToolMode.AddStep) {
							if (GUILayout.Button(Translation.GetString("Start"))) {
								nodeSelectionLocked = false;

								foreach (var nodeId in MasterTimedTrafficLights.NodeGroup) {
									tlsMan.TrafficLightSimulations[nodeId].TimedLight?.Start();
								}
							}
						}
					}
				}

				if (Mode == TimedTrafficLightsToolMode.EditStep) {
					DragWindow(ref _windowRect);
					return;
				}

				GUILayout.Space(30);

				if (MasterTimedTrafficLights.NodeGroup.Count == 1 && MasterTimedTrafficLights.NumSteps() > 0) {
					GUILayout.BeginHorizontal();

					if (GUILayout.Button(Translation.GetString("Rotate_left"))) {
						MasterTimedTrafficLights.RotateLeft();
						ViewStep(0);
					}

					if (GUILayout.Button(Translation.GetString("Copy"))) {
						TrafficManagerTool.ShowAdvisor(this.GetType().Name + "_Copy");
						nodeIdToCopy = MasterTimedTrafficLights.NodeId;
						Mode = TimedTrafficLightsToolMode.Copy;
					}

					if (GUILayout.Button(Translation.GetString("Rotate_right"))) {
						MasterTimedTrafficLights.RotateRight();
						ViewStep(0);
					}

					GUILayout.EndHorizontal();
				}

				if (!timedLightActive) {
					GUILayout.Space(30);

					if (GUILayout.Button(Translation.GetString("Add_junction_to_timed_light"))) {
						TrafficManagerTool.ShowAdvisor(this.GetType().Name + "_AddJunction");
						Mode = TimedTrafficLightsToolMode.AddNode;
					}

					if (MasterTimedTrafficLights.NodeGroup.Count > 1) {
						if (GUILayout.Button(Translation.GetString("Remove_junction_from_timed_light"))) {
							TrafficManagerTool.ShowAdvisor(this.GetType().Name + "_RemoveJunction");
							Mode = TimedTrafficLightsToolMode.RemoveNode;
						}
					}

					GUILayout.Space(30);

					if (GUILayout.Button(Translation.GetString("Remove_timed_traffic_light"))) {
						RemoveTimedTrafficLights();
					}
				}

				DragWindow(ref _windowRect);
			} catch (Exception e) {
				Log.Error($"TimedTrafficLightsTool._guiTimedControlPanel: {e}");
			}
		}

		public override void Cleanup() {
			SelectedNodeId = 0;
			ClearSelectedNodes();

			_timedShowNumbers = false;
			_hoveredNode = 0;
			_timedShowNumbers = false;
			timedLightActive = false;
			nodeIdToCopy = 0;

			Mode = TimedTrafficLightsToolMode.None;
			MasterTimedTrafficLights = null;
			SelectedStepIndex = -1;
		}

		public override void Initialize() {
			base.Initialize();
			Cleanup();
			if (Options.timedLightsOverlay) {
				RefreshCurrentTimedNodeIds();
			} else {
				currentTimedNodeIds.Clear();
			}
		}

		private void BuildStepChangeMetricDisplay(bool editable) {
			GUILayout.BeginVertical();

			if (editable) {
				GUILayout.Label(Translation.GetString("After_min._time_has_elapsed_switch_to_next_step_if") + ":");

				if (GUILayout.Toggle(_stepMetric == StepChangeMetric.Default, GetStepChangeMetricDescription(StepChangeMetric.Default))) {
					_stepMetric = StepChangeMetric.Default;
				}

				if (GUILayout.Toggle(_stepMetric == StepChangeMetric.FirstFlow, GetStepChangeMetricDescription(StepChangeMetric.FirstFlow))) {
					_stepMetric = StepChangeMetric.FirstFlow;
				}

				if (GUILayout.Toggle(_stepMetric == StepChangeMetric.FirstWait, GetStepChangeMetricDescription(StepChangeMetric.FirstWait))) {
					_stepMetric = StepChangeMetric.FirstWait;
				}

				if (GUILayout.Toggle(_stepMetric == StepChangeMetric.NoFlow, GetStepChangeMetricDescription(StepChangeMetric.NoFlow))) {
					_stepMetric = StepChangeMetric.NoFlow;
				}

				if (GUILayout.Toggle(_stepMetric == StepChangeMetric.NoWait, GetStepChangeMetricDescription(StepChangeMetric.NoWait))) {
					_stepMetric = StepChangeMetric.NoWait;
				}
			} else {
				GUILayout.Label(Translation.GetString("Adaptive_step_switching") + ": " + GetStepChangeMetricDescription(_stepMetric));
			}

			GUILayout.EndVertical();
		}

		private void BuildFlowPolicyDisplay(bool editable) {
			string formatStr;
			if (_waitFlowBalance < 0.01f)
				formatStr = "{0:0.###}";
			else if (_waitFlowBalance < 0.1f)
				formatStr = "{0:0.##}";
			else
				formatStr = "{0:0.#}";

			GUILayout.BeginHorizontal();
			if (editable) {
				GUILayout.Label(Translation.GetString("Sensitivity") + " (" + String.Format(formatStr, _waitFlowBalance) + ", " + getWaitFlowBalanceInfo() + "):");
				if (_waitFlowBalance <= 0.01f) {
					if (_waitFlowBalance >= 0) {
						if (GUILayout.Button("-.001")) {
							_waitFlowBalance -= 0.001f;
						}
					}
					if (_waitFlowBalance < 0.01f) {
						if (GUILayout.Button("+.001")) {
							_waitFlowBalance += 0.001f;
						}
					}
				} else if (_waitFlowBalance <= 0.1f) {
					if (GUILayout.Button("-.01")) {
						_waitFlowBalance -= 0.01f;
					}
					if (_waitFlowBalance < 0.1f) {
						if (GUILayout.Button("+.01")) {
							_waitFlowBalance += 0.01f;
						}
					}
				}
				if (_waitFlowBalance < 0)
					_waitFlowBalance = 0;
				if (_waitFlowBalance > 10)
					_waitFlowBalance = 10;
				GUILayout.EndHorizontal();

				_waitFlowBalance = GUILayout.HorizontalSlider(_waitFlowBalance, 0.001f, 10f);

				// step snapping
				if (_waitFlowBalance < 0.001f) {
					_waitFlowBalance = 0.001f;
				} else if (_waitFlowBalance < 0.01f) {
					_waitFlowBalance = Mathf.Round(_waitFlowBalance * 1000f) * 0.001f;
				} else if (_waitFlowBalance < 0.1f) {
					_waitFlowBalance = Mathf.Round(_waitFlowBalance * 100f) * 0.01f;
				} else if (_waitFlowBalance < 10f) {
					_waitFlowBalance = Mathf.Round(_waitFlowBalance * 10f) * 0.1f;
				} else {
					_waitFlowBalance = 10f;
				}

				GUILayout.BeginHorizontal();
				GUIStyle style = new GUIStyle();
				style.normal.textColor = Color.white;
				style.alignment = TextAnchor.LowerLeft;
				GUILayout.Label(Translation.GetString("Low"), style, new GUILayoutOption[] { GUILayout.Height(10) });
				style.alignment = TextAnchor.LowerRight;
				GUILayout.Label(Translation.GetString("High"), style, new GUILayoutOption[] { GUILayout.Height(10) });
			} else {
				GUILayout.Label(Translation.GetString("Sensitivity") + ": " + String.Format(formatStr, _waitFlowBalance) + " (" + getWaitFlowBalanceInfo() + ")");
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(5);
		}

		private string GetStepChangeMetricDescription(StepChangeMetric metric) {
			switch (metric) {
				case StepChangeMetric.Default:
				default:
					return Translation.GetString("flow_ratio") + " < " + Translation.GetString("wait_ratio") + " (" + Translation.GetString("default") + ")";
				case StepChangeMetric.FirstFlow:
					return Translation.GetString("flow_ratio") + " > 0";
				case StepChangeMetric.FirstWait:
					return Translation.GetString("wait_ratio") + " > 0";
				case StepChangeMetric.NoFlow:
					return Translation.GetString("flow_ratio") + " = 0";
				case StepChangeMetric.NoWait:
					return Translation.GetString("wait_ratio") + " = 0";
			}
		}

		private void _guiTimedTrafficLightsNode() {
			_cursorInSecondaryPanel = false;

			_windowRect2 = GUILayout.Window(252, _windowRect2, _guiTimedTrafficLightsNodeWindow, Translation.GetString("Select_nodes_windowTitle"), WindowStyle);

			_cursorInSecondaryPanel = _windowRect2.Contains(Event.current.mousePosition);
		}

		private void _guiTimedTrafficLights() {
			TrafficLightSimulationManager tlsMan = TrafficLightSimulationManager.Instance;
			CustomSegmentLightsManager customTrafficLightsManager = CustomSegmentLightsManager.Instance;
			TrafficPriorityManager prioMan = TrafficPriorityManager.Instance;

			_cursorInSecondaryPanel = false;

			_windowRect = GUILayout.Window(253, _windowRect, _guiTimedControlPanel, Translation.GetString("Timed_traffic_lights_manager"), WindowStyle);

			_cursorInSecondaryPanel = _windowRect.Contains(Event.current.mousePosition);

			GUI.matrix = Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.identity, new Vector3(1, 1, 1)); // revert scaling
			ShowGUI();
		}

		private void _guiTimedTrafficLightsCopy() {
			_cursorInSecondaryPanel = false;

			_windowRect2 = GUILayout.Window(255, _windowRect2, _guiTimedTrafficLightsPasteWindow, Translation.GetString("Paste"), WindowStyle);

			_cursorInSecondaryPanel = _windowRect2.Contains(Event.current.mousePosition);
		}

		private void _guiTimedTrafficLightsPasteWindow(int num) {
			GUILayout.Label(Translation.GetString("Select_junction"));
		}

		private void _guiTimedTrafficLightsNodeWindow(int num) {
			if (SelectedNodeIds.Count < 1) {
				GUILayout.Label(Translation.GetString("Select_nodes"));
			} else {
				var txt = SelectedNodeIds.Aggregate("", (current, t) => current + (Translation.GetString("Node") + " " + t + "\n"));

				GUILayout.Label(txt);

				if (SelectedNodeIds.Count > 0 && GUILayout.Button(Translation.GetString("Deselect_all_nodes"))) {
					ClearSelectedNodes();
				}
				if (!GUILayout.Button(Translation.GetString("Setup_timed_traffic_light"))) return;

				SetupTimedTrafficLights();
			}

			DragWindow(ref _windowRect2);
		}

		// ===== v1.11 REFACTORED METHODS *START* =====

		public void SetupTimedTrafficLights() {
			if (SelectedNodeIds.Count <= 0) {
				Log.Error($"TimedTrafficLightsTool.SetupTimedTrafficLights(): Error: SelectedNodeIds.Count={SelectedNodeIds.Count}");
				return;
			}

			TrafficLightSimulationManager tlsMan = TrafficLightSimulationManager.Instance;

			_waitFlowBalance = GlobalConfig.Instance.TimedTrafficLights.FlowToWaitRatio;
			foreach (var nodeId in SelectedNodeIds) {
				tlsMan.SetUpTimedTrafficLight(nodeId, SelectedNodeIds);
				RefreshCurrentTimedNodeIds(nodeId);
			}

			MasterTimedTrafficLights = tlsMan.TrafficLightSimulations[SelectedNodeIds[0]].TimedLight.MasterLights();
			ClearSelectedNodes();
			ShowLights();
		}

		public void RemoveTimedTrafficLights() {
			if (MasterTimedTrafficLights == null || MasterTimedTrafficLights.NodeGroup.Count <= 0) {
				Log.Error($"TimedTrafficLightsTool.RemoveTimedTrafficLights(): Error: MasterTimedTrafficLights={MasterTimedTrafficLights}, MasterTimedTrafficLights.NodeGroup.Count={MasterTimedTrafficLights?.NodeGroup?.Count}");
				return;
			}

			TrafficLightSimulationManager tlsMan = TrafficLightSimulationManager.Instance;

			foreach (var nodeId in MasterTimedTrafficLights.NodeGroup) {
				tlsMan.RemoveNodeFromSimulation(nodeId, true, false);
				RefreshCurrentTimedNodeIds(nodeId);
			}
			SelectNode();
		}

		public void MoveStepUp(int stepIndex) {
			if (MasterTimedTrafficLights == null || stepIndex <= 0 || stepIndex >= MasterTimedTrafficLights.NumSteps()) {
				Log.Error($"TimedTrafficLightsTool.MoveStepUp({stepIndex}): Error: MasterTimedTrafficLights={MasterTimedTrafficLights}, stepIndex={stepIndex}, MasterTimedTrafficLights.NumSteps()={MasterTimedTrafficLights?.NumSteps()}");
				return;
			}

			foreach (var nodeId in MasterTimedTrafficLights.NodeGroup) {
				TrafficLightSimulationManager.Instance.TrafficLightSimulations[nodeId].TimedLight?.MoveStep(stepIndex, stepIndex - 1);
			}
			ViewStep(stepIndex - 1);
		}

		public void MoveStepDown(int stepIndex) {
			if (MasterTimedTrafficLights == null || stepIndex < 0 || stepIndex >= MasterTimedTrafficLights.NumSteps() - 1) {
				Log.Error($"TimedTrafficLightsTool.MoveStepUp({stepIndex}): Error: MasterTimedTrafficLights={MasterTimedTrafficLights}, stepIndex={stepIndex}, MasterTimedTrafficLights.NumSteps()={MasterTimedTrafficLights?.NumSteps()}");
				return;
			}

			foreach (var nodeId in MasterTimedTrafficLights.NodeGroup) {
				TrafficLightSimulationManager.Instance.TrafficLightSimulations[nodeId].TimedLight?.MoveStep(stepIndex, stepIndex + 1);
			}
			ViewStep(stepIndex + 1);
		}

		public void ViewStep(int stepIndex) {
			if (MasterTimedTrafficLights == null || stepIndex < 0 || stepIndex >= MasterTimedTrafficLights.NumSteps()) {
				Log.Error($"TimedTrafficLightsTool.ViewStep({stepIndex}): Error: MasterTimedTrafficLights={MasterTimedTrafficLights}, stepIndex={stepIndex}, MasterTimedTrafficLights.NumSteps()={MasterTimedTrafficLights?.NumSteps()}");
				return;
			}

			Mode = TimedTrafficLightsToolMode.ViewStep;
			SelectedStepIndex = stepIndex;

			foreach (var nodeId in MasterTimedTrafficLights.NodeGroup) {
				TrafficLightSimulationManager.Instance.TrafficLightSimulations[nodeId].TimedLight?.GetStep(stepIndex).UpdateLiveLights(true);
			}
		}

		public void EditStep(int stepIndex) {
			if (MasterTimedTrafficLights == null || stepIndex < 0 || stepIndex >= MasterTimedTrafficLights.NumSteps()) {
				Log.Error($"TimedTrafficLightsTool.EditStep({stepIndex}): Error: MasterTimedTrafficLights={MasterTimedTrafficLights}, stepIndex={stepIndex}, MasterTimedTrafficLights.NumSteps()={MasterTimedTrafficLights?.NumSteps()}");
				return;
			}

			Mode = TimedTrafficLightsToolMode.EditStep;
			SelectedStepIndex = stepIndex;

			foreach (var nodeId in MasterTimedTrafficLights.NodeGroup) {
				TrafficLightSimulationManager.Instance.TrafficLightSimulations[nodeId].TimedLight?.GetStep(stepIndex).UpdateLiveLights(true);
			}
		}

		public void AddStep() {
			if (MasterTimedTrafficLights == null) {
				Log.Error($"TimedTrafficLightsTool.AddStep(): Error: MasterTimedTrafficLights={MasterTimedTrafficLights}");
				return;
			}

			Mode = TimedTrafficLightsToolMode.AddStep;
			SelectedStepIndex = -1;
		}

		public void RemoveStep(int stepIndex) {
			if (MasterTimedTrafficLights == null || stepIndex < 0 || stepIndex >= MasterTimedTrafficLights.NumSteps()) {
				Log.Error($"TimedTrafficLightsTool.RemoveStep({stepIndex}): Error: MasterTimedTrafficLights={MasterTimedTrafficLights}, stepIndex={stepIndex}, MasterTimedTrafficLights.NumSteps()={MasterTimedTrafficLights?.NumSteps()}");
				return;
			}

			foreach (var nodeId in MasterTimedTrafficLights.NodeGroup) {
				TrafficLightSimulationManager.Instance.TrafficLightSimulations[nodeId].TimedLight?.RemoveStep(stepIndex);
			}
			
			if (MasterTimedTrafficLights.NumSteps() > 0) {
				ViewStep(Math.Min(stepIndex, MasterTimedTrafficLights.NumSteps() - 1));
			} else {
				ShowLights();
			}
		}

		public void SelectNode() {
			Cleanup();
			Mode = TimedTrafficLightsToolMode.SelectNode;
		}

		public void ShowLights(ushort? nodeId = null) {
			if (nodeId != null) {
				ITimedTrafficLights timedLight = TrafficLightSimulationManager.Instance.TrafficLightSimulations[(ushort)nodeId].TimedLight;

				if (timedLight != null) {
					MasterTimedTrafficLights = timedLight.MasterLights();
				}
			}

			if (MasterTimedTrafficLights == null) {
				Log.Error($"TimedTrafficLightsTool.ShowLights({nodeId}): Error: MasterTimedTrafficLights={MasterTimedTrafficLights}");
				SelectNode();
				return;
			}

			Mode = TimedTrafficLightsToolMode.ShowLights;
			SelectedStepIndex = -1;
		}

		// ===== v1.11 REFACTORED METHODS *END* =====

		private string getWaitFlowBalanceInfo() {
			if (_waitFlowBalance < 0.1f) {
				return Translation.GetString("Extreme_long_green/red_phases");
			} else if (_waitFlowBalance < 0.5f) {
				return Translation.GetString("Very_long_green/red_phases");
			} else if (_waitFlowBalance < 0.75f) {
				return Translation.GetString("Long_green/red_phases");
			} else if (_waitFlowBalance < 1.25f) {
				return Translation.GetString("Moderate_green/red_phases");
			} else if (_waitFlowBalance < 1.5f) {
				return Translation.GetString("Short_green/red_phases");
			} else if (_waitFlowBalance < 2.5f) {
				return Translation.GetString("Very_short_green/red_phases");
			} else {
				return Translation.GetString("Extreme_short_green/red_phases");
			}
		}

		public void AddSelectedNode(ushort node) {
			SelectedNodeIds.Add(node);
		}

		public bool IsNodeSelected(ushort node) {
			return SelectedNodeIds.Contains(node);
		}

		public void RemoveSelectedNode(ushort node) {
			SelectedNodeIds.Remove(node);
		}

		public void ClearSelectedNodes() {
			SelectedNodeIds.Clear();
		}

		private void drawStraightLightTexture(RoadBaseAI.TrafficLightState state, Rect rect) {
			switch (state) {
				case RoadBaseAI.TrafficLightState.Green:
					GUI.DrawTexture(rect, TextureResources.GreenLightStraightTexture2D);
					break;
				case RoadBaseAI.TrafficLightState.GreenToRed:
					GUI.DrawTexture(rect, TextureResources.YellowLightTexture2D);
					break;
				case RoadBaseAI.TrafficLightState.Red:
				default:
					GUI.DrawTexture(rect, TextureResources.RedLightStraightTexture2D);
					break;
				case RoadBaseAI.TrafficLightState.RedToGreen:
					GUI.DrawTexture(rect, TextureResources.YellowLightStraightTexture2D);
					break;
			}
		}

		private void drawForwardLeftLightTexture(RoadBaseAI.TrafficLightState state, Rect rect) {
			switch (state) {
				case RoadBaseAI.TrafficLightState.Green:
					GUI.DrawTexture(rect, TextureResources.GreenLightForwardLeftTexture2D);
					break;
				case RoadBaseAI.TrafficLightState.GreenToRed:
					GUI.DrawTexture(rect, TextureResources.YellowLightTexture2D);
					break;
				case RoadBaseAI.TrafficLightState.Red:
				default:
					GUI.DrawTexture(rect, TextureResources.RedLightForwardLeftTexture2D);
					break;
				case RoadBaseAI.TrafficLightState.RedToGreen:
					GUI.DrawTexture(rect, TextureResources.YellowLightForwardLeftTexture2D);
					break;
			}
		}

		private void drawForwardRightLightTexture(RoadBaseAI.TrafficLightState state, Rect rect) {
			switch (state) {
				case RoadBaseAI.TrafficLightState.Green:
					GUI.DrawTexture(rect, TextureResources.GreenLightForwardRightTexture2D);
					break;
				case RoadBaseAI.TrafficLightState.GreenToRed:
					GUI.DrawTexture(rect, TextureResources.YellowLightTexture2D);
					break;
				case RoadBaseAI.TrafficLightState.Red:
				default:
					GUI.DrawTexture(rect, TextureResources.RedLightForwardRightTexture2D);
					break;
				case RoadBaseAI.TrafficLightState.RedToGreen:
					GUI.DrawTexture(rect, TextureResources.YellowLightForwardRightTexture2D);
					break;
			}
		}

		private void drawLeftLightTexture(RoadBaseAI.TrafficLightState state, Rect rect) {
			switch (state) {
				case RoadBaseAI.TrafficLightState.Green:
					GUI.DrawTexture(rect, TextureResources.GreenLightLeftTexture2D);
					break;
				case RoadBaseAI.TrafficLightState.GreenToRed:
					GUI.DrawTexture(rect, TextureResources.YellowLightTexture2D);
					break;
				case RoadBaseAI.TrafficLightState.Red:
				default:
					GUI.DrawTexture(rect, TextureResources.RedLightLeftTexture2D);
					break;
				case RoadBaseAI.TrafficLightState.RedToGreen:
					GUI.DrawTexture(rect, TextureResources.YellowLightLeftTexture2D);
					break;
			}
		}

		private void drawRightLightTexture(RoadBaseAI.TrafficLightState state, Rect rect) {
			switch (state) {
				case RoadBaseAI.TrafficLightState.Green:
					GUI.DrawTexture(rect, TextureResources.GreenLightRightTexture2D);
					break;
				case RoadBaseAI.TrafficLightState.GreenToRed:
					GUI.DrawTexture(rect, TextureResources.YellowLightTexture2D);
					break;
				case RoadBaseAI.TrafficLightState.Red:
				default:
					GUI.DrawTexture(rect, TextureResources.RedLightRightTexture2D);
					break;
				case RoadBaseAI.TrafficLightState.RedToGreen:
					GUI.DrawTexture(rect, TextureResources.YellowLightRightTexture2D);
					break;
			}
		}

		private void drawMainLightTexture(RoadBaseAI.TrafficLightState state, Rect rect) {
			switch (state) {
				case RoadBaseAI.TrafficLightState.Green:
					GUI.DrawTexture(rect, TextureResources.GreenLightTexture2D);
					break;
				case RoadBaseAI.TrafficLightState.GreenToRed:
					GUI.DrawTexture(rect, TextureResources.YellowLightTexture2D);
					break;
				case RoadBaseAI.TrafficLightState.Red:
				default:
					GUI.DrawTexture(rect, TextureResources.RedLightTexture2D);
					break;
				case RoadBaseAI.TrafficLightState.RedToGreen:
					GUI.DrawTexture(rect, TextureResources.YellowRedLightTexture2D);
					break;
			}
		}

		public override void ShowGUIOverlay(ToolMode toolMode, bool viewOnly) {
			if (! ToolMode.TimedLights.Equals(toolMode)) {
				// TODO refactor timed light related tool modes to sub tool modes
				return;
			}
			if (viewOnly && !Options.timedLightsOverlay)
				return;

			Vector3 camPos = Singleton<SimulationManager>.instance.m_simulationView.m_position;

			TrafficLightSimulationManager tlsMan = TrafficLightSimulationManager.Instance;

			foreach (ushort nodeId in currentTimedNodeIds) {
				if (!Constants.ServiceFactory.NetService.IsNodeValid((ushort)nodeId)) {
					continue;
				}

				if (MasterTimedTrafficLights != null && MasterTimedTrafficLights.NodeGroup.Contains((ushort)nodeId)) {
					continue;
				}

				if (tlsMan.HasTimedSimulation((ushort)nodeId)) {
					ITimedTrafficLights timedNode = tlsMan.TrafficLightSimulations[nodeId].TimedLight;

					var nodePos = Singleton<NetManager>.instance.m_nodes.m_buffer[nodeId].m_position;

					Texture2D tex = timedNode.IsStarted() ? (timedNode.IsInTestMode() ? TextureResources.ClockTestTexture2D : TextureResources.ClockPlayTexture2D) : TextureResources.ClockPauseTexture2D;
					MainTool.DrawGenericSquareOverlayTexture(tex, camPos, nodePos, 120f, false);
				}
			}
		}

		private void ShowGUI() {
			if (MasterTimedTrafficLights == null) {
				return;
			}

			TrafficLightSimulationManager tlsMan = TrafficLightSimulationManager.Instance;
			CustomSegmentLightsManager customTrafficLightsManager = CustomSegmentLightsManager.Instance;
			JunctionRestrictionsManager junctionRestrictionsManager = JunctionRestrictionsManager.Instance;

			var hoveredSegment = false;
			bool addOrEdit = Mode == TimedTrafficLightsToolMode.AddStep || Mode == TimedTrafficLightsToolMode.EditStep;

			foreach (var nodeId in MasterTimedTrafficLights.NodeGroup) {
				ITimedTrafficLights timedNode = tlsMan.TrafficLightSimulations[nodeId].TimedLight;

				var nodePos = Singleton<NetManager>.instance.m_nodes.m_buffer[nodeId].m_position;

				Vector3 nodeScreenPos;
				bool nodeVisible = MainTool.WorldToScreenPoint(nodePos, out nodeScreenPos);

				if (!nodeVisible)
					continue;

				var diff = nodePos - Camera.main.transform.position;
				var zoom = 1.0f / diff.magnitude * 100f * MainTool.GetBaseZoom();

				NodeGeometry nodeGeometry = NodeGeometry.Get(nodeId);
				foreach (SegmentEndGeometry end in nodeGeometry.SegmentEndGeometries) {
					if (end == null)
						continue;
					ushort srcSegmentId = end.SegmentId; // source segment

					ICustomSegmentLights liveSegmentLights = customTrafficLightsManager.GetSegmentLights(srcSegmentId, end.StartNode, false);
					if (liveSegmentLights == null)
						continue;

					bool showPedLight = liveSegmentLights.PedestrianLightState != null && junctionRestrictionsManager.IsPedestrianCrossingAllowed(liveSegmentLights.SegmentId, liveSegmentLights.StartNode);

					var timedActive = timedNode.IsStarted();
					if (! timedActive) {
						liveSegmentLights.MakeRedOrGreen();
					}

					var offset = 17f;
					Vector3 segmentLightPos = nodePos;

					if (Singleton<NetManager>.instance.m_segments.m_buffer[srcSegmentId].m_startNode == nodeId) {
						segmentLightPos.x += Singleton<NetManager>.instance.m_segments.m_buffer[srcSegmentId].m_startDirection.x * offset;
						segmentLightPos.y += Singleton<NetManager>.instance.m_segments.m_buffer[srcSegmentId].m_startDirection.y;
						segmentLightPos.z += Singleton<NetManager>.instance.m_segments.m_buffer[srcSegmentId].m_startDirection.z * offset;
					} else {
						segmentLightPos.x += Singleton<NetManager>.instance.m_segments.m_buffer[srcSegmentId].m_endDirection.x * offset;
						segmentLightPos.y += Singleton<NetManager>.instance.m_segments.m_buffer[srcSegmentId].m_endDirection.y;
						segmentLightPos.z += Singleton<NetManager>.instance.m_segments.m_buffer[srcSegmentId].m_endDirection.z * offset;
					}

					Vector3 screenPos;
					bool segmentLightVisible = MainTool.WorldToScreenPoint(segmentLightPos, out screenPos);

					if (!segmentLightVisible)
						continue;

					var guiColor = GUI.color;

					var manualPedestrianWidth = 36f * zoom;
					var manualPedestrianHeight = 35f * zoom;

					var pedestrianWidth = 36f * zoom;
					var pedestrianHeight = 61f * zoom;

					// original / 2.5
					var lightWidth = 41f * zoom;
					var lightHeight = 97f * zoom;

					// SWITCH MODE BUTTON
					var modeWidth = 41f * zoom;
					var modeHeight = 38f * zoom;

					if (showPedLight) {
						// pedestrian light

						// SWITCH MANUAL PEDESTRIAN LIGHT BUTTON
						if (!timedActive && addOrEdit) {
							guiColor.a = MainTool.GetHandleAlpha(_hoveredButton[0] == srcSegmentId &&
										 (_hoveredButton[1] == 1 || _hoveredButton[1] == 2) &&
										 _hoveredNode == nodeId);
							GUI.color = guiColor;

							var myRect2 = new Rect(screenPos.x - manualPedestrianWidth / 2 - (addOrEdit ? lightWidth : 0) + 5f * zoom,
								screenPos.y - manualPedestrianHeight / 2 - 9f * zoom, manualPedestrianWidth,
								manualPedestrianHeight);

							GUI.DrawTexture(myRect2, liveSegmentLights.ManualPedestrianMode ? TextureResources.PedestrianModeManualTexture2D : TextureResources.PedestrianModeAutomaticTexture2D);

							if (myRect2.Contains(Event.current.mousePosition) && !IsCursorInPanel()) {
								_hoveredButton[0] = srcSegmentId;
								_hoveredButton[1] = 1;
								_hoveredNode = nodeId;
								hoveredSegment = true;

								if (MainTool.CheckClicked()) {
									liveSegmentLights.ManualPedestrianMode = !liveSegmentLights.ManualPedestrianMode;
								}
							}
						}

						// SWITCH PEDESTRIAN LIGHT
						guiColor.a = MainTool.GetHandleAlpha(_hoveredButton[0] == srcSegmentId && _hoveredButton[1] == 2 && _hoveredNode == nodeId);

						GUI.color = guiColor;

						var myRect3 = new Rect(screenPos.x - pedestrianWidth / 2 - (addOrEdit ? lightWidth : 0) + 5f * zoom, screenPos.y - pedestrianHeight / 2 + 22f * zoom, pedestrianWidth, pedestrianHeight);

						switch (liveSegmentLights.PedestrianLightState) {
							case RoadBaseAI.TrafficLightState.Green:
								GUI.DrawTexture(myRect3, TextureResources.PedestrianGreenLightTexture2D);
								break;
							case RoadBaseAI.TrafficLightState.Red:
							default:
								GUI.DrawTexture(myRect3, TextureResources.PedestrianRedLightTexture2D);
								break;
						}

						if (myRect3.Contains(Event.current.mousePosition) && !IsCursorInPanel()) {
							_hoveredButton[0] = srcSegmentId;
							_hoveredButton[1] = 2;
							_hoveredNode = nodeId;
							hoveredSegment = true;

							if (MainTool.CheckClicked() && !timedActive && addOrEdit) {
								if (!liveSegmentLights.ManualPedestrianMode) {
									liveSegmentLights.ManualPedestrianMode = true;
								} else {
									liveSegmentLights.ChangeLightPedestrian();
								}
							}
						}
					}

					int lightOffset = -1;
					foreach (ExtVehicleType vehicleType in liveSegmentLights.VehicleTypes) {
						HashSet<byte> laneIndices = new HashSet<byte>();
						for (byte laneIndex = 0; laneIndex < liveSegmentLights.VehicleTypeByLaneIndex.Length; ++laneIndex) {
							if (liveSegmentLights.VehicleTypeByLaneIndex[laneIndex] == vehicleType) {
								laneIndices.Add(laneIndex);
							}
						}
						//Log._Debug($"Traffic light @ seg. {srcSegmentId} node {nodeId}. Lane indices for vehicleType {vehicleType}: {string.Join(",", laneIndices.Select(x => x.ToString()).ToArray())}");

						++lightOffset;
						ICustomSegmentLight liveSegmentLight = liveSegmentLights.GetCustomLight(vehicleType);

						Vector3 offsetScreenPos = screenPos;
						offsetScreenPos.y -= (lightHeight + 10f * zoom) * lightOffset;

						if (!timedActive && addOrEdit) {
							guiColor.a = MainTool.GetHandleAlpha(_hoveredButton[0] == srcSegmentId && _hoveredButton[1] == -1 &&
										 _hoveredNode == nodeId);							
							GUI.color = guiColor;

							var myRect1 = new Rect(offsetScreenPos.x - modeWidth / 2,
								offsetScreenPos.y - modeHeight / 2 + modeHeight - 7f * zoom, modeWidth, modeHeight);

							GUI.DrawTexture(myRect1, TextureResources.LightModeTexture2D);
							
							if (myRect1.Contains(Event.current.mousePosition) && !IsCursorInPanel()) {
								_hoveredButton[0] = srcSegmentId;
								_hoveredButton[1] = -1;
								_hoveredNode = nodeId;
								hoveredSegment = true;

								if (MainTool.CheckClicked()) {
									liveSegmentLight.ToggleMode();
									timedNode.ChangeLightMode(srcSegmentId, vehicleType, liveSegmentLight.CurrentMode);
								}
							}
						}

						if (vehicleType != ExtVehicleType.None) {
							// Info sign
							var infoWidth = 56.125f * zoom;
							var infoHeight = 51.375f * zoom;

							int numInfos = 0;
							for (int k = 0; k < TrafficManagerTool.InfoSignsToDisplay.Length; ++k) {
								if ((TrafficManagerTool.InfoSignsToDisplay[k] & vehicleType) == ExtVehicleType.None)
									continue;
								var infoRect = new Rect(offsetScreenPos.x + modeWidth / 2f + 7f * zoom * (float)(numInfos + 1) + infoWidth * (float)numInfos, offsetScreenPos.y - infoHeight / 2f, infoWidth, infoHeight);
								guiColor.a = MainTool.GetHandleAlpha(false);
								GUI.DrawTexture(infoRect, TextureResources.VehicleInfoSignTextures[TrafficManagerTool.InfoSignsToDisplay[k]]);
								++numInfos;
							}
						}

						// Draw light index
						/*if (!timedActive && _timedEditStep < 0 && lightOffset == 0) {
							var indexSize = 20f * zoom;
							var yOffset = indexSize + 77f * zoom - modeHeight * 2;
							//var carNumRect = new Rect(offsetScreenPos.x, offsetScreenPos.y - yOffset, counterSize, counterSize);
							var segIndexRect = new Rect(offsetScreenPos.x, offsetScreenPos.y - yOffset - indexSize - 2f, indexSize, indexSize);

							_counterStyle.fontSize = (int)(15f * zoom);
							_counterStyle.normal.textColor = new Color(0f, 0f, 1f);
							GUI.Label(segIndexRect, $"#{liveSegmentLight.ClockwiseIndex+1}", _counterStyle);
						}*/

#if DEBUG
						if (timedActive /*&& _timedShowNumbers*/) {
							//var prioSeg = TrafficPriorityManager.Instance.GetPrioritySegment(nodeId, srcSegmentId);

							var counterSize = 20f * zoom;
							var yOffset = counterSize + 77f * zoom - modeHeight * 2;
							//var carNumRect = new Rect(offsetScreenPos.x, offsetScreenPos.y - yOffset, counterSize, counterSize);
							var segIdRect = new Rect(offsetScreenPos.x, offsetScreenPos.y - yOffset - counterSize - 2f, counterSize, counterSize);

							_counterStyle.fontSize = (int)(15f * zoom);
							_counterStyle.normal.textColor = new Color(1f, 0f, 0f);

							/*String labelStr = "n/a";
							if (prioSeg != null) {
								labelStr = prioSeg.GetRegisteredVehicleCount(laneIndices).ToString() + " " + Translation.GetString("incoming");
							}
							GUI.Label(carNumRect, labelStr, _counterStyle);*/

							_counterStyle.normal.textColor = new Color(1f, 0f, 0f);
							GUI.Label(segIdRect, Translation.GetString("Segment") + " " + srcSegmentId, _counterStyle);
						}
#endif

						if (lightOffset == 0 && showPedLight) {
							// PEDESTRIAN COUNTER
							if (timedActive && _timedShowNumbers) {
								var counterSize = 20f * zoom;

								var counter = timedNode.CheckNextChange(srcSegmentId, end.StartNode, vehicleType, 3);

								float numOffset;

								if (liveSegmentLights.PedestrianLightState == RoadBaseAI.TrafficLightState.Red) { // TODO check this
									numOffset = counterSize + 53f * zoom - modeHeight * 2;
								} else {
									numOffset = counterSize + 29f * zoom - modeHeight * 2;
								}

								var myRectCounterNum =
									new Rect(offsetScreenPos.x - counterSize + 15f * zoom + (counter >= 10 ? (counter >= 100 ? -10 * zoom : -5 * zoom) : 1f) + 24f * zoom - pedestrianWidth / 2,
										offsetScreenPos.y - numOffset, counterSize, counterSize);

								_counterStyle.fontSize = (int)(15f * zoom);
								_counterStyle.normal.textColor = new Color(1f, 1f, 1f);

								GUI.Label(myRectCounterNum, counter.ToString(), _counterStyle);

								if (myRectCounterNum.Contains(Event.current.mousePosition) && !IsCursorInPanel()) {
									_hoveredButton[0] = srcSegmentId;
									_hoveredButton[1] = 2;
									_hoveredNode = nodeId;
									hoveredSegment = true;
								}
							}
						}

						SegmentGeometry geometry = SegmentGeometry.Get(srcSegmentId);
						if (geometry == null) {
							Log.Error($"TimedTrafficLightsTool.ShowGUI: No geometry information available for segment {srcSegmentId}");
							continue;
						}
						bool startNode = geometry.StartNodeId() == nodeId;
						if (geometry.IsOutgoingOneWay(startNode))
							continue;

						var hasOutgoingLeftSegment = geometry.HasOutgoingLeftSegment(startNode);
						var hasOutgoingForwardSegment = geometry.HasOutgoingStraightSegment(startNode);
						var hasOutgoingRightSegment = geometry.HasOutgoingRightSegment(startNode);

						/*var hasLeftSegment = geometry.HasLeftSegment(startNode);
						var hasForwardSegment = geometry.HasStraightSegment(startNode);
						var hasRightSegment = geometry.HasRightSegment(startNode);*/

						bool hasOtherLight = false;
						switch (liveSegmentLight.CurrentMode) {
							case LightMode.Simple: {
									// no arrow light
									guiColor.a = MainTool.GetHandleAlpha(_hoveredButton[0] == srcSegmentId && _hoveredButton[1] == 3 && _hoveredNode == nodeId);

									GUI.color = guiColor;

									var myRect4 =
										new Rect(offsetScreenPos.x - lightWidth / 2 - (addOrEdit ? lightWidth : 0) - pedestrianWidth + 5f * zoom,
											offsetScreenPos.y - lightHeight / 2, lightWidth, lightHeight);

									drawMainLightTexture(liveSegmentLight.LightMain, myRect4);

									if (myRect4.Contains(Event.current.mousePosition) && !IsCursorInPanel()) {
										_hoveredButton[0] = srcSegmentId;
										_hoveredButton[1] = 3;
										_hoveredNode = nodeId;
										hoveredSegment = true;

										if (MainTool.CheckClicked() && !timedActive && addOrEdit) {
											liveSegmentLight.ChangeMainLight();
										}
									}

									// COUNTER
									if (timedActive && _timedShowNumbers) {
										var counterSize = 20f * zoom;

										var counter = timedNode.CheckNextChange(srcSegmentId, end.StartNode, vehicleType, 0);

										float numOffset;

										if (liveSegmentLight.LightMain == RoadBaseAI.TrafficLightState.Red) {
											numOffset = counterSize + 96f * zoom - modeHeight * 2;
										} else {
											numOffset = counterSize + 40f * zoom - modeHeight * 2;
										}

										var myRectCounterNum =
											new Rect(offsetScreenPos.x - counterSize + 15f * zoom + (counter >= 10 ? (counter >= 100 ? -10 * zoom : -5 * zoom) : 0f) - pedestrianWidth + 5f * zoom,
												offsetScreenPos.y - numOffset, counterSize, counterSize);

										_counterStyle.fontSize = (int)(18f * zoom);
										_counterStyle.normal.textColor = new Color(1f, 1f, 1f);

										GUI.Label(myRectCounterNum, counter.ToString(), _counterStyle);

										if (myRectCounterNum.Contains(Event.current.mousePosition) && !IsCursorInPanel()) {
											_hoveredButton[0] = srcSegmentId;
											_hoveredButton[1] = 3;
											_hoveredNode = nodeId;
											hoveredSegment = true;
										}
									}

									GUI.color = guiColor;
								}
								break;
							case LightMode.SingleLeft:
								if (hasOutgoingLeftSegment) {
									// left arrow light
									guiColor.a = MainTool.GetHandleAlpha(_hoveredButton[0] == srcSegmentId && _hoveredButton[1] == 3 && _hoveredNode == nodeId);

									GUI.color = guiColor;

									var myRect4 =
										new Rect(offsetScreenPos.x - lightWidth / 2 - (addOrEdit ? lightWidth * 2 : lightWidth) - pedestrianWidth + 5f * zoom,
											offsetScreenPos.y - lightHeight / 2, lightWidth, lightHeight);

									drawLeftLightTexture(liveSegmentLight.LightLeft, myRect4);

									if (myRect4.Contains(Event.current.mousePosition) && !IsCursorInPanel()) {
										_hoveredButton[0] = srcSegmentId;
										_hoveredButton[1] = 3;
										_hoveredNode = nodeId;
										hoveredSegment = true;

										if (MainTool.CheckClicked() && !timedActive && addOrEdit) {
											liveSegmentLight.ChangeLeftLight();
										}
									}

									// COUNTER
									if (timedActive && _timedShowNumbers) {
										var counterSize = 20f * zoom;

										var counter = timedNode.CheckNextChange(srcSegmentId, end.StartNode, vehicleType, 1);

										float numOffset;

										if (liveSegmentLight.LightLeft == RoadBaseAI.TrafficLightState.Red) {
											numOffset = counterSize + 96f * zoom - modeHeight * 2;
										} else {
											numOffset = counterSize + 40f * zoom - modeHeight * 2;
										}

										var myRectCounterNum =
											new Rect(offsetScreenPos.x - counterSize + 15f * zoom + (counter >= 10 ? (counter >= 100 ? -10 * zoom : -5 * zoom) : 0f) - pedestrianWidth + 5f * zoom - (addOrEdit ? lightWidth * 2 : lightWidth),
												offsetScreenPos.y - numOffset, counterSize, counterSize);

										_counterStyle.fontSize = (int)(18f * zoom);
										_counterStyle.normal.textColor = new Color(1f, 1f, 1f);

										GUI.Label(myRectCounterNum, counter.ToString(), _counterStyle);

										if (myRectCounterNum.Contains(Event.current.mousePosition) && !IsCursorInPanel()) {
											_hoveredButton[0] = srcSegmentId;
											_hoveredButton[1] = 3;
											_hoveredNode = nodeId;
											hoveredSegment = true;
										}
									}
								}

								// forward-right arrow light
								guiColor.a = MainTool.GetHandleAlpha(_hoveredButton[0] == srcSegmentId && _hoveredButton[1] == 4 && _hoveredNode == nodeId);

								GUI.color = guiColor;

								var myRect5 =
									new Rect(offsetScreenPos.x - lightWidth / 2 - pedestrianWidth - (addOrEdit ? lightWidth : 0f) + 5f * zoom,
										offsetScreenPos.y - lightHeight / 2, lightWidth, lightHeight);

								if (hasOutgoingForwardSegment && hasOutgoingRightSegment) {
									drawForwardRightLightTexture(liveSegmentLight.LightMain, myRect5);
									hasOtherLight = true;
								} else if (hasOutgoingForwardSegment) {
									drawStraightLightTexture(liveSegmentLight.LightMain, myRect5);
									hasOtherLight = true;
								} else if (hasOutgoingRightSegment) {
									drawRightLightTexture(liveSegmentLight.LightMain, myRect5);
									hasOtherLight = true;
								}

								if (hasOtherLight && myRect5.Contains(Event.current.mousePosition) && !IsCursorInPanel()) {
									_hoveredButton[0] = srcSegmentId;
									_hoveredButton[1] = 4;
									_hoveredNode = nodeId;
									hoveredSegment = true;

									if (MainTool.CheckClicked() && !timedActive && addOrEdit) {
										liveSegmentLight.ChangeMainLight();
									}
								}

								// COUNTER
								if (timedActive && _timedShowNumbers) {
									var counterSize = 20f * zoom;
									var counter = timedNode.CheckNextChange(srcSegmentId, end.StartNode, vehicleType, 0);
				
									float numOffset;
									if (liveSegmentLight.LightMain == RoadBaseAI.TrafficLightState.Red) {
										numOffset = counterSize + 96f * zoom - modeHeight * 2;
									} else {
										numOffset = counterSize + 40f * zoom - modeHeight * 2;
									}

									var myRectCounterNum =
										new Rect(offsetScreenPos.x - counterSize + 15f * zoom + (counter >= 10 ? (counter >= 100 ? -10 * zoom : -5 * zoom) : 0f) - pedestrianWidth + 5f * zoom - (addOrEdit ? lightWidth : 0f),
											offsetScreenPos.y - numOffset, counterSize, counterSize);

									_counterStyle.fontSize = (int)(18f * zoom);
									_counterStyle.normal.textColor = new Color(1f, 1f, 1f);

									GUI.Label(myRectCounterNum, counter.ToString(), _counterStyle);

									if (myRectCounterNum.Contains(Event.current.mousePosition) && !IsCursorInPanel()) {
										_hoveredButton[0] = srcSegmentId;
										_hoveredButton[1] = 4;
										_hoveredNode = nodeId;
										hoveredSegment = true;
									}
								}
								break;
							case LightMode.SingleRight: {
									// forward-left light
									guiColor.a = MainTool.GetHandleAlpha(_hoveredButton[0] == srcSegmentId && _hoveredButton[1] == 3 && _hoveredNode == nodeId);

									GUI.color = guiColor;

									var myRect4 = new Rect(offsetScreenPos.x - lightWidth / 2 - (addOrEdit ? lightWidth * 2 : lightWidth) - pedestrianWidth + 5f * zoom,
										offsetScreenPos.y - lightHeight / 2, lightWidth, lightHeight);

									var lightType = 0;

									hasOtherLight = false;
									if (hasOutgoingForwardSegment && hasOutgoingLeftSegment) {
										hasOtherLight = true;
										drawForwardLeftLightTexture(liveSegmentLight.LightMain, myRect4);
										lightType = 1;
									} else if (hasOutgoingForwardSegment) {
										hasOtherLight = true;
										if (!hasOutgoingRightSegment) {
											myRect4 = new Rect(offsetScreenPos.x - lightWidth / 2 - (addOrEdit ? lightWidth : 0f) - pedestrianWidth + 5f * zoom,
												offsetScreenPos.y - lightHeight / 2, lightWidth, lightHeight);
										}

										drawStraightLightTexture(liveSegmentLight.LightMain, myRect4);
									} else if (hasOutgoingLeftSegment) {
										hasOtherLight = true;
										if (!hasOutgoingRightSegment) {
											myRect4 = new Rect(offsetScreenPos.x - lightWidth / 2 - (addOrEdit ? lightWidth : 0f) - pedestrianWidth + 5f * zoom,
												offsetScreenPos.y - lightHeight / 2, lightWidth, lightHeight);
										}

										drawLeftLightTexture(liveSegmentLight.LightMain, myRect4);
									}


									if (hasOtherLight && myRect4.Contains(Event.current.mousePosition) && !IsCursorInPanel()) {
										_hoveredButton[0] = srcSegmentId;
										_hoveredButton[1] = 3;
										_hoveredNode = nodeId;
										hoveredSegment = true;

										if (MainTool.CheckClicked() && !timedActive && addOrEdit) {
											liveSegmentLight.ChangeMainLight();
										}
									}

									// COUNTER
									if (timedActive && _timedShowNumbers) {
										var counterSize = 20f * zoom;

										var counter = timedNode.CheckNextChange(srcSegmentId, end.StartNode, vehicleType, lightType);

										float numOffset;

										if (liveSegmentLight.LightMain == RoadBaseAI.TrafficLightState.Red) {
											numOffset = counterSize + 96f * zoom - modeHeight * 2;
										} else {
											numOffset = counterSize + 40f * zoom - modeHeight * 2;
										}

										var myRectCounterNum =
											new Rect(offsetScreenPos.x - counterSize + 15f * zoom + (counter >= 10 ? (counter >= 100 ? -10 * zoom : -5 * zoom) : 0f) - pedestrianWidth + 5f * zoom - (addOrEdit ? (hasOutgoingRightSegment ? lightWidth * 2 : lightWidth) : (hasOutgoingRightSegment ? lightWidth : 0f)),
												offsetScreenPos.y - numOffset, counterSize, counterSize);

										_counterStyle.fontSize = (int)(18f * zoom);
										_counterStyle.normal.textColor = new Color(1f, 1f, 1f);

										GUI.Label(myRectCounterNum, counter.ToString(), _counterStyle);

										if (myRectCounterNum.Contains(Event.current.mousePosition) && !IsCursorInPanel()) {
											_hoveredButton[0] = srcSegmentId;
											_hoveredButton[1] = 3;
											_hoveredNode = nodeId;
											hoveredSegment = true;
										}
									}

									// right arrow light
									if (hasOutgoingRightSegment) {
										guiColor.a = MainTool.GetHandleAlpha(_hoveredButton[0] == srcSegmentId && _hoveredButton[1] == 4 &&
													 _hoveredNode == nodeId);

										GUI.color = guiColor;

										var rect5 =
											new Rect(offsetScreenPos.x - lightWidth / 2 - (addOrEdit ? lightWidth : 0f) - pedestrianWidth + 5f * zoom,
												offsetScreenPos.y - lightHeight / 2, lightWidth, lightHeight);

										drawRightLightTexture(liveSegmentLight.LightRight, rect5);

										if (rect5.Contains(Event.current.mousePosition) && !IsCursorInPanel()) {
											_hoveredButton[0] = srcSegmentId;
											_hoveredButton[1] = 4;
											_hoveredNode = nodeId;
											hoveredSegment = true;

											if (MainTool.CheckClicked() && !timedActive &&
												addOrEdit) {
												liveSegmentLight.ChangeRightLight();
											}
										}

										// COUNTER
										if (timedActive && _timedShowNumbers) {
											var counterSize = 20f * zoom;

											var counter = timedNode.CheckNextChange(srcSegmentId, end.StartNode, vehicleType, 2);

											float numOffset;

											if (liveSegmentLight.LightRight == RoadBaseAI.TrafficLightState.Red) {
												numOffset = counterSize + 96f * zoom - modeHeight * 2;
											} else {
												numOffset = counterSize + 40f * zoom - modeHeight * 2;
											}

											var myRectCounterNum =
												new Rect(
													offsetScreenPos.x - counterSize + 15f * zoom + (counter >= 10 ? (counter >= 100 ? -10 * zoom : -5 * zoom) : 0f) -
													pedestrianWidth + 5f * zoom -
													(addOrEdit ? lightWidth : 0f),
													offsetScreenPos.y - numOffset, counterSize, counterSize);

											_counterStyle.fontSize = (int)(18f * zoom);
											_counterStyle.normal.textColor = new Color(1f, 1f, 1f);

											GUI.Label(myRectCounterNum, counter.ToString(), _counterStyle);

											if (myRectCounterNum.Contains(Event.current.mousePosition) &&
												!IsCursorInPanel()) {
												_hoveredButton[0] = srcSegmentId;
												_hoveredButton[1] = 4;
												_hoveredNode = nodeId;
												hoveredSegment = true;
											}
										}
									}
								}
								break;
							default:
								// left arrow light
								if (hasOutgoingLeftSegment) {
									guiColor.a = MainTool.GetHandleAlpha(_hoveredButton[0] == srcSegmentId && _hoveredButton[1] == 3 && _hoveredNode == nodeId);

									GUI.color = guiColor;

									var offsetLight = lightWidth;

									if (hasOutgoingRightSegment)
										offsetLight += lightWidth;

									if (hasOutgoingForwardSegment)
										offsetLight += lightWidth;

									var myRect4 =
										new Rect(offsetScreenPos.x - lightWidth / 2 - (addOrEdit ? offsetLight : offsetLight - lightWidth) - pedestrianWidth + 5f * zoom,
											offsetScreenPos.y - lightHeight / 2, lightWidth, lightHeight);

									drawLeftLightTexture(liveSegmentLight.LightLeft, myRect4);

									if (myRect4.Contains(Event.current.mousePosition) && !IsCursorInPanel()) {
										_hoveredButton[0] = srcSegmentId;
										_hoveredButton[1] = 3;
										_hoveredNode = nodeId;
										hoveredSegment = true;

										if (MainTool.CheckClicked() && !timedActive && addOrEdit) {
											liveSegmentLight.ChangeLeftLight();
										}
									}

									// COUNTER
									if (timedActive && _timedShowNumbers) {
										var counterSize = 20f * zoom;

										var counter = timedNode.CheckNextChange(srcSegmentId, end.StartNode, vehicleType, 1);

										float numOffset;

										if (liveSegmentLight.LightLeft == RoadBaseAI.TrafficLightState.Red) {
											numOffset = counterSize + 96f * zoom - modeHeight * 2;
										} else {
											numOffset = counterSize + 40f * zoom - modeHeight * 2;
										}

										var myRectCounterNum =
											new Rect(
												offsetScreenPos.x - counterSize + 15f * zoom + (counter >= 10 ? (counter >= 100 ? -10 * zoom : -5 * zoom) : 0f) -
												pedestrianWidth + 5f * zoom -
												(addOrEdit ? offsetLight : offsetLight - lightWidth),
												offsetScreenPos.y - numOffset, counterSize, counterSize);

										_counterStyle.fontSize = (int)(18f * zoom);
										_counterStyle.normal.textColor = new Color(1f, 1f, 1f);

										GUI.Label(myRectCounterNum, counter.ToString(), _counterStyle);

										if (myRectCounterNum.Contains(Event.current.mousePosition) &&
											!IsCursorInPanel()) {
											_hoveredButton[0] = srcSegmentId;
											_hoveredButton[1] = 3;
											_hoveredNode = nodeId;
											hoveredSegment = true;
										}
									}
								}

								// forward arrow light
								if (hasOutgoingForwardSegment) {
									guiColor.a = MainTool.GetHandleAlpha(_hoveredButton[0] == srcSegmentId && _hoveredButton[1] == 4 && _hoveredNode == nodeId);

									GUI.color = guiColor;

									var offsetLight = lightWidth;

									if (hasOutgoingRightSegment)
										offsetLight += lightWidth;

									var myRect6 =
										new Rect(offsetScreenPos.x - lightWidth / 2 - (addOrEdit ? offsetLight : offsetLight - lightWidth) - pedestrianWidth + 5f * zoom,
											offsetScreenPos.y - lightHeight / 2, lightWidth, lightHeight);

									drawStraightLightTexture(liveSegmentLight.LightMain, myRect6);

									if (myRect6.Contains(Event.current.mousePosition) && !IsCursorInPanel()) {
										_hoveredButton[0] = srcSegmentId;
										_hoveredButton[1] = 4;
										_hoveredNode = nodeId;
										hoveredSegment = true;

										if (MainTool.CheckClicked() && !timedActive && addOrEdit) {
											liveSegmentLight.ChangeMainLight();
										}
									}

									// COUNTER
									if (timedActive && _timedShowNumbers) {
										var counterSize = 20f * zoom;

										var counter = timedNode.CheckNextChange(srcSegmentId, end.StartNode, vehicleType, 0);

										float numOffset;

										if (liveSegmentLight.LightMain == RoadBaseAI.TrafficLightState.Red) {
											numOffset = counterSize + 96f * zoom - modeHeight * 2;
										} else {
											numOffset = counterSize + 40f * zoom - modeHeight * 2;
										}

										var myRectCounterNum =
											new Rect(
												offsetScreenPos.x - counterSize + 15f * zoom + (counter >= 10 ? (counter >= 100 ? -10 * zoom : -5 * zoom) : 0f) -
												pedestrianWidth + 5f * zoom -
												(addOrEdit ? offsetLight : offsetLight - lightWidth),
												offsetScreenPos.y - numOffset, counterSize, counterSize);

										_counterStyle.fontSize = (int)(18f * zoom);
										_counterStyle.normal.textColor = new Color(1f, 1f, 1f);

										GUI.Label(myRectCounterNum, counter.ToString(), _counterStyle);

										if (myRectCounterNum.Contains(Event.current.mousePosition) &&
											!IsCursorInPanel()) {
											_hoveredButton[0] = srcSegmentId;
											_hoveredButton[1] = 4;
											_hoveredNode = nodeId;
											hoveredSegment = true;
										}
									}
								}

								// right arrow light
								if (hasOutgoingRightSegment) {
									guiColor.a = MainTool.GetHandleAlpha(_hoveredButton[0] == srcSegmentId && _hoveredButton[1] == 5 && _hoveredNode == nodeId);

									GUI.color = guiColor;

									var rect6 =
										new Rect(offsetScreenPos.x - lightWidth / 2 - (addOrEdit ? lightWidth : 0f) - pedestrianWidth + 5f * zoom,
											offsetScreenPos.y - lightHeight / 2, lightWidth, lightHeight);

									drawRightLightTexture(liveSegmentLight.LightRight, rect6);

									if (rect6.Contains(Event.current.mousePosition) && !IsCursorInPanel()) {
										_hoveredButton[0] = srcSegmentId;
										_hoveredButton[1] = 5;
										_hoveredNode = nodeId;
										hoveredSegment = true;

										if (MainTool.CheckClicked() && !timedActive && addOrEdit) {
											liveSegmentLight.ChangeRightLight();
										}
									}

									// COUNTER
									if (timedActive && _timedShowNumbers) {
										var counterSize = 20f * zoom;

										var counter = timedNode.CheckNextChange(srcSegmentId, end.StartNode, vehicleType, 2);

										float numOffset;

										if (liveSegmentLight.LightRight == RoadBaseAI.TrafficLightState.Red) {
											numOffset = counterSize + 96f * zoom - modeHeight * 2;
										} else {
											numOffset = counterSize + 40f * zoom - modeHeight * 2;
										}

										var myRectCounterNum =
											new Rect(
												offsetScreenPos.x - counterSize + 15f * zoom + (counter >= 10 ? (counter >= 100 ? -10 * zoom : -5 * zoom) : 0f) -
												pedestrianWidth + 5f * zoom -
												(addOrEdit ? lightWidth : 0f),
												offsetScreenPos.y - numOffset, counterSize, counterSize);

										_counterStyle.fontSize = (int)(18f * zoom);
										_counterStyle.normal.textColor = new Color(1f, 1f, 1f);

										GUI.Label(myRectCounterNum, counter.ToString(), _counterStyle);

										if (myRectCounterNum.Contains(Event.current.mousePosition) &&
											!IsCursorInPanel()) {
											_hoveredButton[0] = srcSegmentId;
											_hoveredButton[1] = 5;
											_hoveredNode = nodeId;
											hoveredSegment = true;
										}
									}
								}
								break;
						} // end switch liveSegmentLight.CurrentMode
					} // end foreach light
				} // end foreach segment
			} // end foreach node

			if (!hoveredSegment) {
				_hoveredButton[0] = 0;
				_hoveredButton[1] = 0;
			}
		}
	}
}
