using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BioIK {
	[DisallowMultipleComponent]
	public class IKTip : MonoBehaviour {

		public enum ObjectiveType{Position, Orientation, Pose, LookAt}
		[System.Serializable]
		public class IKObjective {
			public ObjectiveType Type = ObjectiveType.Position;	//The Optimization Objective
			public float MaximumPositionError = 0.01f; 			//In Metres
			public float MaximumOrientationError = 0.01f; 		//In Radians
			public float MaximumDirectionError = 0.01f; 		//In Radians
			public Vector3 Direction = Vector3.forward;			//Vector
		}

		//Target specification
		public Transform Target;
		public IKObjective Objective;
		public float Weight = 1f;								//Should always be between 0 and 1

		//--Frame-Constant State--
		public float TPX {get; private set;}
		public float TPY {get; private set;}
		public float TPZ {get; private set;}
		public float TRX {get; private set;}
		public float TRY {get; private set;}
		public float TRZ {get; private set;}
		public float TRW {get; private set;}

		void Awake() {
			UpdateState();

			IKBody body = SearchIKBody();
			if(body != null) {
				body.Rebuild();
			}
		}

		void Update() {
			UpdateState();
		}

		void OnEnable() {
			IKBody body = SearchIKBody();
			if(body != null) {
				body.Rebuild();
			}
		}

		void OnDisable() {
			IKBody body = SearchIKBody();
			if(body != null) {
				body.Rebuild();
			}
		}

		void OnDestroy() {
			IKBody body = SearchIKBody();
			if(body != null) {
				body.Rebuild();
			}
		}

		public void SetTarget(Transform target) {
			Target = target;
		}

		public void SetTarget(Vector3 position) {
			Target = null;
			TPX = position.x;
			TPY = position.y;
			TPZ = position.z;
			Objective.Type = ObjectiveType.Position;
		}

		public void SetTarget(Quaternion rotation) {
			Target = null;
			TRX = rotation.x;
			TRY = rotation.y;
			TRZ = rotation.z;
			TRW = rotation.w;
			Objective.Type = ObjectiveType.Orientation;
		}

		public void SetTarget(Vector3 position, Quaternion rotation) {
			Target = null;
			TPX = position.x;
			TPY = position.y;
			TPZ = position.z;
			TRX = rotation.x;
			TRY = rotation.y;
			TRZ = rotation.z;
			TRW = rotation.w;
			Objective.Type = ObjectiveType.Pose;
		}

		private void UpdateState() {
			if(Target != null) {
				Vector3 targetPosition = Target.position;
				TPX = targetPosition.x;
				TPY = targetPosition.y;
				TPZ = targetPosition.z;

				Quaternion targetRotation = Target.rotation;
				TRX = targetRotation.x;
				TRY = targetRotation.y;
				TRZ = targetRotation.z;
				TRW = targetRotation.w;
			}
		}

		private IKBody SearchIKBody() {
			Transform t = transform;
			while(true) {
				IKBody body = t.GetComponent<IKBody>();
				if(body != null) {
					return body;
				} else if(t != t.root) {
					t = t.parent;
				} else {
					return null;
				}
			}
		}
		
		//----------------------------------------------------------------------------------------------------
		//====================================================================================================
		//Editor
		#if UNITY_EDITOR
		[CustomEditor(typeof(IKTip))]
		public class IKTip_CE : Editor {
			private IKTip Target;

			void Awake() {
				Target = (IKTip)target;
			}

			public override void OnInspectorGUI() {
				Undo.RecordObject(Target, Target.name);

				using (var scope = new EditorGUILayout.VerticalScope ("Button")) {
					EditorGUILayout.HelpBox("Settings", MessageType.None);

					Target.Target = (Transform)EditorGUILayout.ObjectField("Target", Target.Target, typeof(Transform), true);
					Target.Objective.Type = (ObjectiveType)EditorGUILayout.EnumPopup("Objective", Target.Objective.Type);

					using (var error = new EditorGUILayout.VerticalScope ("Box")) {
						EditorGUILayout.LabelField("Maximum Error");
						if(Target.Objective.Type == ObjectiveType.Position) {
							Target.Objective.MaximumPositionError = EditorGUILayout.FloatField("Position", Target.Objective.MaximumPositionError);
						}
						if(Target.Objective.Type == ObjectiveType.Orientation) {
							Target.Objective.MaximumOrientationError = EditorGUILayout.FloatField("Orientation", Target.Objective.MaximumOrientationError);
						}
						if(Target.Objective.Type == ObjectiveType.Pose) {
							Target.Objective.MaximumPositionError = EditorGUILayout.FloatField("Position", Target.Objective.MaximumPositionError);
							Target.Objective.MaximumOrientationError = EditorGUILayout.FloatField("Orientation", Target.Objective.MaximumOrientationError);
						}
						if(Target.Objective.Type == ObjectiveType.LookAt) {
							Target.Objective.Direction = EditorGUILayout.Vector3Field("Direction", Target.Objective.Direction);
							Target.Objective.MaximumDirectionError = EditorGUILayout.FloatField("Error", Target.Objective.MaximumDirectionError);
						}
					}

					using (var degreeoffreedom = new EditorGUILayout.VerticalScope ("Button")) {
						IKBody body = SearchIKBody();
						if(body != null) {
							Chain chain = new Chain(body.transform, Target.transform);
							EditorGUILayout.LabelField("Length: " + chain.Length);
							EditorGUILayout.LabelField("Degree of Freedom: " + chain.DoF);
						} else {
							EditorGUILayout.HelpBox("Could not find a connected IK Body. Make sure to add a 'IKBody' component to your model from where the IK problem shall be solved.", MessageType.Warning);
						}
					}
				}

				EditorUtility.SetDirty(Target);
			}

			public virtual void OnSceneGUI() {
				//Draw Kinematic Chain
				IKBody body = SearchIKBody();
				if(body != null) {
					DrawKinematicChain(new Chain(body.transform, Target.transform));
				}

				//Draw Objective
				if(Target.Objective != null) {
					if(Target.Objective.Type == ObjectiveType.Position) {
						Handles.SphereCap(0, Target.transform.position, Quaternion.identity, Target.Objective.MaximumPositionError);
					}
					if(Target.Objective.Type == ObjectiveType.Orientation) {
						//Visualized by Unity's Transform
					}
					if(Target.Objective.Type == ObjectiveType.Pose) {
						Handles.SphereCap(0, Target.transform.position, Quaternion.identity, Target.Objective.MaximumPositionError);
					}
					if(Target.Objective.Type == ObjectiveType.LookAt) {
						Handles.ArrowCap(0, Target.transform.position, Target.transform.rotation*Quaternion.LookRotation(Target.Objective.Direction), 0.05f);
					}
				}
			}

			private IKBody SearchIKBody() {
				Transform t = Target.transform;
				while(true) {
					IKBody body = t.GetComponent<IKBody>();
					if(body != null) {
						return body;
					} else if(t != t.root) {
						t = t.parent;
					} else {
						return null;
					}
				}
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