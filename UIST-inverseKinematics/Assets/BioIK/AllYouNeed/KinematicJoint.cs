using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

//
//---NOTE---
//THIS CLASS IS CURRENTLY UNDER CONSTRUCTION BY CREATING A MORE REALISTIC MOTION!
//FULL FUNCTIONALITY IS PROVIDED BUT UPDATES WILL FOLLOW VERY SOON! =)
//(sorry for the messy source code)
//
//

namespace BioIK {
	public enum JointType {Revolute, Continuous, Prismatic};
	public enum JointState {Free, Fixed};
	public enum MotionType {Realistic, Smooth, Teleport};

	[System.Serializable]
	public class JointMotion {
		public KinematicJoint Joint;
		public JointState State = JointState.Fixed;
		public MotionType MotionType = MotionType.Realistic;
		[SerializeField] private float MaximumVelocity = 0f;
		[SerializeField] private float MaximumAcceleration = 0f;
		[SerializeField] private float LowerLimit = 0f;
		[SerializeField] private float UpperLimit = 0f;
		[SerializeField] private float TargetValue = 0f;
		public float CurrentValue {get; private set;}
		public float CurrentError {get; private set;}
		public float CurrentAcceleration {get; private set;}
		public float CurrentVelocity {get; private set;}

		private float Speedup = 1f;	//BETA - ADJUST THESE TO CHANGE THE REALISTIC MOTION SETTINGS
		private float Slowdown = 0.5f; //BETA - ADJUST THESE TO CHANGE THE REALISTIC MOTION SETTINGS
		
		/*
		private string TimeOutput = string.Empty;
		private string VelocityOutput = string.Empty;
		private string AccelerationOutput = string.Empty;
		private string StateOutput = string.Empty;
		public bool Output = false;
		public bool Record = false;
		*/

		private float LastDeltaTime = 0f;
		//private float AccelerationTime = 0f;
		private float DeccelerationTime = 0f;

		public void UpdateMotion() {
			if(State == JointState.Fixed) {
				//Nothing to do
				return;
			}

			if(MotionType == MotionType.Teleport) {
				//Teleport
				CurrentValue = TargetValue;
				CurrentError = 0f;
				CurrentVelocity = 0f;
				CurrentAcceleration = 0f;
			} else {
				//Compute Current Error
				CurrentError = TargetValue-CurrentValue;
				
				if(CurrentError == 0f) {
					CurrentVelocity = 0f;
					CurrentAcceleration = 0f;
				} else {
					if(MotionType == MotionType.Realistic) {
						float stoppingDistance = Mathf.Abs((CurrentVelocity*CurrentVelocity)/(2f*MaximumAcceleration*Slowdown));
						if(Slowdown != 1f) {
							stoppingDistance += Mathf.Abs(CurrentVelocity)*Time.deltaTime;
						}

						if(Mathf.Abs(CurrentError) >= stoppingDistance) {
							//Accelerate
							/*
							float timeToAccelerate = (MaximumVelocity - Mathf.Abs(CurrentVelocity)) / MaximumAcceleration;
							float distanceToAccelerate = Mathf.Abs(CurrentVelocity)*timeToAccelerate + (MaximumAcceleration/2f)*timeToAccelerate*timeToAccelerate;
							float distanceToBreaking = Mathf.Abs(CurrentError) - Mathf.Abs(stoppingDistance);
							float increase = Mathf.Abs(CurrentAcceleration) + Mathf.Abs(MaximumAcceleration)*Time.deltaTime*Mathf.Sqrt(Mathf.Abs(2f*Mathf.Min(distanceToBreaking, distanceToAccelerate))/MaximumAcceleration);
							float decrease = (MaximumVelocity-Mathf.Abs(CurrentVelocity)) * Mathf.Sqrt(Mathf.Abs(2f*Mathf.Min(distanceToBreaking, distanceToAccelerate))/MaximumAcceleration);
							float acceleration = Mathf.Min(MaximumAcceleration, increase, decrease);
							*/

							CurrentAcceleration = Mathf.Sign(CurrentError)*Mathf.Min((MaximumVelocity-Mathf.Abs(CurrentVelocity))/Time.deltaTime, MaximumAcceleration*Speedup);//, increase, decrease);
						} else {
							//Deccelerate
							CurrentAcceleration = -Mathf.Sign(CurrentVelocity)*Mathf.Min(Mathf.Abs(CurrentVelocity)/Time.deltaTime, MaximumAcceleration, Mathf.Abs((CurrentVelocity*CurrentVelocity)/(2f*CurrentError)));
						}
						CurrentVelocity += CurrentAcceleration*Time.deltaTime;

						//Precision Correction
						if(Mathf.Abs(CurrentVelocity)*Time.deltaTime > Mathf.Abs(CurrentError)) {
							CurrentVelocity = CurrentError/Time.deltaTime;
							CurrentAcceleration = 0f; ///TODO: -CurrentVelocity/Time.deltaTime;
						}
					}
				}

				if(MotionType == MotionType.Smooth) {
					//Compute Current Motion
					float stoppingDistance = Mathf.Abs((CurrentVelocity*CurrentVelocity)/(2f*MaximumAcceleration)) + Mathf.Abs(CurrentVelocity)*LastDeltaTime;
					float relaxedDistance = Mathf.Abs(CurrentVelocity*Mathf.Sqrt(Mathf.Abs(2f*CurrentError)/MaximumAcceleration)) + Mathf.Abs(CurrentVelocity)*LastDeltaTime;
					float distanceToBreaking = Mathf.Abs(CurrentError) - Mathf.Abs(relaxedDistance);

					//float stoppingTime = Mathf.Sqrt(Mathf.Abs(2f*stoppingDistance)/MaximumAcceleration);
					//float relaxedTime = Mathf.Sqrt(Mathf.Abs(2f*relaxedDistance)/MaximumAcceleration);

					float lastVelocity = Mathf.Abs(CurrentVelocity);

					if(CurrentError == 0f) {
						CurrentVelocity = 0f;
						CurrentAcceleration = 0f;
					} else {
						if(Mathf.Abs(CurrentError) > relaxedDistance) {
							//Accelerate
							DeccelerationTime = 0f;

							float timeToAccelerate = (MaximumVelocity - Mathf.Abs(CurrentVelocity)) / MaximumAcceleration;
							float distanceToAccelerate = Mathf.Abs(CurrentVelocity)*timeToAccelerate + (MaximumAcceleration/2f)*timeToAccelerate*timeToAccelerate;
							//float distanceToDeccelerate = Mathf.Abs(CurrentVelocity*Mathf.Sqrt(Mathf.Abs(2f*distanceToBreaking)/MaximumAcceleration));

							float increase = Mathf.Abs(CurrentAcceleration) + Mathf.Abs(MaximumAcceleration)*Time.deltaTime*Mathf.Sqrt(Mathf.Abs(2f*Mathf.Min(distanceToBreaking, distanceToAccelerate))/MaximumAcceleration);
							float decrease = (MaximumVelocity-Mathf.Abs(CurrentVelocity)) * Mathf.Sqrt(Mathf.Abs(2f*Mathf.Min(distanceToBreaking, distanceToAccelerate))/MaximumAcceleration);
							float acceleration = Mathf.Min(MaximumAcceleration, increase, decrease);

							CurrentAcceleration = Mathf.Sign(CurrentError) * acceleration;;
							CurrentVelocity += CurrentAcceleration*Time.deltaTime;
							if(Mathf.Abs(CurrentVelocity) > MaximumVelocity) {
								CurrentVelocity = Mathf.Sign(CurrentVelocity)*MaximumVelocity;
								CurrentAcceleration = Mathf.Sign(CurrentError) * (MaximumVelocity - lastVelocity)/Time.deltaTime;
							}
						} else {
							//Deccelerate
							DeccelerationTime += Time.deltaTime;

							float overheadTime = (relaxedDistance-stoppingDistance) / Mathf.Abs(CurrentVelocity);
							float relaxedDecceleration = Mathf.Abs((CurrentVelocity*CurrentVelocity)/(2f*CurrentError));
							float acceleration = Mathf.Min(MaximumAcceleration, (DeccelerationTime*DeccelerationTime)/(overheadTime*overheadTime)*MaximumAcceleration, relaxedDecceleration);
							//float acceleration = Mathf.Min(MaximumAcceleration, DeccelerationTime*MaximumAcceleration, Mathf.Abs((CurrentVelocity*CurrentVelocity)/(2f*CurrentError)));
							//float acceleration = Mathf.Min(MaximumAcceleration, Mathf.Abs((CurrentVelocity*CurrentVelocity)/(2f*CurrentError)));

							CurrentAcceleration = -Mathf.Sign(CurrentVelocity)*acceleration;
							CurrentVelocity += CurrentAcceleration*Time.deltaTime;
						}
							
						//Precision Correction
						if(Mathf.Abs(CurrentVelocity*Time.deltaTime) > Mathf.Abs(CurrentError)) {
							CurrentVelocity = CurrentError/Time.deltaTime;
							CurrentAcceleration = Mathf.Sign(CurrentError) * Mathf.Abs(Mathf.Abs(CurrentVelocity) - lastVelocity)/Time.deltaTime;
						}
					}
				}

				//Update Current Value
				CurrentValue += CurrentVelocity*Time.deltaTime;

				//Remember last delta time for accurate motion control
				LastDeltaTime = Time.deltaTime;

				//Record Data
				/*
				if(Record) {
					//Debug.Log("Breaking Distance: " + breakingDistance + " Floating Distance: " + floatingDistance + " Linear Distance: " + linearDistance);
					TimeOutput += Time.time.ToString(new System.Globalization.CultureInfo("en-US"));
					TimeOutput += "; ";
					VelocityOutput += CurrentVelocity.ToString(new System.Globalization.CultureInfo("en-US"));
					VelocityOutput += "; ";
					AccelerationOutput += CurrentAcceleration.ToString(new System.Globalization.CultureInfo("en-US"));
					AccelerationOutput += "; ";
					StateOutput += CurrentValue.ToString(new System.Globalization.CultureInfo("en-US"));
					StateOutput += "; ";
					
					if(Input.GetKeyDown(KeyCode.T)) {
						Debug.Log(
						"T = [" + TimeOutput + "]"
						+ "\n" + "T_V = [" + VelocityOutput + "]"
						+ "\n" + "T_A = [" + AccelerationOutput + "]"
						+ "\n" + "T_S = [" + StateOutput + "]");
					}
				}
				*/
			}
		}

		public void Reset() {
			CurrentError = 0f;
			CurrentVelocity = 0f;
			CurrentValue = 0f;
			TargetValue = 0f;
		}

		public void Stop() {
			TargetValue = CurrentValue;
		}

		public string GetName() {
			return Joint.name;
		}

		public void SetTargetValue(float value) {
			TargetValue = ConstrainToLimits(value);
		}

		public float GetTargetValue() {
			return TargetValue;
		}

		public void SetMaximumVelocity(float value) {
			MaximumVelocity = Mathf.Max(0f, value);
		}

		public float GetMaximumVelocity() {
			return MaximumVelocity;
		}

		public void SetMaximumAcceleration(float value) {
			MaximumAcceleration = Mathf.Max(0f, value);
		}

		public float GetMaximumAcceleration() {
			return MaximumAcceleration;
		}

		public void SetLowerLimit(float value) {
			LowerLimit = Mathf.Min(0f, value);
		}

		public float GetLowerLimit() {
			if(Joint.Type == JointType.Continuous) {
				return -Mathf.PI;
			} else {
				return LowerLimit;
			}
		}

		public void SetUpperLimit(float value) {
			UpperLimit = Mathf.Max(0f, value);
		}

		public float GetUpperLimit() {
			if(Joint.Type == JointType.Continuous) {
				return Mathf.PI;
			} else {
				return UpperLimit;
			}
		}

		private float ConstrainToLimits(float value) {
			if(Joint.Type == JointType.Continuous) {
				return value;
			} else {
				return Mathf.Clamp(value, LowerLimit, UpperLimit);
			}
		}
	}
		
	[DisallowMultipleComponent]
	public class KinematicJoint : MonoBehaviour {
		
		public JointType Type = JointType.Revolute;
		public Vector3 Connection = Vector3.zero;
		public Vector3 ConnectionInWorldSpace {get; private set;}
		public Vector3 AxisOrientation = Vector3.zero;
		public float AnimationWeight = 0.0f;

		public JointMotion XMotion = new JointMotion();
		public JointMotion YMotion = new JointMotion();
		public JointMotion ZMotion = new JointMotion();

		//Keep the initial transformation as frame of reference 
		private float RPX, RPY, RPZ, RRX, RRY, RRZ, RRW;			//Local Reference Frame
		private float PX, PY, PZ;									//Pivot
		private float XAX, XAY, XAZ, YAX, YAY, YAZ, ZAX, ZAY, ZAZ;	//Axes

		void Awake() {
			//Time.fixedDeltaTime = 0.01f;

			RPX = transform.localPosition.x;
			RPY = transform.localPosition.y;
			RPZ = transform.localPosition.z;
			RRX = transform.localRotation.x;
			RRY = transform.localRotation.y;
			RRZ = transform.localRotation.z;
			RRW = transform.localRotation.w;
			XMotion.Joint = this;
			YMotion.Joint = this;
			ZMotion.Joint = this;

			//XMotion.MotionType = MotionType.Teleport;
			//YMotion.MotionType = MotionType.Teleport;
			//ZMotion.MotionType = MotionType.Teleport;

			UpdateState();
		}

		void LateUpdate() {
			UpdateState();
			
			//Capture Animation
			Vector3 animTranslation = transform.localPosition - new Vector3(RPX, RPY, RPZ);
			Quaternion animRotation = transform.localRotation * Quaternion.Inverse(new Quaternion(RRX, RRY, RRZ, RRW));

			//Plan Motion
			XMotion.UpdateMotion();
			YMotion.UpdateMotion();
			ZMotion.UpdateMotion();

			//Compute Kinematics for IK
			float lpX, lpY, lpZ, lrX, lrY, lrZ, lrW;
			ComputeTransformation(XMotion.CurrentValue, YMotion.CurrentValue, ZMotion.CurrentValue, out lpX, out lpY, out lpZ, out lrX, out lrY, out lrZ, out lrW);

			//Apply Position
			transform.localPosition = new Vector3(lpX, lpY, lpZ) + AnimationWeight*animTranslation;

			//Apply Rotation
			Quaternion ikRotation = new Quaternion(lrX, lrY, lrZ, lrW);
			Quaternion combinedRotation = ikRotation*animRotation;
			transform.localRotation = Quaternion.Slerp(ikRotation, combinedRotation, AnimationWeight);
		}

		private void UpdateState() {
			Vector3 xAxis = ComputeXAxis();
			XAX = xAxis.x;
			XAY = xAxis.y;
			XAZ = xAxis.z;
			Vector3 yAxis = ComputeYAxis();
			YAX = yAxis.x;
			YAY = yAxis.y;
			YAZ = yAxis.z;
			Vector3 zAxis = ComputeZAxis();
			ZAX = zAxis.x;
			ZAY = zAxis.y;
			ZAZ = zAxis.z;
			PX = Connection.x * transform.localScale.x;
			PY = Connection.y * transform.localScale.y;
			PZ = Connection.z * transform.localScale.z;
			ConnectionInWorldSpace = ComputeConnectionInWorldSpace();
		}

		//Fast implementation to compute the local transform given the joint values
		public void ComputeTransformation(float valueX, float valueY, float valueZ, out float lpX, out float lpY, out float lpZ, out float lrX, out float lrY, out float lrZ, out float lrW) {
			float x, y, z, w, sin, cos;
			if(Type == JointType.Prismatic) {
				Vector3 axis = Quaternion.Euler(AxisOrientation) * new Vector3(valueX, valueY, valueZ);
				lpX = (1f - 2f * (RRY * RRY + RRZ * RRZ)) * axis.x + 2f * (RRX * RRY - RRW * RRZ) * axis.y + 2f * (RRX * RRZ + RRW * RRY) * axis.z + RPX;
				lpY = 2f * (RRX * RRY + RRW * RRZ) * axis.x + (1f - 2f * (RRX * RRX + RRZ * RRZ)) * axis.y + 2f * (RRY * RRZ - RRW * RRX) * axis.z + RPY;
				lpZ = 2f * (RRX * RRZ - RRW * RRY) * axis.x + 2f * (RRY * RRZ + RRW * RRX) * axis.y + (1f - 2f * (RRX * RRX + RRY * RRY)) * axis.z + RPZ;
				lrX = RRX;
				lrY = RRY;
				lrZ = RRZ;
				lrW = RRW;
			} else {
				//Z-X-Y Order
				if(valueZ != 0f) {
					sin = Mathf.Sin(valueZ/2f);
					lrX = ZAX * sin;
					lrY = ZAY * sin;
					lrZ = ZAZ * sin;
					lrW = Mathf.Cos(valueZ/2f);
					if(valueX != 0f) {
						sin = Mathf.Sin(valueX/2f);
						cos = Mathf.Cos(valueX/2f);
						x = lrX;
						y = lrY;
						z = lrZ;
						w = lrW;
						lrX = x * cos + y * XAZ * sin - z * XAY * sin + w * XAX * sin;
						lrY = -x * XAZ * sin + y * cos + z * XAX * sin + w * XAY * sin;
						lrZ = x * XAY * sin - y * XAX * sin + z * cos + w * XAZ * sin;
						lrW = -x * XAX * sin - y * XAY * sin - z * XAZ * sin + w * cos;
						if(valueY != 0f) {
							sin = Mathf.Sin(valueY/2f);
							cos = Mathf.Cos(valueY/2f);
							x = lrX;
							y = lrY;
							z = lrZ;
							w = lrW;
							lrX = x * cos + y * YAZ * sin - z * YAY * sin + w * YAX * sin;
							lrY = -x * YAZ * sin + y * cos + z * YAX * sin + w * YAY * sin;
							lrZ = x * YAY * sin - y * YAX * sin + z * cos + w * YAZ * sin;
							lrW = -x * YAX * sin - y * YAY * sin - z * YAZ * sin + w * cos;
						}
					} else if(valueY != 0f) {
						sin = Mathf.Sin(valueY/2f);
						cos = Mathf.Cos(valueY/2f);
						x = lrX;
						y = lrY;
						z = lrZ;
						w = lrW;
						lrX = x * cos + y * YAZ * sin - z * YAY * sin + w * YAX * sin;
						lrY = -x * YAZ * sin + y * cos + z * YAX * sin + w * YAY * sin;
						lrZ = x * YAY * sin - y * YAX * sin + z * cos + w * YAZ * sin;
						lrW = -x * YAX * sin - y * YAY * sin - z * YAZ * sin + w * cos;
					}
				} else if(valueX != 0f) {
					sin = Mathf.Sin(valueX/2f);
					lrX = XAX * sin;
					lrY = XAY * sin;
					lrZ = XAZ * sin;
					lrW = Mathf.Cos(valueX/2f);
					if(valueY != 0f) {
						sin = Mathf.Sin(valueY/2f);
						cos = Mathf.Cos(valueY/2f);
						x = lrX;
						y = lrY;
						z = lrZ;
						w = lrW;
						lrX = x * cos + y * YAZ * sin - z * YAY * sin + w * YAX * sin;
						lrY = -x * YAZ * sin + y * cos + z * YAX * sin + w * YAY * sin;
						lrZ = x * YAY * sin - y * YAX * sin + z * cos + w * YAZ * sin;
						lrW = -x * YAX * sin - y * YAY * sin - z * YAZ * sin + w * cos;
					}
				} else if(valueY != 0f) {
					sin = Mathf.Sin(valueY/2f);
					lrX = YAX * sin;
					lrY = YAY * sin;
					lrZ = YAZ * sin;
					lrW = Mathf.Cos(valueY/2f);
				} else {
					lpX = RPX;
					lpY = RPY;
					lpZ = RPZ;
					lrX = RRX;
					lrY = RRY;
					lrZ = RRZ;
					lrW = RRW;
					return;
				}

				x = (1f - 2f * (lrY * lrY + lrZ * lrZ)) * -PX + 2f * (lrX * lrY - lrW * lrZ) * -PY + 2f * (lrX * lrZ + lrW * lrY) * -PZ + PX;
				y = 2f * (lrX * lrY + lrW * lrZ) * -PX + (1f - 2f * (lrX * lrX + lrZ * lrZ)) * -PY + 2f * (lrY * lrZ - lrW * lrX) * -PZ + PY;
				z = 2f * (lrX * lrZ - lrW * lrY) * -PX + 2f * (lrY * lrZ + lrW * lrX) * -PY + (1f - 2f * (lrX * lrX + lrY * lrY)) * -PZ + PZ;
				lpX = RPX + (1f - 2f * (RRY * RRY + RRZ * RRZ)) * x + 2f * (RRX * RRY - RRW * RRZ) * y + 2f * (RRX * RRZ + RRW * RRY) * z;
				lpY = RPY + 2f * (RRX * RRY + RRW * RRZ) * x + (1f - 2f * (RRX * RRX + RRZ * RRZ)) * y + 2f * (RRY * RRZ - RRW * RRX) * z;
				lpZ = RPZ + 2f * (RRX * RRZ - RRW * RRY) * x + 2f * (RRY * RRZ + RRW * RRX) * y + (1f - 2f * (RRX * RRX + RRY * RRY)) * z;

				x = lrX;
				y = lrY;
				z = lrZ;
				w = lrW;
				lrX = RRX * w + RRY * z - RRZ * y + RRW * x;
				lrY = -RRX * z + RRY * w + RRZ * x + RRW * y;
				lrZ = RRX * y - RRY * x + RRZ * w + RRW * z;
				lrW = -RRX * x - RRY * y - RRZ * z + RRW * w;
			}
		}

		public int GetDOF() {
			int dof = 0;
			if(XMotion.State == JointState.Free) {
				dof += 1;
			}
			if(YMotion.State == JointState.Free) {
				dof += 1;
			}
			if(ZMotion.State == JointState.Free) {
				dof += 1;
			}
			return dof;
		}

		public Vector3 ComputeXAxis() {
			return Quaternion.Euler(AxisOrientation) * Vector3.right;
		}

		public Vector3 ComputeYAxis() {
			return Quaternion.Euler(AxisOrientation) * Vector3.up;
		}

		public Vector3 ComputeZAxis() {
			return Quaternion.Euler(AxisOrientation) * Vector3.forward;
		}

		public Vector3 GetCurrentValue() {
			return new Vector3(XMotion.CurrentValue, YMotion.CurrentValue, ZMotion.CurrentValue);
		}

		public Vector3 GetCurrentError() {
			return new Vector3(XMotion.CurrentError, YMotion.CurrentError, ZMotion.CurrentError);
		}

		public Vector3 GetCurrentVelocity() {
			return new Vector3(XMotion.CurrentVelocity, YMotion.CurrentVelocity, ZMotion.CurrentVelocity);
		}

		public Vector3 GetCurrentAcceleration() {
			return new Vector3(XMotion.CurrentAcceleration, YMotion.CurrentAcceleration, ZMotion.CurrentAcceleration);
		}

		public Vector3 ComputeConnectionInWorldSpace() {
			return transform.localToWorldMatrix.MultiplyPoint3x4(Connection);
		}

		#if UNITY_EDITOR
		[CustomEditor(typeof(KinematicJoint))]
		public class KinematicJoint_CE : Editor {
			private KinematicJoint Target;

			void Awake() {
				Target = (KinematicJoint)target;
				Target.XMotion.Joint = Target;
				Target.YMotion.Joint = Target;
				Target.ZMotion.Joint = Target;
			}

			public override void OnInspectorGUI() {
				Undo.RecordObject(Target, Target.name);
				
				using (var scope = new EditorGUILayout.VerticalScope ("Button")) {
					EditorGUILayout.HelpBox("Geometry", MessageType.None);
					Target.Type = (JointType)EditorGUILayout.EnumPopup("Type", Target.Type);
					Target.Connection = EditorGUILayout.Vector3Field("Connection", Target.Connection);
					Target.AxisOrientation = EditorGUILayout.Vector3Field("Axis Orientation", Target.AxisOrientation);
				}

				using (var scope = new EditorGUILayout.VerticalScope ("Button")) {
					Target.AnimationWeight = EditorGUILayout.Slider("Animation Weight", Target.AnimationWeight, 0f, 1f);;
				}

				DrawMotionInspector(Target.XMotion, "X Motion");
				DrawMotionInspector(Target.YMotion, "Y Motion");
				DrawMotionInspector(Target.ZMotion, "Z Motion");

				//EditorGUILayout.HelpBox("X Axis: " + Target.ComputeXAxis().ToString("F3") + "\nY Axis: " + Target.ComputeYAxis().ToString("F3") + "\nZ Axis: " + Target.ComputeZAxis().ToString("F3"), MessageType.None);
				EditorGUILayout.HelpBox(
					"Current Value: " + Target.GetCurrentValue().ToString("F3") + "\n" +
					"Current Error: " + Target.GetCurrentError().ToString("F3") + "\n" +
					"Current Velocity: " + Target.GetCurrentVelocity().ToString("F3") + "\n" + 
					"Current Acceleration: " + Target.GetCurrentAcceleration().ToString("F3"), MessageType.None);

				EditorUtility.SetDirty(Target);
			}

			private void DrawMotionInspector(JointMotion motion, string name) {
				using (var scope = new EditorGUILayout.VerticalScope ("Button")) {
					EditorGUILayout.HelpBox(name, MessageType.None);
					motion.State = (JointState)EditorGUILayout.EnumPopup("State", motion.State);
					if(motion.State != JointState.Fixed) {
						motion.MotionType = (MotionType)EditorGUILayout.EnumPopup("Motion Type", motion.MotionType);
						if(motion.MotionType != MotionType.Teleport) {
							motion.SetMaximumVelocity(EditorGUILayout.FloatField("Max Velocity", motion.GetMaximumVelocity()));
							motion.SetMaximumAcceleration(EditorGUILayout.FloatField("Max Acceleration", motion.GetMaximumAcceleration()));
						}
						if(motion.Joint.Type != JointType.Continuous) {
							motion.SetLowerLimit(EditorGUILayout.FloatField("Lower Limit", motion.GetLowerLimit()));
							motion.SetUpperLimit(EditorGUILayout.FloatField("Upper Limit", motion.GetUpperLimit()));
						}
						motion.SetTargetValue(EditorGUILayout.FloatField("Target Value", motion.GetTargetValue()));
					}

					//motion.Output = EditorGUILayout.Toggle("Output", motion.Output);
					//motion.Record = EditorGUILayout.Toggle("Record", motion.Record);
				}
			}

			void OnSceneGUI() {
				//Draw Connection
				Vector3 connection = Target.ComputeConnectionInWorldSpace();
				Handles.color = Color.magenta;
				Handles.SphereCap(0, connection, Quaternion.identity, 1/100f);
				Handles.Label(connection, "Connection");

				//Draw Axes
				Quaternion rotation = Quaternion.identity;
				if(Application.isPlaying) {
					if(Target.transform.parent != null) {
						rotation = Target.transform.parent.rotation;
					}
					rotation *= new Quaternion(Target.RRX, Target.RRY, Target.RRZ, Target.RRW);
				} else {
					rotation = Target.transform.rotation;
				}

				if(Target.XMotion.State == JointState.Free) {
					Handles.color = new Color(1f, 0f, 0f, 0.2f);
					Vector3 scale = Vector3.zero;
					if(Target.transform.root != Target.transform) {
						scale = Target.transform.parent.lossyScale;
					}

					if(Target.Type == JointType.Prismatic) {
						Vector3 pivot = connection - Vector3.Scale(rotation * Quaternion.Euler(Target.AxisOrientation) * Target.GetCurrentValue(), scale);
						Vector3 A = pivot + Vector3.Scale(Target.XMotion.GetLowerLimit() * (rotation * Target.ComputeXAxis()), scale);
						Vector3 B = pivot + Vector3.Scale(Target.XMotion.GetUpperLimit() * (rotation * Target.ComputeXAxis()), scale);
						Handles.DrawLine(connection, A);
						Handles.CubeCap(0, A, rotation, 0.025f);
						Handles.DrawLine(connection, B);
						Handles.CubeCap(0, B, rotation, 0.025f);
					} else {
						float lowerLimit = Mathf.Rad2Deg*Target.XMotion.GetLowerLimit();
						float upperLimit = Mathf.Rad2Deg*Target.XMotion.GetUpperLimit();
						Handles.DrawSolidArc(connection, rotation * Target.ComputeXAxis(), Quaternion.AngleAxis(lowerLimit, rotation * Target.ComputeXAxis()) * rotation * Target.ComputeZAxis(), upperLimit-lowerLimit, 0.075f);
					}
					Handles.color = Color.red;
				} else {
					Handles.color = Color.grey;
				}
				Handles.ArrowCap(0, connection, rotation * Quaternion.LookRotation(Target.ComputeXAxis()), 0.1f);

				if(Target.YMotion.State == JointState.Free) {
					Handles.color = new Color(0f, 1f, 0f, 0.2f);
					Vector3 scale = Vector3.zero;
					if(Target.transform.root != Target.transform) {
						scale = Target.transform.parent.lossyScale;
					}

					if(Target.Type == JointType.Prismatic) {
						Vector3 pivot = connection - Vector3.Scale(rotation * Quaternion.Euler(Target.AxisOrientation) *  Target.GetCurrentValue(), scale);
						Vector3 A = pivot + Vector3.Scale(Target.YMotion.GetLowerLimit() * (rotation * Target.ComputeYAxis()), scale);
						Vector3 B = pivot + Vector3.Scale(Target.YMotion.GetLowerLimit() * (rotation * Target.ComputeYAxis()), scale);
						Handles.DrawLine(connection, A);
						Handles.CubeCap(0, A, rotation, 0.025f);
						Handles.DrawLine(connection, B);
						Handles.CubeCap(0, B, rotation, 0.025f);
					} else {
						float lowerLimit = Mathf.Rad2Deg*Target.YMotion.GetLowerLimit();
						float upperLimit = Mathf.Rad2Deg*Target.YMotion.GetUpperLimit();
						Handles.DrawSolidArc(connection, rotation * Target.ComputeYAxis(), Quaternion.AngleAxis(lowerLimit, rotation * Target.ComputeYAxis()) * rotation * Target.ComputeXAxis(), upperLimit-lowerLimit, 0.075f);
					}
					Handles.color = Color.green;
				} else {
					Handles.color = Color.grey;
				}
				Handles.ArrowCap(0, connection, rotation * Quaternion.LookRotation(Target.ComputeYAxis()), 0.1f);

				if(Target.ZMotion.State == JointState.Free) {
					Handles.color = new Color(0f, 0f, 1f, 0.2f);
					Vector3 scale = Vector3.zero;
					if(Target.transform.root != Target.transform) {
						scale = Target.transform.parent.lossyScale;
					}

					if(Target.Type == JointType.Prismatic) {
						Vector3 pivot = connection - Vector3.Scale(rotation * Quaternion.Euler(Target.AxisOrientation) * Target.GetCurrentValue(), scale);
						Vector3 A = pivot + Vector3.Scale(Target.ZMotion.GetLowerLimit() * (rotation * Target.ComputeZAxis()), scale);
						Vector3 B = pivot + Vector3.Scale(Target.ZMotion.GetUpperLimit() * (rotation * Target.ComputeZAxis()), scale);
						Handles.DrawLine(connection, A);
						Handles.CubeCap(0, A, rotation, 0.025f);
						Handles.DrawLine(connection, B);
						Handles.CubeCap(0, B, rotation, 0.025f);
					} else {
						float lowerLimit = Mathf.Rad2Deg*Target.ZMotion.GetLowerLimit();
						float upperLimit = Mathf.Rad2Deg*Target.ZMotion.GetUpperLimit();
						Handles.DrawSolidArc(connection, rotation * Target.ComputeZAxis(), Quaternion.AngleAxis(lowerLimit, rotation * Target.ComputeZAxis()) * rotation * Target.ComputeYAxis(), upperLimit-lowerLimit, 0.075f);
					}
					Handles.color = Color.blue;
				} else {
					Handles.color = Color.grey;
				}
				Handles.ArrowCap(0, connection, rotation * Quaternion.LookRotation(Target.ComputeZAxis()), 0.1f);
			}

			/*
			private bool HasNonUniformScaling() {
				if(Target.transform.childCount == 0) {
					return false;
				}

				Vector3 scale = Target.transform.lossyScale;
				if(Mathf.Approximately(Mathf.Abs(scale.x), Mathf.Abs(scale.y)) && Mathf.Approximately(Mathf.Abs(scale.y), Mathf.Abs(scale.z))) {
					return false;
				} else {
					return true;
				}
			}
			*/
		}
		#endif
	}
}
