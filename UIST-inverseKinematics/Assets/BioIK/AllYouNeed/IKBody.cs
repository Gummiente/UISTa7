using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BioIK {
	[DisallowMultipleComponent]
	public class IKBody : MonoBehaviour {

		//Algorithm parameters
		public double MaximumFrameTime = 0.001; //Specify the maximum allowed time for optimization during one frame
		public int PopulationSize = 12;			//Should be much higher than the number of elites
		public int Elites = 4;					//Should be chosen comparatively low

		//Optimization algorithm
		private Model Model;
		private Evolution Evolution;
		private bool RequireReset;

		private int Generations;
		private double ElapsedTime;

		void Awake() {
			RequireReset = true;
		}

		void Update() {
			if(RequireReset) {
				Model = new Model(this);
				Evolution = new Evolution(Model, PopulationSize, Elites);
				RequireReset = false;
			}

			Model.UpdateState();

			Generations = 0;
			ElapsedTime = 0.0;
			if(!Model.IsConverged(Evolution.GetSolution())) {
				while(ElapsedTime < MaximumFrameTime && !Model.IsConverged(Evolution.GetSolution())) {
			//	while(Generations < 1) {
					System.DateTime then = System.DateTime.Now;
					if(Evolution.Evolve()) {
						Assign(Evolution.GetSolution());
					}
					Generations += 1;
					ElapsedTime += (System.DateTime.Now-then).Duration().TotalSeconds;
				}
			}
		}

		public void SetPopulationSize(int value) {
			if(PopulationSize != value) {
				PopulationSize = Mathf.Max(Elites, value);
				Rebuild();
			}
		}

		public void SetElites(int value) {
			if(Elites != value) {
				Elites = Mathf.Min(PopulationSize, value);
				Rebuild();
			}
		}

		public void Rebuild() {
			RequireReset = true;
		}

		public Model GetModel() {
			return Model;
		}

		public Evolution GetEvolution() {
			return Evolution;
		}

		public int GetGenerations() {
			return Generations;
		}

		public double GetElapsedTime() {
			return ElapsedTime;
		}

		private void Assign(float[] configuration) {
			for(int i=0; i<configuration.Length; i++) {
				if(Model.Motions[i].Motion.Joint.Type == JointType.Continuous) {
					Model.Motions[i].Motion.SetTargetValue(Model.Motions[i].Motion.GetTargetValue() + Mathf.Deg2Rad*Mathf.DeltaAngle(Mathf.Rad2Deg*Model.Motions[i].Motion.GetTargetValue(), Mathf.Rad2Deg*configuration[i]));
				}
				if(Model.Motions[i].Motion.Joint.Type == JointType.Revolute) {
					/*
					float targetValue = Model.Motions[i].Motion.GetTargetValue() + Mathf.Deg2Rad*Mathf.DeltaAngle(Mathf.Rad2Deg*Model.Motions[i].Motion.GetTargetValue(), Mathf.Rad2Deg*configuration[i]);
					if(Model.Motions[i].Motion.ConstrainToLimits(targetValue) != targetValue) {
						targetValue = configuration[i];
					}
					Model.Motions[i].Motion.SetTargetValue(targetValue);
					*/
					Model.Motions[i].Motion.SetTargetValue(configuration[i]);
				}
				if(Model.Motions[i].Motion.Joint.Type == JointType.Prismatic) {
					Model.Motions[i].Motion.SetTargetValue(configuration[i]);
				}
			}
		}

		//----------------------------------------------------------------------------------------------------
		//====================================================================================================
		//Editor
		#if UNITY_EDITOR
		[CustomEditor(typeof(IKBody))]
		public class IKBody_CE : Editor {

			/*
			Tool LastTool = Tool.None;
			void OnEnable() {
				LastTool = Tools.current;
				Tools.current = Tool.None;
			}
			void OnDisable() {
				Tools.current = LastTool;
			}
			*/

			private IKBody Target;

			void Awake() {
				Target = (IKBody)target;
			}

			public override void OnInspectorGUI() {
				Undo.RecordObject(Target, Target.name);

				//Show Solver Settings
				using (var scope = new EditorGUILayout.VerticalScope ("Button")) {
					EditorGUILayout.HelpBox("Solver", MessageType.None);

					Target.MaximumFrameTime = EditorGUILayout.DoubleField("Maximum Frame Time", Target.MaximumFrameTime);
					Target.SetPopulationSize(EditorGUILayout.IntField("Population Size", Target.PopulationSize));
					Target.SetElites(EditorGUILayout.IntField("Elites", Target.Elites));
				}

				//Search IK Tips
				IKTip[] tips = SearchIKTips(Target.transform, new List<IKTip>());

				//Show IK Targets
				using (var scope = new EditorGUILayout.VerticalScope ("Button")) {
					EditorGUILayout.HelpBox("IK Tips", MessageType.None);

					for(int i=0; i<tips.Length; i++) {
						Undo.RecordObject(tips[i], tips[i].name);

						using (var box = new EditorGUILayout.VerticalScope ("Box")) {
							EditorGUILayout.LabelField(tips[i].name);
							tips[i].Weight = EditorGUILayout.Slider("Weight", tips[i].Weight, 0f, 1f);;
						}

						EditorUtility.SetDirty(tips[i]);
					}
				}

				//Show DoF
				using (var degreeoffreedom = new EditorGUILayout.VerticalScope ("Button")) {
					List<KinematicJoint> jointSet = new List<KinematicJoint>();
					for(int i=0; i<tips.Length; i++) {
						Chain chain = new Chain(Target.transform, tips[i].transform);
						for(int j=0; j<chain.Joints.Length; j++) {
							if(!jointSet.Contains(chain.Joints[j])) {
								jointSet.Add(chain.Joints[j]);
							}
						}
					}
					int dof = 0;
					for(int i=0; i<jointSet.Count; i++) {
						dof += jointSet[i].GetDOF();
					}
					EditorGUILayout.LabelField("Degree of Freedom: " + dof);
				}

				//Performance
				using (var degreeoffreedom = new EditorGUILayout.VerticalScope ("Button")) {
					EditorGUILayout.HelpBox("Performance", MessageType.None);
					EditorGUILayout.LabelField("Generations: " + Target.Generations);
					EditorGUILayout.LabelField("Elapsed Time: " + Target.ElapsedTime);
				}

				EditorUtility.SetDirty(Target);
			}

			public virtual void OnSceneGUI() {
				IKTip[] tips = SearchIKTips(Target.transform, new List<IKTip>());
				for(int i=0; i<tips.Length; i++) {
					DrawKinematicChain(new Chain(Target.transform, tips[i].transform));
				}
			}

			private IKTip[] SearchIKTips(Transform t, List<IKTip> tips) {
				IKTip tip = t.GetComponent<IKTip>();
				if(tip != null) {
					if(tip.isActiveAndEnabled) {
						tips.Add(tip);
					}
				}
				for(int i=0; i<t.childCount; i++) {
					SearchIKTips(t.GetChild(i), tips);
				}
				return tips.ToArray();
			}

			private void DrawKinematicChain(Chain chain) {
				//Visualize Joints and Kinematic Chain
				if(chain.Joints.Length > 0) {
					KinematicJoint reference = chain.Joints[0];
					VisualizeKinematicJoint(reference);
					for(int k=1; k<chain.Joints.Length; k++) {
						Handles.color = Color.cyan;
						Handles.DrawLine(reference.ComputeConnectionInWorldSpace(), chain.Joints[k].ComputeConnectionInWorldSpace());
						reference = chain.Joints[k];
						VisualizeKinematicJoint(reference);
					}

					Handles.color = Color.cyan;
					Handles.DrawLine(chain.Joints[chain.Joints.Length-1].ComputeConnectionInWorldSpace(), chain.Segments[chain.Segments.Length-1].transform.position);

					Handles.color = new Color(0.25f, 0.25f, 0.25f, 1f);
					Handles.SphereCap(0, chain.Segments[chain.Segments.Length-1].position, chain.Segments[chain.Segments.Length-1].rotation, 0.01f);
				}
			}

			private void VisualizeKinematicJoint(KinematicJoint joint) {
				Vector3 connection = joint.ComputeConnectionInWorldSpace();
				Handles.color = Color.magenta;
				Handles.SphereCap(0, connection, Quaternion.identity, 1/100f);

				GUIStyle style = new GUIStyle();
				style.normal.textColor = Color.black;
				Handles.Label(connection, joint.name, style);

				if(joint.XMotion.State == JointState.Free) {
					Handles.color = Color.red;
					Handles.ArrowCap(0, connection, joint.transform.rotation * Quaternion.LookRotation(joint.ComputeXAxis()), 0.1f);
				}
				if(joint.YMotion.State == JointState.Free) {
					Handles.color = Color.green;
					Handles.ArrowCap(0, connection, joint.transform.rotation * Quaternion.LookRotation(joint.ComputeYAxis()), 0.1f);
				}
				if(joint.ZMotion.State == JointState.Free) {
					Handles.color = Color.blue;
					Handles.ArrowCap(0, connection, joint.transform.rotation * Quaternion.LookRotation(joint.ComputeZAxis()), 0.1f);
				}
			}
		}
		#endif
		//====================================================================================================
		//----------------------------------------------------------------------------------------------------
	}
}